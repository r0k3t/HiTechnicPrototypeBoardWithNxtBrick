//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: MindSensorsCompassSensor.cs $ $Revision: 20 $
//-----------------------------------------------------------------------

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

using pxbrick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick.Proxy;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using pxanalogsensor = Microsoft.Robotics.Services.AnalogSensor.Proxy;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using W3C.Soap;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;

namespace Microsoft.Robotics.Services.Sample.MindSensors.Compass
{
    
    /// <summary>
    /// MindSensors Compass sensor Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(pxanalogsensor.Contract.Identifier)]
    [Description("Provides access to the MindSensors Compass Sensor.\n(for use with 'Lego NXT Brick (v2)' service)")]
    [DisplayName("(User) MindSensors Compass Sensor")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb663557.aspx")]
    [DssCategory(pxbrick.LegoCategories.NXT)]
    public class MindSensorsCompassSensor : DsspServiceBase
    {
        /// <summary>
        /// _state
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/MindSensors.Nxt.Compass.config.xml")]
        private CompassSensorState _state = new CompassSensorState();
        private pxanalogsensor.AnalogSensorState _genericState = new pxanalogsensor.AnalogSensorState();


        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/mindsensors/nxt/compass", AllowMultipleInstances = true)]
        private CompassSensorOperations _internalMainPort = new CompassSensorOperations();
        private CompassSensorOperations _reliableMainPort = null;

        [AlternateServicePort("/analogsensor", 
            AllowMultipleInstances = true,
            AlternateContract = pxanalogsensor.Contract.Identifier)]
        private pxanalogsensor.AnalogSensorOperations _genericPort = new pxanalogsensor.AnalogSensorOperations();

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.MindSensors.Transforms.Compass.user.xslt")]
        string _transforms = string.Empty;
        
        /// <summary>
        /// Partner with the LEGO NXT Brick
        /// </summary>
        [Partner("brick",
            Contract = pxbrick.Contract.Identifier,
            CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate,
            Optional = false)]
        private pxbrick.NxtBrickOperations _legoBrickPort = new pxbrick.NxtBrickOperations();
        private pxbrick.NxtBrickOperations _legoBrickNotificationPort = new pxbrick.NxtBrickOperations();

        /// <summary>
        /// Subscription manager partner for Compass Sensor
        /// </summary>
        [Partner(Partners.SubscriptionManagerString, Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _subMgrPort = new submgr.SubscriptionManagerPort();

        /// <summary>
        /// Subscription manager partner for generic analog sensor
        /// </summary>
        [Partner(Partners.SubscriptionManagerString + "/analogsensor", Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _genericSubMgrPort = new submgr.SubscriptionManagerPort();


        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public MindSensorsCompassSensor(DsspServiceCreationPort creationPort) : 
                base(creationPort)
        {
        }

        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            InitializeState();

			base.Start();
            base.MainPortInterleave.CombineWith(new Interleave(
                new ExclusiveReceiverGroup(
                    Arbiter.Receive<pxbrick.LegoSensorUpdate>(true, _legoBrickNotificationPort, NotificationHandler)
                ),
                new ConcurrentReceiverGroup()));

            // Set up the reliable port using DSS forwarder to ensure exception and timeout conversion to fault.
            _reliableMainPort = ServiceForwarder<CompassSensorOperations>(this.ServiceInfo.Service);
            _reliableMainPort.ConnectToBrick(_state);
        }

        private void InitializeState()
        {
            if (_state == null)
            {
                _state = new CompassSensorState();
                _state.SensorPort = (NxtSensorPort)LegoNxtPort.AnySensorPort;
            }

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;


            if (_state.Heading == null)
                _state.Heading = new CompassReading();
            else
            {
                _state.Heading.Degrees = 0.0;
                _state.Heading.TimeStamp = DateTime.MinValue;
            }
            _state.Connected = false;

        }


        /// <summary>
        /// Sync the generic analog sensor state with our compass sensor.
        /// </summary>
        /// <returns></returns>
        private pxanalogsensor.AnalogSensorState SyncGenericState()
        {
            _genericState.RawMeasurement = _state.Heading.Degrees;
            _genericState.RawMeasurementRange = 360.0;
            _genericState.NormalizedMeasurement = _genericState.RawMeasurement;
            _genericState.TimeStamp = _state.Heading.TimeStamp;

            // Set the hardware identifier from the connected sensor port.
            switch (_state.SensorPort)
            {
                case NxtSensorPort.Sensor1:
                    _genericState.HardwareIdentifier = 1;
                    break;
                case NxtSensorPort.Sensor2:
                    _genericState.HardwareIdentifier = 2;
                    break;
                case NxtSensorPort.Sensor3:
                    _genericState.HardwareIdentifier = 3;
                    break;
                case NxtSensorPort.Sensor4:
                    _genericState.HardwareIdentifier = 4;
                    break;
                default:
                    _genericState.HardwareIdentifier = 0;
                    break;
            }
            return _genericState;
        }

        /// <summary>
        /// Handle periodic sensor readings from the pxbrick
        /// </summary>
        /// <param name="update"></param>
        private void NotificationHandler(pxbrick.LegoSensorUpdate update)
        {
            I2CResponseMindSensorsCompassSensor inputValues = new I2CResponseMindSensorsCompassSensor(update.Body.CommandData);
            if (inputValues.Success && inputValues.Heading < 360)
            {
                if (_state.Heading.Degrees != inputValues.Heading || _state.Heading.TimeStamp == DateTime.MinValue)
                {
                    _state.Heading.TimeStamp = inputValues.TimeStamp;
                    _state.Heading.Degrees = inputValues.Heading;
                    SendNotification<CompassSensorUpdate>(_subMgrPort, _state.Heading);
                    SendNotification<pxanalogsensor.Replace>(_genericSubMgrPort, SyncGenericState());
                }
            }
        }

        /// <summary>
        /// Get Compass Sensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetCompassSensorHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }

        /// <summary>
        /// Get Analog Sensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GetAnalogSensorHandler(pxanalogsensor.Get get)
        {
            get.ResponsePort.Post(SyncGenericState());
            yield break;
        }
        

        /// <summary>
        /// HttpGet CompassSensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> HttpGetCompassSensorHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(HttpStatusCode.OK, _state, _transforms));
            yield break;
        }


        /// <summary>
        /// HttpGet AnalogSensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        /// [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> HttpGetAnalogSensorHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(SyncGenericState()));
            yield break;
        }

        /// <summary>
        /// Replace Handler
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> ReplaceHandler(pxanalogsensor.Replace replace)
        {
            replace.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The MindSensors Compass sensor is updated from hardware.")));
            yield break;
        }

        /// <summary>
        /// CompassSensorUpdate Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> CompassSensorUpdateHandler(CompassSensorUpdate update)
        {
            update.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The MindSensors Compass sensor is updated from hardware.")));
            yield break;
        }

        /// <summary>
        /// ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ReliableSubscribeHandler(pxanalogsensor.ReliableSubscribe subscribe)
        {
            yield return (Choice)base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);

            if (_state.Heading.TimeStamp != DateTime.MinValue)
            {
                SendNotificationToTarget<CompassSensorUpdate>(subscribe.Body.Subscriber, _subMgrPort, _state.Heading);
            }
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> SubscribeHandler(pxanalogsensor.Subscribe subscribe)
        {
            yield return (Choice)base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);

            if (_state.Heading.TimeStamp != DateTime.MinValue)
            {
                SendNotificationToTarget<CompassSensorUpdate>(subscribe.Body.Subscriber, _subMgrPort, _state.Heading);
            }
        }

        /// <summary>
        /// ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericReliableSubscribeHandler(pxanalogsensor.ReliableSubscribe subscribe)
        {
            yield return (Choice)base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);

            if (_state.Heading.TimeStamp != DateTime.MinValue)
            {
                SendNotificationToTarget<pxanalogsensor.Replace>(subscribe.Body.Subscriber, _genericSubMgrPort, SyncGenericState());
            }
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericSubscribeHandler(pxanalogsensor.Subscribe subscribe)
        {
            yield return (Choice)base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);

            if (_state.Heading.TimeStamp != DateTime.MinValue)
            {
                SendNotificationToTarget<pxanalogsensor.Replace>(subscribe.Body.Subscriber, _genericSubMgrPort, SyncGenericState());
            }
        }

        /// <summary>
        /// Drop Handler
        /// </summary>
        /// <param name="drop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        [ServiceHandler(ServiceHandlerBehavior.Teardown, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> DropHandler(DsspDefaultDrop drop)
        {
            _legoBrickPort.Detach(ServiceInfo.Service);

            // drop the service
            base.DefaultDropHandler(drop);
            yield break;
        }


        /// <summary>
        /// ConnectToBrick Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ConnectToBrickHandler(ConnectToBrick update)
        {

            // Validate the sensor port.
            if ((update.Body.SensorPort & NxtSensorPort.AnySensorPort)
                != update.Body.SensorPort)
            {
                update.ResponsePort.Post(
                    Fault.FromException(
                        new ArgumentException(
                            string.Format("Invalid Sensor Port: {0}",
                                ((LegoNxtPort)update.Body.SensorPort)))));
                yield break;
            }

            _state.SensorPort = update.Body.SensorPort;
            _state.Connected = false;

            if (!string.IsNullOrEmpty(update.Body.Name))
                _state.Name = update.Body.Name;
            _state.PollingFrequencyMs = update.Body.PollingFrequencyMs;

            Fault fault = null;

            pxbrick.Registration registration = new pxbrick.Registration(
                    new LegoNxtConnection((LegoNxtPort)_state.SensorPort), 
                    LegoDeviceType.DigitalSensor,
                    Contract.DeviceModel, 
                    Contract.Identifier, 
                    ServiceInfo.Service,
                    _state.Name);

            // Reserve the port
            yield return Arbiter.Choice(_legoBrickPort.ReserveDevicePort(registration),
                delegate(pxbrick.AttachResponse reserveResponse) 
                {
                    if (reserveResponse.DeviceModel == registration.DeviceModel)
                    {
                        registration.Connection = reserveResponse.Connection;
                    }
                },
                delegate(Fault f) 
                {
                    fault = f;
                    LogError(fault);
                    registration.Connection.Port = LegoNxtPort.NotConnected;
                });


            if (registration.Connection.Port == LegoNxtPort.NotConnected)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring MindSensors Compass on Port " + update.Body.ToString()));
                update.ResponsePort.Post(fault);
                yield break;
            }

            pxbrick.AttachRequest attachRequest = new pxbrick.AttachRequest(registration);

            attachRequest.InitializationCommands = new nxtcmd.NxtCommandSequence(
                new nxtcmd.LegoSetInputMode((NxtSensorPort)registration.Connection.Port, LegoSensorType.I2C_9V, LegoSensorMode.RawMode),
                new I2CInitializeMindSensorsCompass((NxtSensorPort)registration.Connection.Port));

            attachRequest.PollingCommands = new nxtcmd.NxtCommandSequence(_state.PollingFrequencyMs,
                new I2CReadMindSensorsCompassSensor(_state.SensorPort));

            pxbrick.AttachResponse response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(pxbrick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            if (response == null)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring MindSensors Compass"));
                update.ResponsePort.Post(fault);
                yield break;
            }

            _state.Connected = (response.Connection.Port != LegoNxtPort.NotConnected);
            if (_state.Connected)
            {
                _state.SensorPort = (NxtSensorPort)response.Connection.Port;
            }
            else if (update.Body.SensorPort != NxtSensorPort.NotConnected)
            {
                update.ResponsePort.Post(Fault.FromException(new Exception(string.Format("Failure Configuring MindSensors Compass on port: {0}", update.Body.SensorPort))));
                yield break;
            }


            // Set the compass name
            if (string.IsNullOrEmpty(_state.Name)
                || _state.Name.StartsWith("Compass Sensor on "))
                _state.Name = "Compass Sensor on " + response.Connection.Port.ToString();

            // Send a notification of the connected port
            update.Body.Name = _state.Name;
            update.Body.PollingFrequencyMs = _state.PollingFrequencyMs;
            update.Body.SensorPort = _state.SensorPort;
            SendNotification<ConnectToBrick>(_subMgrPort, update);

            // Send the message response
            update.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

    }
}
