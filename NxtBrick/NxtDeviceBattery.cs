//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceBattery.cs $ $Revision: 20 $
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

using dssphttp = Microsoft.Dss.Core.DsspHttp;
using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using pxbattery = Microsoft.Robotics.Services.Battery.Proxy;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using Microsoft.Dss.Core.DsspHttp;


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Battery
{
    
    /// <summary>
    /// Lego NXT Battery Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Battery (v2).")]
    [DisplayName("(User) Lego NXT Battery \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870566.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    [AlternateContract(pxbattery.Contract.Identifier)]
    public class NxtBattery : DsspServiceBase
    {
        /// <summary>
        /// Keep track of first time initialization
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// _state
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.Battery.config.xml")]
        private BatteryState _state = new BatteryState();

        private pxbattery.BatteryState _genericState = new pxbattery.BatteryState();

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Battery.user.xslt")]
        private string _transform = string.Empty;

        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/battery", AllowMultipleInstances=true)]
        private BatteryOperations _mainPort = new BatteryOperations();

        [AlternateServicePort("/generic", AllowMultipleInstances=true, AlternateContract = pxbattery.Contract.Identifier)]
        private pxbattery.BatteryOperations _genericBatteryPort = new pxbattery.BatteryOperations();

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
        /// Default Service Constructor
        /// </summary>
        public NxtBattery(DsspServiceCreationPort creationPort) : 
                base(creationPort)
        {
        }
        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            InitializeState(false);
            base.Start();
            SpawnIterator(ConnectToBrick);
        }

        /// <summary>
        /// Initialize and Validate the state
        /// </summary>
        /// <param name="connected"></param>
        private void InitializeState(bool connected)
        {
            bool changed = false;

            if (_state == null)
            {
                _state = new BatteryState();
                changed = true;
            }

            // Set default Maximum Battery at 9.0 volts
            if (_state.MaxVoltage < 5.0 || _state.MaxVoltage > 12.0)
            {
                _state.MaxVoltage = 9.0;
                changed = true;
            }

            // Set default Minimum Battery at 5 volts
            if (_state.MinVoltage < 3.0 || _state.MinVoltage > _state.MaxVoltage)
            {
                _state.MinVoltage = 5.0;
                changed = true;
            }

            if (_state.CriticalBatteryVoltage < _state.MinVoltage || _state.CriticalBatteryVoltage > _state.MaxVoltage)
            {
                _state.CriticalBatteryVoltage = 5.8;
                changed = true;
            }

            // Initialize the runtime values
            _state.TimeStamp = DateTime.MinValue;
            _state.PercentBatteryPower = 0.0;
            _state.CurrentBatteryVoltage = 0.0;
            _state.Connected = connected;

            if (changed)
            {
                SaveState(_state);
            }
        }

        /// <summary>
        /// Connect to the LEGO NXT Brick
        /// </summary>
        /// <returns></returns>
        private IEnumerator<ITask> ConnectToBrick()
        {
            #region Attach to the NXT Brick
            _state.Connected = false;

            brick.AttachRequest attachRequest = new brick.AttachRequest(
                new brick.Registration(
                    new LegoNxtConnection(LegoNxtPort.Battery),
                    LegoDeviceType.Internal,
                    Contract.DeviceModel,  // "Battery"
                    Contract.Identifier, 
                    ServiceInfo.Service,
                    Contract.DeviceModel));
            
            attachRequest.PollingCommands = new nxtcmd.NxtCommandSequence(ValidatePollingFrequency(_state.BatteryPollingSeconds), 
                new nxtcmd.LegoGetBatteryLevel());

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) 
                {
                    _state.Connected = (rsp.Connection.Port != LegoNxtPort.NotConnected);
                },
                delegate(Fault fault) 
                { 
                    LogError(LogGroups.Console, "LEGO NXT Battery error attaching to brick", fault); 
                });

            #endregion

            #region One Time Initialization to activate the main port and receive notifications
            if (!_initialized)
            {
                _initialized = true;

                base.MainPortInterleave.CombineWith(new Interleave(
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<brick.LegoSensorUpdate>(true, _legoBrickNotificationPort, NotificationHandler)
                    ),
                    new ConcurrentReceiverGroup()));
            }
            #endregion

            yield break;
        }

        /// <summary>
        /// Return a validated polling frequency (ms)
        /// </summary>
        /// <param name="pollingSeconds"></param>
        /// <returns></returns>
        private int ValidatePollingFrequency(int pollingSeconds)
        {
            int pollingFrequencyMs = pollingSeconds * 1000;

            // For invalid values, use the default
            if (pollingFrequencyMs == 0)
                pollingFrequencyMs = Contract.DefaultBatteryPollingSeconds * 1000;

            // For values below one second, use one second.
            if (pollingFrequencyMs < 1000)
                pollingFrequencyMs = 1000;

            return pollingFrequencyMs;
        }

        /// <summary>
        /// Receive Battery Notifications
        /// </summary>
        /// <param name="update"></param>
        private void NotificationHandler(brick.LegoSensorUpdate update)
        {
            nxtcmd.LegoResponseGetBatteryLevel batteryLevel = new nxtcmd.LegoResponseGetBatteryLevel(update.Body.CommandData);
            if (batteryLevel != null && batteryLevel.Success)
            {
                if (_state.MaxVoltage < batteryLevel.Voltage)
                {
                    _state.TimeStamp = update.Body.TimeStamp; 
                    _state.MaxVoltage = batteryLevel.Voltage;
                    SaveState(_state);
                }

                if (batteryLevel.Voltage != 0 &&
                    _state.MinVoltage > batteryLevel.Voltage)
                {
                    _state.TimeStamp = update.Body.TimeStamp;
                    _state.MinVoltage = batteryLevel.Voltage;
                    SaveState(_state);
                }

                double percentBatteryPower = batteryLevel.Voltage / _state.MaxVoltage;
                if (_state.PercentBatteryPower != percentBatteryPower 
                    || _state.CurrentBatteryVoltage != batteryLevel.Voltage)
                {
                    _state.TimeStamp = update.Body.TimeStamp; 
                    _state.CurrentBatteryVoltage = batteryLevel.Voltage;
                    _state.PercentBatteryPower = percentBatteryPower;
                    SendNotification<pxbattery.Replace>(_subMgrPort, _state.SyncGenericState(ref _genericState));
                }

                if (_state.CurrentBatteryVoltage < _state.CriticalBatteryVoltage)
                {
                    pxbattery.UpdateCriticalBattery criticalBattery = new pxbattery.UpdateCriticalBattery();
                    criticalBattery.PercentCriticalBattery = _state.PercentBatteryPower;
                    SendNotification<pxbattery.SetCriticalLevel>(_subMgrPort, criticalBattery);
                }
            }
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
        public virtual IEnumerator<ITask> HttpGetHandler(HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(HttpStatusCode.OK, _state, _transform));
            yield break;
        }

        /// <summary>
        /// Configure Battery Handler
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ConfigureBatteryHandler(ConfigureBattery configure)
        {
            if (_state.BatteryPollingSeconds != configure.Body.PollingFrequencySeconds)
            {
                _state.BatteryPollingSeconds = configure.Body.PollingFrequencySeconds;
                Fault faultResponse = null;
                yield return Arbiter.Choice(
                    _legoBrickPort.AdjustPollingFrequency(ServiceInfo.Service, _state.BatteryPollingSeconds * 1000),
                    delegate(brick.AdjustPollingFrequencyResponse response)
                    {
                        _state.BatteryPollingSeconds = response.PollingFrequencyMs;
                    },
                    delegate(Fault fault)
                    {
                        faultResponse = fault;
                    });

                if (faultResponse != null)
                {
                    configure.ResponsePort.Post(faultResponse);
                    yield break;
                }
            }

            _state.CriticalBatteryVoltage = configure.Body.CriticalBatteryVoltage;
            _state.MinVoltage = configure.Body.MinVoltage;
            _state.MaxVoltage = configure.Body.MaxVoltage;
            InitializeState(_state.Connected);
            configure.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }


        #endregion

        #region Generic Port Handlers
        /// <summary>
        /// HttpGet Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericBatteryPort")]
        public virtual IEnumerator<ITask> GenericHttpGetHandler(Microsoft.Dss.Core.DsspHttp.HttpGet get)
        {
            _state.SyncGenericState(ref _genericState);
            get.ResponsePort.Post(new dssphttp.HttpResponseType(_genericState));
            yield break;
        }

        /// <summary>
        /// Drop Handler
        /// </summary>
        /// <param name="drop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Teardown, PortFieldName = "_genericBatteryPort")]
        public virtual IEnumerator<ITask> GenericDropHandler(DsspDefaultDrop drop)
        {
            // detach from the brick
            _legoBrickPort.Detach(ServiceInfo.Service);

            // drop the service
            base.DefaultDropHandler(drop);
            yield break;
        }

        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_genericBatteryPort")]
        public virtual IEnumerator<ITask> GenericGetHandler(pxbattery.Get get)
        {
            _state.SyncGenericState(ref _genericState);
            get.ResponsePort.Post(_genericState);
            yield break;
        }

        /// <summary>
        /// Replace Handler
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericBatteryPort")]
        public virtual IEnumerator<ITask> GenericReplaceHandler(pxbattery.Replace replace)
        {
            _state.CopyFrom(replace.Body);
            replace.ResponsePort.Post(DefaultReplaceResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericBatteryPort")]
        public virtual IEnumerator<ITask> GenericSubscribeHandler(pxbattery.Subscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);

            if (_state.TimeStamp != DateTime.MinValue)
            {
                SendNotificationToTarget<pxbattery.Replace>(subscribe.Body.Subscriber, _subMgrPort, _state.SyncGenericState(ref _genericState));
            }

            yield break;
        }
        /// <summary>
        /// SetCriticalLevel Handler
        /// </summary>
        /// <param name="setCriticalLevel"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_genericBatteryPort")]
        public virtual IEnumerator<ITask> GenericSetCriticalLevelHandler(pxbattery.SetCriticalLevel setCriticalLevel)
        {
            _state.CriticalBatteryVoltage = _state.MaxVoltage * setCriticalLevel.Body.PercentCriticalBattery;
            SendNotification<pxbattery.SetCriticalLevel>(_subMgrPort, setCriticalLevel);
            setCriticalLevel.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }
        #endregion
    }
}
