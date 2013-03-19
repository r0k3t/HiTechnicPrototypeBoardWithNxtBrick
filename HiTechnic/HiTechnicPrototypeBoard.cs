using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using pxbrick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick.Proxy;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using W3C.Soap;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;

namespace Microsoft.Robotics.Services.Sample.HiTechnic.PrototypeBoard
{
    /// <summary>
    /// HiTechnic Accelerometer sensor Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [Description("Provides access to the HiTechnic Prototype Board.\n(for use with 'Lego NXT Brick (v2)' service)")]
    [DisplayName("(User) HiTechnic Prototype Board")]
    [DssCategory(pxbrick.LegoCategories.NXT)]
    public class HiTechnicPrototypeBoard : DsspServiceBase
    {
        /// <summary>
        /// _state
        /// </summary>v2
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/HiTechnic.Nxt.PrototypeBoard.config.xml")] 
        private PrototypeBoardState _state = new PrototypeBoardState();


        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/hitechnic/nxt/prototypeBoard", AllowMultipleInstances = false)] 
        private PrototypeBoardOperations _internalMainPort = new PrototypeBoardOperations();
        private PrototypeBoardOperations _reliableMainPort = null;

        /// <summary>
        /// Partner with the LEGO NXT Brick
        /// </summary>
        [Partner("brick", Contract = pxbrick.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate, Optional = false)] 
        private readonly pxbrick.NxtBrickOperations _legoBrickPort = new pxbrick.NxtBrickOperations();
        private readonly pxbrick.NxtBrickOperations _legoBrickNotificationPort = new pxbrick.NxtBrickOperations();

        /// <summary>
        /// Subscription manager partner for prototype board
        /// </summary>
        [Partner(Partners.SubscriptionManagerString, Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)] 
        private readonly submgr.SubscriptionManagerPort _subMgrPort = new submgr.SubscriptionManagerPort();

        ///// <summary>
        ///// Subscription manager partner for generic analog sensor
        ///// </summary>
        //[Partner(Partners.SubscriptionManagerString + "/analogsensor", Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)] 
        //private submgr.SubscriptionManagerPort _genericSubMgrPort = new submgr.SubscriptionManagerPort();


        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public HiTechnicPrototypeBoard(DsspServiceCreationPort creationPort) : base(creationPort)
        {
            LogInfo("HiTechnicPrototypeBoard Service Created.");
        }

        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            InitializeState();

            base.Start();
            var concurrentRecieverGroup = new ConcurrentReceiverGroup();
            var exclusiveReceiverGroup = new ExclusiveReceiverGroup(Arbiter.Receive<pxbrick.LegoSensorUpdate>(true, _legoBrickNotificationPort, NotificationHandler));
            MainPortInterleave.CombineWith(new Interleave(exclusiveReceiverGroup, concurrentRecieverGroup));

            // Set up the reliable port using DSS forwarder to ensure exception and timeout conversion to fault.
            _reliableMainPort = ServiceForwarder<PrototypeBoardOperations>(this.ServiceInfo.Service);
            _reliableMainPort.ConnectToBrick(_state);
        }

        private void InitializeState()
        {
            if (_state == null)
            {
                _state = new PrototypeBoardState
                    {
                        SensorPort = (NxtSensorPort)LegoNxtPort.Sensor1, 
                        Name = "My Prototype Board"
                    };
            }

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            _state.Connected = false;
            _state.ManufactureInfo = "Not Set";
        }

        private void NotificationHandler(pxbrick.LegoSensorUpdate update)
        {
            var sensorResponse = LegoResponse.Upcast<I2CResponseSensorType>(update.Body);
            LogInfo(string.Format("Notification Handler: {0} - {1}", sensorResponse.Manufacturer, sensorResponse.SensorType));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ledStatus"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SetLedHandler(SetLed ledStatus)
        {
            var ledStatusByte = BitConverter.GetBytes((int) ledStatus.Body.Status);

            var ledWrite = new LegoLSWrite
                {
                    Port = _state.SensorPort,
                    TXData = new byte[] {0x10, 0x51, ledStatusByte[0]},
                    ExpectedI2CResponseSize = 0
                };

            Activate(Arbiter.Choice(_legoBrickPort.SendNxtCommand(ledWrite),
                                    x => ledStatus.ResponsePort.Post(new DefaultUpdateResponseType()),
                                    f => ledStatus.ResponsePort.Post(f)));

            yield break;
        }


        /// <summary>
        /// Read data from a specific I2C address using a LegoLSWrite
        /// </summary>
        /// <param name="readRequest"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ReadFromI2cAddressHandler(ReadFromI2cAddress readRequest)
        {
            var write = new LegoLSWrite
            {
                Port = _state.SensorPort,
                TXData = readRequest.Body.TxData,
                ExpectedI2CResponseSize = readRequest.Body.ExpectedResponseSize
            };
    

            Activate(Arbiter.Choice(_legoBrickPort.SendNxtCommand(write),
                                    EmptyHandler,
                                    f => LogInfo(f.ToException().InnerException + " " + f.ToException().Message)));

            Activate(Arbiter.Receive(false, TimeoutPort(80), EmptyHandler));

            var read = new LegoLSRead(_state.SensorPort);
            Activate(Arbiter.Choice(_legoBrickPort.SendNxtCommand(read),
                                    r => readRequest.ResponsePort.Post(new ReadResponse {Bytes = r.CommandData}),
                                    f => readRequest.ResponsePort.Post(f)));
            yield break;
        }

        /// <summary>
        /// Get prototype board handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetHandler(Get get)
        {
            var write = new LegoLSWrite
                {
                    Port = _state.SensorPort,
                    TXData = new byte[] {0x10, 0x07},
                    ExpectedI2CResponseSize = 15
                };

            Activate(Arbiter.Choice(_legoBrickPort.SendNxtCommand(write),
                                    EmptyHandler,
                                    f => LogInfo(f.ToException().InnerException + " " + f.ToException().Message)));

            Activate(Arbiter.Receive(false, TimeoutPort(500), EmptyHandler));

            var read = new LegoLSRead(_state.SensorPort);
            Activate(Arbiter.Choice(_legoBrickPort.SendNxtCommand(read),
                                    r =>
                                        {
                                            var response = LegoResponse.Upcast<I2CResponseSensorType>(r);
                                            if (response != null)
                                            {
                                                LogInfo(string.Format("Read response: {0} {1}", response.Manufacturer, response.SensorType));
                                                _state.ManufactureInfo = string.Format("{0}", response.SensorType);
                                                //_state.ManufactureInfo = string.Format("Hiya!");
                                                LogInfo(string.Format(_state.ManufactureInfo));
                                                get.ResponsePort.Post(_state);
                                            }
                                        },
                                    f =>
                                        {
                                            LogInfo(f.Detail.ToString());
                                            get.ResponsePort.Post(f);
                                        }));
            //get.ResponsePort.Post(_state);
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
            //////Debugger.Break();
            _legoBrickPort.Detach(ServiceInfo.Service);

            // drop the service
            base.DefaultDropHandler(drop);
            yield break;
        }

        /// <summary>
        ///     ConnectToBrick Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ConnectToBrickHandler(ConnectToBrick update)
        {
            if ((update.Body.SensorPort & NxtSensorPort.AnySensorPort) != update.Body.SensorPort)
            {
                update.ResponsePort.Post(Fault.FromException(new ArgumentException(string.Format("Invalid Sensor Port: {0}", ((LegoNxtPort) update.Body.SensorPort)))));
                yield break;
            }

            _state.SensorPort = update.Body.SensorPort;
            _state.Connected = false;

            if (!string.IsNullOrEmpty(update.Body.Name))
                _state.Name = update.Body.Name;
            _state.PollingFrequencyMs = update.Body.PollingFrequencyMs;

            Fault fault = null;

            var registration = new pxbrick.Registration(
                new LegoNxtConnection((LegoNxtPort) _state.SensorPort),
                LegoDeviceType.DigitalSensor,
                Contract.DeviceModel,
                Contract.Identifier,
                ServiceInfo.Service,
                _state.Name) {I2CBusAddress = 0x10};

            yield return Arbiter.Choice(_legoBrickPort.ReserveDevicePort(registration),
                                        reserveResponse =>
                                            {
                                                if (reserveResponse.DeviceModel == registration.DeviceModel)
                                                    registration.Connection = reserveResponse.Connection;
                                            },
                                        f =>
                                            {
                                                fault = f;
                                                LogError(fault);
                                                registration.Connection.Port = LegoNxtPort.NotConnected;
                                            });

            if (registration.Connection.Port == LegoNxtPort.NotConnected)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring HiTechnic Accelerometer on Port " + update.Body));
                update.ResponsePort.Post(fault);
                yield break;
            }

            var attachRequest = new pxbrick.AttachRequest(registration)
                {
                    InitializationCommands = new NxtCommandSequence(
                        (LegoCommand) new LegoSetInputMode((NxtSensorPort) registration.Connection.Port, LegoSensorType.I2C_9V, LegoSensorMode.RawMode))
                };

            //attachRequest.PollingCommands = new NxtCommandSequence(500, new I2CReadHiTechnicPrototypeBoard(_state.SensorPort));

            pxbrick.AttachResponse response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                                        rsp => response = rsp,
                                        f => fault = f);

            if (response == null)
            {
                LogError("* Failed to attach prototype board.");
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring Prototype Board"));
                update.ResponsePort.Post(fault);
                yield break;
            }
            LogInfo(string.Format("LOGINFO: Attach respoonse: {0} on {1}", response.DeviceModel, response.Connection.Port.ToString()));

            _state.Connected = (response.Connection.Port != LegoNxtPort.NotConnected);
            if (_state.Connected)
                _state.SensorPort = (NxtSensorPort) response.Connection.Port;
            else if (update.Body.SensorPort != NxtSensorPort.NotConnected)
            {
                update.ResponsePort.Post(Fault.FromException(new Exception(string.Format("Failure Configuring HiTechnic Prototype Board on port: {0}", update.Body.SensorPort))));
                yield break;
            }

            _state.Name = "Prototype Board on " + response.Connection.Port.ToString();

            // Send a notification of the connected port
            update.Body.Name = _state.Name;
            update.Body.PollingFrequencyMs = _state.PollingFrequencyMs;
            update.Body.SensorPort = _state.SensorPort;
            SendNotification(_subMgrPort, update);

            // Send the message response
            update.ResponsePort.Post(DefaultUpdateResponseType.Instance);
        }

       
    }
}
