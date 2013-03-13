//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceColorSensor.cs $ $Revision: 1 $
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

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.ColorSensor
{
    
    /// <summary>
    /// Lego NXT Color Sensor Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(pxanalogsensor.Contract.Identifier)]
    [Description("Provides access to the LEGO® MINDSTORMS® NXT Color Sensor (v2).")]
    [DisplayName("(User) Lego NXT Color Sensor (v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/ff631052.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    public class NxtColorSensor : DsspServiceBase
    {
        /// <summary>
        /// Service State
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.ColorSensor.config.xml")]
        private ColorSensorState _state = new ColorSensorState();
        private pxanalogsensor.AnalogSensorState _genericState = new pxanalogsensor.AnalogSensorState();

        /// <summary>
        /// Main Operations Port
        /// </summary>
        [ServicePort("/lego/nxt/colorsensor", AllowMultipleInstances=true)]
        private ColorSensorOperations _internalMainPort = new ColorSensorOperations();
        private ColorSensorOperations _reliableMainPort = null;

        /// <summary>
        /// Alternate Port to expose Reading as a Generic Analog Sensor
        /// </summary>
        [AlternateServicePort("/analogsensor", 
            AllowMultipleInstances = true, 
            AlternateContract = pxanalogsensor.Contract.Identifier)]
        private pxanalogsensor.AnalogSensorOperations _genericPort = new pxanalogsensor.AnalogSensorOperations();

        /// <summary>
        /// XSLT Transform for the service web page
        /// </summary>
        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.ColorSensor.user.xslt")]
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
        public NxtColorSensor(DsspServiceCreationPort creationPort) 
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
            _reliableMainPort = ServiceForwarder<ColorSensorOperations>(this.ServiceInfo.Service);
            _reliableMainPort.ConnectToBrick(_state);

        }

        /// <summary>
        /// Initialize the Color Sensor state
        /// </summary>
        private void InitializeState()
        {
            if (_state == null)
            {
                _state = new ColorSensorState();
                // Default to Color mode if no config file
                _state.SensorMode = ColorSensorMode.Color;
            }

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            _state.TimeStamp = DateTime.MinValue;
            _state.Reading = 0;
            _state.Connected = false;


            if (_genericState == null)
                _genericState = new pxanalogsensor.AnalogSensorState();

            // Analog range is 0-1023 (10-bit A/D)
            _genericState.RawMeasurementRange = 1023.0;
            _genericState.RawMeasurement = 0.0;
            _genericState.NormalizedMeasurement = 0.0;
            _genericState.TimeStamp = DateTime.MinValue;

            // Set the hardware identifier from the connected motor port.
            _genericState.HardwareIdentifier = NxtCommon.HardwareIdentifier(_state.SensorPort);
        }

        /// <summary>
        /// Sync the generic analog sensor state with the color sensor.
        /// </summary>
        /// <returns>Analog Sensor State</returns>
        private pxanalogsensor.AnalogSensorState SyncGenericState()
        {
            _genericState.RawMeasurement = (double)_state.Reading;
            // Only normalize if NOT in Color Mode otherwise the Color Number will be messed up
            if (_state.SensorMode != ColorSensorMode.Color)
                _genericState.NormalizedMeasurement = _genericState.RawMeasurement / _genericState.RawMeasurementRange;
            else
                _genericState.NormalizedMeasurement = (double)_state.Reading;
            _genericState.TimeStamp = _state.TimeStamp;

            return _genericState;
        }

        /// <summary>
        /// Handle sensor notifications from the brick
        /// </summary>
        /// <param name="update">Update message from the Brick</param>
        private void NotificationHandler(brick.LegoSensorUpdate update)
        {
            LegoResponseGetInputValues inputValues = new LegoResponseGetInputValues(update.Body.CommandData);
            if (inputValues != null && inputValues.Success)
            {
                bool firstTime = (_state.TimeStamp == DateTime.MinValue);
                _state.TimeStamp = inputValues.TimeStamp;
                if (_state.Reading != inputValues.ScaledValue || firstTime)
                {
                    _state.Reading = inputValues.ScaledValue;
                    // Send Replace notifications to both the generic and native subscribers
                    SendNotification<pxanalogsensor.Replace>(_genericSubMgrPort, SyncGenericState());
                    SendNotification<Replace>(_subMgrPort, _state);
                }
            }
        }

        #region Handlers

        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get">Get request (not used)</param>
        /// <returns>State is posted back</returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }

        /// <summary>
        /// Generic Get Handler
        /// </summary>
        /// <param name="get">Analog Get request (not used)</param>
        /// <returns>Analog State is posted back</returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> AnalogGetHandler(pxanalogsensor.Get get)
        {
            get.ResponsePort.Post(SyncGenericState());
            yield break;
        }
        

        /// <summary>
        /// HttpGet Handler
        /// </summary>
        /// <param name="get">HTTP Get request</param>
        /// <returns>Formatted web page</returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> HttpGetHandler(dssphttp.HttpGet get)
        {
            // Post back the state with a transform file so it will be formatted nicely
            get.ResponsePort.Post(new dssphttp.HttpResponseType(HttpStatusCode.OK, _state, _transform));
            yield break;
        }


        /// <summary>
        /// HttpGet AnalogSensor Handler
        /// </summary>
        /// <param name="get">HTTP Get request for Analog Sensor</param>
        /// <returns>Raw XML page</returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> AnalogHttpGetHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(SyncGenericState()));
            yield break;
        }


        /// <summary>
        /// Replace Handler for Analog Sensor
        /// </summary>
        /// <param name="replace">Replace request</param>
        /// <returns>Default Response</returns>
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
        /// <param name="replace">Replace request</param>
        /// <returns>Fault</returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ReplaceHandler(Replace replace)
        {
            // "Native" sensor does not support Replace
            throw (new Exception("Replace is used for notifications only"));
        }


        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe">Subscribe request</param>
        /// <returns>Default response</returns>
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
        /// <returns>Default response</returns>
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
        /// <returns>Default response</returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericPort")]
        public virtual IEnumerator<ITask> AnalogSubscribeHandler(pxanalogsensor.Subscribe subscribe)
        {
            base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// Drop Handler
        /// </summary>
        /// <param name="drop">Drop request</param>
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
        /// SetMode Handler
        /// </summary>
        /// <param name="setMode">SetMode request</param>
        /// <returns>Default response or Fault</returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> SetModeHandler(SetMode setMode)
        {
            _state.SensorMode = setMode.Body.Mode;

            // Convert Sensor Mode to a Sensor Type
            // The Color Sensor has several sensor types just for it
            LegoSensorType st;
            switch (_state.SensorMode)
            {
                case ColorSensorMode.Color:
                    st = LegoSensorType.ColorFull;
                    break;
                case ColorSensorMode.Red:
                    st = LegoSensorType.ColorRed;
                    break;
                case ColorSensorMode.Green:
                    st = LegoSensorType.ColorGreen;
                    break;
                case ColorSensorMode.Blue:
                    st = LegoSensorType.ColorBlue;
                    break;
                case ColorSensorMode.None:
                    st = LegoSensorType.ColorNone;
                    break;
                default:
                    st = LegoSensorType.ColorFull;
                    break;
            }

            LegoSetInputMode cmd = new LegoSetInputMode(_state.SensorPort, st, LegoSensorMode.RawMode);
            _legoBrickPort.SendNxtCommand(cmd);

            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                delegate(LegoResponse response)
                {
                    if (response.Success)
                    {
                        setMode.ResponsePort.Post(DefaultUpdateResponseType.Instance);
                        // SetMode notifications are only sent to subscribers to the native service
                        SendNotification<SetMode>(_subMgrPort, setMode);
                    }
                    else
                    {
                        setMode.ResponsePort.Post(
                            Fault.FromException(
                                new InvalidOperationException(response.ErrorCode.ToString())));
                    }
                },
                delegate(Fault fault)
                {
                    setMode.ResponsePort.Post(fault);
                });

            yield break;
        }

        /// <summary>
        /// ConnectToBrick Handler
        /// </summary>
        /// <param name="update">Connect message from the Brick</param>
        /// <returns>Nothing</returns>
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
            _state.SensorMode = update.Body.SensorMode;

            // Set the hardware identifier from the connected sensor port.
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

            // Get the correct code for the Sensor Type that the Brick understands
            LegoSensorType st;
            switch (_state.SensorMode)
            {
                case ColorSensorMode.Color:
                    st = LegoSensorType.ColorFull;
                    break;
                case ColorSensorMode.Red:
                    st = LegoSensorType.ColorRed;
                    break;
                case ColorSensorMode.Green:
                    st = LegoSensorType.ColorGreen;
                    break;
                case ColorSensorMode.Blue:
                    st = LegoSensorType.ColorBlue;
                    break;
                case ColorSensorMode.None:
                    st = LegoSensorType.ColorNone;
                    break;
                default:
                    st = LegoSensorType.ColorFull;
                    break;

            }

            // The Color Sensor is a special case of an Analog sensor so if uses the
            // LegoSetInputMode request. Note that it is in Raw mode.
            attachRequest.InitializationCommands = new NxtCommandSequence(
                new LegoSetInputMode(_state.SensorPort, st, LegoSensorMode.RawMode));

            // Polling uses LegoGetInputValues to read the analog value
            attachRequest.PollingCommands = new NxtCommandSequence(_state.PollingFrequencyMs,
                new LegoGetInputValues(_state.SensorPort));

            brick.AttachResponse response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            if (response == null)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring NXT Color Sensor"));
                update.ResponsePort.Post(fault);
                yield break;
            }

            _state.Connected = (response.Connection.Port != LegoNxtPort.NotConnected);
            if (_state.Connected)
                _state.SensorPort = (NxtSensorPort)response.Connection.Port;
            else if (update.Body.SensorPort != NxtSensorPort.NotConnected)
            {
                fault = Fault.FromException(new Exception("Failure Configuring NXT Color Sensor on Port " + update.Body.ToString()));
                update.ResponsePort.Post(fault);
                yield break;
            }

            // Set the Color Sensor name
            if (string.IsNullOrEmpty(_state.Name)
                || _state.Name.StartsWith("Color Sensor on "))
                _state.Name = "Color Sensor on " + response.Connection.ToString();

            // Send a notification of the connected port
            // Only send connection notifications to native subscribers
            update.Body.Name = _state.Name;
            update.Body.PollingFrequencyMs = _state.PollingFrequencyMs;
            update.Body.SensorPort = _state.SensorPort;
            update.Body.SensorMode = _state.SensorMode;
            SendNotification<ConnectToBrick>(_subMgrPort, update);

            update.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }
        #endregion
    }
}
