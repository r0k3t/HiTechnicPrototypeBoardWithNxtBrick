//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceMotor.cs $ $Revision: 38 $
//-----------------------------------------------------------------------

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using W3C.Soap;

using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using pxmotor = Microsoft.Robotics.Services.Motor.Proxy;
using pxencoder = Microsoft.Robotics.Services.Encoder.Proxy;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Motor
{
    
    /// <summary>
    /// Lego NXT Motor Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [AlternateContract(pxmotor.Contract.Identifier)]
    [AlternateContract(pxencoder.Contract.Identifier)]
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Motor and Encoder (v2).")]
    [DisplayName("(User) Lego NXT Motor \u200b(v2)")]
    [DssServiceDescription("http://msdn.microsoft.com/library/bb870564.aspx")]
    [DssCategory(brick.LegoCategories.NXT)]
    public class NxtMotor : DsspServiceBase
    {
        #region Private State

        /// <summary>
        /// Motor State
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.Motor.config.xml")]
        private MotorState _state = new MotorState();
        private bool _initialized = false;

        /// <summary>
        /// Generic State
        /// </summary>
        pxmotor.MotorState _genericState = new pxmotor.MotorState();

        #region RPM tracking
        /// <summary>
        /// RPM Prior Stats
        /// </summary>
        private LegoResponseGetOutputState[] _rpmEncoderList = new LegoResponseGetOutputState[4];
        /// <summary>
        /// RPM Index
        /// </summary>
        private int _rpmIndex = 0;
        #endregion
        
        #endregion

        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/motor", AllowMultipleInstances = true)]
        private MotorOperations _internalMainPort = new MotorOperations();
        private MotorOperations _reliableMainPort = null;

        [EmbeddedResource("Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Motor.user.xslt")]
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
        /// Subscription manager partner
        /// </summary>
        [Partner(Partners.SubscriptionManagerString, Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _subMgrPort = new submgr.SubscriptionManagerPort();

        #endregion

        #region Alternate Contract Ports

        [AlternateServicePort("/generic/motor", AlternateContract = pxmotor.Contract.Identifier)]
        private pxmotor.MotorOperations _motorPort = new pxmotor.MotorOperations();

        [AlternateServicePort("/generic/encoder", AlternateContract = pxencoder.Contract.Identifier)]
        private pxencoder.EncoderOperations _encoderPort = new pxencoder.EncoderOperations();

        #endregion

        #region Internal CCR Ports

        /// <summary>
        /// Internal CCR port for tracking an encoder target
        /// </summary>
        private Port<bool> _targetEncoderReachedPort = null;
        private bool _targetEncoderPending = false;

        /// <summary>
        /// Internal CCR port which handles all motor rotation requests (AllStop, SetMotorRotation, [generic] SetMotorPower)
        /// </summary>
        Port<SetMotorRotation> _internalMotorRotationPort = new Port<SetMotorRotation>();

        /// <summary>
        /// Internal CCR port which implements an rpm smoothing function.
        /// </summary>
        private Port<LegoResponseGetOutputState> _rpmCalcPort = new Port<LegoResponseGetOutputState>();
        #endregion


        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public NxtMotor(DsspServiceCreationPort creationPort) 
            : base(creationPort)
        {
        }
        
        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            InitializeState();
            base.DirectoryInsert();

            // Connect to the brick
            Port<ConnectToBrick> initPort = new Port<ConnectToBrick>();
            initPort.Post(new ConnectToBrick(_state));

            Activate<ITask>(
                // Set up Notification Handler
                Arbiter.ReceiveWithIterator<brick.LegoSensorUpdate>(true, _legoBrickNotificationPort, NotificationHandler),
                // Wait one time for an RPM Calculation request
                Arbiter.ReceiveWithIterator<LegoResponseGetOutputState>(false, _rpmCalcPort, CalculateRpmHandler),
                // Wait one time for an InternalMotorPower command
                Arbiter.ReceiveWithIterator<SetMotorRotation>(false, _internalMotorRotationPort, InternalMotorRotationHandler),
                // Wait one time for a Connect To Brick request on the internal initialization port.
                Arbiter.ReceiveWithIterator<ConnectToBrick>(false, initPort, ConnectToBrickHandler)
            );

            // Set up the reliable port using DSS forwarder to ensure exception and timeout conversion to fault.
            _reliableMainPort = ServiceForwarder<MotorOperations>(this.ServiceInfo.Service);
        }

        /// <summary>
        /// Handle periodic sensor readings from the pxbrick
        /// </summary>
        /// <param name="update"></param>
        private IEnumerator<ITask> NotificationHandler(brick.LegoSensorUpdate update)
        {
            LegoResponseGetOutputState outputState = new LegoResponseGetOutputState(update.Body.CommandData);
            if (outputState.Success && _state.CurrentEncoderTimeStamp < outputState.TimeStamp)
            {
                LogVerbose(LogGroups.Console, outputState.ToString());

                int reversePolaritySign = (_state.ReversePolarity) ? -1 : 1;

                _rpmCalcPort.Post(outputState);

                LogVerbose(LogGroups.Console, string.Format("{0} Encoder: RPM {1} AvgPoll {2}",
                    outputState.TimeStamp.ToString("HH:mm:ss.fffffff"),
                    _state.CurrentMotorRpm,
                    _state.AvgEncoderPollingRateMs,
                    outputState.RegulationMode
                    ));

                long newEncoderDegrees = outputState.EncoderCount * reversePolaritySign;
                long newResettableEncoderDegrees = outputState.ResettableCount * reversePolaritySign;
                bool encodersChanged = (_state.ResetableEncoderDegrees != newResettableEncoderDegrees);

                // Update the current encoder readings as soon as possible.
                _state.CurrentPower = ((double)(outputState.PowerSetPoint * reversePolaritySign)) / 100.0;
                _state.CurrentEncoderTimeStamp = outputState.TimeStamp;
                _state.CurrentEncoderDegrees = newEncoderDegrees;
                _state.ResetableEncoderDegrees = newResettableEncoderDegrees;

                if (encodersChanged)
                {
                    // Prepare the encoder notification
                    pxencoder.UpdateTickCount tickNotification = new pxencoder.UpdateTickCount(
                        new pxencoder.UpdateTickCountRequest(outputState.TimeStamp, (int)newResettableEncoderDegrees));

                    SendNotification<pxencoder.UpdateTickCount>(_subMgrPort, tickNotification);
                }

                // get a snapshot of the current state.
                MotorState currentState = _state.Clone();

                #region Simple PD Control

                if (currentState.TargetEncoderDegrees != 0)
                {
                    double direction = Math.Sign(currentState.TargetPower);
                    double pwrtarget = currentState.TargetPower * direction;
                    double enctarget = currentState.TargetEncoderDegrees * direction;
                    double encoder = outputState.EncoderCount * direction;
                    double rpm = (double)currentState.CurrentMotorRpm * direction;
                    double msUntilNext = currentState.AvgEncoderPollingRateMs;

                    // negative remaining means we have passed the target.
                    double remaining = enctarget - encoder;

                    int goodEnough = (msUntilNext < 100) ? 5 : (msUntilNext < 300) ? 20 : 45;
                    bool targetReached = (remaining <= goodEnough);

                    LogVerbose(LogGroups.Console, string.Format("UpdateState: TargetPower {5} CurrentEncoder {0} {1}  Target {2}  done? {3}  RPM {4} AvgMs {6}",
                        currentState.CurrentEncoderDegrees,
                        currentState.CurrentEncoderTimeStamp,
                        currentState.TargetEncoderDegrees,
                        targetReached,
                        currentState.CurrentMotorRpm,
                        currentState.TargetPower,
                        currentState.AvgEncoderPollingRateMs));

                    // If a prior encoder target was active, signal that it is cancelled.
                    if (_targetEncoderPending && targetReached)
                    {
                        lock (_targetEncoderReachedPort)
                        {
                            if (_targetEncoderPending)
                            {
                                LogVerbose(LogGroups.Console, "Signal target is complete!");
                                _targetEncoderReachedPort.Post(true);
                                _targetEncoderPending = false;
                            }
                        }
                    }

                    // How soon do we want to start slowing down?
                    // This is a factor of how fast and for how long the motor has been building up inertia.
                    double forwardLooking = (pwrtarget < 0.5) ? ((pwrtarget * 150.0) + 20) : ((pwrtarget * 200.0) + 20.0);
                    double degreesAtNextNotification = forwardLooking;
                    if (rpm != 0.0 && msUntilNext != 0.0)
                        // The distance in degrees we will travel at this rpm by the next encoder reading.
                        degreesAtNextNotification = (rpm * 360.0) * msUntilNext / 60000.0;
                    
                    // We are not up-to-speed, wait a little longer before stopping
                    if (rpm < pwrtarget)
                    {
                        if (rpm > 0)
                            forwardLooking *= 0.7;

                        if (rpm < 0)
                            LogVerbose(LogGroups.Console, string.Format("Traveling in the wrong direction: Pwr {0} Enc {1} Tgt {2} RPM {3} Direction {4}  FwdLook: {5}  Remain: {6}",
                                pwrtarget, encoder, enctarget, rpm, direction, forwardLooking, remaining));
                        else
                            LogVerbose(LogGroups.Console, string.Format("Not up to speed: Pwr {0} Enc {1} Tgt {2} RPM {3} Direction {4}  FwdLook: {5}  Remain: {6}",
                                pwrtarget, encoder, enctarget, rpm, direction, forwardLooking, remaining));
                    }
                    else
                    {
                        LogVerbose(LogGroups.Console, string.Format("Targeting: Pwr {0} Enc {1} Tgt {2} RPM {3} Direction {4}  FwdLook: {5}  Remain: {6}",
                            pwrtarget, encoder, enctarget, rpm, direction, forwardLooking, remaining));
                    }

                    if (remaining < Math.Max(degreesAtNextNotification, forwardLooking)
                        || remaining < forwardLooking
                        || rpm <= 0)
                    {
                        LegoOutputMode outputMode = LegoOutputMode.MotorOn | LegoOutputMode.Regulated;
                        LegoRegulationMode regulationMode = LegoRegulationMode.Individual;
                        RunState powerAdjustment = RunState.Constant;
                        long tachoLimit = outputState.EncoderLimit;

                        int powerSetPoint = outputState.PowerSetPoint * (int)direction;
                        if (Math.Abs(remaining) < 5 && rpm == 0)
                        {
                            // If we are completely stopped,
                            // set the brake/coast and disengage active monitoring
                            // in the notification handler
                            _state.TargetEncoderDegrees = 0;
                            powerSetPoint = 0;
                            if (currentState.TargetStopState == MotorStopState.Brake)
                            {
                                outputMode |= LegoOutputMode.Brake;
                            }
                            else
                            {
                                regulationMode = LegoRegulationMode.Idle;
                                powerAdjustment = RunState.Idle;
                            }
                        }
                        else if ( remaining >= 0 &&
                            (    (remaining < 100 && powerSetPoint > 0 && rpm > 30)
                              || (remaining < goodEnough && rpm < 10)))
                        {
                            powerSetPoint = 0;

                            if (remaining < (pwrtarget / 2.0))
                                outputMode |= LegoOutputMode.Brake;
                        }
                        else if (forwardLooking < degreesAtNextNotification 
                            && (rpm >= pwrtarget) /* and up-to-speed */)
                        {
                            // we need to stop before the next encoder reading
                            powerSetPoint = 0;
                            outputMode |= LegoOutputMode.Brake;
                            // The ms until we reach the point where forwardlooking rotations are remaining.
                            double msUntilWeShouldStop = (degreesAtNextNotification - forwardLooking) / ((rpm * 360.0) / 60000.0);
                            yield return Timeout(msUntilWeShouldStop);
                        }
                        else if (remaining < 0)
                        {
                            // Overshot, reverse direction
                            powerSetPoint = CalculateMaxMotorPower((long)-remaining, -currentState.TargetPower, rpm, degreesAtNextNotification) / 2;
                        }
                        else
                        {
                            powerSetPoint = CalculateMaxMotorPower((long)remaining, currentState.TargetPower, rpm, degreesAtNextNotification);
                        }

                        if (degreesAtNextNotification < remaining)
                            powerAdjustment = RunState.RampDown;

                        if ((powerSetPoint != outputState.PowerSetPoint)
                            || (outputState.Mode != outputMode)
                            || (rpm < pwrtarget))
                        {
                            LogVerbose(LogGroups.Console, string.Format("Target: {0}  Encoder: {1}  New Power: {2}",
                                currentState.TargetEncoderDegrees,
                                newEncoderDegrees,
                                powerSetPoint));

                            #region Send an interim LEGO Motor command

                            LegoSetOutputState setOutputState = new LegoSetOutputState(currentState.MotorPort,
                                powerSetPoint,
                                outputMode,
                                regulationMode,
                                0, /* turn ratio */
                                powerAdjustment,
                                tachoLimit);
                            setOutputState.RequireResponse = (rpm == 0.0);
                            LogVerbose(LogGroups.Console, setOutputState.ToString());

                            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(setOutputState),
                                delegate(LegoResponse ok)
                                {
                                    if (ok.Success)
                                        LogVerbose(LogGroups.Console, "Moto Power was set");
                                    else
                                        LogError(LogGroups.Console, "Moto Power Failed: " + ok.ErrorCode.ToString());
                                },
                                delegate(Fault fail)
                                {
                                    LogError(LogGroups.Console, "Moto Power Failed: " + fail.ToString());
                                });
                            #endregion
                        }
                    }
                    else
                    {
                        LogVerbose(LogGroups.Console, string.Format("Remaining: {0}  Current Power: {1}  Forward Looking: {2}",
                            remaining,
                            currentState.TargetPower,
                            forwardLooking));
                    }
                }

                #endregion



            }
            yield break;
        }

        /// <summary>
        /// Exclusive Handler for updating RPM's
        /// </summary>
        /// <param name="encoder"></param>
        /// <returns></returns>
        private IEnumerator<ITask> CalculateRpmHandler(LegoResponseGetOutputState encoder)
        {
            try
            {
                // Save the newest reading and increment our _rpmIndex.
                int ixNewest = _rpmIndex++;
                _rpmIndex %= _rpmEncoderList.Length;
                _rpmEncoderList[ixNewest] = encoder;

                // Do we have a full set of readings?
                if (_rpmEncoderList[_rpmIndex] != null)
                {
                    double ms = _rpmEncoderList[ixNewest].TimeStamp.Subtract(_rpmEncoderList[_rpmIndex].TimeStamp).TotalMilliseconds;
                    double degreesTraveled = (double)(_rpmEncoderList[ixNewest].EncoderCount - _rpmEncoderList[_rpmIndex].EncoderCount);

                    // rpm = (degreesTraveled / 360) / ms * msPerMinute
                    _state.CurrentMotorRpm = (int)(degreesTraveled * 60000.0 / ms / 360.0);
                    _state.AvgEncoderPollingRateMs = ms / (double)(_rpmEncoderList.Length - 1);
                }
            }
            finally
            {
                Activate(Arbiter.ReceiveWithIterator<LegoResponseGetOutputState>(false, _rpmCalcPort, CalculateRpmHandler));
            }
            yield break;
        }

        /// <summary>
        /// Internal Motor Rotation Handler
        /// Exclusive handler which controls all motor commands
        /// Responds to a flood of incoming motor commands by 
        /// only processing the newest command.
        /// </summary>
        /// <param name="setMotorPower"></param>
        /// <returns></returns>
        public virtual IEnumerator<ITask> InternalMotorRotationHandler(SetMotorRotation setMotorPower)
        {
            try
            {
                #region Check for a backlog of motor commands
                // Take a snapshot of the number of pending commands at the time
                // we entered this routine.
                // This will prevent a livelock which can occur if we try to
                // process the queue until it is empty, but the inbound queue
                // is growing faster than we are pulling off the queue.
                int pendingCommands = _internalMotorRotationPort.ItemCount;

                // If newer commands have been issued,
                // respond fault to the older command and
                // move to the newer one.
                SetMotorRotation newerRotationRequest;
                Fault faultNewerCommand = Fault.FromException(new InvalidOperationException("A newer motor command was received before this command was processed."));
                while (pendingCommands > 0)
                {
                    if (_internalMotorRotationPort.Test(out newerRotationRequest))
                    {
                        setMotorPower.ResponsePort.Post(faultNewerCommand);
                        setMotorPower = newerRotationRequest;
                    }
                    pendingCommands--;
                }
                #endregion

                // Get a snapshot of our state.  
                MotorState currentState = _state.Clone();
                Fault faultResponse = null;

                int motorPower;
                long targetEncoderDegrees = 0;
                long targetRotationDegrees;
                CalculatePowerAndTargetDegrees(setMotorPower.Body, currentState.ReversePolarity, 
                    out motorPower,
                    out targetRotationDegrees);

                #region Cancel any prior encoder target

                // If a prior encoder target was active, 
                // signal that it is cancelled by a new Motor command.
                if (_targetEncoderPending)
                {
                    lock (_targetEncoderReachedPort)
                    {
                        if (_targetEncoderPending)
                        {
                            LogVerbose(LogGroups.Console, string.Format("Cancel prior target! New TgtDegrees: {0}", targetRotationDegrees));
                            _targetEncoderReachedPort.Post(false);
                            _targetEncoderPending = false;
                            _state.TargetEncoderDegrees = 0;
                            _state.CurrentEncoderTimeStamp = DateTime.Now;
                        }
                    }
                }
                #endregion

                _state.TargetPower = setMotorPower.Body.TargetPower;

                if (_state.TargetPower == 0)
                    SendNotification<AllStop>(_subMgrPort, new AllStop(setMotorPower.Body.StopState));

                // If rotating for a specific number of rotations, 
                // set up the extended transaction and get the most recent encoder reading.
                if (targetRotationDegrees != 0)
                {

                    #region If encoder reading is old, update the encoder now.
                    if (DateTime.Now.Subtract(currentState.CurrentEncoderTimeStamp).TotalMilliseconds > 45)
                    {
                        LegoGetOutputState getEncoder = new LegoGetOutputState(currentState.MotorPort);
                        getEncoder.TryCount = 2;
                        yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(getEncoder),
                            delegate(LegoResponse legoResponse)
                            {
                                LegoResponseGetOutputState encoderResponse = legoResponse as LegoResponseGetOutputState;
                                if (encoderResponse == null) encoderResponse = new LegoResponseGetOutputState(legoResponse.CommandData);
                                if (encoderResponse.Success)
                                {
                                    int reversePolaritySign = (currentState.ReversePolarity) ? -1 : 1;
                                    _state.CurrentPower = ((double)(encoderResponse.PowerSetPoint * reversePolaritySign)) / 100.0;
                                    _state.CurrentEncoderTimeStamp = encoderResponse.TimeStamp;
                                    _state.CurrentEncoderDegrees = encoderResponse.EncoderCount * reversePolaritySign;
                                    _state.ResetableEncoderDegrees = encoderResponse.ResettableCount * reversePolaritySign;
                                    currentState = _state.Clone();
                                }
                                else
                                {
                                    LogError(LogGroups.Console, "Failed to set motor: " + legoResponse.ErrorCode.ToString());
                                    faultResponse = Fault.FromException(new InvalidOperationException(legoResponse.ErrorCode.ToString()));
                                }
                            },
                            delegate(Fault fault)
                            {
                                LogError(LogGroups.Console, fault.ToString());
                                faultResponse = fault;
                            });

                        if (faultResponse != null)
                        {
                            setMotorPower.ResponsePort.Post(faultResponse);
                            yield break;
                        }
                    }
                    #endregion

                    // Calcuate a new target encoder which is based on the current encoder position.
                    targetEncoderDegrees = targetRotationDegrees + currentState.CurrentEncoderDegrees;

                    // A target of zero diables the PD control.  Make sure our result isn't 0.
                    if (targetEncoderDegrees == 0) targetEncoderDegrees++;

                    LogVerbose(LogGroups.Console, string.Format("*** Target Degrees {0} Encoder {1}", targetRotationDegrees, targetEncoderDegrees));
                    _state.TargetEncoderDegrees = targetEncoderDegrees;
                    _state.TargetStopState = setMotorPower.Body.StopState;
                    lock (_targetEncoderReachedPort)
                    {
                        _targetEncoderPending = true;
                    }
                }
                else
                {
                    _state.TargetEncoderDegrees = 0;
                }

                LogVerbose(LogGroups.Console, string.Format("{0} SetMotorPower: {1}  Encoder: {2}  AdjPwr: {3}  TgtRot: {4}  TgtEnc {5}",
                    DateTime.Now.ToString("HH:mm:ss.fffffff"),
                    motorPower,
                    currentState.CurrentEncoderDegrees,
                    motorPower,
                    targetRotationDegrees,
                    targetEncoderDegrees));

                #region Set up the LEGO Motor Command

                // If we aren't polling, let the motor regulate it's own distance.
                int motorRotationLimit = 0;
                if (targetRotationDegrees != 0 && (currentState.PollingFrequencyMs <= 0 || currentState.PollingFrequencyMs > 100))
                    motorRotationLimit = (int)targetRotationDegrees;

                LegoRegulationMode regulationMode = LegoRegulationMode.Individual;
                LegoOutputMode outputMode = LegoOutputMode.MotorOn | LegoOutputMode.Regulated;
                RunState powerAdjustment = (setMotorPower.Body.RampUp && targetRotationDegrees != 0) ? RunState.RampUp : RunState.Constant;
                
                // Are we stopping?
                if (setMotorPower.Body.TargetPower == 0.0)
                {
                    motorRotationLimit = 0;
                    switch (setMotorPower.Body.StopState)
                    {
                        case MotorStopState.Brake:
                            outputMode = LegoOutputMode.MotorOn | LegoOutputMode.Regulated | LegoOutputMode.Brake;
                            powerAdjustment = RunState.Constant;
                            break;
                        case MotorStopState.Coast:
                            powerAdjustment = RunState.Idle;
                            regulationMode = LegoRegulationMode.Idle;
                            break;
                    }
                }

                // Set up the LEGO motor command
                LegoSetOutputState motorCmd = new LegoSetOutputState(
                    currentState.MotorPort,
                    motorPower,
                    outputMode,
                    regulationMode,
                    0,  /* Turn ratio */
                    powerAdjustment,
                    motorRotationLimit);

                #endregion

                #region Send the LEGO Motor Command

                bool abort = false;
                LogVerbose(LogGroups.Console, motorCmd.ToString());
                yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(motorCmd),
                    delegate(LegoResponse response)
                    {
                        if (!response.Success)
                        {
                            LogError(LogGroups.Console, "Invalid SetMotorOutput " + response.ErrorCode.ToString());
                            abort = true;
                            setMotorPower.ResponsePort.Post(
                                Fault.FromException(
                                    new InvalidOperationException(response.ErrorCode.ToString())));
                        }
                    },
                    delegate(Fault fault)
                    {
                        abort = true;
                        LogError(LogGroups.Console, "Fault in SetMotorOutput " + fault.ToString());
                        setMotorPower.ResponsePort.Post(fault);
                    });

                #endregion

                if (abort)
                    yield break;

                // Send a notification that the power has been set.
                SendNotification<SetMotorRotation>(_subMgrPort, setMotorPower);

                // Wait for the target encoder to either be cancelled or reach its target.
                if (_targetEncoderPending)
                {
                    LogVerbose(LogGroups.Console, "Waiting for motor to reach target...");

                    // Wait for the target encoder to either be cancelled or reached.
                    Activate(Arbiter.Receive<bool>(false, _targetEncoderReachedPort,
                        delegate(bool finished)
                        {
                            RespondToMotorPowerRequest(setMotorPower.ResponsePort, finished);
                        }));
                }
                else
                {
                    RespondToMotorPowerRequest(setMotorPower.ResponsePort, true);
                }
                yield break;
            }
            finally
            {
                Activate(Arbiter.ReceiveWithIterator<SetMotorRotation>(false, _internalMotorRotationPort, InternalMotorRotationHandler));
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
        /// ConnectToBrick Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> ConnectToBrickHandler(ConnectToBrick update)
        {

            // Validate the motor port.
            if ((update.Body.MotorPort & NxtMotorPort.AnyMotorPort)
                != update.Body.MotorPort)
            {
                update.ResponsePort.Post(
                    Fault.FromException(
                        new ArgumentException(
                            string.Format("Invalid Motor Port: {0}",
                                ((LegoNxtPort)update.Body.MotorPort)))));

                yield break;
            }

            Fault fault = null;
            _state.Connected = false;
            MotorState currentState = _state.Clone();

            // Registration
            brick.Registration registration = new brick.Registration(
                new LegoNxtConnection((LegoNxtPort)update.Body.MotorPort),
                LegoDeviceType.Actuator,
                Contract.DeviceModel,
                Contract.Identifier,
                ServiceInfo.Service,
                _state.Name);

            // Reserve the port
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
                string genericError = string.Format("Failure Configuring Motor on Port {0}", update.Body.MotorPort);
                if (fault == null)
                    fault = Fault.FromException(new Exception(genericError));
                else 
                {
                    if (fault.Reason == null) fault.Reason = new ReasonText[1];
                    if (fault.Reason[0] == null) fault.Reason[0] = new ReasonText();
                    if (string.IsNullOrEmpty(fault.Reason[0].Value)) fault.Reason[0].Value = genericError;
                }

                LogError(LogGroups.Console, fault.Reason[0].Value);
                update.ResponsePort.Post(fault);
                yield break;
            }

            brick.AttachRequest attachRequest = new brick.AttachRequest(registration);

            attachRequest.InitializationCommands = new NxtCommandSequence(
                new LegoResetMotorPosition((NxtMotorPort)registration.Connection.Port, false));

            attachRequest.PollingCommands = new NxtCommandSequence(currentState.PollingFrequencyMs,
                new LegoGetOutputState((NxtMotorPort)registration.Connection.Port));

            brick.AttachResponse response = null;

            yield return Arbiter.Choice(_legoBrickPort.AttachAndSubscribe(attachRequest, _legoBrickNotificationPort),
                delegate(brick.AttachResponse rsp) { response = rsp; },
                delegate(Fault f) { fault = f; });

            if (response == null)
            {
                string genericError = string.Format("Failure Configuring Motor on Port {0}", update.Body.MotorPort);
                if (fault == null)
                    fault = Fault.FromException(new Exception(genericError));
                else
                {
                    if (fault.Reason == null) fault.Reason = new ReasonText[1];
                    if (fault.Reason[0] == null) fault.Reason[0] = new ReasonText();
                    if (string.IsNullOrEmpty(fault.Reason[0].Value)) fault.Reason[0].Value = genericError;
                }
                LogError(LogGroups.Console, fault.Reason[0].Value);
                update.ResponsePort.Post(fault);
                yield break;
            }

            _state.ReversePolarity = update.Body.ReversePolarity;
            _state.PollingFrequencyMs = (update.Body.PollingFrequencyMs == 0) ? Contract.DefaultPollingFrequencyMs : update.Body.PollingFrequencyMs;

            _state.Connected = (response.Connection.Port != LegoNxtPort.NotConnected);
            if (_state.Connected)
                _state.MotorPort = (NxtMotorPort)response.Connection.Port;

            // Set the motor name
            if (!string.IsNullOrEmpty(update.Body.Name))
                _state.Name = update.Body.Name;
            else if (string.IsNullOrEmpty(currentState.Name) 
                || (currentState.Name.StartsWith("Motor") && currentState.Name.Length == 6))
                _state.Name = response.Connection.ToString();

            // Send a notification of the connected port
            update.Body.Name = _state.Name;
            update.Body.PollingFrequencyMs = _state.PollingFrequencyMs;
            update.Body.MotorPort = _state.MotorPort;
            update.Body.ReversePolarity = _state.ReversePolarity;
            SendNotification<ConnectToBrick>(_subMgrPort, update);

            update.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            LogVerbose(string.Format("Motor is attached on port {0}", update.Body.MotorPort));

            if (!_initialized)
            {
                _initialized = true;
                base.ActivateDsspOperationHandlers();
            }
            yield break;
        }


        /// <summary>
        /// Stop Handler
        /// </summary>
        /// <param name="stop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> AllStopHandler(AllStop stop)
        {
            if (!ValidateConnection(_state, stop.ResponsePort))
                yield break;

            SetMotorRotation setMotorRotation = new SetMotorRotation(new SetMotorRotationRequest(0.0));
            setMotorRotation.Body.StopState = stop.Body.StopState;
            _internalMotorRotationPort.Post(setMotorRotation);

            yield return Arbiter.Choice(setMotorRotation.ResponsePort,
                delegate(DefaultUpdateResponseType response)
                {
                    stop.ResponsePort.Post(response);
                },
                delegate(Fault fault)
                {
                    stop.ResponsePort.Post(fault);
                });

            yield break;
        }

        /// <summary>
        /// SetMotorRotation Handler
        /// </summary>
        /// <param name="setMotorRotation"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SetMotorRotationHandler(SetMotorRotation setMotorRotation)
        {
            if (!ValidateConnection(_state, setMotorRotation.ResponsePort))
                yield break;

            _internalMotorRotationPort.Post(setMotorRotation);
            yield break;
        }

        /// <summary>
        /// RotateForDuration Handler
        /// </summary>
        /// <param name="rotateForDuration"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> RotateForDurationHandler(RotateForDuration rotateForDuration)
        {
            if (!ValidateConnection(_state, rotateForDuration.ResponsePort))
                yield break;

            // Send a notification that the power has been set.
            SendNotification<RotateForDuration>(_subMgrPort, rotateForDuration);

            SetMotorRotation setMotorRotation = new SetMotorRotation(new SetMotorRotationRequest(rotateForDuration.Body.TargetPower));
            _internalMotorRotationPort.Post(setMotorRotation);

            yield return Timeout(rotateForDuration.Body.StopAfterMs);
                Arbiter.Receive(false, TimeoutPort(new TimeSpan((long)(10000.0 * rotateForDuration.Body.StopAfterMs))), EmptyHandler<DateTime>);

            setMotorRotation = new SetMotorRotation(new SetMotorRotationRequest(0.0), rotateForDuration.ResponsePort);
            setMotorRotation.Body.StopState = rotateForDuration.Body.StopState;
            _internalMotorRotationPort.Post(setMotorRotation);
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


        #endregion

        #region Encoder Handlers

        /// <summary>
        /// ReliableSubscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_encoderPort")]
        public virtual IEnumerator<ITask> EncoderReliableSubscribeHandler(pxencoder.ReliableSubscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_encoderPort")]
        public virtual IEnumerator<ITask> EncoderSubscribeHandler(pxencoder.Subscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_encoderPort")]
        public virtual IEnumerator<ITask> EncoderGetHandler(pxencoder.Get get)
        {
            int hardwareIdentifier = _state.ToGenericState(_genericState).HardwareIdentifier;
            pxencoder.EncoderState encoderState = new pxencoder.EncoderState(hardwareIdentifier, (int)_state.CurrentEncoderDegrees);
            get.ResponsePort.Post(encoderState);
            yield break;
        }

        /// <summary>
        /// Encoder Reset Handler
        /// </summary>
        /// <param name="resetEncoder"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_encoderPort")]
        public virtual IEnumerator<ITask> EncoderResetHandler(pxencoder.Reset resetEncoder)
        {
            if (!ValidateConnection(_state, resetEncoder.ResponsePort))
                yield break;

            // Reset the encoder relative to the last power command.
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(new LegoResetMotorPosition(_state.MotorPort, true)),
                delegate(LegoResponse response) 
                {
                    // The Current encoder will be updated the next time we poll.
                    SendNotification<pxencoder.Reset>(_subMgrPort, resetEncoder);
                    resetEncoder.ResponsePort.Post(DefaultUpdateResponseType.Instance); 
                },
                delegate(Fault fault) { resetEncoder.ResponsePort.Post(fault); });

            yield break;
        }

        /// <summary>
        /// UpdateTickCount Handler
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_encoderPort")]
        public virtual IEnumerator<ITask> EncoderUpdateTickCountHandler(pxencoder.UpdateTickCount update)
        {
            throw new InvalidOperationException("Outbound sensor notifications are not valid for sending requests.");
        }

        /// <summary>
        /// Replace Handler
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_encoderPort")]
        public virtual IEnumerator<ITask> EncoderReplaceHandler(pxencoder.Replace replace)
        {
            _state.ResetableEncoderDegrees = replace.Body.CurrentReading;
            _state.CurrentEncoderTimeStamp = replace.Body.TimeStamp;
            SendNotification<pxencoder.Replace>(_subMgrPort, replace);
            replace.ResponsePort.Post(DefaultReplaceResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// HttpGet Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_encoderPort")]
        public virtual IEnumerator<ITask> EncoderHttpGetHandler(HttpGet get)
        {
            int hardwareIdentifier = _state.ToGenericState(_genericState).HardwareIdentifier;
            pxencoder.EncoderState encoderState = new pxencoder.EncoderState(hardwareIdentifier, (int)_state.CurrentEncoderDegrees);
            get.ResponsePort.Post(new dssphttp.HttpResponseType(encoderState));
            yield break;
        }

        #endregion

        #region Generic Motor Handlers

        /// <summary>
        /// Motor Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_motorPort")]
        public virtual IEnumerator<ITask> GenericMotorGetHandler(pxmotor.Get get)
        {
            get.ResponsePort.Post(_state.ToGenericState(_genericState));
            yield break;
        }


        /// <summary>
        /// Replace Handler
        /// </summary>
        /// <param name="replace"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive, PortFieldName = "_motorPort")]
        public virtual IEnumerator<ITask> GenericMotorReplaceHandler(pxmotor.Replace replace)
        {
            _state.CopyFromGenericState(replace.Body);
            replace.ResponsePort.Post(DefaultReplaceResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// HttpGet Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_motorPort")]
        public virtual IEnumerator<ITask> GenericMotorHttpGetHandler(HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(_state.ToGenericState(_genericState)));
            yield break;
        }


        /// <summary>
        /// SetMotorPower Handler
        /// </summary>
        /// <param name="genericMotorPower"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent, PortFieldName = "_motorPort")]
        public virtual IEnumerator<ITask> GenericSetMotorPowerHandler(pxmotor.SetMotorPower genericMotorPower)
        {
            if (!ValidateConnection(_state, genericMotorPower.ResponsePort))
                yield break;

            SetMotorRotation setMotorRotation = new SetMotorRotation(new SetMotorRotationRequest(genericMotorPower.Body.TargetPower));
            _internalMotorRotationPort.Post(setMotorRotation);

            yield return Arbiter.Choice(setMotorRotation.ResponsePort,
                delegate(DefaultUpdateResponseType response)
                {
                    SendNotification<pxmotor.SetMotorPower>(_subMgrPort, genericMotorPower);
                    genericMotorPower.ResponsePort.Post(response);
                },
                delegate(Fault fault)
                {
                    genericMotorPower.ResponsePort.Post(fault);
                });

            yield break;
        }
        #endregion

        #region Internal Methods
        
        /// <summary>
        /// Initialize starting state
        /// </summary>
        private void InitializeState()
        {
            if (_state == null)
            {
                _state = new MotorState();
                _state.MotorPort = NxtMotorPort.AnyMotorPort;
            }
            else
            {
                _state.ResetableEncoderDegrees = 0;
                _state.CurrentEncoderDegrees = 0;
                _state.CurrentEncoderTimeStamp = DateTime.MinValue;
                _state.CurrentMotorRpm = 0;
                _state.AvgEncoderPollingRateMs = 0.0;
                _state.CurrentPower = 0.0;
                _state.TargetEncoderDegrees = 0;
                _state.TargetPower = 0;
                _state.TargetStopState = MotorStopState.Default;
            }

            if (_state.PollingFrequencyMs == 0)
                _state.PollingFrequencyMs = Contract.DefaultPollingFrequencyMs;

            _state.Connected = false;
            _targetEncoderPending = false;
            _targetEncoderReachedPort = new Port<bool>();
        }


        /// <summary>
        /// Validate the Motor Connection
        /// Post a Fault if the Motor is not connected.
        /// When ValidateConnection returns false, 
        /// the calling handler should exit immediately
        /// without posting to the response port.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="responsePort"></param>
        /// <returns></returns>
        private bool ValidateConnection(MotorState state, IPort responsePort)
        {
            if (!state.Connected)
            {
                responsePort.PostUnknownType(
                    Fault.FromException(
                        new InvalidOperationException("The LEGO NXT Motor is not connected to a brick")));

                return false;
            }
            return true;
        }

        /// <summary>
        /// Calculate the starting power and target degrees
        /// </summary>
        /// <param name="request"></param>
        /// <param name="reversePolarity"></param>
        /// <param name="motorPower"></param>
        /// <param name="targetRotationDegrees"></param>
        private void CalculatePowerAndTargetDegrees(SetMotorRotationRequest request, bool reversePolarity, out int motorPower, out long targetRotationDegrees)
        {
            targetRotationDegrees = (long)Math.Max(Math.Abs(request.StopAfterDegrees), Math.Abs(request.StopAfterRotations * 360.0));

            motorPower = CalculateMaxMotorPower(targetRotationDegrees, request.TargetPower, _state.CurrentMotorRpm, _state.AvgEncoderPollingRateMs);

            if (reversePolarity)
                motorPower *= -1;

            if (Math.Sign(motorPower) != Math.Sign(targetRotationDegrees))
                targetRotationDegrees *= -1;
        }

        /// <summary>
        /// Calculate the maximum allowable target motor power (+-100)
        /// based on the remaining degrees to travel.
        /// </summary>
        /// <param name="degreesRemaining">Negative values indicate we are past the target.</param>
        /// <param name="targetPower"></param>
        /// <param name="rpm"></param>
        /// <param name="degreesAtNextNotification"></param>
        /// <returns></returns>
        internal static int CalculateMaxMotorPower(long degreesRemaining, double targetPower, double rpm, double degreesAtNextNotification)
        {
            int motorPower = Math.Min(100, Math.Max(-100, (int)(targetPower * 100.0)));

            // Not stopping
            if (degreesRemaining == 0)
                return motorPower;

            // overshot
            if (degreesRemaining < 0)
                return 0;

            // Less than half a turn
            if (degreesRemaining <= 180)
                return Math.Min(Math.Abs(motorPower), (int)degreesRemaining / 7) * Math.Sign(targetPower);

            if (degreesRemaining <= 270)
                return Math.Min(Math.Abs(motorPower), (int)degreesRemaining / 6) * Math.Sign(targetPower);

            // Max power is 20% of the target degrees.
            if (degreesRemaining < Math.Abs(5 * motorPower))
                return Math.Min(Math.Abs(motorPower), (int)degreesRemaining / 5) * Math.Sign(targetPower);
            
            if (degreesAtNextNotification != 0 && degreesRemaining < degreesAtNextNotification)
                return Math.Min(Math.Abs(motorPower / 3), (int)(10.0 * (double)motorPower / degreesAtNextNotification)) * Math.Sign(targetPower);
            
            return motorPower;
        }

        /// <summary>
        /// Send a success or failure SetMotorPower response. 
        /// </summary>
        /// <param name="responsePort"></param>
        /// <param name="success"></param>
        private void RespondToMotorPowerRequest(PortSet<DefaultUpdateResponseType, Fault> responsePort, bool success)
        {
            if (success)
            {
                LogVerbose(LogGroups.Console, "SetMotorPower Completed Successfully");
                responsePort.Post(DefaultUpdateResponseType.Instance);
            }
            else
            {
                string msg = "The motor operation was cancelled by a newer motor request.";
                responsePort.Post(Fault.FromException(new OperationCanceledException(msg)));
                LogVerbose(LogGroups.Console, msg);
            }
        }

        #endregion
    }
}
