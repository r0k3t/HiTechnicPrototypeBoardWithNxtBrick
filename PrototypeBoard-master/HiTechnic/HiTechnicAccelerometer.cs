//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: HiTechnicAccelerometer.cs $ $Revision: 19 $
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
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using W3C.Soap;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;

namespace Microsoft.Robotics.Services.Sample.HiTechnic.Accelerometer
{
    
    /// <summary>
    /// HiTechnic Accelerometer sensor Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [Description("Provides access to the HiTechnic Acceleration Sensor.\n(for use with 'Lego NXT Brick (v2)' service)")]
    [DisplayName("(User) HiTechnic Acceleration Sensor")]
    [DssCategory(pxbrick.LegoCategories.NXT)]
    public class HiTechnicAccelerationSensor : DsspServiceBase
    {
        /// <summary>
        /// _state
        /// </summary>v2
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/HiTechnic.Nxt.Accelerometer.config.xml")]
        private AccelerometerState _state = new AccelerometerState();


        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/hitechnic/nxt/accelerometer", AllowMultipleInstances=true)]
        private AccelerometerOperations _internalMainPort = new AccelerometerOperations();
        private AccelerometerOperations _reliableMainPort = null;

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.HiTechnic.Transforms.Accelerometer.user.xslt")]
        string _transform = string.Empty;

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
        /// Subscription manager partner for Accelerometer Sensor
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
        public HiTechnicAccelerationSensor(DsspServiceCreationPort creationPort) : 
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
            _reliableMainPort = ServiceForwarder<AccelerometerOperations>(this.ServiceInfo.Service);
            _reliableMainPort.ConnectToBrick(_state);
        }

        private void InitializeState()
        {
            if (_state == null)
            {
                _state = new AccelerometerState();
                _state.SensorPort = (NxtSensorPort)LegoNxtPort.AnySensorPort;
            }

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            if (_state.Tilt == null)
                _state.Tilt = new AccelerometerReading();
            else
            {
                _state.Tilt.X = 0.0;
                _state.Tilt.Y = 0.0;
                _state.Tilt.Z = 0.0;
                _state.Tilt.TimeStamp = DateTime.MinValue;
            }
            _state.Connected = false;

        }

        /// <summary>
        /// Handle periodic sensor readings from the brick
        /// </summary>
        /// <param name="update"></param>
        private void NotificationHandler(pxbrick.LegoSensorUpdate update)
        {
            I2CResponseHiTechnicAccelerationSensor inputValues = new I2CResponseHiTechnicAccelerationSensor(update.Body.CommandData);
            if (inputValues.Success)
            {
                _state.Tilt.TimeStamp = inputValues.TimeStamp;

                if (_state.ZeroOffset == null)
                {
                    _state.Tilt.X = inputValues.X;
                    _state.Tilt.Y = inputValues.Y;
                    _state.Tilt.Z = inputValues.Z;
                }
                else
                {
                    // Adjust Zero Offset
                    _state.Tilt.X = (inputValues.X + 512.0 - _state.ZeroOffset.X) % 1024.0 - 512.0;
                    _state.Tilt.Y = (inputValues.Y + 512.0 - _state.ZeroOffset.Y) % 1024.0 - 512.0;
                    _state.Tilt.Z = (inputValues.Z + 512.0 - _state.ZeroOffset.Z) % 1024.0 - 512.0;
                }

                SendNotification<AccelerometerUpdate>(_subMgrPort, _state.Tilt);
            }
        }

        /// <summary>
        /// Get Accelerometer Sensor Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetAccelerometerHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }


        /// <summary>
        /// HttpGet Accelerometer Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> HttpGetAccelerometerHandler(dssphttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(HttpStatusCode.OK, _state, _transform));
            yield break;
        }



        /// <summary>
        /// AccelerometerUpdate Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> AccelerometerUpdateHandler(AccelerometerUpdate update)
        {
            update.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The HiTechnic Accelerometer sensor is updated from hardware.")));
            yield break;
        }

        /// <summary>
        /// ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ReliableSubscribeHandler(ReliableSubscribe subscribe)
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
        public virtual IEnumerator<ITask> SubscribeHandler(Subscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
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
                    fault = Fault.FromException(new Exception("Failure Configuring HiTechnic Accelerometer on Port " + update.Body.ToString()));
                update.ResponsePort.Post(fault);
                yield break;
            }

            pxbrick.AttachRequest attachRequest = new pxbrick.AttachRequest(registration);

            attachRequest.InitializationCommands = new nxtcmd.NxtCommandSequence(
                (nxtcmd.LegoCommand)new nxtcmd.LegoSetInputMode((NxtSensorPort)registration.Connection.Port, LegoSensorType.I2C_9V, LegoSensorMode.RawMode));

            attachRequest.PollingCommands = new nxtcmd.NxtCommandSequence(_state.PollingFrequencyMs,
                new I2CReadHiTechnicAccelerationSensor(_state.SensorPort));

            pxbrick.AttachResponse response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(pxbrick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            if (response == null)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring HiTechnic Accelerometer"));
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
                update.ResponsePort.Post(Fault.FromException(new Exception(string.Format("Failure Configuring HiTechnic Acceleration Sensor on port: {0}", update.Body.SensorPort))));
                yield break;
            }

            // Set the accelerometer name
            if (string.IsNullOrEmpty(_state.Name)
                || _state.Name.StartsWith("Accelerometer Sensor on "))
                _state.Name = "Accelerometer Sensor on " + response.Connection.Port.ToString();

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
