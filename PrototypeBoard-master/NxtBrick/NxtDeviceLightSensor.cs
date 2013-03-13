//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceLightSensor.cs $ $Revision: 22 $
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
using pxanalogsensor = Microsoft.Robotics.Services.AnalogSensor.Proxy;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.LightSensor
{
    
    /// <summary>
    /// Lego NXT Light Sensor Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(pxanalogsensor.Contract.Identifier)]
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Light Sensor (v2).")]
    [DisplayName("(User) Lego NXT Light Sensor \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870553.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    public class NxtLightSensor : DsspServiceBase
    {
        /// <summary>
        /// _state
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.LightSensor.config.xml")]
        private LightSensorState _state = new LightSensorState();
        private pxanalogsensor.AnalogSensorState _genericState = new pxanalogsensor.AnalogSensorState();

        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/lightsensor", AllowMultipleInstances=true)]
        private LightSensorOperations _internalMainPort = new LightSensorOperations();
        private LightSensorOperations _reliableMainPort = null;

        [AlternateServicePort("/analogsensor", 
            AllowMultipleInstances = true, 
            AlternateContract = pxanalogsensor.Contract.Identifier)]
        private pxanalogsensor.AnalogSensorOperations _genericPort = new pxanalogsensor.AnalogSensorOperations();

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.LightSensor.user.xslt")]
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
        /// Subscription manager partner
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
        public NxtLightSensor(DsspServiceCreationPort creationPort) 
            : base(creationPort)
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
            _reliableMainPort = ServiceForwarder<LightSensorOperations>(this.ServiceInfo.Service);
            _reliableMainPort.ConnectToBrick(_state);

        }

        /// <summary>
        /// Initialize the Light Sensor state
        /// </summary>
        private void InitializeState()
        {
            if (_state == null)
                _state = new LightSensorState();

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            _state.TimeStamp = DateTime.MinValue;
            _state.Intensity = 0;
            _state.Connected = false;


            if (_genericState == null)
                _genericState = new pxanalogsensor.AnalogSensorState();

            _genericState.RawMeasurementRange = 255.0;
            _genericState.RawMeasurement = 0.0;
            _genericState.NormalizedMeasurement = 0.0;
            _genericState.TimeStamp = DateTime.MinValue;

            // Set the hardware identifier from the connected motor port.
            _genericState.HardwareIdentifier = NxtCommon.HardwareIdentifier(_state.SensorPort);
        }

        /// <summary>
        /// Sync the generic analog sensor state with our touch sensor.
        /// </summary>
        /// <returns></returns>
        private pxanalogsensor.AnalogSensorState SyncGenericState()
        {
            _genericState.RawMeasurement = (double)_state.Intensity;
            _genericState.NormalizedMeasurement = _genericState.RawMeasurement / _genericState.RawMeasurementRange;
            _genericState.TimeStamp = _state.TimeStamp;

            return _genericState;
        }

        /// <summary>
        /// Handle sensor notifications from the brick
        /// </summary>
        /// <param name="update"></param>
        private void NotificationHandler(brick.LegoSensorUpdate update)
        {
            LegoResponseGetInputValues inputValues = new LegoResponseGetInputValues(update.Body.CommandData);
            if (inputValues != null && inputValues.Success)
            {
                bool firstTime = (_state.TimeStamp == DateTime.MinValue);
                _state.TimeStamp = inputValues.TimeStamp;
                if (_state.Intensity != inputValues.ScaledValue || firstTime)
                {
                    _state.Intensity = inputValues.ScaledValue;
                    // Send notifications to both the generic and native subscribers
                    SendNotification<pxanalogsensor.Replace>(_genericSubMgrPort, SyncGenericState());
                    SendNotification<Replace>(_subMgrPort, _state);
                }
            }
        }

        #region Handlers

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
        /// Generic Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> AnalogGetHandler(pxanalogsensor.Get get)
        {
            get.ResponsePort.Post(SyncGenericState());
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
            get.ResponsePort.Post(new dssphttp.HttpResponseType(HttpStatusCode.OK, _state, _transform));
            yield break;
        }


        /// <summary>
        /// HttpGet AnalogSensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> AnalogHttpGetHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(SyncGenericState()));
            yield break;
        }


        /// <summary>
        /// Replace Handler for Analog Sensor
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> AnalogReplaceHandler(pxanalogsensor.Replace replace)
        {
            _state.SensorPort = NxtCommon.GetNxtSensorPortFromHardwareIdentifier(replace.Body.HardwareIdentifier);
            SendNotification<pxanalogsensor.Replace>(_genericSubMgrPort, replace);
            replace.ResponsePort.Post(DefaultReplaceResponseType.Instance);
            yield break;
        }


        /// <summary>
        /// Replace Handler
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ReplaceHandler(Replace replace)
        {
            throw (new Exception("Replace is used for notifications only"));
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
            yield break;
        }

        /// <summary>
        /// Generic ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> AnalogReliableSubscribeHandler(pxanalogsensor.ReliableSubscribe subscribe)
        {
            base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// Generic Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> AnalogSubscribeHandler(pxanalogsensor.Subscribe subscribe)
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
        /// Spotlight Handler
        /// </summary>
        /// <param name="spotlight"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> SpotlightHandler(Spotlight spotlight)
        {
            _state.SpotlightOn = spotlight.Body.SpotlightOn;
            LegoSetInputMode cmd = new LegoSetInputMode(_state.SensorPort, _state.SpotlightOn ? LegoSensorType.LightActive : LegoSensorType.LightInactive, LegoSensorMode.PercentFullScaleMode);
            _legoBrickPort.SendNxtCommand(cmd);

            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                delegate(LegoResponse response)
                {
                    if (response.Success)
                    {
                        spotlight.ResponsePort.Post(DefaultUpdateResponseType.Instance);
                        // Spotlight notifications are only sent to subscribers to the native service
                        SendNotification<Spotlight>(_subMgrPort, spotlight);
                    }
                    else
                    {
                        spotlight.ResponsePort.Post(
                            Fault.FromException(
                                new InvalidOperationException(response.ErrorCode.ToString())));
                    }
                },
                delegate(Fault fault)
                {
                    spotlight.ResponsePort.Post(fault);
                });

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
            _state.SpotlightOn = update.Body.SpotlightOn;

            // Set the hardware identifier from the connected motor port.
            _genericState.HardwareIdentifier = NxtCommon.HardwareIdentifier(_state.SensorPort);

            Fault fault = null;

            brick.AttachRequest attachRequest = new brick.AttachRequest(
                new brick.Registration( 
                    new LegoNxtConnection((LegoNxtPort)_state.SensorPort), 
                    LegoDeviceType.AnalogSensor,
                    Contract.DeviceModel, 
                    Contract.Identifier, 
                    ServiceInfo.Service,
                    _state.Name));

            attachRequest.InitializationCommands = new NxtCommandSequence(
                new LegoSetInputMode(_state.SensorPort, _state.SpotlightOn ? LegoSensorType.LightActive : LegoSensorType.LightInactive, LegoSensorMode.PercentFullScaleMode));

            attachRequest.PollingCommands = new NxtCommandSequence(_state.PollingFrequencyMs,
                new LegoGetInputValues(_state.SensorPort));

            brick.AttachResponse response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            if (response == null)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring NXT Motor"));
                update.ResponsePort.Post(fault);
                yield break;
            }

            _state.Connected = (response.Connection.Port != LegoNxtPort.NotConnected);
            if (_state.Connected)
                _state.SensorPort = (NxtSensorPort)response.Connection.Port;
            else if (update.Body.SensorPort != NxtSensorPort.NotConnected)
            {
                fault = Fault.FromException(new Exception("Failure Configuring NXT Light Sensor on Port " + update.Body.ToString()));
                update.ResponsePort.Post(fault);
                yield break;
            }

            // Set the motor name
            if (string.IsNullOrEmpty(_state.Name)
                || _state.Name.StartsWith("Light Sensor on "))
                _state.Name = "Light Sensor on " + response.Connection.ToString();

            // Send a notification of the connected port
            update.Body.Name = _state.Name;
            update.Body.PollingFrequencyMs = _state.PollingFrequencyMs;
            update.Body.SensorPort = _state.SensorPort;
            update.Body.SpotlightOn = _state.SpotlightOn;
            // Only send connection notifications to native subscribers
            SendNotification<ConnectToBrick>(_subMgrPort, update);

            update.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }
        #endregion
    }
}
