//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtContactSensorArray.cs $ $Revision: 10 $
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using W3C.Soap;

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;

using analog = Microsoft.Robotics.Services.AnalogSensor.Proxy;
using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using bumper = Microsoft.Robotics.Services.ContactSensor.Proxy;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using sonar = Microsoft.Robotics.Services.Sample.Lego.Nxt.SonarSensor;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using touch = Microsoft.Robotics.Services.Sample.Lego.Nxt.TouchSensor;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.ContactSensorArray
{
    
    /// <summary>
    /// Contact Sensor Array Service
    /// Aggregates up to four LEGO NXT sensors to produce a Contact Sensor Array.
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(bumper.Contract.Identifier)]
    [Description("Aggregates up to four LEGO NXT sensors to produce a Contact Sensor Array.")]
    [DisplayName("(User) Lego NXT Contact Sensor Array \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870566.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    public class NxtContactSensorArray : DsspServiceBase
    {
        /// <summary>
        /// Configures the minimum and maximum Contact Sensor range for any LEGO NXT device 
        /// which implements the Generice Analog Sensor contract.
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.ContactSensorArray.config.xml")]
        private NxtContactSensorArrayState _state = new NxtContactSensorArrayState();

        /// <summary>
        /// ContactSensorArray State
        /// </summary>
        private bumper.ContactSensorArrayState _contactSensorArrayState = new bumper.ContactSensorArrayState();



        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.ContactSensorArray.user.xslt")]
        private string _transform = string.Empty;

        /// <summary>
        /// Partner with the LEGO NXT Brick
        /// </summary>
        [Partner("brick",
            Contract = brick.Contract.Identifier,
            CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate,
            Optional = false)]
        private brick.NxtBrickOperations _legoBrickPort = new brick.NxtBrickOperations();
        private brick.NxtBrickOperations _legoBrickNotificationPort = new brick.NxtBrickOperations();

        private analog.AnalogSensorOperations _analogSensorNotificationsPort = new analog.AnalogSensorOperations();

        /// <summary>
        /// Subscription manager partner for Touch Sensor
        /// </summary>
        [Partner(Partners.SubscriptionManagerString, Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _subMgrPort = new submgr.SubscriptionManagerPort();


        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/contactsensorarray", AllowMultipleInstances = true)]
        private NxtContactSensorArrayOperations _mainPort = new NxtContactSensorArrayOperations();

        /// <summary>
        /// Generic Port
        /// </summary>
        [AlternateServicePort("/generic",
            AllowMultipleInstances = true,
            AlternateContract = bumper.Contract.Identifier)]
        private bumper.ContactSensorArrayOperations _genericPort = new bumper.ContactSensorArrayOperations();

        private Dictionary<string, SubscribeResponseType> _sensorList = new Dictionary<string, SubscribeResponseType>();

        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public NxtContactSensorArray(DsspServiceCreationPort creationPort) : 
                base(creationPort)
        {
        }

        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            ValidateState();

            base.Start();

            base.MainPortInterleave.CombineWith(new Interleave(
                new ExclusiveReceiverGroup(
                    Arbiter.Receive<brick.ConnectToHardware>(true, _legoBrickNotificationPort, InitializationHandler)
                ),
                new ConcurrentReceiverGroup()));

            // Create a persistent handler for all Analaog sensor notifications.
            Activate(Arbiter.Receive<analog.Replace>(true, _analogSensorNotificationsPort, AnalogSensorNotificationHandler));

            _legoBrickPort.Subscribe(_legoBrickNotificationPort);

        }

        /// <summary>
        /// Validate the State.
        /// </summary>
        private void ValidateState()
        {
            bool saveState = false;

            if (_state == null)
            {
                saveState = true;
                _state = new NxtContactSensorArrayState();
            }
            if (_state.SensorConfiguration == null)
            {
                saveState = true;
                _state.SensorConfiguration = new List<SensorConfiguration>();
            }
            if (_state.SensorConfiguration.Count == 0)
            {
                saveState = true;
                _state.SensorConfiguration.Add(new SensorConfiguration("Pressed", touch.Contract.DeviceModel, string.Empty, 1.0, 1.0));
                _state.SensorConfiguration.Add(new SensorConfiguration("Near", sonar.Contract.DeviceModel, string.Empty, 0.0, 20.0));
                _state.SensorConfiguration.Add(new SensorConfiguration("Far", sonar.Contract.DeviceModel, string.Empty, 220.0, 255.0));
            }

            // Clear Runtime Configuration
            _state.RuntimeConfiguration = null;
            UnsubscribeAllSensors();

            // Clear generic state
            if (_contactSensorArrayState == null)
                _contactSensorArrayState = new bumper.ContactSensorArrayState();
            if (_contactSensorArrayState.Sensors == null)
                _contactSensorArrayState.Sensors = new List<bumper.ContactSensor>();

            if (saveState)
                SaveState(_state);

            // Initialize Runtime Configuration
            _state.RuntimeConfiguration = new Dictionary<SensorRange, PortConfiguration>();
        }

        /// <summary>
        /// Find sensors on the brick which can be added to 
        /// our contact sensor array.
        /// </summary>
        /// <returns></returns>
        private IEnumerator<ITask> FindSensors()
        {
            #region Attach to the NXT Brick

            LegoNxtConnection aggregate = new LegoNxtConnection(LegoNxtPort.Aggregation);
            aggregate.PortOverride = "Contact Sensor Array";
            brick.AttachRequest attachRequest = new brick.AttachRequest(
                new brick.Registration(
                    aggregate,
                    LegoDeviceType.Aggregation,
                    "ContactSensorArray",
                    Contract.Identifier,
                    ServiceInfo.Service,
                    "ContactSensorArray"));

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp)
                {
                },
                delegate(Fault fault)
                {
                    LogError("Error in LEGO NXT Contact Sensor Array while attaching to brick", fault);
                });
            #endregion

            PortSet<brick.NxtBrickState, Fault> brickResponse = _legoBrickPort.Get();
            yield return Arbiter.Choice(
                Arbiter.ReceiveWithIterator<brick.NxtBrickState>(false, brickResponse, ProcessBrickState),
                Arbiter.Receive<Fault>(false, brickResponse, EmptyHandler<Fault>));

            yield break;
        }

        /// <summary>
        /// Process Brick State
        /// </summary>
        /// <param name="brickState"></param>
        private IEnumerator<ITask> ProcessBrickState(brick.NxtBrickState brickState)
        {
            foreach (string key in brickState.Runtime.Devices.Keys)
            {
                brick.AttachRequest device = brickState.Runtime.Devices[key];
                if (device.Registration.DeviceType != LegoDeviceType.AnalogSensor
                    && device.Registration.DeviceType != LegoDeviceType.DigitalSensor)
                {
                    continue;
                }

                PortSet<DsspDefaultLookup, DsspDefaultGet> lookupPort = ServiceForwarder<PortSet<DsspDefaultLookup, DsspDefaultGet>>(device.Registration.ServiceUri);
                DsspDefaultLookup lu = new DsspDefaultLookup();
                lookupPort.Post(lu);
                yield return Arbiter.Choice(lu.ResponsePort,
                    delegate(LookupResponse luResponse)
                    {
                        foreach(PartnerType pt in luResponse.PartnerList)
                        {
                            // See if this service supports the analog sensor contract
                            if (pt.Contract == analog.Contract.Identifier)
                            {
                                // Check if we have already processed this one.
                                if (_sensorList.ContainsKey(pt.Service))
                                    break;

                                string name = device.Registration.Name;
                                string model = device.Registration.DeviceModel;
                                int hardwareIdentifier = NxtCommon.HardwareIdentifier(device.Registration.Connection.Port);

                                LogVerbose(LogGroups.Console, string.Format("Configuring {0}:{1} on {2} with analog service at {3}", model, name, hardwareIdentifier, pt.Service));
                                analog.AnalogSensorOperations sensorPort = ServiceForwarder<analog.AnalogSensorOperations>(pt.Service);
                                Activate(Arbiter.Choice(sensorPort.Subscribe(_analogSensorNotificationsPort),
                                    delegate(SubscribeResponseType response) 
                                    {
                                        // Keep track of the subscription manager response 
                                        // so that we can unsubscribe later.
                                        _sensorList.Add(pt.Service, response);
                                    },
                                    delegate(Fault fault)
                                    {
                                        LogError(LogGroups.Console, string.Format("Failure subscribing to {0} on port {1}.", model, hardwareIdentifier));
                                    }));

                                foreach (SensorConfiguration cfg in _state.SensorConfiguration)
                                {
                                    if (cfg.DeviceModel != model)
                                        continue;

                                    SensorRange range = new SensorRange(hardwareIdentifier, model, name, cfg.RangeName);
                                    PortConfiguration portConfig = new PortConfiguration(hardwareIdentifier, range.ContactSensorName, cfg.SuccessRangeMin, cfg.SuccessRangeMax);
                                    portConfig.AnalogSensorServiceUri = pt.Service;

                                    if (portConfig != null)
                                        _state.RuntimeConfiguration.Add(range, portConfig);
                                }
                                break;
                            }
                        }
                    },
                    delegate(Fault f) { });

            }
        }

        /// <summary>
        /// Generate a Port Configuration for the specified device.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="name"></param>
        /// <param name="hardwareIdentifier"></param>
        /// <param name="serviceUri"></param>
        /// <returns></returns>
        private PortConfiguration GeneratePortConfiguration(string model, string name, int hardwareIdentifier, string serviceUri)
        {
            PortConfiguration sensorConfig = new PortConfiguration(hardwareIdentifier, name, 0.0, 0.0);
            bool found = false;
            foreach (SensorConfiguration config in _state.SensorConfiguration)
            {
                if (config.DeviceModel != model)
                    continue;

                found = true;
                sensorConfig.SuccessRangeMin = config.SuccessRangeMin;
                sensorConfig.SuccessRangeMax = config.SuccessRangeMax;
                sensorConfig.AnalogSensorServiceUri = serviceUri;

                // If we found a name override, we are done.
                if (!string.IsNullOrEmpty(config.DeviceName) && config.DeviceName == name)
                    break;

                // otherwise, keep looking for an override.
            }

            if (!found)
                return null;

            return sensorConfig;
        }

        /// <summary>
        /// Wait until the LEGO is connected to the hardware before initializing this service.
        /// </summary>
        /// <param name="ready"></param>
        private void InitializationHandler(brick.ConnectToHardware ready)
        {
            SpawnIterator(FindSensors);
        }

        /// <summary>
        /// Analog Sensor Notification Handler
        /// </summary>
        /// <param name="notification"></param>
        private void AnalogSensorNotificationHandler(analog.Replace notification)
        {
            LogVerbose(LogGroups.Console, string.Format("Sensor Notification: {0} {1}", notification.Body.HardwareIdentifier, notification.Body.RawMeasurement));

            foreach (SensorRange key in _state.RuntimeConfiguration.Keys)
            {
                if (key.HardwareIdentifier != notification.Body.HardwareIdentifier)
                    continue;

                PortConfiguration sensorConfig = _state.RuntimeConfiguration[key];
                string contactSensorName = key.ContactSensorName;

                int priorIx = _contactSensorArrayState.Sensors.FindIndex(
                    delegate(bumper.ContactSensor gencs) 
                    {
                        return gencs.HardwareIdentifier == notification.Body.HardwareIdentifier
                            && gencs.Name == contactSensorName; 
                    });
                bool priorPressed = (priorIx < 0) ? false : _contactSensorArrayState.Sensors[priorIx].Pressed;

                // Send a ContactSensor notification here.
                bumper.ContactSensor cs = new bumper.ContactSensor(sensorConfig.HardwareIdentifier, contactSensorName);
                cs.Pressed = sensorConfig.Pressed(notification.Body.RawMeasurement);
                sensorConfig.Contact = cs.Pressed;

                if (priorIx < 0)
                    _contactSensorArrayState.Sensors.Add(cs);

                if (priorIx < 0 || priorPressed != cs.Pressed)
                {
                    if (priorIx >= 0)
                        _contactSensorArrayState.Sensors[priorIx].Pressed = cs.Pressed;

                    SendNotification<bumper.Update>(_subMgrPort, new bumper.Update(cs));
                }
            }
        }

        /// <summary>
        /// Unsubscribe from all prior analog sensor requests
        /// </summary>
        private void UnsubscribeAllSensors()
        {
            if (_sensorList == null)
                return;

            foreach(SubscribeResponseType sub in _sensorList.Values)
            {
                submgr.SubscriptionManagerPort subMgr = ServiceForwarder<submgr.SubscriptionManagerPort>(sub.SubscriptionManager);
                subMgr.Post(new submgr.DeleteSubscription(new submgr.DeleteSubscriptionMessage(sub.Subscriber)));
            }
            _sensorList.Clear();
        }

        #region Main Port Handlers
        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }

        /// <summary>
        /// HttpGet Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> HttpGetHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(System.Net.HttpStatusCode.OK, _state, _transform));
            yield break;
        }

        /// <summary>
        /// Configure Device Handler
        /// </summary>
        /// <param name="configureDevice"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ConfigureDeviceHandler(ConfigureDevice configureDevice)
        {
            int ix = _state.SensorConfiguration.FindIndex(
                delegate(SensorConfiguration c)
                {
                    return c.DeviceModel == configureDevice.Body.DeviceModel
                        && (c.DeviceName ?? string.Empty) == (configureDevice.Body.DeviceName ?? string.Empty);
                });

            if (ix < 0)
                _state.SensorConfiguration.Add(configureDevice.Body);
            else
            {
                _state.SensorConfiguration[ix].SuccessRangeMin = configureDevice.Body.SuccessRangeMin;
                _state.SensorConfiguration[ix].SuccessRangeMax = configureDevice.Body.SuccessRangeMax;
            }

            ValidateState();
            
            yield return Arbiter.ExecuteToCompletion(Environment.TaskQueue, 
                new IterativeTask(FindSensors));

            configureDevice.ResponsePort.Post(DefaultUpsertResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// ResetConfiguration Handler
        /// </summary>
        /// <param name="resetConfiguration"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> RemoveDeviceConfigurationHandler(ResetConfiguration resetConfiguration)
        {
            // Clear Runtime Configuration
            UnsubscribeAllSensors();
            _state.SensorConfiguration = new List<SensorConfiguration>();
            _state.RuntimeConfiguration = null;
            _contactSensorArrayState = new bumper.ContactSensorArrayState();

            resetConfiguration.ResponsePort.Post(DefaultDeleteResponseType.Instance);
            yield break;
        }

        #endregion

        #region Contact Sensor Array Operation Handlers

        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericGetHandler(bumper.Get get)
        {
            get.ResponsePort.Post(_contactSensorArrayState);
            yield break;
        }

        /// <summary>
        /// HttpGet Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericHttpGetHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(_contactSensorArrayState));
            yield break;
        }

        /// <summary>
        /// Replace Handler
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericReplaceHandler(bumper.Replace replace)
        {
            throw new InvalidOperationException("The LEGO NXT Contact Sensor Array is configured by the LEGO NXT Brick service.");
        }

        /// <summary>
        /// Update Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericUpdateHandler(bumper.Update update)
        {
            throw new InvalidOperationException("Contact Sensor Update is a Notification and not valid in this context.");
        }

        /// <summary>
        /// ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericReliableSubscribeHandler(bumper.ReliableSubscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericSubscribeHandler(bumper.Subscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        #endregion

    }


}
