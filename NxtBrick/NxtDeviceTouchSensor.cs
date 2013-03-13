//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceTouchSensor.cs $ $Revision: 23 $
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

using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using contactsensor = Microsoft.Robotics.Services.ContactSensor.Proxy;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.TouchSensor
{
    
    /// <summary>
    /// Lego NXT Touch Sensor Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(contactsensor.Contract.Identifier)]
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Touch Sensor (v2).")]
    [DisplayName("(User) Lego NXT Touch Sensor \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870566.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    public class NxtTouchSensor : DsspServiceBase
    {
        /// <summary>
        /// _state
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.TouchSensor.config.xml")]
        private TouchSensorState _state = new TouchSensorState();
        private contactsensor.ContactSensorArrayState _genericContactState = new contactsensor.ContactSensorArrayState();

        // Create a local ContactSensor for convenience
        private contactsensor.ContactSensor _genericSensor = new contactsensor.ContactSensor();

        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/touchsensor", AllowMultipleInstances=true)]
        private TouchSensorOperations _internalMainPort = new TouchSensorOperations();
        private TouchSensorOperations _reliableMainPort = null;

        // Support the standard "bumper" contract as well as the LEGO TouchSensor
        [AlternateServicePort("/contactsensor",
            AllowMultipleInstances = true,
            AlternateContract = contactsensor.Contract.Identifier)]
        private contactsensor.ContactSensorArrayOperations _genericContactPort = new contactsensor.ContactSensorArrayOperations();

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.TouchSensor.user.xslt")]
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

        /// <summary>
        /// Subscription manager partner for Touch Sensor
        /// </summary>
        [Partner(Partners.SubscriptionManagerString, Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _subMgrPort = new submgr.SubscriptionManagerPort();

        /// <summary>
        /// Subscription manager partner for generic analog sensor
        /// </summary>
        [Partner(Partners.SubscriptionManagerString + "/touchsensor", Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _genericSubMgrPort = new submgr.SubscriptionManagerPort();


        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public NxtTouchSensor(DsspServiceCreationPort creationPort) : 
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
            _reliableMainPort = ServiceForwarder<TouchSensorOperations>(this.ServiceInfo.Service);
            _reliableMainPort.ConnectToBrick(_state);

        }

        /// <summary>
        /// Initialize State
        /// </summary>
        private void InitializeState()
        {

            if (_state == null)
                _state = new TouchSensorState();

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            _state.TimeStamp = DateTime.MinValue;
            _state.Connected = false;
            _state.TouchSensorOn = false;

            if (_genericContactState == null)
            {
                _genericContactState = new contactsensor.ContactSensorArrayState();
            }
            if (_genericContactState.Sensors == null)
            {
                _genericContactState.Sensors = new List<contactsensor.ContactSensor>();
            }
            if (_genericContactState.Sensors.Count == 0)
            {
                _genericContactState.Sensors.Add(new contactsensor.ContactSensor(0, "Touch Sensor"));
            }
        }

        private void NotificationHandler(brick.LegoSensorUpdate update)
        {
            nxtcmd.LegoResponseGetInputValues inputValues =  new nxtcmd.LegoResponseGetInputValues(update.Body.CommandData);
            if (inputValues != null && inputValues.Success)
            {
                bool touch = (inputValues.ScaledValue == 1);
                if (_state.TouchSensorOn != touch || _state.TimeStamp == DateTime.MinValue)
                {
                    _state.TouchSensorOn = touch;
                    _state.TimeStamp = inputValues.TimeStamp;
                    // Send Touch Sensor notification
                    SendNotification<TouchSensorUpdate>(_subMgrPort, _state);
                    // Send contact sensor notification
                    SyncGenericContactState();
                    SendNotification<contactsensor.Update>(_genericSubMgrPort, _genericSensor);
                }
            }
        }

        /// <summary>
        /// Sync the generic contact sensor state with our touch sensor.
        /// </summary>
        /// <returns></returns>
        private contactsensor.ContactSensorArrayState SyncGenericContactState()
        {
            // If the Contact Sensor Array has not been created there is a problem!
            if (_genericContactState.Sensors.Count == 0)
                throw(new Exception("Contact Sensor Array State is invalid"));

            // Set up the shadow generic sensor
            // NOTE: This is a side-effect, but some code relies on it
            _genericSensor.Name = _state.Name;
            _genericSensor.TimeStamp = _state.TimeStamp;
            _genericSensor.Pressed = _state.TouchSensorOn;
            // Set the hardware identifier from the connected sensor port.
            switch (_state.SensorPort)
            {
                case NxtSensorPort.Sensor1:
                    _genericSensor.HardwareIdentifier = 1;
                    break;
                case NxtSensorPort.Sensor2:
                    _genericSensor.HardwareIdentifier = 2;
                    break;
                case NxtSensorPort.Sensor3:
                    _genericSensor.HardwareIdentifier = 3;
                    break;
                case NxtSensorPort.Sensor4:
                    _genericSensor.HardwareIdentifier = 4;
                    break;
                default:
                    _genericSensor.HardwareIdentifier = 0;
                    break;
            }

            // Copy the sensor info to the state
            _genericContactState.Sensors[0] = _genericSensor;

            return _genericContactState;
        }

        /// <summary>
        /// Get Touch Sensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetTouchSensorHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }

        /// <summary>
        /// Get Contact Sensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericContactPort")]
        public virtual IEnumerator<ITask> GetContatSensorHandler(contactsensor.Get get)
        {
            get.ResponsePort.Post(SyncGenericContactState());
            yield break;
        }

        /// <summary>
        /// HttpGet TouchSensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> HttpGetTouchSensorHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(HttpStatusCode.OK, _state, _transform));
            yield break;
        }
        /*
        /// <summary>
        /// HttpGet AnalogSensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericAnalogPort")]
        public virtual IEnumerator<ITask> HttpGetAnalogSensorHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(SyncGenericAnalogState()));
            yield break;
        }
        */
        /// <summary>
        /// HttpGet Contact Sensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericContactPort")]
        public virtual IEnumerator<ITask> HttpGetContactSensorHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(SyncGenericContactState()));
            yield break;
        }

        /// <summary>
        /// Replace Handler
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericContactPort")]
        public virtual IEnumerator<ITask> ReplaceHandler(contactsensor.Replace replace)
        {
            replace.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The LEGO NXT touch sensor is updated from hardware.")));
            yield break;
        }

        /// <summary>
        /// TouchSensorUpdate Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> TouchSensorUpdateHandler(TouchSensorUpdate update)
        {
            update.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The LEGO NXT touch sensor is updated from hardware.")));
            yield break;
        }

        /// <summary>
        /// ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ReliableSubscribeHandler(contactsensor.ReliableSubscribe subscribe)
        {
            yield return (Choice)base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> SubscribeHandler(contactsensor.Subscribe subscribe)
        {
            yield return (Choice)base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
            if (_state.TimeStamp != DateTime.MinValue)
            {
                SendNotificationToTarget<contactsensor.Replace>(subscribe.Body.Subscriber, _genericSubMgrPort, SyncGenericContactState());
            }
        }

        /// <summary>
        /// ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericContactPort")]
        public virtual IEnumerator<ITask> GenericReliableSubscribeHandler(contactsensor.ReliableSubscribe subscribe)
        {
            yield return (Choice)base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericContactPort")]
        public virtual IEnumerator<ITask> GenericSubscribeHandler(contactsensor.Subscribe subscribe)
        {
            yield return (Choice)base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);
            if (_state.TimeStamp != DateTime.MinValue)
            {
                // Send the intial notifications
                SendNotificationToTarget<TouchSensorUpdate>(subscribe.Body.Subscriber, _subMgrPort, _state);
                SendNotificationToTarget<contactsensor.Update>(subscribe.Body.Subscriber, _subMgrPort, SyncGenericContactState());
            }
        }

        /// <summary>
        /// Drop Handler
        /// </summary>
        /// <param name="drop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        [ServiceHandler(ServiceHandlerBehavior.Teardown, PortFieldName = "_genericAnalogPort")]
        [ServiceHandler(ServiceHandlerBehavior.Teardown, PortFieldName = "_genericContactPort")]
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
            _state.PollingFrequencyMs = update.Body.PollingFrequencyMs;

            if (!string.IsNullOrEmpty(update.Body.Name))
                _state.Name = update.Body.Name;

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
                new nxtcmd.LegoSetInputMode(_state.SensorPort, LegoSensorType.Switch, LegoSensorMode.BooleanMode));

            attachRequest.PollingCommands = new nxtcmd.NxtCommandSequence(_state.PollingFrequencyMs,
                new nxtcmd.LegoGetInputValues(_state.SensorPort));

            brick.AttachResponse response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            _state.Connected = (response != null && (response.Connection.Port != LegoNxtPort.NotConnected));
            if (response == null
                || (!_state.Connected  && update.Body.SensorPort != NxtSensorPort.NotConnected))
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring NXT Touch Sensor on port: " + update.Body.SensorPort.ToString()));

                update.ResponsePort.Post(fault);
                yield break;
            }

            if (_state.Connected)
            {
                _state.SensorPort = (NxtSensorPort)response.Connection.Port;

                // Set the sensor name
                if (string.IsNullOrEmpty(_state.Name)
                    || _state.Name.StartsWith("Touch Sensor on "))
                    _state.Name = "Touch Sensor on " + response.Connection.ToString();

                // Send a connection notification
                update.Body.Name = _state.Name;
                update.Body.PollingFrequencyMs = _state.PollingFrequencyMs;
                update.Body.SensorPort = _state.SensorPort;
                SendNotification<ConnectToBrick>(_subMgrPort, update);
            }

            // Send the message response
            update.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

    }
}
