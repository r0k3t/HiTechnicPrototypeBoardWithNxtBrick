//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceDrive.cs $ $Revision: 41 $
//-----------------------------------------------------------------------

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Services.SubscriptionManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using W3C.Soap;

using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;

using dssphttp = Microsoft.Dss.Core.DsspHttp;
using pxdrive = Microsoft.Robotics.Services.Drive.Proxy;
using pxencoder = Microsoft.Robotics.Services.Encoder.Proxy;
using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using motor = Microsoft.Robotics.Services.Sample.Lego.Nxt.Motor;
using pxmotor = Microsoft.Robotics.Services.Motor.Proxy;


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Drive
{
    
    /// <summary>
    /// Lego NXT Drive Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(pxdrive.Contract.Identifier)]
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Drive (v2).")]
    [DisplayName("(User) Lego NXT Drive \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870567.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    public class NxtDrive : DsspServiceBase
    {
        #region Private State
        /// <summary>
        /// Robotics.LegoNxtDrive.LegoNxtDriveService State
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.Drive.config.xml")]
        private DriveState _state = new DriveState();


        /// <summary>
        /// Generic State instance which is derrived from NxtDriveState.
        /// </summary>
        private pxdrive.DriveDifferentialTwoWheelState _genericState = new pxdrive.DriveDifferentialTwoWheelState();

        private bool[] _targetEncoderPending = new bool[2];
        private const int LEFT = 0;
        private const int RIGHT = 1;
        private pxdrive.DriveRequestOperation _internalPendingDriveOperation;

        #endregion


        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/drive", AllowMultipleInstances = true)]
        private DriveOperations _internalMainPort = new DriveOperations();
        private DriveOperations _reliableMainPort = new DriveOperations();


        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Drive.user.xslt")]
        string _transform = string.Empty;

        #region Partner Ports

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
        /// Subscription manager partner for the "native" notifications
        /// </summary>
        [Partner(Partners.SubscriptionManagerString, Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _subMgrPort = new submgr.SubscriptionManagerPort();

        /// <summary>
        /// Subscription manager partner for Generic notifications
        /// </summary>
        /// <remarks>All notifications through this submgr should be for pxdrive operations</remarks>
        [Partner(Partners.SubscriptionManagerString + "/genericdrive", Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _genericSubMgrPort = new submgr.SubscriptionManagerPort();

        #endregion

        #region Alternate Contract Ports

        /// <summary>
        /// Generic Drive Operations Port
        /// </summary>
        [AlternateServicePort("/generic", AlternateContract = pxdrive.Contract.Identifier)]
        private pxdrive.DriveOperations _drivePort = new pxdrive.DriveOperations();

        #endregion

        #region Internal CCR Ports
        // Used for driving the motors. Always execute the newest pending drive request.
        private Port<DriveDistance> _internalDrivePowerPort = new Port<DriveDistance>();
        private Port<bool>[] _targetEncoderReachedPort = new Port<bool>[2];

        #endregion

        
        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public NxtDrive(DsspServiceCreationPort creationPort) 
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

            // Add Encoder Notifications to the main interleave
            base.MainPortInterleave.CombineWith(new Interleave(
                new ExclusiveReceiverGroup(
                    Arbiter.ReceiveWithIterator<brick.LegoSensorUpdate>(true, _legoBrickNotificationPort, NotificationHandler)
                ),
                new ConcurrentReceiverGroup()));

            // Wait one time for an InternalDrivePower command
            Activate(Arbiter.ReceiveWithIterator<DriveDistance>(false, _internalDrivePowerPort, InternalDrivePowerHandler));

            // Set up the reliable port using DSS forwarder to ensure exception and timeout conversion to fault.
            _reliableMainPort = ServiceForwarder<DriveOperations>(this.ServiceInfo.Service);

            _reliableMainPort.ConnectToBrick(_state);
        }

        /// <summary>
        /// Handle periodic sensor readings from the pxbrick
        /// </summary>
        /// <param name="update"></param>
        private IEnumerator<ITask> NotificationHandler(brick.LegoSensorUpdate update)
        {
            DriveState currentState = _state.Clone();

            LegoResponseGetOutputState outputState = new LegoResponseGetOutputState(update.Body.CommandData);
            if (outputState.Success && _state.RuntimeStatistics.LeftEncoderTimeStamp < outputState.TimeStamp)
            {
                LogVerbose(LogGroups.Console, outputState.ToString());

                int leftReversePolaritySign = (currentState.LeftWheel.ReversePolarity) ? -1 : 1;
                int rightReversePolaritySign = (currentState.LeftWheel.ReversePolarity) ? -1 : 1;

                double direction, enctarget, encoder, remaining;

                _state.TimeStamp = outputState.TimeStamp;

                if (currentState.LeftWheel.MotorPort == outputState.MotorPort
                    && currentState.RuntimeStatistics.LeftEncoderCurrent != (outputState.EncoderCount * leftReversePolaritySign))
                {
                    if (currentState.LeftWheel.ReversePolarity)
                    {
                        outputState.EncoderCount *= -1;
                        outputState.BlockTachoCount *= -1;
                        outputState.ResettableCount *= -1;
                    }

                    currentState.RuntimeStatistics.LeftEncoderCurrent = outputState.EncoderCount;
                    _state.RuntimeStatistics.LeftEncoderCurrent = currentState.RuntimeStatistics.LeftEncoderCurrent;
                    _state.RuntimeStatistics.LeftEncoderTimeStamp = currentState.RuntimeStatistics.LeftEncoderTimeStamp = outputState.TimeStamp;

                    direction = Math.Sign(currentState.RuntimeStatistics.LeftPowerTarget);
                    enctarget = currentState.RuntimeStatistics.LeftEncoderTarget * direction;
                    encoder = outputState.EncoderCount * direction;
                    // negative remaining means we have passed the target.
                    remaining = enctarget - encoder;
                    if (enctarget != 0)
                    {
                        if (remaining <= 5 && _targetEncoderPending[LEFT])
                        {
                            _state.RuntimeStatistics.LeftPowerCurrent = 0.0;
                            _state.RuntimeStatistics.LeftPowerTarget = 0.0;

                            _targetEncoderReachedPort[LEFT].Post(true);
                            _targetEncoderPending[LEFT] = false;

                            if (_internalPendingDriveOperation == pxdrive.DriveRequestOperation.RotateDegrees)
                            {
                                _state.RuntimeStatistics.RightPowerCurrent = 0.0;
                                _state.RuntimeStatistics.RightPowerTarget = 0.0;

                                _targetEncoderReachedPort[RIGHT].Post(true);
                                _targetEncoderPending[RIGHT] = false;
                            }
                        }
                    }
                }

                if (currentState.RightWheel.MotorPort == outputState.MotorPort
                    && currentState.RuntimeStatistics.RightEncoderCurrent != (outputState.EncoderCount * ((currentState.RightWheel.ReversePolarity) ? -1 : 1)))
                {
                    if (currentState.RightWheel.ReversePolarity)
                    {
                        outputState.EncoderCount *= -1;
                        outputState.BlockTachoCount *= -1;
                        outputState.ResettableCount *= -1;
                    }

                    currentState.RuntimeStatistics.RightEncoderCurrent = outputState.EncoderCount;
                    _state.RuntimeStatistics.RightEncoderCurrent = currentState.RuntimeStatistics.RightEncoderCurrent;
                    _state.RuntimeStatistics.RightEncoderTimeStamp = currentState.RuntimeStatistics.RightEncoderTimeStamp = outputState.TimeStamp;

                    direction = Math.Sign(currentState.RuntimeStatistics.RightPowerTarget);
                    enctarget = currentState.RuntimeStatistics.RightEncoderTarget * direction;
                    encoder = outputState.EncoderCount * direction;
                    // negative remaining means we have passed the target.
                    remaining = enctarget - encoder;
                    if (enctarget != 0)
                    {
                        if (remaining <= 5 && _targetEncoderPending[RIGHT])
                        {
                            _state.RuntimeStatistics.RightPowerCurrent = 0.0;
                            _state.RuntimeStatistics.RightPowerTarget = 0.0;

                            _targetEncoderReachedPort[RIGHT].Post(true);
                            _targetEncoderPending[RIGHT] = false;

                            if (_internalPendingDriveOperation == pxdrive.DriveRequestOperation.RotateDegrees)
                            {
                                _state.RuntimeStatistics.LeftPowerCurrent = 0.0;
                                _state.RuntimeStatistics.LeftPowerTarget = 0.0;

                                _targetEncoderReachedPort[LEFT].Post(true);
                                _targetEncoderPending[LEFT] = false;
                            }

                        }
                    }
                }

                // Send encoder notifications.
                SendNotification<DriveEncodersUpdate>(_subMgrPort, currentState.RuntimeStatistics);
            }
            yield break;
        }

        /// <summary>
        /// Process the most recent Drive Power command
        /// When complete, self activate for the next internal command
        /// </summary>
        /// <param name="driveDistance"></param>
        /// <returns></returns>
        public virtual IEnumerator<ITask> InternalDrivePowerHandler(DriveDistance driveDistance)
        {
            try
            {
                #region Check for a backlog of drive commands

                // Take a snapshot of the number of pending commands at the time
                // we entered this routine.
                // This will prevent a livelock which can occur if we try to
                // process the queue until it is empty, but the inbound queue
                // is growing at the same rate as we are pulling off the queue.
                int pendingCommands = _internalDrivePowerPort.ItemCount;

                // If newer commands have been issued,
                // respond success to the older command and
                // move to the newer one.
                DriveDistance newerUpdate;
                while (pendingCommands > 0)
                {
                    if (_internalDrivePowerPort.Test(out newerUpdate))
                    {                        
                        driveDistance = newerUpdate;
                    }
                    pendingCommands--;
                }
                #endregion

                // Get a snapshot of our state. 
                DriveState currentState = _state.Clone();

                #region Cancel any prior encoder target

                // If a prior encoder target was active, 
                // signal that it is cancelled by a new Motor command.
                if (_targetEncoderPending[LEFT] || _targetEncoderPending[RIGHT])
                {
                    lock (_targetEncoderPending)
                    {
                        LogVerbose(LogGroups.Console, "Cancel prior target!");
                        if (_targetEncoderPending[LEFT])
                        {
                            _targetEncoderReachedPort[LEFT].Post(false);
                            _targetEncoderPending[LEFT] = false;
                        }
                        if (_targetEncoderPending[RIGHT])
                        {
                            _targetEncoderReachedPort[RIGHT].Post(false);
                            _targetEncoderPending[RIGHT] = false;
                        }

                        _state.RuntimeStatistics.LeftEncoderTarget = 0;
                        _state.RuntimeStatistics.RightEncoderTarget = 0;
                    }
                }
                #endregion

                // Calculate the motor power and target degrees for both motors.
                int leftMotorPower, rightMotorPower;
                long leftTargetEncoderDegrees, rightTargetEncoderDegrees;
                CalculatePowerAndTargetDegrees(driveDistance.Body, currentState, 
                    out leftMotorPower, 
                    out rightMotorPower, 
                    out leftTargetEncoderDegrees, 
                    out rightTargetEncoderDegrees);

                _state.RuntimeStatistics.LeftPowerCurrent = ((double)leftMotorPower) / 100.0; ;
                _state.RuntimeStatistics.LeftPowerTarget = driveDistance.Body.LeftPower;
                _state.RuntimeStatistics.RightPowerCurrent = ((double)rightMotorPower) / 100.0;
                _state.RuntimeStatistics.RightPowerTarget = driveDistance.Body.RightPower;

                LegoOutputMode leftOutputMode = (leftMotorPower == 0  || leftTargetEncoderDegrees != 0) ? LegoOutputMode.PowerBrake : LegoOutputMode.PowerRegulated;
                LegoOutputMode rightOutputMode = (rightMotorPower == 0 || rightTargetEncoderDegrees != 0) ? LegoOutputMode.PowerBrake : LegoOutputMode.PowerRegulated;
                if (leftTargetEncoderDegrees != 0)
                {
                    // Calcuate a new target encoder which is based on the current encoder position.
                    leftTargetEncoderDegrees += currentState.RuntimeStatistics.LeftEncoderCurrent;

                    // A target of zero diables the PD control.  Make sure our result isn't 0.
                    if (leftTargetEncoderDegrees == 0) leftTargetEncoderDegrees++;

                    _state.RuntimeStatistics.TargetStopState = driveDistance.Body.StopState;
                    _targetEncoderPending[LEFT] = true;
                }
                _state.RuntimeStatistics.LeftEncoderTarget = leftTargetEncoderDegrees;

                if (rightTargetEncoderDegrees != 0)
                {
                    // Calcuate a new target encoder which is based on the current encoder position.
                    rightTargetEncoderDegrees += currentState.RuntimeStatistics.RightEncoderCurrent;

                    // A target of zero diables the PD control.  Make sure our result isn't 0.
                    if (rightTargetEncoderDegrees == 0) rightTargetEncoderDegrees++;

                    _state.RuntimeStatistics.TargetStopState = driveDistance.Body.StopState;
                    _targetEncoderPending[RIGHT] = true;
                }
                _state.RuntimeStatistics.RightEncoderTarget = rightTargetEncoderDegrees;

                _state.TimeStamp = DateTime.Now;

                LegoRegulationMode syncMode = LegoRegulationMode.Individual;

                // Send the left motor command to the brick
                LegoSetOutputState motorCmd = new LegoSetOutputState(
                    currentState.LeftWheel.MotorPort,
                    leftMotorPower,
                    leftOutputMode,
                    syncMode,
                    0, // turnratio
                    RunState.Constant,
                    driveDistance.Body.LeftStopAtRotationDegrees);

                motorCmd.RequireResponse = false;
                LogVerbose(LogGroups.Console, motorCmd.ToString());

                Fault fault = null;
                PortSet<LegoResponse, Fault> leftResponse = _legoBrickPort.SendNxtCommand(motorCmd);

                // Send the right motor comand to the brick
                motorCmd.MotorPort = currentState.RightWheel.MotorPort;
                motorCmd.PowerSetPoint = rightMotorPower;
                motorCmd.Mode = rightOutputMode;
                motorCmd.TurnRatio = 0;
                motorCmd.EncoderLimit = driveDistance.Body.RightStopAtRotationDegrees;

                LogVerbose(LogGroups.Console, motorCmd.ToString());
                PortSet<LegoResponse, Fault> rightResponse = _legoBrickPort.SendNxtCommand(motorCmd);

                yield return Arbiter.Choice(leftResponse,
                    delegate(LegoResponse response)
                    {
                        if (!response.Success)
                            fault = Fault.FromException(
                                 new InvalidOperationException(response.ErrorCode.ToString()));
                    },
                    delegate(Fault f)
                    {
                        fault = f;
                    });

                yield return Arbiter.Choice(rightResponse,
                    delegate(LegoResponse response)
                    {
                        if (!response.Success)
                            fault = Fault.FromException(
                                 new InvalidOperationException(response.ErrorCode.ToString()));
                    },
                    delegate(Fault f)
                    {
                        fault = f;
                    });

                if (leftTargetEncoderDegrees != 0 && rightTargetEncoderDegrees != 0)
                {
                    // Wait for the target encoders to either be cancelled or reach its target.
                    Activate(Arbiter.MultiplePortReceive<bool>(false, _targetEncoderReachedPort,
                        delegate(bool[] finished)
                        {
                            HandleDriveResponse(driveDistance, finished[LEFT] && finished[RIGHT], null);
                        }));
                }
                else if (leftTargetEncoderDegrees != 0 || rightTargetEncoderDegrees != 0)
                {
                    int ix = (leftTargetEncoderDegrees != 0) ? LEFT : RIGHT;

                    // Wait for the target encoder to either be cancelled or reach its target.
                    Activate(Arbiter.Receive<bool>(false, _targetEncoderReachedPort[ix],
                        delegate(bool finished)
                        {
                            HandleDriveResponse(driveDistance, finished, null);
                        }));
                }
                else
                {
                    bool success = (fault == null);
                    HandleDriveResponse(driveDistance, success, fault);
                }

            }
            finally
            {
                // Wait one time for the next InternalDrivePower command
                Activate(Arbiter.ReceiveWithIterator(false, _internalDrivePowerPort, InternalDrivePowerHandler));
            }
            yield break;
        }

        #region Main Port Handlers

        /// <summary>
        /// ConnectToBrick Handler
        /// </summary>
        /// <param name="connectToBrick"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> MainConnectToBrickHandler(ConnectToBrick connectToBrick)
        {
            DriveState currentState = _state.Clone();

            // Validate the motor port.
            if ((connectToBrick.Body.LeftWheel.MotorPort & NxtMotorPort.AnyMotorPort)
                != connectToBrick.Body.LeftWheel.MotorPort)
                if ((connectToBrick.Body.RightWheel.MotorPort & NxtMotorPort.AnyMotorPort)
                    != connectToBrick.Body.RightWheel.MotorPort)
                {
                    connectToBrick.ResponsePort.Post(
                        Fault.FromException(
                            new ArgumentException(
                                string.Format("Invalid Drive Ports: {0},{1}",
                                    ((LegoNxtPort)connectToBrick.Body.LeftWheel.MotorPort), ((LegoNxtPort)connectToBrick.Body.RightWheel.MotorPort)))));
                    yield break;
                }


            _state.PollingFrequencyMs = connectToBrick.Body.PollingFrequencyMs;
            _state.DistanceBetweenWheels = connectToBrick.Body.DistanceBetweenWheels;

            _state.LeftWheel.WheelDiameter = connectToBrick.Body.LeftWheel.WheelDiameter;
            _state.LeftWheel.MotorPort = connectToBrick.Body.LeftWheel.MotorPort;
            _state.LeftWheel.ReversePolarity = connectToBrick.Body.LeftWheel.ReversePolarity;

            _state.RightWheel.WheelDiameter = connectToBrick.Body.RightWheel.WheelDiameter;
            _state.RightWheel.MotorPort = connectToBrick.Body.RightWheel.MotorPort;
            _state.RightWheel.ReversePolarity = connectToBrick.Body.RightWheel.ReversePolarity;

            _state.Connected = false;

            currentState = _state.Clone();
            Fault fault = null;

            // Registration
            brick.Registration registration = new brick.Registration(
                new LegoNxtConnection((LegoNxtPort)currentState.LeftWheel.MotorPort),
                LegoDeviceType.Actuator,
                Contract.DeviceModel,
                Contract.Identifier,
                ServiceInfo.Service,
                Contract.DeviceModel);

            #region Configure Left Motor

            // Reserve the Left Motor Port
            yield return Arbiter.Choice(_legoBrickPort.ReserveDevicePort(registration),
                delegate(brick.AttachResponse reserveResponse)
                {
                    if (reserveResponse.DeviceModel == registration.DeviceModel)
                        registration.Connection = reserveResponse.Connection;
                    else
                        registration.Connection.Port = LegoNxtPort.NotConnected;
                },
                delegate(Fault f)
                {
                    fault = f;
                    LogError(fault);
                    registration.Connection.Port = LegoNxtPort.NotConnected;
                });


            if (registration.Connection.Port == LegoNxtPort.NotConnected)
            {
                _state.LeftWheel.MotorPort = NxtMotorPort.NotConnected;
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring Drive on Left Motor Port " + connectToBrick.Body.ToString()));
                connectToBrick.ResponsePort.Post(fault);
                yield break;
            }

            brick.AttachRequest attachRequest = new brick.AttachRequest(registration);

            attachRequest.PollingCommands = new NxtCommandSequence(currentState.PollingFrequencyMs,
                new LegoGetOutputState((NxtMotorPort)registration.Connection.Port));

            brick.AttachResponse response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            if (response == null)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring Left Motor on NXT Drive"));

                // Update state
                _state.LeftWheel.MotorPort = NxtMotorPort.NotConnected;

                connectToBrick.ResponsePort.Post(fault);
                yield break;
            }

            if ((LegoNxtPort)currentState.LeftWheel.MotorPort != response.Connection.Port)
            {
                _state.LeftWheel.MotorPort = currentState.LeftWheel.MotorPort = (NxtMotorPort)response.Connection.Port;
            }

            #endregion

            #region Configure Right Motor
            registration.Connection.Port = (LegoNxtPort)connectToBrick.Body.RightWheel.MotorPort;

            // Reserve the Right Motor Port
            yield return Arbiter.Choice(_legoBrickPort.ReserveDevicePort(registration),
                delegate(brick.AttachResponse reserveResponse)
                {
                    if (reserveResponse.DeviceModel == registration.DeviceModel)
                        registration.Connection = reserveResponse.Connection;
                    else
                        registration.Connection.Port = LegoNxtPort.NotConnected;
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
                    fault = Fault.FromException(new Exception("Failure Configuring Drive on Right Motor Port " + connectToBrick.Body.ToString()));
                _state.RightWheel.MotorPort = NxtMotorPort.NotConnected;
                connectToBrick.ResponsePort.Post(fault);
                yield break;
            }

            attachRequest = new brick.AttachRequest(registration);

            attachRequest.PollingCommands = new NxtCommandSequence(currentState.PollingFrequencyMs,
                new LegoGetOutputState((NxtMotorPort)registration.Connection.Port));

            response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            if (response == null)
            {
                if (fault == null)
                    fault = Fault.FromException(new Exception("Failure Configuring Right Motor on NXT Drive"));

                // Update state
                _state.RightWheel.MotorPort = NxtMotorPort.NotConnected;

                connectToBrick.ResponsePort.Post(fault);
                yield break;
            }

            if ((LegoNxtPort)currentState.RightWheel.MotorPort != response.Connection.Port)
            {
                _state.RightWheel.MotorPort = currentState.RightWheel.MotorPort = (NxtMotorPort)response.Connection.Port;
            }

            #endregion

            // If we made it to this point, both motors are connected.
            _state.Connected = true;

            // save and refresh the state.
            currentState = _state.Clone();

            // Send a notification of the connected port
            connectToBrick.Body.LeftWheel.MotorPort = currentState.LeftWheel.MotorPort;
            connectToBrick.Body.RightWheel.MotorPort = currentState.RightWheel.MotorPort;
            SendNotification(_subMgrPort, connectToBrick);

            connectToBrick.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> MainGetHandler(Get get)
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
        public virtual IEnumerator<ITask> MainHttpGetHandler(HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(HttpStatusCode.OK, _state, _transform));
            yield break;
        }

        /// <summary>
        /// AllStop Handler
        /// </summary>
        /// <param name="allStop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> MainAllStopHandler(motor.AllStop allStop)
        {
            DriveDistance drive = new DriveDistance(new SetDriveRequest());
            drive.Body.LeftPower = 0.0;
            drive.Body.RightPower = 0.0;
            drive.Body.LeftStopAtRotationDegrees = 0;
            drive.Body.RightStopAtRotationDegrees = 0;
            drive.Body.StopState = allStop.Body.StopState;
            drive.ResponsePort = allStop.ResponsePort;
            drive.Body.DriveRequestOperation = pxdrive.DriveRequestOperation.AllStop;
            _internalDrivePowerPort.Post(drive);

            allStop.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// DriveDistance Handler
        /// </summary>
        /// <param name="driveDistance"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> MainDriveDistanceHandler(DriveDistance driveDistance)
        {
            _internalDrivePowerPort.Post(driveDistance);
            driveDistance.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// DriveEncodersUpdate Handler
        /// </summary>
        /// <param name="driveEncodersUpdate"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> MainDriveEncodersUpdateHandler(DriveEncodersUpdate driveEncodersUpdate)
        {
            throw new InvalidOperationException("Drive Encoders are updated by the LEGO NXT Hardware.  This operation type is only valid for receiving notifications.");
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> MainSubscribeHandler(Subscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        #endregion

        #region Shared Handlers

        /// <summary>
        /// DsspDefaultDrop Handler
        /// </summary>
        /// <param name="drop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        [ServiceHandler(ServiceHandlerBehavior.Teardown, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> DualDropHandler(DsspDefaultDrop drop)
        {
            // detach from the brick
            _legoBrickPort.Detach(ServiceInfo.Service);

            base.DefaultDropHandler(drop);
            yield break;
        }

        #endregion

        #region Generic Drive Handlers

        /// <summary>
        /// Generic Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericGetHandler(pxdrive.Get get)
        {
            get.ResponsePort.Post(_state.CopyToGenericState(_genericState));
            yield break;
        }

        /// <summary>
        /// Generic HttpGet Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericHttpGetHandler(HttpGet get)
        {
            get.ResponsePort.Post(
                new dssphttp.HttpResponseType(
                    _state.CopyToGenericState(_genericState)));

            yield break;
        }

        /// <summary>
        /// HttpPost Handler
        /// </summary>
        /// <param name="submit"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericHttpPostHandler(dssphttp.HttpPost submit)
        {
            throw new NotImplementedException("HttpPost is not implemented.");
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericSubscribeHandler(pxdrive.Subscribe subscribe)
        {
            base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericReliableSubscribeHandler(pxdrive.ReliableSubscribe subscribe)
        {
            base.SubscribeHelper(_genericSubMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// Update Handler
        /// </summary>
        /// <param name="updateGenericDrive"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericUpdateHandler(pxdrive.Update updateGenericDrive)
        {
            _state.CopyFromGenericState(updateGenericDrive.Body);
            updateGenericDrive.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// EnableDrive Handler
        /// </summary>
        /// <param name="enableDrive"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericEnableDriveHandler(pxdrive.EnableDrive enableDrive)
        {
            _genericState.IsEnabled = enableDrive.Body.Enable;
            _genericState.TimeStamp = DateTime.Now;
            _state.TimeStamp = _genericState.TimeStamp;
            

            SendNotification<pxdrive.EnableDrive>(_genericSubMgrPort, enableDrive);
            enableDrive.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            yield break;

        }

        /// <summary>
        /// SetDrivePower Handler
        /// </summary>
        /// <param name="drivePower"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericSetDrivePowerHandler(pxdrive.SetDrivePower drivePower)
        {
            // set back response immediately or fault if drive is not enabled.
            ValidateDriveEnabledAndRespondHelper(drivePower.ResponsePort);

            DriveDistance drive = new DriveDistance(new SetDriveRequest());
            drive.Body.LeftPower = drivePower.Body.LeftWheelPower;
            drive.Body.RightPower = drivePower.Body.RightWheelPower;
            drive.Body.LeftStopAtRotationDegrees = 0;
            drive.Body.RightStopAtRotationDegrees = 0;
            drive.Body.StopState = MotorStopState.Default;
            drive.Body.isGenericOperation = true;
            drive.ResponsePort = drivePower.ResponsePort;
            _internalDrivePowerPort.Post(drive);
            yield break;
        }

        /// <summary>
        /// SetDriveSpeed Handler
        /// </summary>
        /// <param name="driveSpeed"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericSetDriveSpeedHandler(pxdrive.SetDriveSpeed driveSpeed)
        {
            // set back response immediately or fault if drive is not enabled.
            ValidateDriveEnabledAndRespondHelper(driveSpeed.ResponsePort);

            DriveDistance drive = new DriveDistance(new SetDriveRequest());
            drive.Body.LeftPower = driveSpeed.Body.LeftWheelSpeed;
            drive.Body.RightPower = driveSpeed.Body.RightWheelSpeed;
            drive.Body.LeftStopAtRotationDegrees = 0;
            drive.Body.RightStopAtRotationDegrees = 0;
            drive.Body.StopState = MotorStopState.Default;
            drive.Body.isGenericOperation = true;

            drive.ResponsePort = driveSpeed.ResponsePort;
            _internalDrivePowerPort.Post(drive);
            yield break;
        }

        /// <summary>
        /// RotateDegrees Handler
        /// </summary>
        /// <param name="rotateDegrees"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericRotateDegreesHandler(pxdrive.RotateDegrees rotateDegrees)
        {
            if (_state.DistanceBetweenWheels <= 0)
            {
                rotateDegrees.ResponsePort.Post(Fault.FromException(new ArgumentOutOfRangeException("DistanceBetweenWheels must be specified in the Drive Configuration.")));
                yield break;
            }

            // set back response immediately or fault if drive is not enabled.
            ValidateDriveEnabledAndRespondHelper(rotateDegrees.ResponsePort);

            // distance = circumference / 360 * degreesToTurn
            double distance = Math.PI * _state.DistanceBetweenWheels * rotateDegrees.Body.Degrees / 360.0;

            // axleRotationDegrees = distance (meters) / wheelCircumference (pi * diameter) * 360
            long axleRotationDegrees = (long)Math.Round(Math.Abs(distance) / (Math.PI * _state.LeftWheel.WheelDiameter) * 360.0);

            LogVerbose(LogGroups.Console, "RotateDegrees: Wheel Distance: " + distance.ToString() + "  Axle Rotation Degrees: " + axleRotationDegrees.ToString());

            double leftDirection = 1.0, rightDirection = 1.0;
            if (rotateDegrees.Body.Degrees > 0)
                leftDirection = -1.0;
            else
                rightDirection = -1.0;

            DriveDistance drive = new DriveDistance(new SetDriveRequest());
            drive.Body.LeftStopAtRotationDegrees = axleRotationDegrees;
            drive.Body.RightStopAtRotationDegrees = axleRotationDegrees;
            drive.Body.StopState = MotorStopState.Brake;
            drive.Body.isGenericOperation = true;
            drive.Body.DriveRequestOperation = pxdrive.DriveRequestOperation.RotateDegrees;
            _internalPendingDriveOperation = pxdrive.DriveRequestOperation.RotateDegrees;
            bool synchronized = false;

            if (synchronized)
            {
                drive.Body.LeftPower = Math.Abs(rotateDegrees.Body.Power);
                drive.Body.RightPower = Math.Abs(rotateDegrees.Body.Power);
            }
            else
            {
                drive.Body.LeftPower = rotateDegrees.Body.Power * leftDirection;
                drive.Body.RightPower = rotateDegrees.Body.Power * rightDirection;
            }
            drive.ResponsePort = rotateDegrees.ResponsePort;

            // notify subscribers of rotate degrees start
            rotateDegrees.Body.RotateDegreesStage = pxdrive.DriveStage.Started;
            pxdrive.RotateDegrees rotateDegreesUpdate = new pxdrive.RotateDegrees(rotateDegrees.Body);
            SendNotification<pxdrive.RotateDegrees>(_genericSubMgrPort, rotateDegreesUpdate);

            _internalDrivePowerPort.Post(drive);
            yield break;
        }

        /// <summary>
        /// DriveDistance Handler
        /// </summary>
        /// <param name="driveDistance"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericDriveDistanceHandler(pxdrive.DriveDistance driveDistance)
        {
            double distance = driveDistance.Body.Distance;

            // set back response immediately or fault if drive is not enabled.
            ValidateDriveEnabledAndRespondHelper(driveDistance.ResponsePort);

            // rotations = distance (meters) / circumference (pi * diameter) 
            // degrees = rotations * 360
            double stopLeftWheelAtDegrees = Math.Round(Math.Abs(distance) / (Math.PI * _state.LeftWheel.WheelDiameter) * 360.0);
            double stopRightWheelAtDegrees = Math.Round(Math.Abs(distance) / (Math.PI * _state.RightWheel.WheelDiameter) * 360.0);

            DriveDistance drive = new DriveDistance(new SetDriveRequest());
            drive.Body.LeftPower = driveDistance.Body.Power;
            drive.Body.RightPower = driveDistance.Body.Power;
            drive.Body.LeftStopAtRotationDegrees = (long)stopLeftWheelAtDegrees;
            drive.Body.RightStopAtRotationDegrees = (long)stopRightWheelAtDegrees;
            drive.Body.StopState = MotorStopState.Brake;
            drive.Body.isGenericOperation = true;
            drive.Body.DriveRequestOperation = pxdrive.DriveRequestOperation.DriveDistance;
            _internalPendingDriveOperation = pxdrive.DriveRequestOperation.DriveDistance;
            drive.ResponsePort = driveDistance.ResponsePort;

            // notify subscribers of drive distance start
            driveDistance.Body.DriveDistanceStage = pxdrive.DriveStage.Started;
            pxdrive.DriveDistance driveDistanceUpdate = new pxdrive.DriveDistance(driveDistance.Body);
            SendNotification<pxdrive.DriveDistance>(_genericSubMgrPort, driveDistanceUpdate);

            _internalDrivePowerPort.Post(drive);
            yield break;
        }

        /// <summary>
        /// AllStop Handler
        /// </summary>
        /// <param name="allStop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_drivePort")]
        public virtual IEnumerator<ITask> GenericAllStopHandler(pxdrive.AllStop allStop)
        {
            DriveDistance drive = new DriveDistance(new SetDriveRequest());
            drive.Body.LeftPower = 0.0;
            drive.Body.RightPower = 0.0;
            drive.Body.LeftStopAtRotationDegrees = 0;
            drive.Body.RightStopAtRotationDegrees = 0;
            drive.Body.StopState = MotorStopState.Brake;
            drive.ResponsePort = allStop.ResponsePort;
            drive.Body.DriveRequestOperation = pxdrive.DriveRequestOperation.AllStop;
            _internalDrivePowerPort.Post(drive);

            // disable drive
            _genericState.IsEnabled = false;
            pxdrive.EnableDrive disableDrive = new pxdrive.EnableDrive();
            disableDrive.Body.Enable = _genericState.IsEnabled;
            _genericState.TimeStamp = DateTime.Now;
            _state.TimeStamp = _genericState.TimeStamp;
            SendNotification<pxdrive.EnableDrive>(_genericSubMgrPort, disableDrive);


            _drivePort.Post(disableDrive);
            yield break;
        }

        /// <summary>
        /// Resets the encoder tick count to zero on both wheels
        /// </summary>
        /// <param name="reset">Request message</param>
        [ServiceHandler(PortFieldName = "_drivePort")]
        public void EncoderResetHandler(pxdrive.ResetEncoders reset)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Initialize and validate startup DriveState
        /// </summary>
        private void InitializeState()
        {
            if (_state == null)
                _state = new DriveState();

            if (_state.LeftWheel == null)
                _state.LeftWheel = new WheelConfiguration();

            if (_state.RightWheel == null)
                _state.RightWheel = new WheelConfiguration();

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            if (_state.LeftWheel.WheelDiameter == 0.0)
                _state.LeftWheel.WheelDiameter = 0.055;

            if (_state.RightWheel.WheelDiameter == 0.0)
                _state.RightWheel.WheelDiameter = 0.055;

            // Always initialize the runtime statistics when we start.
            _state.RuntimeStatistics = new RuntimeStatistics();

            _targetEncoderReachedPort[LEFT] = new Port<bool>();
            _targetEncoderReachedPort[RIGHT] = new Port<bool>();

            _state.Connected = false;

            _genericState.IsEnabled = true;
            _internalPendingDriveOperation = pxdrive.DriveRequestOperation.NotSpecified;

        }

        /// <summary>
        /// Convert Motor Port (A-C) to HardwareIdentifier (1-3)
        /// </summary>
        /// <param name="motorPort"></param>
        /// <returns></returns>
        internal static int MotorPortToHardwareIdentifier(NxtMotorPort motorPort)
        {
            // Set the hardware identifier from the connected motor port.
            switch (motorPort)
            {
                case NxtMotorPort.MotorA:
                    return 1;
                case NxtMotorPort.MotorB:
                    return 2;
                case NxtMotorPort.MotorC:
                    return 3;
            }
            return 0;
        }

        /// <summary>
        /// Convert Hardware Identifier (1-3) to MotorPort (A-C)
        /// </summary>
        /// <param name="hardwareIdentifier"></param>
        /// <param name="defaultMotorPort"></param>
        /// <returns></returns>
        internal static NxtMotorPort HardwareIdentifierToMotorPort(int hardwareIdentifier, NxtMotorPort defaultMotorPort)
        {
            NxtMotorPort motorPort;
            switch (hardwareIdentifier)
            {
                case 1:
                    motorPort = NxtMotorPort.MotorA;
                    break;
                case 2:
                    motorPort = NxtMotorPort.MotorB;
                    break;
                case 3:
                    motorPort = NxtMotorPort.MotorC;
                    break;
                default:
                    motorPort = defaultMotorPort;
                    break;
            }

            return motorPort;
        }

        /// <summary>
        /// Post a success or fault to complete a DriveDistance request.
        /// </summary>
        /// <param name="driveDistance"></param>
        /// <param name="success"></param>
        /// <param name="fault"></param>
        private void HandleDriveResponse(DriveDistance driveDistance, bool success, Fault fault)
        {
            if (success)
            {
                LogVerbose(LogGroups.Console, "InternalDrive Completed Successfully");
                

                if (driveDistance.Body.isGenericOperation == true)
                {
                    //notify subscribers of generic drive distance -- completed
                    HandleDriveResponseForGenericOperationsNotifications(driveDistance, success, fault);
                }
                else
                {
                    //notify subscribers of specific drive distance -- completed
                }

            }
            else
            {

                string msg;
                if (fault == null)
                {
                    msg = "The Drive operation was canceled due to a newer drive command.";
                    fault = Fault.FromException(new OperationCanceledException(msg));
                }
                else
                {
                    msg = fault.ToString();
                }
                driveDistance.ResponsePort.Post(fault);
                LogVerbose(LogGroups.Console, msg);
            }
        }

        /// <summary>
        /// Notified subscribers a success or fault to completed Drive request.
        /// </summary>
        /// <param name="driveDistance"></param>
        /// <param name="success"></param>
        /// <param name="fault"></param>
        public void HandleDriveResponseForGenericOperationsNotifications(DriveDistance driveDistance, bool success, Fault fault)
        {
            if (fault == null)
            {
                if (driveDistance.Body.isGenericOperation == true)
                {
                    //notify subscribers of generic drive distance -- complete

                    switch (driveDistance.Body.DriveRequestOperation)
                    {
                        case pxdrive.DriveRequestOperation.DriveDistance:
                            pxdrive.DriveDistanceRequest driveDistanceRequest = new pxdrive.DriveDistanceRequest();
                            driveDistanceRequest.DriveDistanceStage = pxdrive.DriveStage.Completed;

                            pxdrive.DriveDistance driveDistanceUpdate = new pxdrive.DriveDistance(driveDistanceRequest);
                            SendNotification<pxdrive.DriveDistance>(_genericSubMgrPort, driveDistanceUpdate);
                            break;

                        case pxdrive.DriveRequestOperation.RotateDegrees:
                            pxdrive.RotateDegreesRequest rotateDegreesRequest = new pxdrive.RotateDegreesRequest();
                            rotateDegreesRequest.RotateDegreesStage = pxdrive.DriveStage.Completed;

                            pxdrive.RotateDegrees rotateDegreesUpdate = new pxdrive.RotateDegrees(rotateDegreesRequest);
                            SendNotification<pxdrive.RotateDegrees>(_genericSubMgrPort, rotateDegreesUpdate);
                            break;
                    }
                }
                else
                {
                    // Operation canceled.
                    driveDistance.Body.DriveDistanceStage = pxdrive.DriveStage.Canceled;
                    SendNotification<pxdrive.SetDrivePower>(_genericSubMgrPort, driveDistance.Body);
                }
                _internalPendingDriveOperation = pxdrive.DriveRequestOperation.NotSpecified;
            }
        }

        /// <summary>
        /// Calculate the optimal power and target encoder degrees.
        /// </summary>
        /// <param name="drive">SetDriveRequest</param>
        /// <param name="currentState">DriveState</param>
        /// <param name="leftMotorPower"></param>
        /// <param name="rightMotorPower"></param>
        /// <param name="leftTargetEncoderDegrees"></param>
        /// <param name="rightTargetEncoderDegrees"></param>
        private void CalculatePowerAndTargetDegrees(
            SetDriveRequest drive, 
            DriveState currentState, 
            out int leftMotorPower, 
            out int rightMotorPower, 
            out long leftTargetEncoderDegrees, 
            out long rightTargetEncoderDegrees)
        {
            leftTargetEncoderDegrees = Math.Abs(drive.LeftStopAtRotationDegrees);
            rightTargetEncoderDegrees = Math.Abs(drive.RightStopAtRotationDegrees);

            leftMotorPower = motor.NxtMotor.CalculateMaxMotorPower(leftTargetEncoderDegrees, drive.LeftPower, 0.0, 0.0);
            rightMotorPower = motor.NxtMotor.CalculateMaxMotorPower(rightTargetEncoderDegrees, drive.RightPower, 0.0, 0.0);

            // Adjust for Reverse Polarity
            if (currentState.LeftWheel.ReversePolarity)
                leftMotorPower *= -1;
            if (currentState.RightWheel.ReversePolarity)
                rightMotorPower *= -1;

            // Adjust encoder sign to match power.
            if (Math.Sign(leftMotorPower) != Math.Sign(leftTargetEncoderDegrees))
                leftTargetEncoderDegrees *= -1;
            if (Math.Sign(rightMotorPower) != Math.Sign(rightTargetEncoderDegrees))
                rightTargetEncoderDegrees *= -1;

        }

        private bool ValidateDriveEnabledAndRespondHelper(PortSet<DefaultUpdateResponseType, Fault> responsePort)
        {
            Fault fault = null;

            // Acknowledge request or fault
            if (_genericState.IsEnabled == false)
            {
                fault = Fault.FromException(new InvalidOperationException("Attempting to process a drive operation, but the differential drive is not enabled"));
                responsePort.Post(fault);
            }
            else
            {
                responsePort.Post(DefaultUpdateResponseType.Instance);
            }
            return _genericState.IsEnabled;
        }

        #endregion
    }
}
