//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceButtons.cs $ $Revision: 20 $
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
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Buttons
{
    
    /// <summary>
    /// Lego NXT Buttons Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Buttons (v2).")]
    [DisplayName("(User) Lego NXT Buttons \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870566.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    public class NxtButtons : DsspServiceBase
    {
        /// <summary>
        /// Keep track of first time initialization
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// _state
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.Buttons.config.xml")]
        private ButtonState _state = new ButtonState();

        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/buttons", AllowMultipleInstances=true)]
        private ButtonOperations _mainPort = new ButtonOperations();

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Buttons.user.xslt")]
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
        /// Default Service Constructor
        /// </summary>
        public NxtButtons(DsspServiceCreationPort creationPort) : 
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
            SpawnIterator(ConnectToBrick);
        }

        /// <summary>
        /// Initializate and Validate the state
        /// </summary>
        private void InitializeState()
        {
            if (_state == null)
                _state = new ButtonState();

            // Always initialize the button readings when we start.
            _state.Buttons = new NxtButtonReadings();

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            _state.Connected = false;

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
                    new LegoNxtConnection(LegoNxtPort.Buttons),
                    LegoDeviceType.Internal,
                    Contract.DeviceModel,  // "Buttons"
                    Contract.Identifier, 
                    ServiceInfo.Service,
                    Contract.DeviceModel));
            
            // Read Buttons
            attachRequest.PollingCommands = new NxtCommandSequence(_state.PollingFrequencyMs,
                new LegoGetButtonState());

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) 
                {
                    _state.Connected = (rsp.Connection.Port != LegoNxtPort.NotConnected);
                },
                delegate(Fault fault) 
                { 
                    LogError("LEGO NXT Buttons error attaching to brick", fault); 
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
        /// Receive Buttons Notifications
        /// </summary>
        /// <param name="update"></param>
        private void NotificationHandler(brick.LegoSensorUpdate update)
        {
            // Receive Button notifications from the NXT Brick
            LegoResponseGetButtonState buttonsUpdate = new LegoResponseGetButtonState(update.Body.CommandData);
            if (buttonsUpdate != null && buttonsUpdate.Success)
            {
                if (_state.Buttons.PressedLeft != buttonsUpdate.PressedLeft
                    || _state.Buttons.PressedEnter != buttonsUpdate.PressedEnter
                    || _state.Buttons.PressedRight != buttonsUpdate.PressedRight
                    || _state.Buttons.PressedCancel != buttonsUpdate.PressedCancel
                    || _state.Buttons.TimeStamp == DateTime.MinValue)
                {
                    _state.Buttons.TimeStamp = update.Body.TimeStamp;
                    _state.Buttons.PressedLeft = buttonsUpdate.PressedLeft;
                    _state.Buttons.PressedEnter = buttonsUpdate.PressedEnter;
                    _state.Buttons.PressedRight = buttonsUpdate.PressedRight;
                    _state.Buttons.PressedCancel = buttonsUpdate.PressedCancel;

                    SendNotification<ButtonsUpdate>(_subMgrPort, _state.Buttons);
                }
            }
        }

        #region Main Port Handlers

        /// <summary>
        /// Drop Handler
        /// </summary>
        /// <param name="drop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public virtual IEnumerator<ITask> DropHandler(DsspDefaultDrop drop)
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
        public virtual IEnumerator<ITask> HttpGetHandler(Microsoft.Dss.Core.DsspHttp.HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(HttpStatusCode.OK, _state, _transform));
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
            if (_state.Buttons.TimeStamp != DateTime.MinValue)
            {
                SendNotificationToTarget<ButtonsUpdate>(subscribe.Body.Subscriber, _subMgrPort, _state.Buttons);
            }
            yield break;
        }

        /// <summary>
        /// Outbound Notification ButtonsUpdate is not valid for inbound requests.
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> ButtonsUpdateHandler(ButtonsUpdate notification)
        {
            notification.ResponsePort.Post(Fault.FromException(new InvalidOperationException("The LEGO NXT buttons are updated from hardware.")));
            yield break;
        }

        #endregion

    }
}
