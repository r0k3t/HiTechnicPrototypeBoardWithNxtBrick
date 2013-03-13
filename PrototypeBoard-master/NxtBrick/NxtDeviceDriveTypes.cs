//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceDriveTypes.cs $ $Revision: 21 $
//-----------------------------------------------------------------------

using Microsoft.Dss.Core.Attributes;
using System;
using System.Collections.Generic;
using Microsoft.Dss.ServiceModel.Dssp;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using Microsoft.Ccr.Core;
using System.Xml.Serialization;
using W3C.Soap;
using System.ComponentModel;
using pxdrive = Microsoft.Robotics.Services.Drive.Proxy;
using pxmotor = Microsoft.Robotics.Services.Motor.Proxy;
using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using motor = Microsoft.Robotics.Services.Sample.Lego.Nxt.Motor;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Drive
{
    
    /// <summary>
    /// LegoNxtDrive Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the LegoNxtDrive service
        [DataMember, Description("Identifies the Unique Contract Identifier for the LegoNxtDrive service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/drive.user.html";

        /// <summary>
        /// The Unique Device Type
        /// </summary>
        [DataMember, Description("Identifies the Drive (Motor Pair) Device Model.")]
        public const string DeviceModel = "NxtMotorPair";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 100;

    }

    /// <summary>
    /// LEGO NXT Drive State
    /// </summary>
    [DataContract, Description("Specifies the Drive state which controls two motors in synchronization.")]
    public class DriveState 
    {
        /// <summary>
        /// Is the Drive connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// Indicates the distance between the drive wheels (meters).
        /// </summary>
        [DataMember, Description("Indicates the distance between the drive wheels in meters. \n(Example: 11.3cm = 0.113)")]
        public double DistanceBetweenWheels;

        /// <summary>
        /// Left Wheel Configuration
        /// </summary>
        [DataMember, Description("Specifies the Left Wheel Configuration.")]
        public WheelConfiguration LeftWheel;


        /// <summary>
        /// Right Wheel Configuration
        /// </summary>
        [DataMember, Description("Specifies the Right Wheel Configuration.")]
        public WheelConfiguration RightWheel;

        /// <summary>
        /// Polling Freqency Milliseconds (0-N, -1 disabled)
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds. \n(0 = default; -1 = disabled; > 0 = ms)")]
        public int PollingFrequencyMs;

        /// <summary>
        /// Indicates the timestamp of the last state change.
        /// </summary>
        [DataMember(XmlOmitDefaultValue = true), Description("Indicates the time of the last encoder readings.")]
        [Browsable(false)]
        [DefaultValue(typeof(DateTime), "0001-01-01T00:00:00")]
        public DateTime TimeStamp;

        /// <summary>
        /// Indicates the runtime statistics of entire drive.
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates the runtime statistics of entire drive (power, encoders, rpms).")]
        [Browsable(false)]
        public RuntimeStatistics RuntimeStatistics;

        /// <summary>
        /// Copy state to generic state.
        /// </summary>
        /// <param name="genericState"></param>
        public pxdrive.DriveDifferentialTwoWheelState CopyToGenericState(pxdrive.DriveDifferentialTwoWheelState genericState)
        {
            genericState.DistanceBetweenWheels = this.DistanceBetweenWheels;
            genericState.IsEnabled = this.LeftWheel.MotorPort != NxtMotorPort.NotConnected && this.RightWheel.MotorPort != NxtMotorPort.NotConnected;
            genericState.TimeStamp = this.TimeStamp;

            if (genericState.LeftWheel == null)
                genericState.LeftWheel = new pxmotor.WheeledMotorState();
            if (genericState.LeftWheel.MotorState == null)
                genericState.LeftWheel.MotorState = new pxmotor.MotorState();
            genericState.LeftWheel.MotorState.CurrentPower = this.RuntimeStatistics.LeftPowerCurrent;
            genericState.LeftWheel.MotorState.HardwareIdentifier = NxtDrive.MotorPortToHardwareIdentifier(this.LeftWheel.MotorPort);
            genericState.LeftWheel.MotorState.PowerScalingFactor = 100.0;
            genericState.LeftWheel.MotorState.ReversePolarity = this.LeftWheel.ReversePolarity;
            genericState.LeftWheel.Radius = this.LeftWheel.WheelDiameter / 2.0;

            if (genericState.RightWheel == null)
                genericState.RightWheel = new pxmotor.WheeledMotorState();
            if (genericState.RightWheel.MotorState == null)
                genericState.RightWheel.MotorState = new pxmotor.MotorState();
            genericState.RightWheel.MotorState.CurrentPower = this.RuntimeStatistics.RightPowerCurrent;
            genericState.RightWheel.MotorState.HardwareIdentifier = NxtDrive.MotorPortToHardwareIdentifier(this.RightWheel.MotorPort);
            genericState.RightWheel.MotorState.PowerScalingFactor = 100.0;
            genericState.RightWheel.MotorState.ReversePolarity = this.RightWheel.ReversePolarity;
            genericState.RightWheel.Radius = this.RightWheel.WheelDiameter / 2.0;

            return genericState;

        }

        /// <summary>
        /// Transform generic state to this instance.
        /// </summary>
        /// <param name="genericState"></param>
        public void CopyFromGenericState(pxdrive.DriveDifferentialTwoWheelState genericState)
        {
            if (genericState.DistanceBetweenWheels > 0.0)
                this.DistanceBetweenWheels = genericState.DistanceBetweenWheels;

            this.TimeStamp = genericState.TimeStamp;

            if (genericState.LeftWheel != null)
            {
                if (genericState.LeftWheel.Radius > 0.0)
                    this.LeftWheel.WheelDiameter = genericState.LeftWheel.Radius * 2.0;

                if (genericState.LeftWheel.MotorState != null)
                {
                    if (this.RuntimeStatistics == null) this.RuntimeStatistics = new RuntimeStatistics();
                    this.RuntimeStatistics.LeftPowerCurrent = genericState.LeftWheel.MotorState.CurrentPower;
                    this.LeftWheel.ReversePolarity = genericState.LeftWheel.MotorState.ReversePolarity;
                    if (genericState.LeftWheel.MotorState.HardwareIdentifier > 0 && genericState.LeftWheel.MotorState.HardwareIdentifier <= 3)
                    {
                        if (genericState.LeftWheel.MotorState.HardwareIdentifier > 0 && genericState.LeftWheel.MotorState.HardwareIdentifier <= 3)
                            this.LeftWheel.MotorPort = NxtDrive.HardwareIdentifierToMotorPort(genericState.LeftWheel.MotorState.HardwareIdentifier, this.LeftWheel.MotorPort);
                    }
                }
            }

            if (genericState.RightWheel != null)
            {
                if (genericState.RightWheel.Radius > 0.0)
                    this.RightWheel.WheelDiameter = genericState.RightWheel.Radius * 2.0;

                if (genericState.RightWheel.MotorState != null)
                {
                    if (this.RuntimeStatistics == null) this.RuntimeStatistics = new RuntimeStatistics();
                    this.RuntimeStatistics.RightPowerCurrent = genericState.RightWheel.MotorState.CurrentPower;
                    this.RightWheel.ReversePolarity = genericState.RightWheel.MotorState.ReversePolarity;
                    if (genericState.RightWheel.MotorState.HardwareIdentifier > 0)
                    {
                        if (genericState.RightWheel.MotorState.HardwareIdentifier > 0 && genericState.RightWheel.MotorState.HardwareIdentifier <= 3)
                            this.RightWheel.MotorPort = NxtDrive.HardwareIdentifierToMotorPort(genericState.RightWheel.MotorState.HardwareIdentifier, this.RightWheel.MotorPort);
                    }

                }
            }
        }


        /// <summary>
        /// Deep Clone DriveState
        /// </summary>
        /// <returns></returns>
        public DriveState Clone()
        {
            DriveState clone = new DriveState();
            clone.DistanceBetweenWheels = this.DistanceBetweenWheels;
            clone.LeftWheel = (this.LeftWheel == null) ? null : (WheelConfiguration)this.LeftWheel.Clone();
            clone.RightWheel = (this.RightWheel == null) ? null : (WheelConfiguration)this.RightWheel.Clone();
            clone.PollingFrequencyMs = this.PollingFrequencyMs;
            clone.TimeStamp = this.TimeStamp;
            clone.RuntimeStatistics = (this.RuntimeStatistics == null) ? null : (RuntimeStatistics)this.RuntimeStatistics.Clone();
            return clone;
        }

    }

    /// <summary>
    /// LEGO Drive Runtime Statistics
    /// </summary>
    [DataContract, Description("Provides Runtime statistics for the LEGO NXT Drive.")]
    public class RuntimeStatistics : ICloneable
    {
        /// <summary>
        /// Indicates the Left motor power; range is -1.0 to 1.0.
        /// </summary>
        [DataMember, Description("Indicates the current power of the Left Motor. \n(range is -1.0 to 1.0)")]
        public double LeftPowerCurrent;

        /// <summary>
        /// Indicates the Left motor target power; range is -1.0 to 1.0.
        /// </summary>
        [DataMember, Description("Indicates the desired Target Power of the Left motor. \n(range is -1.0 to 1.0)")]
        public double LeftPowerTarget;

        /// <summary>
        /// Left Encoder TimeStamp.
        /// </summary>
        [DataMember, Description("Indicates the time of the Left Encoder reading.")]
        public DateTime LeftEncoderTimeStamp;

        /// <summary>
        /// Left Motor Encoder.  Current reading in degrees.
        /// </summary>
        [DataMember, Description("Indicates the current reading of the Left Motor Encoder in degrees.")]
        public long LeftEncoderCurrent;

        /// <summary>
        /// Left Motor Encoder Target.  Desired target of the Left encoder in degrees.
        /// </summary>
        [DataMember, Description("Indicates the Desired Target of the Left Motor Encoder in degrees.")]
        public long LeftEncoderTarget;

        /// <summary>
        /// The Left Motor Speed (RPM).
        /// </summary>
        [DataMember, Description("Indicates the Left Motor Speed (RPM).")]
        public Int32 LeftMotorRpm;


        /// <summary>
        /// Indicates the Right motor power; range is -1.0 to 1.0.
        /// </summary>
        [DataMember, Description("Indicates the current power of the Right Motor. \n(range is -1.0 to 1.0)")]
        public double RightPowerCurrent;

        /// <summary>
        /// Indicates the Right motor target power; range is -1.0 to 1.0.
        /// </summary>
        [DataMember, Description("Indicates the desired Target Power of the Right motor. \n(range is -1.0 to 1.0)")]
        public double RightPowerTarget;

        /// <summary>
        /// Right Encoder TimeStamp.
        /// </summary>
        [DataMember, Description("Indicates the time of the Right Encoder reading.")]
        public DateTime RightEncoderTimeStamp;

        /// <summary>
        /// Right Motor Encoder.  Current reading in degrees.
        /// </summary>
        [DataMember, Description("Indicates the current reading of the Right Motor Encoder in degrees.")]
        public long RightEncoderCurrent;

        /// <summary>
        /// Right Motor Encoder Target.  Desired target of the Right encoder in degrees.
        /// </summary>
        [DataMember, Description("Indicates the Desired Target of the Right Motor Encoder in degrees.")]
        public long RightEncoderTarget;

        /// <summary>
        /// The Right Motor Speed (RPM).
        /// </summary>
        [DataMember, Description("Indicates the Right Motor Speed (RPM).")]
        public Int32 RightMotorRpm;

        /// <summary>
        /// Specifies how to stop after the Target Encoder degrees are reached.
        /// Valid when Stopping after a specified number of rotation degrees.
        /// </summary>
        [DataMember, Description("Specifies how to stop after the Target Encoder degrees are reached. \nValid when Stopping after a specified number of rotation degrees.")]
        [Browsable(false)]
        public MotorStopState TargetStopState;

        #region ICloneable Members

        /// <summary>
        /// Clone RuntimeStatistics
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            RuntimeStatistics clone = new RuntimeStatistics();
            clone.LeftPowerCurrent = this.LeftPowerCurrent;
            clone.RightPowerCurrent = this.RightPowerCurrent;
            clone.LeftEncoderCurrent = this.LeftEncoderCurrent;
            clone.RightEncoderCurrent = this.RightEncoderCurrent;
            clone.LeftEncoderTimeStamp = this.LeftEncoderTimeStamp;
            clone.RightEncoderTimeStamp = this.RightEncoderTimeStamp;
            clone.LeftEncoderTarget = this.LeftEncoderTarget;
            clone.RightEncoderTarget = this.RightEncoderTarget;
            clone.LeftPowerTarget = this.LeftPowerTarget;
            clone.RightPowerTarget = this.RightPowerTarget;
            clone.LeftMotorRpm = this.LeftMotorRpm;
            clone.RightMotorRpm = this.RightMotorRpm;
            clone.TargetStopState = this.TargetStopState;
            return clone;
        }

        #endregion
    }


    /// <summary>
    /// LEGO NXT Wheel Configuration
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Wheel Configuration.")]
    public class WheelConfiguration : ICloneable
    {
        #region Constructors
        /// <summary>
        /// LEGO NXT Wheel Configuration
        /// </summary>
        public WheelConfiguration()
        {
        }

        /// <summary>
        /// LEGO NXT Wheel Configuration
        /// </summary>
        /// <param name="motorPort"></param>
        /// <param name="reversePolarity"></param>
        /// <param name="wheelDiameter"></param>
        public WheelConfiguration(NxtMotorPort motorPort, bool reversePolarity, double wheelDiameter)
        {
            this.MotorPort = motorPort;
            this.ReversePolarity = reversePolarity;
            this.WheelDiameter = wheelDiameter;
        }
        #endregion

        /// <summary>
        /// Motor Port Configuration
        /// </summary>
        [DataMember, Description("Specifies the Motor Port Configuration.")]
        [DataMemberConstructor(Order = 1)]
        public NxtMotorPort MotorPort;

        /// <summary>
        /// Reverse Motor Polarity
        /// </summary>
        [DataMember, Description("Indicates the direction (polarity) of the motor.\n(Enabling this option (true) reverses the motor.)")]
        [DataMemberConstructor(Order = 2)]
        public bool ReversePolarity;

        /// <summary>
        /// Diameter of the wheel (meters)
        /// </summary>
        [DataMember, Description("Specifies the diameter of the wheel in meters \n(Example 5.2cm = 0.052)")]
        [DataMemberConstructor(Order = 3)]
        public double WheelDiameter;

        #region ICloneable Members

        /// <summary>
        /// Deep Clone WheelConfiguration
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            WheelConfiguration clone = new WheelConfiguration(this.MotorPort, this.ReversePolarity, this.WheelDiameter);
            return clone;
        }

        #endregion
    }

    /// <summary>
    /// LEGO NXT specific drive request which stops after the specified motor rotation degrees.
    /// </summary>
    [DataContract, Description("Specifies a LEGO NXT specific drive request which stops after the specified motor rotation degrees.")]
    public class SetDriveRequest
    {
        /// <summary>
        /// Left Motor Power (-1.0 - 1.0)
        /// </summary>
        [DataMember, Description("Specifies the Left Motor Power (-1.0 - 1.0)")]
        [DataMemberConstructor(Order = 1)]
        public double LeftPower;

        /// <summary>
        /// Right Motor Power (-1.0 - 1.0)
        /// </summary>
        [DataMember, Description("Specifies the Right Motor Power (-1.0 - 1.0)")]
        [DataMemberConstructor(Order = 2)]
        public double RightPower;

        /// <summary>
        /// Stop the Left Motor after it has rotated the specified degrees (0-continuous).
        /// </summary>
        [DataMember, Description("Stops the Left Motor after it has rotated the specified degrees (0-continuous).")]
        [DataMemberConstructor(Order = 3)]
        public long LeftStopAtRotationDegrees;

        /// <summary>
        /// Stop the Right Motor after it has rotated the specified degrees (0-continuous).
        /// </summary>
        [DataMember, Description("Stops the Right Motor after it has rotated the specified degrees (0-continuous).")]
        public long RightStopAtRotationDegrees;

        /// <summary>
        /// Stop by applying brakes or coasting. \nValid when Stopping after the specified Rotation Degrees
        /// </summary>
        [DataMember, Description("Stops by applying brakes or coasting. \nValid when Stopping after the specified Rotation Degrees.")]
        public MotorStopState StopState;

        /// <summary>
        /// This request orignated from a generic operation
        /// </summary>
        /// 
        [DataMember, Description("Request originated from a generic operation.")]
        [DataMemberConstructor(Order = -1)]
        public bool isGenericOperation;

        /// <summary>
        /// DriveDistance stage
        /// </summary>
        /// 
        [DataMember, Description("Drive distance stage.")]
        [DataMemberConstructor(Order = -1 )]
        public pxdrive.DriveStage DriveDistanceStage;

        /// <summary>
        /// RotageDegrees stage
        /// </summary>
        /// 
        [DataMember, Description("Rotate degrees stage.")]
        [DataMemberConstructor(Order = -1)]
        public pxdrive.DriveStage RotateDegreesStage;


        /// <summary>
        /// Drive request;
        /// </summary>
        [DataMember, Description("Drive request.")]
        [DataMemberConstructor(Order = -1)]
        public pxdrive.DriveRequestOperation DriveRequestOperation;
    }

    /// <summary>
    /// Reset the Motor Encoder Position
    /// </summary>
    [DataContract, Description("Resets the Motor Encoder Position.")]
    public class ResetMotorPositionRequest
    {
        /// <summary>
        /// Relative to last position (true) or Absolute (false)
        /// </summary>
        [DataMember, Description("Specifies that the motor is to be reset Relative to last position (true).")]
        public bool Relative;
    }

    #region Operation Types

    /// <summary>
    /// Get the LEGO Motor State
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Drive.")]
    public class Get : Get<GetRequestType, PortSet<DriveState, Fault>>
    {
    }


    /// <summary>
    /// Run Motor
    /// </summary>
    [XmlTypeAttribute(IncludeInSchema = false)]
    [Description("Runs the LEGO Drive at the specified left and right motor power.\nOptionally stop afer specified degrees.")]
    public class DriveDistance : Update<SetDriveRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Run Motor
        /// </summary>
        public DriveDistance()
        {
        }
        /// <summary>
        /// Run Motor
        /// </summary>
        public DriveDistance(SetDriveRequest body)
            :
                base(body)
        {
        }
        /// <summary>
        /// Run Motor
        /// </summary>
        public DriveDistance(SetDriveRequest body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            :
                base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Configure Drive Connection
    /// </summary>
    [XmlTypeAttribute(IncludeInSchema = false)]
    [Description("Connects the LEGO NXT Drive to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<DriveState, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Configure Drive Connection
        /// </summary>
        public ConnectToBrick()
        {
        }
        /// <summary>
        /// Configure Drive Connection
        /// </summary>
        public ConnectToBrick(DriveState body)
            : base(body)
        {
        }
        /// <summary>
        /// Configure Drive Connection
        /// </summary>
        public ConnectToBrick(DriveState body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }


    /// <summary>
    /// Subscribe to LEGO NXT Drive notifications
    /// </summary>
    [Description("Subscribes to LEGO NXT Drive notifications.")]
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>>
    {
    }

  
    /// <summary>
    /// Indicates an update to the Left and Right Wheel encoders.
    /// </summary>
    [Description("Indicates an update to the Left and Right Wheel encoders.")]
    public class DriveEncodersUpdate : Update<RuntimeStatistics, PortSet<DefaultUpdateResponseType, Fault>> { }

    #endregion

    /// <summary>
    /// LEGO NXT Drive Operations Port
    /// </summary>
    [ServicePort]
    [XmlTypeAttribute(IncludeInSchema = false)]
    public class DriveOperations : PortSet
    {
        /// <summary>
        /// LEGO NXT Drive Operations Port
        /// </summary>
        public DriveOperations()
            : base(
                typeof(DsspDefaultLookup),
                typeof(DsspDefaultDrop),
                typeof(Get),
                typeof(dssphttp.HttpGet),
                typeof(motor.AllStop),
                typeof(DriveDistance),
                typeof(ConnectToBrick),
                typeof(DriveEncodersUpdate),
                typeof(Subscribe))
        { }

        #region Implicit Operators
        /// <summary>
        /// Implicit Operator for Port of DsspDefaultLookup
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<DsspDefaultLookup>(DriveOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<DsspDefaultLookup>)portSet[typeof(DsspDefaultLookup)];
        }
        /// <summary>
        /// Implicit Operator for Port of DsspDefaultDrop
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<DsspDefaultDrop>(DriveOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<DsspDefaultDrop>)portSet[typeof(DsspDefaultDrop)];
        }
        /// <summary>
        /// Implicit Operator for Port of Get
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<Get>(DriveOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<Get>)portSet[typeof(Get)];
        }
        /// <summary>
        /// Implicit Operator for Port of Microsoft.Dss.Core.DsspHttp.HttpGet
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<Microsoft.Dss.Core.DsspHttp.HttpGet>(DriveOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<Microsoft.Dss.Core.DsspHttp.HttpGet>)portSet[typeof(Microsoft.Dss.Core.DsspHttp.HttpGet)];
        }
        /// <summary>
        /// Implicit Operator for Port of AllStop
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<motor.AllStop>(DriveOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<motor.AllStop>)portSet[typeof(motor.AllStop)];
        }
        /// <summary>
        /// Implicit Operator for Port of DriveDistance
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<DriveDistance>(DriveOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<DriveDistance>)portSet[typeof(DriveDistance)];
        }
        /// <summary>
        /// Implicit Operator for Port of DriveEncodersUpdate
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<DriveEncodersUpdate>(DriveOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<DriveEncodersUpdate>)portSet[typeof(DriveEncodersUpdate)];
        }
        /// <summary>
        /// Implicit Operator for Port of ConnectToBrick
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<ConnectToBrick>(DriveOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<ConnectToBrick>)portSet[typeof(ConnectToBrick)];
        }
        /// <summary>
        /// Implicit Operator for Port of Subscribe
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<Subscribe>(DriveOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<Subscribe>)portSet[typeof(Subscribe)];
        }
        #endregion

        #region Post 
        /// <summary>
        /// Post(DsspDefaultLookup)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(DsspDefaultLookup item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(DsspDefaultDrop)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(DsspDefaultDrop item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(Get)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(Get item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(dssphttp.HttpGet)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(dssphttp.HttpGet item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(motor.AllStop)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(motor.AllStop item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(DriveDistance)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(DriveDistance item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(DriveEncodersUpdate)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(DriveEncodersUpdate item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(ConnectToBrick)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(ConnectToBrick item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(Subscribe)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(Subscribe item) { base.PostUnknownType(item); }
        #endregion

        #region Helper Methods

        /// <summary>
        /// Post Connect To Brick with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(DriveState body)
        {
            ConnectToBrick op = new ConnectToBrick(body);
            this.Post(op);
            return op.ResponsePort;
        }

        #endregion
    }


}
