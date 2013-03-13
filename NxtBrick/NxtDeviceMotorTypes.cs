//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceMotorTypes.cs $ $Revision: 25 $
//-----------------------------------------------------------------------

using Microsoft.Dss.Core.Attributes;
using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core;
using System.Xml.Serialization;
using Microsoft.Robotics.Services.Motor.Proxy;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using W3C.Soap;
using Microsoft.Dss.ServiceModel.Dssp;
using pxmotor = Microsoft.Robotics.Services.Motor.Proxy;
using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using System.ComponentModel;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Motor
{
    
    /// <summary>
    /// LegoNxtMotor Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the LegoNxtMotor service
        [DataMember, Description("Identifies the Unique Contract Identifier for the LegoNxtMotor service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/motor.user.html";

        /// <summary>
        /// The Unique Device Type
        /// </summary>
        [DataMember, Description("Identifies the Nxt Motor Device Type.")]
        public const string DeviceModel = "NxtMotor";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 100;

    }

    /// <summary>
    /// LEGO NXT Motor State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Motor state.")]
    public class MotorState 
    {
        /// <summary>
        /// Is the Motor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// Specifies the descriptive identifier for the motor.
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Motor.")]
        public string Name;

        /// <summary>
        /// LEGO NXT Motor Port.
        /// </summary>
        [DataMember, Description("Specifies the LEGO NXT Motor Port.")]
        public NxtMotorPort MotorPort;

        /// <summary>
        /// Indicates the direction (polarity) of the motor.
        /// (Enabling this option (true) reverses the motor)
        /// </summary>
        [DataMember, Description(@"Indicates the direction (polarity) of the motor.\n(Enabling this option (true) reverses the motor.)")]
        public bool ReversePolarity;

        /// <summary>
        /// Polling Freqency Milliseconds (0-N, -1 disabled)
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds. \n(0 = default; -1 = disabled; > 0 = ms)")]
        public int PollingFrequencyMs;

        /// <summary>
        /// Motor Encoder. Current reading in degrees since the last user reset.
        /// </summary>
        [DataMember, Description("Identifies the current value of the Motor Encoder (degrees) since the last user reset.")]
        [Browsable(false)]
        public long ResetableEncoderDegrees;

        /// <summary>
        /// Motor Encoder.  Current reading in degrees since the NXT was activated.
        /// </summary>
        [DataMember, Description("Identifies the current value of the Motor Encoder (degrees) since the NXT was activated.")]
        [Browsable(false)]
        public long CurrentEncoderDegrees;

        /// <summary>
        /// Current Encoder TimeStamp.
        /// </summary>
        [DataMember, Description("Identifies the time of the last Encoder reading.")]
        [Browsable(false)]
        public DateTime CurrentEncoderTimeStamp;

        /// <summary>
        /// The Current Motor Speed (RPM).
        /// </summary>
        [DataMember, Description("Identifies the current Motor Speed (RPM).")]
        [Browsable(false)]
        public Int32 CurrentMotorRpm;

        /// <summary>
        /// The average polling rate (ms).
        /// </summary>
        [DataMember, Description("Identifies the average polling rate (ms).")]
        [Browsable(false)]
        public double AvgEncoderPollingRateMs;

        /// <summary>
        /// Indicates the current power applied to the motor; range is -1.0 to 1.0.
        /// </summary>
        [DataMember, Description("Indicates the current power applied to the motor; range is -1.0 to 1.0.")]
        [Browsable(false)]
        [DataMemberConstructor(Order = -1)]
        public double CurrentPower;

        /// <summary>
        /// Indicates the motor power which was last requested. (range is -1.0 to 1.0)
        /// </summary>
        [DataMember, Description("Indicates the motor power which was last requested. \n(range is -1.0 to 1.0)")]
        [Browsable(false)]
        [DataMemberConstructor(Order = -1)]
        public double TargetPower;

        /// <summary>
        /// Current Motor Encoder Target (0-No Target).
        /// </summary>
        [DataMember, Description("Identifies the current Motor Encoder Target (0-No Target).")]
        [Browsable(false)]
        public long TargetEncoderDegrees;

        /// <summary>
        /// Specifies how to stop after the Target Encoder degrees are reached.
        /// Valid when Stopping after Degrees or Rotations.
        /// </summary>
        [DataMember, Description("Identifies how to stop after the Target Encoder degrees are reached. \nValid when Stopping after Degrees or Rotations.")]
        [Browsable(false)]
        public MotorStopState TargetStopState;


        /// <summary>
        /// Transform current state to the specified generic state.
        /// </summary>
        /// <param name="genericState"></param>
        /// <returns></returns>
        public pxmotor.MotorState ToGenericState(pxmotor.MotorState genericState)
        {
            genericState.Name = this.Name;

            switch (this.MotorPort)
            {
                case NxtMotorPort.MotorA:
                    genericState.HardwareIdentifier = 1;
                    break;
                case NxtMotorPort.MotorB:
                    genericState.HardwareIdentifier = 2;
                    break;
                case NxtMotorPort.MotorC:
                    genericState.HardwareIdentifier = 3;
                    break;
                default:
                    genericState.HardwareIdentifier = 0;
                    break;
            }

            genericState.CurrentPower = this.TargetPower;
            genericState.ReversePolarity = this.ReversePolarity;
            genericState.PowerScalingFactor = 100.0;
            return genericState;
        }

        

        /// <summary>
        /// Clone the LEGO NXT Motor State
        /// </summary>
        /// <returns></returns>
        internal MotorState Clone()
        {
            MotorState clone = new MotorState();
            clone.Connected = this.Connected;
            clone.CurrentEncoderDegrees = this.CurrentEncoderDegrees;
            clone.CurrentEncoderTimeStamp = this.CurrentEncoderTimeStamp;
            clone.CurrentMotorRpm = this.CurrentMotorRpm;
            clone.AvgEncoderPollingRateMs = this.AvgEncoderPollingRateMs;
            clone.CurrentPower = this.CurrentPower;
            clone.PollingFrequencyMs = this.PollingFrequencyMs;
            clone.MotorPort = this.MotorPort;
            clone.Name = this.Name;
            clone.ResetableEncoderDegrees = this.ResetableEncoderDegrees;
            clone.ReversePolarity = this.ReversePolarity;
            clone.TargetEncoderDegrees = this.TargetEncoderDegrees;
            clone.TargetPower = this.TargetPower;
            clone.TargetStopState = this.TargetStopState;
            return clone;
        }

        /// <summary>
        /// Copy generic state to this state, transforming data members.
        /// </summary>
        /// <param name="genericState"></param>
        public void CopyFromGenericState(pxmotor.MotorState genericState)
        {
            if (!string.IsNullOrEmpty(genericState.Name))
                this.Name = genericState.Name;

            switch (genericState.HardwareIdentifier)
            {
                case 1:
                    this.MotorPort = NxtMotorPort.MotorA;
                    break;
                case 2:
                    this.MotorPort = NxtMotorPort.MotorB;
                    break;
                case 3:
                    this.MotorPort = NxtMotorPort.MotorC;
                    break;
            }

            this.TargetPower = genericState.CurrentPower;
            this.ReversePolarity = genericState.ReversePolarity;
        }
    }

    /// <summary>
    /// Rotate the LEGO Motor at the specified motor power.
    /// Optionally stop afer the specified degrees.
    /// </summary>
    [DataContract, Description("Rotates the LEGO Motor at the specified motor power. \nOptionally stop afer the specified degrees.")]
    public class SetMotorRotationRequest
    {
        #region Constructors

        /// <summary>
        /// Rotate the LEGO Motor at the specified motor power.
        /// Optionally stop afer the specified degrees.
        /// </summary>
        public SetMotorRotationRequest()
        {
            this.TargetPower = 0.0;
            this.StopAfterDegrees = 0.0;
            this.StopAfterRotations = 0.0;
        }

        /// <summary>
        /// Rotate the LEGO Motor at the specified motor power.
        /// Optionally stop afer the specified degrees.
        /// </summary>
        /// <param name="targetPower"></param>
        public SetMotorRotationRequest(double targetPower)
        {
            this.TargetPower = targetPower;
            this.StopAfterDegrees = 0.0;
            this.StopAfterRotations = 0.0;
        }

        /// <summary>
        /// Rotate the LEGO Motor at the specified motor power.
        /// Optionally stop afer the specified degrees.
        /// </summary>
        /// <param name="targetPower"></param>
        /// <param name="stopAfterDegrees"></param>
        public SetMotorRotationRequest(double targetPower, double stopAfterDegrees)
        {
            this.TargetPower = targetPower;
            this.StopAfterDegrees = stopAfterDegrees;
            this.StopAfterRotations = stopAfterDegrees / 360.0;
        }

        #endregion

        /// <summary>
        /// Target Power (-1.0 - 1.0)
        /// </summary>
        [DataMember, Description("Specifies the Target Power (-1.0 - 1.0)")]
        [DataMemberConstructor(Order = 1)]
        public double TargetPower;

        /// <summary>
        /// Gradually Ramp Up the power until the target Power is reached.
        /// </summary>
        [DataMember, Description("Requests a gradual Ramp Up of the power until the target Power is reached.")]
        public bool RampUp;

        /// <summary>
        /// Stop Motor after the specified Degrees (0-ignore).
        /// </summary>
        [DataMember, Description("Stops the Motor after the specified Degrees (0-ignore).")]
        [DataMemberConstructor(Order = 2)]
        public double StopAfterDegrees;

        /// <summary>
        /// Stop Motor after the specified Rotations (0-ignore).
        /// </summary>
        [DataMember, Description("Stops the Motor after the specified Rotations (0-ignore).")]
        [DataMemberConstructor(Order = 2)]
        public double StopAfterRotations;

        /// <summary>
        /// Stop State
        /// </summary>
        [DataMember, Description("Stops by applying brakes or coasting. \nValid when Stopping after Degrees or Rotations.")]
        public MotorStopState StopState;
    }

    /// <summary>
    /// Rotate the LEGO Motor at the specified motor power for the specified duration.
    /// </summary>
    [DataContract, Description("Rotates the LEGO Motor at the specified motor power for the specified duration.")]
    public class RotateForDurationRequest
    {
        #region Constructors

        /// <summary>
        /// Rotate the LEGO Motor at the specified motor power for the specified duration.
        /// </summary>
        public RotateForDurationRequest()
        {
            this.TargetPower = 0.0;
            this.StopAfterMs = 0.0;
        }

        /// <summary>
        /// Rotate the LEGO Motor at the specified motor power for the specified duration.
        /// </summary>
        /// <param name="targetPower"></param>
        /// <param name="stopAfterMs"></param>
        public RotateForDurationRequest(double targetPower, double stopAfterMs)
        {
            this.TargetPower = targetPower;
            this.StopAfterMs = stopAfterMs;
        }

        #endregion

        /// <summary>
        /// Target Power (-1.0 - 1.0)
        /// </summary>
        [DataMember, Description("Specifies the Target Power (-1.0 - 1.0)")]
        [DataMemberConstructor(Order = 1)]
        public double TargetPower;

        /// <summary>
        /// Gradually Ramp Up the power until the target Power is reached.
        /// </summary>
        [DataMember, Description("Requests the motor to gradually Ramp Up the power until the target Power is reached.")]
        public bool RampUp;

        /// <summary>
        /// Stop Motor after the specified duration (ms).
        /// </summary>
        [DataMember, Description("Stops Motor after the specified duration (ms).")]
        [DataMemberConstructor(Order = 2)]
        public double StopAfterMs;

        /// <summary>
        /// Stop State
        /// </summary>
        [DataMember, Description("Stops by applying brakes or coasting.")]
        public MotorStopState StopState;
    }

    /// <summary>
    /// Stop Request
    /// </summary>
    [DataContract, Description("Requests to Stop a motor.")]
    [DataMemberConstructor]
    public class AllStopRequest
    {
        /// <summary>
        /// Stop Request
        /// </summary>
        public AllStopRequest() { }
        /// <summary>
        /// Stop Request
        /// </summary>
        /// <param name="motorStopState"></param>
        public AllStopRequest(MotorStopState motorStopState) { this.StopState = motorStopState; }

        /// <summary>
        /// Stop State
        /// </summary>
        [DataMember, Description("Stops by applying brakes or coasting.")]
        public MotorStopState StopState;
    }



    
    /// <summary>
    /// Get the LEGO Motor State
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Motor.")]
    public class Get : Get<GetRequestType, PortSet<MotorState, Fault>>
    {
    }

    /// <summary>
    /// Stop Motor
    /// </summary>
    [XmlTypeAttribute(IncludeInSchema = false)]
    [Description("Stops the NXT Motor by braking or coasting the motor.")]
    public class AllStop : Update<AllStopRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Stop Motor
        /// </summary>
        public AllStop()
        {
        }

        /// <summary>
        /// Stop Motor
        /// </summary>
        public AllStop(MotorStopState motorStopState)
            : base(new AllStopRequest(motorStopState))
        {
        }

        /// <summary>
        /// Stop Motor
        /// </summary>
        public AllStop(AllStopRequest body)
            : base(body)
        {
        }
        /// <summary>
        /// Stop Motor
        /// </summary>
        public AllStop(AllStopRequest body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            :
                base(body, responsePort)
        {
        }
    }


    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate")]
    [Description("Connects the LEGO NXT Motor to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<MotorConfig, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Configure Device Connection
        /// </summary>
        public ConnectToBrick()
        {
        }

        /// <summary>
        /// Configure Device Connection
        /// </summary>
        /// <param name="motorPort"></param>
        public ConnectToBrick(NxtMotorPort motorPort)
            : base(new MotorConfig(motorPort))
        {
        }

        /// <summary>
        /// Configure Device Connection
        /// </summary>
        /// <param name="state"></param>
        public ConnectToBrick(MotorState state)
            : base(new MotorConfig(state))
        {
        }

    }

    /// <summary>
    /// LEGO NXT Motor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Motor Configuration.")]
    public class MotorConfig
    {
        /// <summary>
        /// LEGO NXT Motor Configuration.
        /// </summary>
        public MotorConfig() { }

        /// <summary>
        /// LEGO NXT Motor Configuration.
        /// </summary>
        /// <param name="motorPort"></param>
        public MotorConfig(NxtMotorPort motorPort)
        {
            this.MotorPort = motorPort;
        }

        /// <summary>
        /// LEGO NXT Motor Configuration.
        /// </summary>
        /// <param name="state"></param>
        public MotorConfig(MotorState state)
        {
            this.MotorPort = state.MotorPort;
            this.Name = state.Name;
            this.PollingFrequencyMs = state.PollingFrequencyMs;
        }

        /// <summary>
        /// The name of this Motor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Motor.")]
        public string Name;

        /// <summary>
        /// LEGO NXT Motor Port
        /// </summary>
        [DataMember, Description("Specifies the LEGO NXT Motor Port.")]
        [DataMemberConstructor]
        public NxtMotorPort MotorPort;

        /// <summary>
        /// Indicates the direction (polarity) of the motor.
        /// (Enabling this option (true) reverses the motor)
        /// </summary>
        [DataMember, Description(@"Indicates the direction (polarity) of the motor.\n(Enabling this option (true) reverses the motor.)")]
        public bool ReversePolarity;

        /// <summary>
        /// Polling Freqency Milliseconds (0-N, -1 disabled)
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds. \n(0 = default; -1 = disabled; > 0 = ms)")]
        public int PollingFrequencyMs;

    }


    /// <summary>
    /// Rotate the LEGO Motor at the specified motor power.
    /// Optionally stop afer the specified degrees.
    /// </summary>
    [XmlTypeAttribute(IncludeInSchema = false)]
    [Description("Rotates the LEGO Motor at the specified motor power. \nOptionally stop afer the specified degrees.")]
    public class SetMotorRotation : Update<SetMotorRotationRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Run Motor
        /// </summary>
        public SetMotorRotation()
        {
        }
        /// <summary>
        /// Run Motor
        /// </summary>
        public SetMotorRotation(SetMotorRotationRequest body)
            : base(body)
        {
        }
        /// <summary>
        /// Run Motor
        /// </summary>
        public SetMotorRotation(SetMotorRotationRequest body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Rotate the LEGO Motor at the specified motor power, stopping automatically after the specified duration.
    /// </summary>
    [XmlTypeAttribute(IncludeInSchema = false)]
    [Description("Rotates the LEGO Motor at the specified motor power, stopping automatically after the specified duration.")]
    public class RotateForDuration : Update<RotateForDurationRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Run Motor
        /// </summary>
        public RotateForDuration()
        {
        }
        /// <summary>
        /// Run Motor
        /// </summary>
        public RotateForDuration(RotateForDurationRequest body)
            : base(body)
        {
        }
        /// <summary>
        /// Run Motor
        /// </summary>
        public RotateForDuration(RotateForDurationRequest body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }


    /// <summary>
    /// Subscribe to LEGO NXT Motor notifications
    /// </summary>
    [Description("Subscribes to LEGO NXT Motor and Encoder notifications.")]
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>>
    {
    }

    /// <summary>
    /// Motor Operations Port
    /// </summary>
    [ServicePort]
    [XmlTypeAttribute(IncludeInSchema = false)]
    public class MotorOperations : PortSet
    {
        /// <summary>
        /// Motor Operations Port
        /// </summary>
        public MotorOperations()
            : base(
                typeof(DsspDefaultLookup),
                typeof(DsspDefaultDrop),
                typeof(Get),
                typeof(dssphttp.HttpGet),
                typeof(AllStop),
                typeof(SetMotorRotation),
                typeof(RotateForDuration),
                typeof(ConnectToBrick),
                typeof(Subscribe))
        { }

        #region Proxy Generated Helpers

        /// <summary>
        /// Post Dssp Default Lookup and return the response port.
        /// </summary>
        public virtual PortSet<LookupResponse, Fault> DsspDefaultLookup()
        {
            LookupRequestType body = new LookupRequestType();
            DsspDefaultLookup op = new DsspDefaultLookup(body);
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Dssp Default Lookup with body and return the response port.
        /// </summary>
        public virtual PortSet<LookupResponse, Fault> DsspDefaultLookup(LookupRequestType body)
        {
            DsspDefaultLookup op = new DsspDefaultLookup();
            op.Body = body ?? new LookupRequestType();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Dssp Default Drop and return the response port.
        /// </summary>
        public virtual PortSet<DefaultDropResponseType, Fault> DsspDefaultDrop()
        {
            DropRequestType body = new DropRequestType();
            DsspDefaultDrop op = new DsspDefaultDrop(body);
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Dssp Default Drop with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultDropResponseType, Fault> DsspDefaultDrop(DropRequestType body)
        {
            DsspDefaultDrop op = new DsspDefaultDrop();
            op.Body = body ?? new DropRequestType();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Get and return the response port.
        /// </summary>
        public virtual PortSet<MotorState, Fault> Get()
        {
            Get op = new Get();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }


        /// <summary>
        /// Post Get with body and return the response port.
        /// </summary>
        public virtual PortSet<MotorState, Fault> Get(GetRequestType body)
        {
            Get op = new Get();
            op.Body = body ?? new GetRequestType();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }


        /// <summary>
        /// Post Set Motor Power and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> SetMotorRotation()
        {
            pxmotor.SetMotorPowerRequest body = new pxmotor.SetMotorPowerRequest();
            pxmotor.SetMotorPower op = new pxmotor.SetMotorPower(body);
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Set Motor Power with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> SetMotorRotation(pxmotor.SetMotorPowerRequest body)
        {
            pxmotor.SetMotorPower op = new pxmotor.SetMotorPower();
            op.Body = body ?? new pxmotor.SetMotorPowerRequest();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        
        /// <summary>
        /// Post Http Get and return the response port.
        /// </summary>
        public virtual PortSet<HttpResponseType, Fault> HttpGet()
        {
            dssphttp.HttpGetRequestType body = new dssphttp.HttpGetRequestType();
            HttpGet op = new HttpGet(body);
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Http Get with body and return the response port.
        /// </summary>
        public virtual PortSet<HttpResponseType, Fault> HttpGet(dssphttp.HttpGetRequestType body)
        {
            HttpGet op = new HttpGet();
            op.Body = body ?? new dssphttp.HttpGetRequestType();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Subscribe and return the response port.
        /// </summary>
        public virtual PortSet<SubscribeResponseType, Fault> Subscribe(IPort notificationPort)
        {
            Subscribe op = new Subscribe();
            op.Body = new SubscribeRequestType();
            op.NotificationPort = notificationPort;
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Subscribe with body and return the response port.
        /// </summary>
        public virtual PortSet<SubscribeResponseType, Fault> Subscribe(SubscribeRequestType body, IPort notificationPort)
        {
            Subscribe op = new Subscribe();
            op.Body = body ?? new SubscribeRequestType();
            op.NotificationPort = notificationPort;
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Stop Motor with parameters and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> AllStop(MotorStopState stopState)
        {
            AllStopRequest body = new AllStopRequest();
            body.StopState = stopState;
            AllStop op = new AllStop(body);
            this.PostUnknownType(op);
            return op.ResponsePort;
        }


        /// <summary>
        /// Post Stop Motor with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> AllStop(AllStopRequest body)
        {
            AllStop op = new AllStop();
            op.Body = body ?? new AllStopRequest();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }


        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(MotorConfig body)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = body ?? new MotorConfig();
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(MotorState state)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = new MotorConfig(state.MotorPort);
            op.Body.Name = state.Name;
            op.Body.PollingFrequencyMs = state.PollingFrequencyMs;

            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        /// <param name="motorPort"></param>
        /// <returns></returns>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(NxtMotorPort motorPort)
        {
            ConnectToBrick op = new ConnectToBrick(motorPort);
            this.PostUnknownType(op);
            return op.ResponsePort;
        }



        #endregion
    }

}
