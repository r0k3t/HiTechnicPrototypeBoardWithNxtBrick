//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceSonarSensor.cs $ $Revision: 20 $
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
using W3C.Soap;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;

using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using pxanalogsensor = Microsoft.Robotics.Services.AnalogSensor.Proxy;
using submgr = Microsoft.Dss.Services.SubscriptionManager;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.SonarSensor
{
    
    /// <summary>
    /// Lego NXT Ultrasonic Sensor Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(pxanalogsensor.Contract.Identifier)]
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Ultrasonic Sensor (v2).")]
    [DisplayName("(User) Lego NXT Ultrasonic Sensor \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870566.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    public class NxtUltrasonicSensor : DsspServiceBase
    {
        /// <summary>
        /// _state
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.UltrasonicSensor.config.xml")]
        private SonarSensorState _state = new SonarSensorState();
        private pxanalogsensor.AnalogSensorState _genericState = new pxanalogsensor.AnalogSensorState();


        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/ultrasonicsensor", AllowMultipleInstances=true)]
        private UltrasonicSensorOperations _internalMainPort = new UltrasonicSensorOperations();
        private UltrasonicSensorOperations _reliableMainPort = null;

        [AlternateServicePort("/analogsensor", 
            AllowMultipleInstances = true,
            AlternateContract = pxanalogsensor.Contract.Identifier)]
        private pxanalogsensor.AnalogSensorOperations _genericPort = new pxanalogsensor.AnalogSensorOperations();

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.SonarSensor.user.xslt")]
        string _transform = string.Empty;

        /// <summary>
        /// Partner with the LEGO NXT Brick
        /// </summary>
        [Partner("brick",
            Contract = brick.Contract.Identifier,
            CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate,
            Optional = false)]
        private brick.NxtBrickOperations _legoBrickPort = new brick.NxtBrickOperations();
        private brick.NxtBrickOperations _legoBrickNotificationPort = new brick.NxtBrickOperations();

        /// <summary>
        /// Subscription manager partner for Ultrasonic Sensor
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
        public NxtUltrasonicSensor(DsspServiceCreationPort creationPort) : 
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
                    Arbiter.Receive<brick.LegoSensorUpdate>(true, _legoBrickNotificationPort, NotificationHandler)
                ),
                new ConcurrentReceiverGroup()));

            // Set up the reliable port using DSS forwarder to ensure exception and timeout conversion to fault.
            _reliableMainPort = ServiceForwarder<UltrasonicSensorOperations>(this.ServiceInfo.Service);
            _reliableMainPort.ConnectToBrick(_state);

        }

        /// <summary>
        /// Initialize State
        /// </summary>
        private void InitializeState()
        {

            if (_state == null)
            {
                _state = new SonarSensorState();

                // I2C devices can autodetect which port they are on.
                _state.SensorPort = NxtSensorPort.AnySensorPort;
            }

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            _state.TimeStamp = DateTime.MinValue;
            _state.Distance = 0;
            _state.Connected = false;

        }


        /// <summary>
        /// Sync the generic analog sensor state with our sonar sensor.
        /// </summary>
        /// <returns></returns>
        private pxanalogsensor.AnalogSensorState SyncGenericState()
        {
            _genericState.RawMeasurement = (double)(_state.Distance);
            // cm to meters
            _genericState.RawMeasurementRange = 10.0;
            _genericState.NormalizedMeasurement = _genericState.RawMeasurement / _genericState.RawMeasurementRange;
            _genericState.TimeStamp = _state.TimeStamp;

            // Set the hardware identifier from the connected motor port.
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
        /// Handle periodic sensor readings from the brick
        /// </summary>
        /// <param name="update"></param>
        private void NotificationHandler(brick.LegoSensorUpdate update)
        {
            nxtcmd.I2CResponseSonarSensor inputValues = new nxtcmd.I2CResponseSonarSensor(update.Body.CommandData);
            if (inputValues.Success)
            {
                bool firstTime = (_state.TimeStamp == DateTime.MinValue);
                _state.TimeStamp = inputValues.TimeStamp;
                if (_state.Distance != inputValues.UltraSonicVariable || firstTime)
                {
                    _state.Distance = inputValues.UltraSonicVariable;
                    SendNotification<SonarSensorUpdate>(_subMgrPort, _state);
                    SendNotification<pxanalogsensor.Replace>(_genericSubMgrPort, SyncGenericState());
                }
            }
        }


        #region Operation Handlers

        /// <summary>
        /// Get Ultrasonic Sensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetSonarSensorHandler(Get get)
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
        /// HttpGet SonarSensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> HttpGetSonarSensorHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(HttpStatusCode.OK,_state,_transform));
            yield break;
        }


        /// <summary>
        /// HttpGet AnalogSensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
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
            replace.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The LEGO NXT Ultrasonic sensor is updated from hardware.")));
            yield break;
        }

        /// <summary>
        /// SonarSensorUpdate Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SonarSensorUpdateHandler(SonarSensorUpdate update)
        {
            update.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The LEGO NXT Ultrasonic sensor is updated from hardware.")));
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
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> SubscribeHandler(pxanalogsensor.Subscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericReliableSubscribeHandler(pxanalogsensor.ReliableSubscribe subscribe)
        {
            base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> GenericSubscribeHandler(pxanalogsensor.Subscribe subscribe)
        {
            base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
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
            // detach from the brick
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

            brick.Registration registration = new brick.Registration(
                   new LegoNxtConnection((LegoNxtPort)_state.SensorPort),
                   LegoDeviceType.DigitalSensor,
                   Contract.DeviceModel,
                   Contract.Identifier,
                   ServiceInfo.Service,
                   _state.Name);

            // Reserve the port
            yield return Arbiter.Choice(_legoBrickPort.ReserveDevicePort(registration),
                delegate(brick.AttachResponse reserveResponse)
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


            if (registration.Connection.Port == LegoNxtPort.NotConnected && update.Body.SensorPort != NxtSensorPort.NotConnected)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring NXT Ultrasonic Sensor on Port " + update.Body.ToString()));
                update.ResponsePort.Post(fault);
                yield break;
            }

            _state.Connected = true;
            brick.AttachRequest attachRequest = new brick.AttachRequest(registration);

            brick.AttachResponse response = null;
            byte[] requestSingleShotReading = { 0x02, 0x41, 0x01 };
            byte[] startContinuousReadings = { 0x02, 0x41, 0x02 };
            byte[] setContinuousReadingInterval = { 0x02, 0x40, 0x010 };

            attachRequest.InitializationCommands = new nxtcmd.NxtCommandSequence( 
                new nxtcmd.LegoLSGetStatus((NxtSensorPort)registration.Connection.Port),
                new nxtcmd.LegoLSRead((NxtSensorPort)registration.Connection.Port),
                new nxtcmd.LegoSetInputMode((NxtSensorPort)registration.Connection.Port, LegoSensorType.I2C_9V, LegoSensorMode.RawMode),
                new nxtcmd.LegoLSWrite((NxtSensorPort)registration.Connection.Port, startContinuousReadings, 0));

            attachRequest.PollingCommands = new nxtcmd.NxtCommandSequence(_state.PollingFrequencyMs,
                new nxtcmd.I2CReadSonarSensor(_state.SensorPort, nxtcmd.UltraSonicPacket.ReadMeasurement1));

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            if (response == null)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring NXT Ultrasonic Sensor"));
                update.ResponsePort.Post(fault);
                yield break;
            }

            if ((LegoNxtPort)_state.SensorPort != response.Connection.Port)
                _state.SensorPort = (NxtSensorPort)response.Connection.Port;


            // Set the motor name
            if (string.IsNullOrEmpty(_state.Name)
                || _state.Name.StartsWith("Ultrasonic Sensor on "))
                _state.Name = "Ultrasonic Sensor on " + response.Connection.ToString();

            // Send a notification of the connected port
            update.Body.Name = _state.Name;
            update.Body.PollingFrequencyMs = _state.PollingFrequencyMs;
            update.Body.SensorPort = _state.SensorPort;
            SendNotification<ConnectToBrick>(_subMgrPort, update);

            // Send the message response
            update.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

        #endregion

    }
}
