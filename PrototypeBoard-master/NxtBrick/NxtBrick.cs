//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtBrick.cs $ $Revision: 46 $
//-----------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using W3C.Soap;

using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using ds = Microsoft.Dss.Services.Directory;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using Microsoft.Dss.Core.Utilities;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using comm = Microsoft.Robotics.Services.Sample.Lego.Nxt.Comm;
using constructor = Microsoft.Dss.Services.Constructor;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using Microsoft.Dss.Core.DsspHttp;
using System.Net;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick
{
    
    /// <summary>
    /// Implementation class for NxtBrick
    /// </summary>
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Brick Service (v2).")]
    [DisplayName("(User) Lego NXT Brick \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870565.aspx")]
    [Contract(Contract.Identifier)]
    [DssCategory(LegoCategories.NXT)]
    public class NxtBrick : DsspServiceBase
    {
        /// <summary>
        /// _state
        /// </summary>
        [InitialStatePartner(Optional = true, 
            ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.Brick.config.xml")]
        private NxtBrickState _state = new NxtBrickState();
        private InternalBrickStatus _internalStatus = new InternalBrickStatus();

        


        private comm.LegoCommOperations _brickPort = null;
        private comm.LegoCommOperations _brickNotificationPort = new comm.LegoCommOperations();

        private Port<PollingEntry> _pollingPort = new Port<PollingEntry>();
        private List<AttachRequest> _pendingRemoval = new List<AttachRequest>();        

        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/brick", AllowMultipleInstances = true)]
        private NxtBrickOperations _internalMainPort = new NxtBrickOperations();
        private NxtBrickOperations _reliableMainPort = null;

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Brick.user.xslt")]
        string _transform = string.Empty;


        private LegoLSGetStatus _cmdLSGetStatus = new LegoLSGetStatus();

        /// <summary>
        /// Subscription manager partner
        /// </summary>
        [Partner(Partners.SubscriptionManagerString, Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _subMgrPort = new submgr.SubscriptionManagerPort();

        private Dictionary<string, IPort> _DeviceSubscriptions = new Dictionary<string, IPort>();

        /// <summary>
        /// Internal port for processing I2C commands one at a time.
        /// </summary>
        private Port<SendLowSpeedCommand> _internalI2CPort = new Port<SendLowSpeedCommand>();


        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public NxtBrick(DsspServiceCreationPort creationPort) : 
                base(creationPort)
        {
        }

        /// <summary>
        /// Initialize starting state
        /// </summary>
        private void InitializeState()
        {
            bool saveState = false;

            if (_state == null)
            {
                _state = new NxtBrickState();
                saveState = true;
            }

            if (_state.Configuration == null)
            {
                _state.Configuration = new BrickConfiguration();
                _state.Configuration.ShowInBrowser = true;
                _state.Configuration.ConnectionType = LegoConnectionType.Bluetooth;
                saveState = true;
            }

            if (_state.Configuration.BaudRate < 300)
            {
                _state.Configuration.BaudRate = 115200;
                saveState = true;
            }

            // ***************************************************************
            // Save State

            if (saveState)
            {
                NxtRuntime saveRuntime = _state.Runtime;
                _state.Runtime = null;
                SaveState(_state);
                _state.Runtime = saveRuntime;
            }

            // ***************************************************************
            // Initialize Runtime settings

            if (_state.Runtime == null)
            {
                _state.Runtime = new NxtRuntime();
            }

            if (_state.Runtime.Devices == null)
            {
                _state.Runtime.Devices = new DssDictionary<string, AttachRequest>();
            }

        }

        /// <summary>
        /// Open the LEGO NXT Service in a web browser.
        /// </summary>
        private void OpenLegoNxtServiceInBrowser()
        {
            //start up IE to our state page so user can view
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = FindServiceAliasFromScheme(Uri.UriSchemeHttp);
            process.Start();
        }

        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            InitializeState();
            
            // Set up the reliable port using DSS forwarder to ensure exception and timeout conversion to fault.
            _reliableMainPort = ServiceForwarder<NxtBrickOperations>(this.ServiceInfo.Service);

            // Make the service visible immediately
            DirectoryInsert();

            // Handle some handlers all the time -- even during initialization and exclusive handlers.
            Activate<ITask>(
                Arbiter.ReceiveWithIterator<Get>(true, _internalMainPort, GetHandler),
                Arbiter.Receive<HttpGet>(true, _internalMainPort, HttpGetHandler),
                Arbiter.Receive<DsspDefaultLookup>(true, _internalMainPort, base.DefaultLookupHandler),
                Arbiter.ReceiveWithIterator<LegoSensorUpdate>(true, _internalMainPort, LegoSensorUpdateHandler)
               );

            // Start initializing the communications service
            DsspResponsePort<CreateResponse> nxtCommunicationResponse = CreateNxtCommunicationService();

            // Activate on all operations.
            // Operations which require a connection to the hardware will throw a fault.
            Interleave initializationInterleave = new Interleave(
                new TeardownReceiverGroup(
                    Arbiter.ReceiveWithIterator<CreateResponse>(false, nxtCommunicationResponse, ServiceInitialization),
                    Arbiter.ReceiveWithIterator<DsspDefaultDrop>(false, _internalMainPort, DropHandler)
                ),
                new ExclusiveReceiverGroup(
                    Arbiter.ReceiveWithIterator<AttachAndSubscribe>(true, _internalMainPort, AttachAndSubscribeHandler),
                    Arbiter.ReceiveWithIterator<ReserveDevicePort>(true, _internalMainPort, ReserveDevicePortHandler),
                    Arbiter.ReceiveWithIterator<Subscribe>(true, _internalMainPort, SubscribeHandler),
                    Arbiter.ReceiveWithIterator<Detach>(true, _internalMainPort, DetachHandler),
                    Arbiter.ReceiveWithIterator <AdjustPollingFrequency>(true, _internalMainPort, AdjustPollingFrequencyHandler)
                ),
                new ConcurrentReceiverGroup());

            // Wait on this interleave until a CreateResponse is posted to nxtCommunicationResponse port.
            Activate(initializationInterleave);

            // Set up a seperate interleave which processes I2C Commands one at a time.
            Activate(new Interleave(
                new ExclusiveReceiverGroup(
                    Arbiter.ReceiveWithIterator<SendLowSpeedCommand>(true, _internalI2CPort, I2cLowSpeedCommandHandler)),
                new ConcurrentReceiverGroup()));

        }

        /// <summary>
        /// Creates an instance of the LEGO NXT Communications service.
        /// </summary>
        /// <returns>Result PortSet for retrieving service creation response</returns>
        private DsspResponsePort<CreateResponse> CreateNxtCommunicationService()
        {
            DsspResponsePort<CreateResponse> createResponsePort = new DsspResponsePort<CreateResponse>();
            ServiceInfoType si = new ServiceInfoType(comm.Contract.Identifier, null);

            Microsoft.Dss.Services.Constructor.Create create =
                new Microsoft.Dss.Services.Constructor.Create(si, createResponsePort);

            base.ConstructorPort.Post(create);

            return createResponsePort;
        }

        /// <summary>
        /// Service Initialization
        /// Connect to the Brick and then open up the service for all requests
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator<ITask> ServiceInitialization(CreateResponse createResponse)
        {
            // Set up the _brickPort to our communications service
            _brickPort = ServiceForwarder<comm.LegoCommOperations>(createResponse.Service);

            // Subscribe to the communications port.
            yield return Arbiter.Choice(_brickPort.Subscribe(_brickNotificationPort),
                EmptyHandler<SubscribeResponseType>,
                EmptyHandler<Fault>);

            // If the SerialPort is not set, start the service, 
            // but do not attempt to connect to the actual hardware.
            bool done = (_state.Configuration.SerialPort == 0);

            if (!done)
            {
                // If we are not done yet, attempt to connect to the hardware.
                NxtBrickOperations _initPort = new NxtBrickOperations();
                PortSet<DefaultUpdateResponseType, Fault> _initResponse = new PortSet<DefaultUpdateResponseType, Fault>();
                ConnectToHardware connectToHardware = new ConnectToHardware(_state.Configuration, _initResponse);
                _initPort.Post(connectToHardware);

                // Special one time handler to connect to the hardware before we open up the service to receive messages.
                Activate<ITask>(
                    Arbiter.ReceiveWithIterator<ConnectToHardware>(false, _initPort, ConnectToHardwareHandler),
                    new Interleave(
                        new TeardownReceiverGroup(
                            Arbiter.ReceiveWithIterator<DefaultUpdateResponseType>(false, _initResponse, InitializationComplete),
                            Arbiter.ReceiveWithIterator<Fault>(false, _initResponse, InitializationComplete),
                            Arbiter.ReceiveWithIterator<DsspDefaultDrop>(false, _internalMainPort, DropHandler)
                            ),
                        new ExclusiveReceiverGroup(
                            Arbiter.ReceiveWithIterator<AttachAndSubscribe>(true, _internalMainPort, AttachAndSubscribeHandler),
                            
                            Arbiter.ReceiveWithIterator<ReserveDevicePort>(true, _internalMainPort, ReserveDevicePortHandler),
                            Arbiter.ReceiveWithIterator<Subscribe>(true, _internalMainPort, SubscribeHandler),
                            Arbiter.ReceiveWithIterator<Detach>(true, _internalMainPort, DetachHandler),
                            Arbiter.ReceiveWithIterator<AdjustPollingFrequency>(true, _internalMainPort, AdjustPollingFrequencyHandler)
                        ),
                        new ConcurrentReceiverGroup())
                );

            }
            else
            {
                SpawnIterator<DefaultUpdateResponseType>(DefaultUpdateResponseType.Instance, InitializationComplete);
            }

            yield break;
        }

        /// <summary>
        /// Service Initialization is Complete. 
        /// Activate the normal operation handlers.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public virtual IEnumerator<ITask> InitializationComplete(object response)
        {
            bool success = (response is DefaultUpdateResponseType);
            if (!success)
            {
                LogError(LogGroups.Console, "Failed to connect to the LEGO NXT hardware");
            }

            // Activate Operation Handlers for the brick 
            ActivateDsspOperationHandlers();

            base.MainPortInterleave.CombineWith(new Interleave(
                new ExclusiveReceiverGroup(
                    Arbiter.Receive<comm.ConnectionUpdate>(true, _brickNotificationPort, ConnectionUpdateHandler)
                ),
                new ConcurrentReceiverGroup()));

            // Start listening for polling requests
            Activate(Arbiter.ReceiveWithIterator<PollingEntry>(true, _pollingPort, PollingHandler));

            if (_state.Configuration.ShowInBrowser)
                OpenLegoNxtServiceInBrowser();

            yield break;
        }

        /// <summary>
        /// Send Any Command, including a two-phase LowSpeed command (LSWrite)
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public PortSet<LegoResponse, Fault> SendAnyCommand(LegoCommand cmd, bool priority)
        {
            
            if (cmd.LegoCommandCode == LegoCommandCode.LSWrite)
            {
                LegoLSWrite cmdLsWrite = cmd as LegoLSWrite;
                if (cmdLsWrite == null)
                    cmdLsWrite = new LegoLSWrite(cmd);

                // Send Low Speed Command
                SendLowSpeedCommand sendCmd = new SendLowSpeedCommand(cmdLsWrite);
                _internalI2CPort.Post(sendCmd);
                return sendCmd.ResponsePort;
            }
            return _brickPort.SendCommand(cmd, priority);
        }

        /// <summary>
        /// The periodic Polling handler
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public IEnumerator<ITask> PollingHandler(PollingEntry entry)
        {
            if (entry == null)
                yield break;

            if (_pendingRemoval.Count > 0 && _pendingRemoval.Contains(entry.AttachRequest))
            {
                lock (_pendingRemoval)
                {
                    _pendingRemoval.Remove(entry.AttachRequest);
                }
                yield break;
            }

            // Drop disabled polling requests.
            if (entry.AttachRequest.PollingCommands.PollingFrequencyMs <= 0)
                yield break;

            // If this polling request is old, drop it.
            string connectionKey = entry.AttachRequest.Registration.Connection.ToString();
            if (_state.Runtime.Devices.ContainsKey(connectionKey)
                && entry.Timestamp < _state.Runtime.Devices[connectionKey].Timestamp)
                yield break;

            DateTime start = DateTime.Now;
            try
            {
                if (entry.StartedPolling == DateTime.MinValue)
                    entry.StartedPolling = start;

                if (_state.Runtime.Connected
                    && entry.AttachRequest.PollingCommands != null
                    && entry.AttachRequest.PollingCommands.Commands != null
                    && entry.AttachRequest.PollingCommands.Commands.Count > 0)
                {
                    bool priority = (entry.AttachRequest.PollingCommands.PollingFrequencyMs <= 50);
                    bool success = true;
                    bool reset = false;
                    foreach (LegoCommand cmd in entry.AttachRequest.PollingCommands.Commands)
                    {
                        // Send Polling Command.
                        yield return Arbiter.Choice(SendAnyCommand(cmd, priority),
                            delegate(LegoResponse ok)
                            {
                                // Send notification back to sensor service.
                                SendNotificationToTarget<LegoSensorUpdate>(entry.AttachRequest.Registration.SubscriberUri, _subMgrPort, ok);
                            },
                            delegate(Fault fault) 
                            {
                                string errorText = (fault.Reason != null && fault.Reason.Length >= 1 && !string.IsNullOrEmpty(fault.Reason[0].Value)) ? fault.Reason[0].Value : "Fault";
                                reset = errorText.Contains("CommunicationBusError") || errorText.Contains("I2C Communication Error");
                                success = false;
                                LogError(fault); 
                            });
                    }
                    if (success)
                        entry.PollingSuccessCount++;
                    else
                    {
                        entry.PollingFailureCount++;
                        if (reset && entry.AttachRequest.InitializationCommands != null)
                        {
                            LogWarning(LogGroups.Console, string.Format("Reinitializing {0} on {1}", entry.AttachRequest.Registration.DeviceModel, entry.AttachRequest.Registration.Connection));
                            foreach (LegoCommand cmd in entry.AttachRequest.InitializationCommands.Commands)
                            {
                                // Send Initialization Sequence.
                                yield return Arbiter.Choice(_brickPort.SendCommand(cmd),
                                    delegate(LegoResponse ok) { },
                                    delegate(Fault fault) { LogError(fault); });
                            }
                        }
                    }
                }
            }
            finally
            {
                if (_state != null
                    && _state.Runtime != null
                    && !_internalStatus.Disconnected)
                {
                    DateTime finish = DateTime.Now;

                    // Calculate the next target time.
                    entry.Next = start.AddMilliseconds(entry.AttachRequest.PollingCommands.PollingFrequencyMs);
                    TimeSpan diff = entry.Next.Subtract(finish);

                    double totalPollingMs = finish.Subtract(entry.StartedPolling).TotalMilliseconds;
                    entry.AttachRequest.PollingCommands.AveragePollingFrequencyMs = totalPollingMs / (double)entry.PollingSuccessCount;

                    // Wait for the next event
                    if (diff.TotalMilliseconds > 0.0)
                    {
                        // Don't wait around.  Just activate when it's time for the next poll.
                        Activate(Arbiter.Receive(false, TimeoutPort(diff),
                            delegate(DateTime next)
                            {
                                _pollingPort.Post(entry);
                            }));
                    }
                    else
                    {
                        // We're behind schedule.  Poll as soon as possible.
                        _pollingPort.Post(entry);
                    }
                }
                else
                {
                    LogInfo(LogGroups.Console, "Stopping Timer Handler");
                }
            }
            yield break;
        }

        /// <summary>
        /// Reply to any request with "Not Connected".
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public IEnumerator<ITask> NotConnectedHandler(Object request)
        {
            throw new InvalidOperationException("The Lego NXT Brick (v2) service is not connected to a brick.");
        }

        /// <summary>
        /// LegoSensorUpdate External Handler
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public IEnumerator<ITask> LegoSensorUpdateHandler(LegoSensorUpdate request)
        {
            throw new InvalidOperationException("The Lego NXT Brick (v2) service is not connected to a brick.");
        }

        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        public virtual IEnumerator<ITask> GetHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }

        /// <summary>
        /// HttpGet Handler
        /// </summary>
        /// <param name="get"></param>
        public void HttpGetHandler(HttpGet get)
        {
            get.ResponsePort.Post(new HttpResponseType(HttpStatusCode.OK, _state, _transform));
        }


        /// <summary>
        /// DisconnectFromHardware Handler
        /// </summary>
        /// <param name="disconnect"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> DisconnectFromHardwareHandler(DisconnectFromHardware disconnect)
        {
            // Close the Connection
            if (_state.Runtime.Connected)
            {
                _internalStatus.Disconnected = true;

                _state.Runtime.Connected = false;
                _internalStatus.ClosePending = true;
                SendNotification<DisconnectFromHardware>(_subMgrPort, disconnect);

                yield return Arbiter.Choice(_brickPort.Close(),
                    EmptyHandler<DefaultSubmitResponseType>,
                    EmptyHandler<Fault>);
            }
            disconnect.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// Configure Handler
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> ConnectToHardwareHandler(ConnectToHardware configure)
        {
            bool success = true;
            Fault fault = null;

            try
            {
                if (_state.Runtime.Connected)
                {
                    // If already connected, ConnectToHardware always forces a connection reset.
                    _internalStatus.ClosePending = true;
                    _state.Runtime.Connected = false;
                    SendNotification<DisconnectFromHardware>(_subMgrPort, new DisconnectFromHardware());
                }

                _state.Configuration = configure.Body;
                if (configure.Body.SerialPort <= 0)
                {
                    // Close the Connection
                    if (_state.Runtime.Connected)
                    {
                        _internalStatus.Disconnected = true;

                        yield return Arbiter.Choice(_brickPort.Close(),
                            EmptyHandler<DefaultSubmitResponseType>,
                            delegate(Fault failure)
                            {
                                success = false;
                                fault = failure;
                            });
                    }
                }
                else
                {
                    // Open the Connection
                    yield return Arbiter.Choice(
                        _brickPort.Open( new comm.OpenRequest(configure.Body.SerialPort, configure.Body.BaudRate, configure.Body.ConnectionType)),
                            delegate(DefaultSubmitResponseType ok)
                            {
                                _internalStatus.Disconnected = false;
                                _state.Runtime.Connected = true;
                            },
                            delegate(Fault failure)
                            {
                                fault = failure;
                                _state.Runtime.Connected = false;
                                success = false;
                            });


                    if (_state.Runtime.Connected)
                    {
                        LegoGetFirmwareVersion getFirmwareVersion = new LegoGetFirmwareVersion();
                        getFirmwareVersion.TryCount = 3;
                        yield return Arbiter.Choice(_brickPort.SendCommand(getFirmwareVersion),
                            delegate(LegoResponse ok) 
                            {
                                LegoResponseGetFirmwareVersion fv = LegoResponse.Upcast<LegoResponseGetFirmwareVersion>(ok);
                                if (fv != null && fv.Success)
                                {
                                    _state.Runtime.Firmware = string.Format("Firmware: {0}.{1}  Protocol: {2}.{3}", fv.MajorFirmwareVersion, fv.MinorFirmwareVersion, fv.MajorProtocolVersion, fv.MinorProtocolVersion);
                                    if (fv.MajorFirmwareVersion == 1 && fv.MinorFirmwareVersion < 3)
                                        LogError(LogGroups.Console, "Your LEGO Firmware is out of date. \n"
                                            + "Please refer to the LEGO MINDSTORMS website \n"
                                            + "and use the LEGO MINDSTORMS Software to update your firmware.");
                                }
                                else
                                {
                                    LogError(LogGroups.Console, fv.ErrorCode.ToString());
                                    success = false;
                                }
                            },
                            delegate(Fault failure) 
                            {
                                success = false;
                                _state.Runtime.Connected = false;
                                fault = failure;
                            });

                        if (_state.Runtime.Connected)
                            yield return Arbiter.Choice(_brickPort.SendCommand(new LegoGetDeviceInfo()),
                                delegate(LegoResponse ok)
                                {
                                    LegoResponseGetDeviceInfo fv = LegoResponse.Upcast<LegoResponseGetDeviceInfo>(ok); 
                                    if (fv != null 
                                        && fv.Success 
                                        && !string.IsNullOrEmpty(fv.BrickName))
                                    {
                                        _state.Runtime.BrickName = fv.BrickName;
                                    }
                                },
                                delegate(Fault failure) 
                                {
                                    _state.Runtime.Connected = false;
                                    success = false;
                                    fault = failure;
                                });


                        // If we already had registered devices
                        // we need to reinitialize them!
                        if (success 
                            && _state.Runtime.Connected
                            && _state.Runtime.Devices != null
                            && _state.Runtime.Devices.Count > 0)
                        {
                            foreach (AttachRequest device in _state.Runtime.Devices.Values)
                            {
                                if (device.InitializationCommands != null)
                                {
                                    foreach (LegoCommand cmd in device.InitializationCommands.Commands)
                                    {
                                        // Send Initialization Sequence.
                                        // If an LSWrite is sent here, it will only send the command
                                        // without LSReading a response.
                                        yield return Arbiter.Choice(_brickPort.SendCommand(cmd),
                                            delegate(LegoResponse ok) { },
                                            delegate(Fault failure) { LogError(failure); });
                                    }
                                }
                            }
                        }
                        else if (!_state.Runtime.Connected)
                        {
                            if (configure.Body.SerialPort != 0)
                                success = false;

                            // We failed to talk to the brick, even though the port is open.
                            // Clean up by closing the serial port.
                            yield return Arbiter.Choice(_brickPort.Close(),
                                EmptyHandler<DefaultSubmitResponseType>,
                                EmptyHandler<Fault>);
                        }

                    }

                    // Send a notification that the Brick service is ready.
                    if (success && _state.Runtime.Connected)
                    {
                        // Play a tone to signal that initialization was successful.
                        yield return Arbiter.Choice(_brickPort.SendCommand(new LegoPlayTone(1074, 500)),
                            delegate(LegoResponse ok) { },
                            delegate(Fault failure) { LogError(failure); });

                        SendNotification<ConnectToHardware>(_subMgrPort, configure);
                    }
                }
            }
            finally
            {
                if (success)
                {
                    configure.ResponsePort.Post(DefaultUpdateResponseType.Instance);
                }
                else 
                {
                    if (fault == null)
                        fault = Fault.FromException(new InvalidOperationException("Failed to connect to the LEGO NXT Hardware"));

                    LogError(fault);
                    configure.ResponsePort.Post(fault);
                }
            }
            yield break;
        }

        /// <summary>
        /// Remove a device from the device list and polling queue.
        /// </summary>
        /// <param name="serviceUri"></param>
        private void UnregisterDevice(string serviceUri)
        {
            if (string.IsNullOrEmpty(serviceUri))
                return;

            string[] keys = new string[_state.Runtime.Devices.Keys.Count];
            _state.Runtime.Devices.Keys.CopyTo(keys, 0);
            foreach(string key in keys)
            {
                if (_state.Runtime.Devices[key].Registration.ServiceUri.Equals(serviceUri))
                {
                    AttachRequest attachedDevice = _state.Runtime.Devices[key];
                    // Remove any polling requests
                    if (attachedDevice.PollingCommands != null
                        && (attachedDevice.PollingCommands.PollingFrequencyMs > 0
                            || attachedDevice.PollingCommands.OriginalPollingFrequencyMs > 0)
                        && attachedDevice.PollingCommands.Commands != null
                        && attachedDevice.PollingCommands.Commands.Count > 0)
                    {
                        _pendingRemoval.Add(attachedDevice);
                    }
                    _state.Runtime.Devices.Remove(key);
                }
            }

        }


        /// <summary>
        /// Reserve a port for the specified device.
        /// </summary>
        /// <param name="reservation"></param>
        /// <returns>The port which will be reserved.</returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ReserveDevicePortHandler(ReserveDevicePort reservation)
        {
            //Debugger.Break();
            AttachResponse response;
            // If the device will be detached.
            if (reservation.Body.Connection.Port == LegoNxtPort.NotConnected)
            {
                response = new AttachResponse(reservation.Body.Connection, reservation.Body.DeviceModel);
                reservation.ResponsePort.Post(response);
                yield break;
            }

            switch (reservation.Body.Connection.Port)
            {
                case LegoNxtPort.A:
                case LegoNxtPort.EncoderA:
                    reservation.Body.Connection.Port = LegoNxtPort.MotorA;
                    break;
                case LegoNxtPort.B:
                case LegoNxtPort.EncoderB:
                    reservation.Body.Connection.Port = LegoNxtPort.MotorB;
                    break;
                case LegoNxtPort.C:
                case LegoNxtPort.EncoderC:
                    reservation.Body.Connection.Port = LegoNxtPort.MotorC;
                    break;
                case LegoNxtPort.AnyMotorPort:
                case LegoNxtPort.AnySensorPort:
                    
                    lock(_state.Runtime.Devices)
                    {
                        // See if the device is already attached.
                        foreach (AttachRequest device in _state.Runtime.Devices.Values)
                        {
                            if (device.Registration != null
                                && device.Registration.ServiceUri != null
                                && device.Registration.ServiceUri.Equals(reservation.Body.ServiceUri, StringComparison.InvariantCultureIgnoreCase))
                            {
                                response = new AttachResponse(device.Registration.Connection, reservation.Body.DeviceModel);
                                reservation.ResponsePort.Post(response);

                                yield break;
                            }
                        }
                    }

                    if (reservation.Body.Connection.Port == LegoNxtPort.AnyMotorPort)
                    {
                        // Use the next available Motor Port
                        if (!_state.Runtime.Devices.ContainsKey(LegoNxtPort.MotorA.ToString())
                            || string.IsNullOrEmpty(_state.Runtime.Devices[LegoNxtPort.MotorA.ToString()].Registration.ServiceUri))
                            reservation.Body.Connection.Port = LegoNxtPort.MotorA;
                        else if (!_state.Runtime.Devices.ContainsKey(LegoNxtPort.MotorB.ToString())
                            || string.IsNullOrEmpty(_state.Runtime.Devices[LegoNxtPort.MotorB.ToString()].Registration.ServiceUri))
                            reservation.Body.Connection.Port = LegoNxtPort.MotorB;
                        else if (!_state.Runtime.Devices.ContainsKey(LegoNxtPort.MotorC.ToString())
                            || string.IsNullOrEmpty(_state.Runtime.Devices[LegoNxtPort.MotorC.ToString()].Registration.ServiceUri))
                            reservation.Body.Connection.Port = LegoNxtPort.MotorC;
                        else
                            reservation.Body.Connection.Port = LegoNxtPort.NotConnected;
                    }
                    else if (reservation.Body.Connection.Port == LegoNxtPort.AnySensorPort)
                    {
                        if (reservation.Body.DeviceType == LegoDeviceType.DigitalSensor)
                        {
                            bool done = !_state.Runtime.Connected;

                            yield return Arbiter.Receive(false, TestPortForI2CSensor(LegoNxtPort.Sensor1, reservation.Body.DeviceModel),
                                delegate(bool success)
                                {
                                    if (success)
                                        reservation.Body.Connection.Port = LegoNxtPort.Sensor1;
                                    done = success;
                                });

                            //if (!done)
                            //    yield return Arbiter.Receive(false, TestPortForI2CSensor(LegoNxtPort.Sensor2, reservation.Body.DeviceModel),
                            //        delegate(bool success)
                            //        {
                            //            if (success)
                            //                reservation.Body.Connection.Port = LegoNxtPort.Sensor2;
                            //            done = success;
                            //        });

                            //if (!done)
                            //    yield return Arbiter.Receive(false, TestPortForI2CSensor(LegoNxtPort.Sensor3, reservation.Body.DeviceModel),
                            //        delegate(bool success)
                            //        {
                            //            if (success)
                            //                reservation.Body.Connection.Port = LegoNxtPort.Sensor3;
                            //            done = success;
                            //        });

                            //if (!done)
                            //{
                            //    LogInfo(LogGroups.Console, "Checking for I2C Sensor on Port 4");
                            //    yield return Arbiter.Receive(false, TestPortForI2CSensor(LegoNxtPort.Sensor4, reservation.Body.DeviceModel),
                            //        delegate(bool success)
                            //        {
                            //            LogInfo(LogGroups.Console, "Found I2C Sensor on Port 4: " + success.ToString());
                            //            if (success)
                            //                reservation.Body.Connection.Port = LegoNxtPort.Sensor4;
                            //            done = success;
                            //        });
                            //}

                        }
                    }

                    break;

            }

            // If we weren't able to assign the Port, exit
            if ( reservation.Body.Connection.Port == LegoNxtPort.NotConnected
                || reservation.Body.Connection.Port == LegoNxtPort.AnyMotorPort
                || reservation.Body.Connection.Port == LegoNxtPort.AnySensorPort
                || reservation.Body.Connection.Port == LegoNxtPort.A
                || reservation.Body.Connection.Port == LegoNxtPort.B
                || reservation.Body.Connection.Port == LegoNxtPort.C
                || reservation.Body.Connection.Port == LegoNxtPort.EncoderA
                || reservation.Body.Connection.Port == LegoNxtPort.EncoderB
                || reservation.Body.Connection.Port == LegoNxtPort.EncoderC)
            {
                reservation.Body.Connection.Port = LegoNxtPort.NotConnected;
            }
            else // Reserve the connection
            {
                _state.Runtime.Devices[reservation.Body.Connection.ToString()] = new AttachRequest(reservation.Body);
            }

            response = new AttachResponse(reservation.Body.Connection, reservation.Body.DeviceModel);
            reservation.ResponsePort.Post(response);

            yield break;

        }

        /// <summary>
        /// Test a Sensor Port for an I2C Sensor
        /// </summary>
        /// <param name="sensorPort"></param>
        /// <param name="sensorType"></param>
        /// <returns></returns>
        private Port<bool> TestPortForI2CSensor(LegoNxtPort sensorPort, string sensorType)
        {
            Port<bool> resultPort = new Port<bool>();
            if (_state.Runtime.Devices.ContainsKey(sensorPort.ToString()))
            {
                LogInfo(LogGroups.Console, string.Format("I2C Sensor Port {0} is already reserved.", sensorPort));

                // If the port is reserved, don't even look at it.
                resultPort.Post(false);
            }
            else
            {
                SpawnIterator<Port<bool>, LegoNxtPort, string>(resultPort, sensorPort, sensorType, TestPortForI2CSensorHandler);
            }
            return resultPort;
        }

        /// <summary>
        /// Test a Sensor Port for an I2C Sensor
        /// </summary>
        /// <param name="response"></param>
        /// <param name="sensorPort"></param>
        /// <param name="sensorType"></param>
        /// <returns></returns>
        private IEnumerator<ITask> TestPortForI2CSensorHandler(Port<bool> response, LegoNxtPort sensorPort, string sensorType)
        {
            LogInfo("- TestPortForI2CSensorHandler");
            //Debugger.Break();
            // Read from I2C to find the device.
            bool found = false;
            bool abort = false;
            LegoSetInputMode setInputMode = null;

            if (_brickPort != null)
            {
                // Configure the port as an I2C sensor.
                setInputMode = new LegoSetInputMode((NxtSensorPort)sensorPort, LegoSensorType.I2C_9V, LegoSensorMode.RawMode);
                setInputMode.TryCount = 2;
                yield return Arbiter.Choice(_brickPort.SendCommand(setInputMode),
                    delegate(LegoResponse ok)
                    {
                        //Debugger.Break();
                        if (!ok.Success)
                            abort = true;
                        else
                            LogInfo(LogGroups.Console, string.Format("{0} set to {1} mode.", sensorPort, setInputMode.SensorType));

                    },
                    delegate(Fault fault)
                    {
                        abort = true;
                    });
            }
            else
                abort = true;

            if (abort)
            {
                LogInfo(LogGroups.Console, string.Format("Failure setting I2C mode on {0}.", sensorPort));
                response.Post(false);
                yield break;
            }

            SendLowSpeedCommand lsCmd = new SendLowSpeedCommand();
            I2CReadSensorType readSensors = new I2CReadSensorType((NxtSensorPort)sensorPort);
            
            LogInfo("The sensor port is: " + sensorPort.ToString());
            lsCmd.Body = readSensors;
            SpawnIterator<SendLowSpeedCommand>(lsCmd, SendLowSpeedCommandHandler);
            yield return Arbiter.Choice(lsCmd.ResponsePort,
                delegate(LegoResponse ok)
                {
                    //Debugger.Break();
                    I2CResponseSensorType sensorResponse = LegoResponse.Upcast<I2CResponseSensorType>(ok); 
                    LogInfo(LogGroups.Console, string.Format("{0} I2C response {1} is {2}.", sensorPort, sensorResponse.ErrorCode, sensorResponse.SensorType));

                    if (sensorResponse.SensorType.Equals(sensorType, StringComparison.InvariantCultureIgnoreCase))
                    {
                        found = true;
                    }
                    else if (sensorType.IndexOf(',') >= 0)
                    {
                        foreach (string subtype in sensorType.Split(','))
                        {
                            if (sensorResponse.SensorType.Equals(subtype, StringComparison.InvariantCultureIgnoreCase))
                                found = true;
                        }
                    }
                    else
                    {
                        LogInfo(LogGroups.Console, string.Format("Found an unattached I2C Sensor from {0}: {1}", sensorResponse.Manufacturer, sensorResponse.SensorType));
                    }
                },
                delegate(Fault fault)
                {
                    string msg = (fault.Reason != null && fault.Reason.Length > 0 && !string.IsNullOrEmpty(fault.Reason[0].Value)) ? fault.Reason[0].Value : string.Empty;
                    LogError(LogGroups.Console, string.Format("{0} fault reading I2C Sensor Type: {1}.", sensorPort, msg));
                    abort = true;
                });

            if (!found)
            {
                LogInfo(LogGroups.Console, string.Format("Set {0} back to No Sensor.", sensorPort));

                setInputMode = new LegoSetInputMode((NxtSensorPort)sensorPort, LegoSensorType.NoSensor, LegoSensorMode.RawMode);
                yield return Arbiter.Choice(_brickPort.SendCommand(setInputMode),
                    delegate(LegoResponse ok)
                    {
                        if (!ok.Success)
                            abort = true;
                    },
                    delegate(Fault fault)
                    {
                        abort = true;
                    });

            }

            LogInfo(LogGroups.Console, string.Format("I2C ReadSensorType on {0} finished: {1}", sensorPort, found));

            response.Post(found);
            yield break;
        }
        

        /// <summary>
        /// Attach Handler
        /// </summary>
        /// <param name="attach"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> AttachAndSubscribeHandler(AttachAndSubscribe attach)
        {

            AttachResponse response;
            // Disconnect request
            if (attach.Body.Registration.Connection.Port == LegoNxtPort.NotConnected)
            {
                // Check if the sensor was already registered.
                UnregisterDevice(attach.Body.Registration.ServiceUri);

                response = new AttachResponse(attach.Body.Registration.Connection, attach.Body.Registration.DeviceModel);
                attach.ResponsePort.Post(response);

                yield break;
            }

            // Invalid Ports, return a fault
            if (attach.Body.Registration.Connection.Port == LegoNxtPort.AnyMotorPort
                || attach.Body.Registration.Connection.Port == LegoNxtPort.AnySensorPort
                || attach.Body.Registration.Connection.Port == LegoNxtPort.A
                || attach.Body.Registration.Connection.Port == LegoNxtPort.B
                || attach.Body.Registration.Connection.Port == LegoNxtPort.C
                || attach.Body.Registration.Connection.Port == LegoNxtPort.EncoderA
                || attach.Body.Registration.Connection.Port == LegoNxtPort.EncoderB
                || attach.Body.Registration.Connection.Port == LegoNxtPort.EncoderC)
            {
                attach.ResponsePort.Post(
                    Fault.FromException(
                        new ArgumentOutOfRangeException(
                            string.Format("Invalid {0} Port: {1}", attach.Body.Registration.DeviceModel, attach.Body.Registration.Connection))));

                yield break;
            }

            // Convert all inherited LegoCommand types to LegoCommands.
            NormalizeAttachRequest(attach.Body);

            string priorUri = null;
            string connectionKey = attach.Body.Registration.Connection.ToString();
            if (_state.Runtime.Devices.ContainsKey(connectionKey))
            {
                priorUri = _state.Runtime.Devices[connectionKey].Registration.ServiceUri;
                if (priorUri != attach.Body.Registration.ServiceUri)
                    UnregisterDevice(priorUri);
            }

            _state.Runtime.Devices[connectionKey] = attach.Body;

            if (!string.IsNullOrEmpty(priorUri) && !priorUri.Equals(attach.Body.Registration.ServiceUri, StringComparison.InvariantCultureIgnoreCase))
            {
                // Send Notification to prior service that it has been disconnected
                PortSet<DsspDefaultLookup, DsspDefaultDrop> priorServicePort = ServiceForwarder<PortSet<DsspDefaultLookup, DsspDefaultDrop>>(priorUri);
                priorServicePort.Post(DsspDefaultDrop.Instance);
            }

            if (_state.Runtime.Connected)
            {
                if (attach.Body.InitializationCommands != null)
                {
                    foreach (LegoCommand cmd in attach.Body.InitializationCommands.Commands)
                    {
                        // Send Initialization Sequence.
                        yield return Arbiter.Choice(_brickPort.SendCommand(cmd),
                            delegate(LegoResponse ok) { },
                            delegate(Fault fault) { LogError(fault); });
                    }
                }
            }

            attach.Body.Registration.SubscriberUri = attach.Body.Subscriber;
            attach.Body.Timestamp = DateTime.Now;

            // Register for periodic Polling.
            if (attach.Body.PollingCommands != null 
                && attach.Body.PollingCommands.Commands != null
                && attach.Body.PollingCommands.Commands.Count > 0)
            {
                _pollingPort.Post(new PollingEntry(attach.Body));
            }

            response = new AttachResponse(attach.Body.Registration.Connection, attach.Body.Registration.DeviceModel);

            yield return Arbiter.Choice(SelectiveSubscribe(attach),
                delegate(SubscribeResponseType ok)
                {
                    attach.ResponsePort.Post(response);
                },
                attach.ResponsePort.Post
            );

            yield break;
        }

        /// <summary>
        /// Normalize the Commands which are part of the attach request
        /// </summary>
        /// <param name="attachRequest"></param>
        private void NormalizeAttachRequest(AttachRequest attachRequest)
        {
            if (attachRequest == null)
                return;

            NormalizeCommandSequence(attachRequest.InitializationCommands);
            NormalizeCommandSequence(attachRequest.PollingCommands);
        }

        /// <summary>
        /// Normalize the Command Sequence to contain the LegoCommand base type.
        /// </summary>
        /// <param name="nxtCommandSequence"></param>
        private void NormalizeCommandSequence(NxtCommandSequence nxtCommandSequence)
        {
            if (nxtCommandSequence == null || nxtCommandSequence.Commands == null || nxtCommandSequence.Commands.Count == 0)
                return;

            RuntimeTypeHandle typeLegoCommand = typeof(LegoCommand).TypeHandle;
            for (int ix = 0; ix < nxtCommandSequence.Commands.Count; ix++)
            {
                // if (nxtCommandSequest.Commands[ix].GetType() == typeof(LegoCommand))
                if (Type.GetTypeHandle(nxtCommandSequence.Commands[ix]).Equals(typeLegoCommand))
                    continue;

                LegoCommand prior = nxtCommandSequence.Commands[ix];
                nxtCommandSequence.Commands[ix] = new LegoCommand(prior.ExpectedResponseSize, prior.CommandData);
                nxtCommandSequence.Commands[ix].TimeStamp = prior.TimeStamp;
            }

        }


        /// <summary>
        /// Create a custom selective subscribe which filters on the 
        /// service uri of the subscriber.
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        private PortSet<SubscribeResponseType, Fault> SelectiveSubscribe(AttachAndSubscribe subscribe)
        {
            submgr.InsertSubscription insert = new submgr.InsertSubscription(subscribe.Body);
            insert.Body.FilterType = submgr.FilterType.Default;

            //List<submgr.QueryType> query = new List<submgr.QueryType>();
            //query.Add(new submgr.QueryType(subscribe.Body.Registration.ServiceUri));
            //insert.Body.QueryList = query.ToArray();

            _subMgrPort.Post(insert);
            return insert.ResponsePort;
        }


        /// <summary>
        /// Detach Handler
        /// </summary>
        /// <param name="detach"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> DetachHandler(Detach detach)
        {
            // Check if the sensor was already registered.
            UnregisterDevice(detach.Body.ServiceUri);
            detach.ResponsePort.Post(DefaultSubmitResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// AdjustPollingFrequency Handler
        /// </summary>
        /// <param name="adjustPollingFrequency"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> AdjustPollingFrequencyHandler(AdjustPollingFrequency adjustPollingFrequency)
        {
            if (string.IsNullOrEmpty(adjustPollingFrequency.Body.ServiceUri))
            {
                adjustPollingFrequency.ResponsePort.Post(Fault.FromException(new ArgumentException("The service uri is not specified")));
                yield break;
            }

            bool success = false;
            bool found = false;
            bool alreadyPolling = false;
            bool requestedPolling = (adjustPollingFrequency.Body.PollingFrequencyMs > 0);
            bool resetPolling = (adjustPollingFrequency.Body.PollingFrequencyMs == 0);
            Fault faultResponse = null;

            foreach (string key in _state.Runtime.Devices.Keys)
            {
                if (_state.Runtime.Devices[key].Registration.ServiceUri.Equals(adjustPollingFrequency.Body.ServiceUri))
                {
                    found = true;
                    AttachRequest attachedDevice = _state.Runtime.Devices[key];

                    if (resetPolling)
                    {
                        adjustPollingFrequency.Body.PollingFrequencyMs = attachedDevice.PollingCommands.OriginalPollingFrequencyMs;
                        requestedPolling = (adjustPollingFrequency.Body.PollingFrequencyMs > 0);
                    }

                    if (attachedDevice.PollingCommands != null)
                    {
                        success = true;
                        alreadyPolling = (attachedDevice.PollingCommands.PollingFrequencyMs > 0);
                        attachedDevice.PollingCommands.PollingFrequencyMs = adjustPollingFrequency.Body.PollingFrequencyMs;

                    }
                    else if (requestedPolling)
                    {
                        faultResponse = Fault.FromException(new InvalidOperationException("The specified LEGO NXT device service has no polling commands."));
                    }
                    else // wasn't polling, and new request does't require polling.
                    {
                        success = true;
                    }

                    if (success)
                    {
                        attachedDevice.Timestamp = DateTime.Now;
                        if (requestedPolling)
                            _pollingPort.Post(new PollingEntry(attachedDevice));
                    }
                }
            }

            if (success)
            {
                adjustPollingFrequency.ResponsePort.Post(new AdjustPollingFrequencyResponse(adjustPollingFrequency.Body.PollingFrequencyMs));
                yield break;
            }

            if (!found)
                faultResponse = Fault.FromException(new InvalidOperationException("The specified service was not found: " + adjustPollingFrequency.Body.ServiceUri));

            if (faultResponse == null)
                faultResponse = Fault.FromException(new InvalidOperationException("Failure changing the polling frequency for service: " + adjustPollingFrequency.Body.ServiceUri));

            adjustPollingFrequency.ResponsePort.Post(faultResponse);
            yield break;
        }

        /// <summary>
        /// Send I2C Command Handler
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SendLowSpeedCommandHandler(SendLowSpeedCommand cmd)
        {
            _internalI2CPort.Post(cmd);
            yield break;
        }

        /// <summary>
        /// Send LEGO Command Handler
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SendLegoCommandHandler(SendNxtCommand cmd)
        {
            yield return Arbiter.Choice(_brickPort.SendCommand(cmd.Body, true),
                delegate(LegoResponse response)
                {
                    cmd.ResponsePort.Post(response);
                },
                delegate(Fault fault)
                {
                    cmd.ResponsePort.Post(fault);
                });
            
            yield break;
        }


            /// <summary>
        /// Play a tone on the internal LEGO NXT Speaker
        /// </summary>
        /// <param name="playTone"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> PlayToneHandler(PlayTone playTone)
        {
            LegoPlayTone cmd = new LegoPlayTone(playTone.Body.Frequency, playTone.Body.Duration);
            yield return Arbiter.Choice(_brickPort.SendCommand(cmd),
                delegate(LegoResponse ok) 
                {
                    // If the command was successful, send the response after the tone duration.
                    Activate(Arbiter.Receive<DateTime>(false, TimeoutPort(playTone.Body.Duration),
                        delegate(DateTime doneWithTone)
                        {
                            playTone.ResponsePort.Post(DefaultSubmitResponseType.Instance);
                        }));
                },
                playTone.ResponsePort.Post);

            yield break;
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> SubscribeHandler(Subscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);

            if (_state.Runtime.Connected)
            {
                // If the brick is already connected, let the subscriber know.
                SendNotificationToTarget<ConnectToHardware>(subscribe.Body.Subscriber, _subMgrPort, _state.Configuration);
            }
            yield break;
        }

        /// <summary>
        /// Drop Handler
        /// </summary>
        /// <param name="drop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public virtual IEnumerator<ITask> DropHandler(DsspDefaultDrop drop)
        {
            // Close the Connection to the LEGO
            if (_brickPort != null)
                _brickPort.Close();

            base.DefaultDropHandler(drop);
            yield break;
        }

        /// <summary>
        /// Connection Update notifications
        /// </summary>
        /// <param name="connectionUpdate"></param>
        public virtual void ConnectionUpdateHandler(comm.ConnectionUpdate connectionUpdate)
        {
            if (_internalStatus.ClosePending)
            {
                _internalStatus.ClosePending = false;
            }
            else if (_state.Runtime.Connected != connectionUpdate.Body.Connected)
            {
                _state.Runtime.Connected = connectionUpdate.Body.Connected;

                if (!_state.Runtime.Connected)
                    SendNotification<DisconnectFromHardware>(_subMgrPort, new DisconnectFromHardware());
            }
        }

        #region Internal

        /// <summary>
        /// Send I2C Command and post a response when it is completed.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public virtual IEnumerator<ITask> I2cLowSpeedCommandHandler(SendLowSpeedCommand cmd)
        {
           
            bool done = false;
            bool retry = false;
            cmd.Body.RequireResponse = false;
            cmd.Body.TryCount = 1;
            yield return Arbiter.Choice(_brickPort.SendCommand(cmd.Body),
                delegate(LegoResponse rsp)
                {
                    if (cmd.Body.ExpectedI2CResponseSize == 0)
                    {
                        cmd.ResponsePort.Post(new LegoResponseLSRead());
                        done = true;
                    }
                },
                delegate(Fault fault)
                {
                    cmd.ResponsePort.Post(fault);
                    done = true;
                });

            if (done)
                yield break;

            // Call LSStatus until the data is ready
            _cmdLSGetStatus.Port = cmd.Body.Port;
            _cmdLSGetStatus.TryCount = 1;
            int lsBytesReady = 0;
            int statusReadCount = 0;
            do
            {
                //Debugger.Break();
                yield return Arbiter.Choice(_brickPort.SendCommand(_cmdLSGetStatus),
                    delegate(LegoResponse rsp)
                    {
                        LegoResponseLSGetStatus lsStatus = rsp as LegoResponseLSGetStatus;
                        if (lsStatus == null)
                        {
                            ////Debugger.Break();
                            lsStatus = new LegoResponseLSGetStatus(rsp.CommandData);
                        }
                        
                        switch (lsStatus.ErrorCode)
                        {
                            case LegoErrorCode.Success:
                                lsBytesReady = lsStatus.BytesReady;
                                break;
                            case LegoErrorCode.PendingCommunicationTransactionInProgress:
                                // Just try status again
                                break;
                            case LegoErrorCode.CommunicationBusError:
                                retry = true;
                                break;
                            default:
                                cmd.ResponsePort.Post(Fault.FromException(new InvalidOperationException("Lego Error Code: " + lsStatus.ErrorCode)));
                                done = true;
                                break;
                        }

                    },
                    delegate(Fault fault)
                    {
                        cmd.ResponsePort.Post(fault);
                        done = true;
                    });

                if (retry)
                {
                    retry = false;

                    // Reset the I2C port by reading 1 byte.
                    LegoLSWrite resetI2C = new I2CReadSonarSensor(cmd.Body.Port, UltraSonicPacket.FactoryZero);
                    //LegoLSWrite resetI2C = new I2CReadSonarSensor(cmd.Body.Port, UltraSonicPacket.ByteEight);
                    SendLowSpeedCommand sendReset = new SendLowSpeedCommand(resetI2C);
                    if (cmd.Body.ExpectedI2CResponseSize == 1 && NxtCommon.ByteArrayIsEqual(cmd.Body.CommandData, resetI2C.CommandData))
                    {
                        // we received a BusError on the reset command.
                        cmd.ResponsePort.Post(Fault.FromException(new InvalidOperationException("Lego Error Code: " + LegoErrorCode.CommunicationBusError)));
                        done = true;
                    }
                    else
                    {
                        LogInfo(LogGroups.Console, "Resetting the I2C Bus on " + cmd.Body.Port.ToString());
                        SpawnIterator<SendLowSpeedCommand>(sendReset, I2cLowSpeedCommandHandler);
                        yield return Arbiter.Choice(sendReset.ResponsePort,
                            delegate(LegoResponse ok)
                            {
                                // try again
                                lsBytesReady = 0;
                            },
                            delegate(Fault fault)
                            {
                                cmd.ResponsePort.Post(fault);
                                done = true;
                            });
                    }
                }

                if (done)
                    yield break;

            } while (lsBytesReady < cmd.Body.ExpectedI2CResponseSize &&  (statusReadCount++ < 10));
            // Call LSRead to get the return packet
            LegoLSRead lsRead = new LegoLSRead(cmd.Body.Port);
            LegoResponse response = null;
            Fault lsReadFault = null;
            //Debugger.Break();
            while (lsBytesReady > 0)
            {
                yield return Arbiter.Choice(
                    _brickPort.SendCommand(lsRead),
                    delegate(LegoResponse rsp) { response = rsp; },
                    delegate(Fault fault) { lsReadFault = fault; });

                lsBytesReady -= cmd.Body.ExpectedI2CResponseSize;
            }
            //Debugger.Break();
            //if (response != null && lsBytesReady == 0 && response.CommandData != null && response.CommandData.Length >= 3 && response.CommandData[2] == 0)
            if (response != null && response.CommandData != null && response.CommandData.Length >= 3 && response.CommandData[2] == 0)
                cmd.ResponsePort.Post(response);
            else if (lsReadFault != null)
                cmd.ResponsePort.Post(lsReadFault);
            else
                cmd.ResponsePort.Post(Fault.FromException(new InvalidOperationException("I2C Communication Error")));

            yield break;
        }


        #endregion


    }

}
