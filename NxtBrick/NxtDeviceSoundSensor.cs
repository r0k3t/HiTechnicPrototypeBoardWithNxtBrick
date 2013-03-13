//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceSoundSensor.cs $ $Revision: 21 $
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
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.SoundSensor
{
    
    /// <summary>
    /// Lego NXT Sound Sensor Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(pxanalogsensor.Contract.Identifier)]
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Sound Sensor (v2).")]
    [DisplayName("(User) Lego NXT Sound Sensor \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870566.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    [DssCategory(DssCategoryPrefixes.Root + "audio.html")]
    public class NxtSoundSensor : DsspServiceBase
    {
        /// <summary>
        /// _state
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.SoundSensor.config.xml")]
        private SoundSensorState _state = new SoundSensorState();
        private pxanalogsensor.AnalogSensorState _genericState = new pxanalogsensor.AnalogSensorState();


        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/soundsensor", AllowMultipleInstances=true)]
        private SoundSensorOperations _internalMainPort = new SoundSensorOperations();
        private SoundSensorOperations _reliableMainPort = null;

        [AlternateServicePort("/analogsensor", 
            AllowMultipleInstances = true,
            AlternateContract = pxanalogsensor.Contract.Identifier)]
        private pxanalogsensor.AnalogSensorOperations _genericPort = new pxanalogsensor.AnalogSensorOperations();

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.SoundSensor.user.xslt")]
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
        /// Subscription manager partner for Sound Sensor
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
        public NxtSoundSensor(DsspServiceCreationPort creationPort) : 
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
            _reliableMainPort = ServiceForwarder<SoundSensorOperations>(this.ServiceInfo.Service);
            _reliableMainPort.ConnectToBrick(_state);

        }

        /// <summary>
        /// Initialize State
        /// </summary>
        private void InitializeState()
        {
            if (_state == null)
                _state = new SoundSensorState();

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            if (_genericState == null)
                _genericState = new pxanalogsensor.AnalogSensorState();

            _state.TimeStamp = DateTime.MinValue;
            _state.Connected = false;
            _state.Intensity = 0;

            _genericState.RawMeasurementRange = 255.0;
        }


        /// <summary>
        /// Sync the generic analog sensor state with our sound sensor.
        /// </summary>
        /// <returns></returns>
        private pxanalogsensor.AnalogSensorState SyncGenericState()
        {
            _genericState.RawMeasurement = (double)(_state.Intensity);
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
            nxtcmd.LegoResponseGetInputValues inputValues = new nxtcmd.LegoResponseGetInputValues(update.Body.CommandData);
            if (inputValues != null && inputValues.Success)
            {
                bool firstNotification = (_state.TimeStamp == DateTime.MinValue);
                _state.TimeStamp = inputValues.TimeStamp;
                if (_state.Intensity != inputValues.ScaledValue || firstNotification)
                {
                    _state.Intensity = inputValues.ScaledValue;
                    SendNotification<SoundSensorUpdate>(_subMgrPort, _state);
                    SendNotification<pxanalogsensor.Replace>(_genericSubMgrPort, SyncGenericState());
                }
            }
        }

        #region Operation Handlers

        /// <summary>
        /// Get Sound Sensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetSoundSensorHandler(Get get)
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
        /// HttpGet SoundSensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> HttpGetSoundSensorHandler(dssphttp.HttpGet get)
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
            replace.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The LEGO NXT sound sensor is updated from hardware.")));
            yield break;
        }

        /// <summary>
        /// SoundSensorUpdate Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SoundSensorUpdateHandler(SoundSensorUpdate update)
        {
            update.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The LEGO NXT sound sensor is updated from hardware.")));
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

            attachRequest.InitializationCommands = new nxtcmd.NxtCommandSequence(
                new nxtcmd.LegoSetInputMode(_state.SensorPort, LegoSensorType.SoundDb, LegoSensorMode.PercentFullScaleMode));

            attachRequest.PollingCommands = new nxtcmd.NxtCommandSequence(_state.PollingFrequencyMs,
                new nxtcmd.LegoGetInputValues(_state.SensorPort));

            brick.AttachResponse response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            if (response == null)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring NXT Sound Sensor"));
                update.ResponsePort.Post(fault);
                yield break;
            }

            if (response.Connection.Port == LegoNxtPort.NotConnected && update.Body.SensorPort != NxtSensorPort.NotConnected)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring NXT Sound Sensor on Port " + update.Body.ToString()));
                update.ResponsePort.Post(fault);
                yield break;
            }

            _state.Connected = (response.Connection.Port != LegoNxtPort.NotConnected);
            if (_state.Connected)
            {
                _state.SensorPort = (NxtSensorPort)response.Connection.Port;
                _genericState.HardwareIdentifier = NxtCommon.HardwareIdentifier(_state.SensorPort);
            }

            // Set the motor name
            if (string.IsNullOrEmpty(_state.Name)
                || _state.Name.StartsWith("Sound Sensor on "))
                _state.Name = "Sound Sensor on " + response.Connection.ToString();

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
