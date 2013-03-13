//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtBrickTypes.cs $ $Revision: 35 $
//-----------------------------------------------------------------------

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using System;
using System.Collections.Generic;
using W3C.Soap;
using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using System.ComponentModel;
using Microsoft.Dss.Core.Utilities;
using System.Diagnostics;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using Microsoft.Dss.Core;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using System.Xml.Serialization;
using Microsoft.Dss.Core.DsspHttp;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick
{
    
    /// <summary>
    /// The NxtBrick State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Brick state.")]
    public class NxtBrickState
    {

        /// <summary>
        /// LEGO Nxt Brick Configuration
        /// </summary>
        [DataMember, Description("Specifies the LEGO NXT Brick Configuration.")]
        public BrickConfiguration Configuration;

        /// <summary>
        /// LEGO NXT Runtime State
        /// </summary>
        [DataMember, Description("Specifies the LEGO NXT Runtime state.")]
        [Browsable(false)]
        public NxtRuntime Runtime;

    }


    /// <summary>
    /// LEGO NXT Runtime State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Runtime state.")]
    public class NxtRuntime
    {
        private string _brickName;

        /// <summary>
        /// The LEGO NXT Brick Name
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Brick.")]
        [Browsable(false)]
        [DisplayName("(User) Name")]
        public string BrickName
        {
            get { return _brickName; }
            set { _brickName = value; }
        }

        /// <summary>
        /// The LEGO NXT Firmware Version
        /// </summary>
        [DataMember, Description("Identifies the LEGO NXT Firmware Version.")]
        [Browsable(false)]
        public string Firmware;

        /// <summary>
        /// Indicates an active connection to the LEGO NXT Brick
        /// </summary>
        [DataMember, Description("Indicates an active connection to the LEGO NXT Brick.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// Identifies Runtime devices which are attached to the LEGO Brick
        /// </summary>
        /// <remarks>The key is LegoNxtConnection.ToString()</remarks>
        [DataMember, Description("Identifies Runtime devices which are attached to the LEGO NXT Brick.")]
        [Browsable(false)]
        public DssDictionary<string, AttachRequest> Devices;

    }



    /// <summary>
    /// Play a tone on the NXT brick for the specified duration
    /// </summary>
    [DataContract, Description("Plays a tone on the NXT brick for the specified duration.")]
    public class Note
    {
        /// <summary>
        /// Play a tone on the NXT brick for the specified duration
        /// </summary>
        public Note() { }

        /// <summary>
        /// Play a tone on the NXT brick for the specified duration
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="duration"></param>
        public Note(int frequency, int duration) 
        {
            this.Frequency = frequency;
            this.Duration = duration;
        }

        /// <summary>
        /// 200 - 14000 Hz
        /// </summary>
        [DataMember, Description("Specifies the frequency of the note (200 - 14000 Hz).")]
        [DataMemberConstructor(Order = 1)]
        public int Frequency;

        /// <summary>
        /// Duration to play tome in ms
        /// </summary>
        [DataMember, Description("Specifies the duration to play the note (in ms).")]
        [DataMemberConstructor(Order = 2)]
        public int Duration;
    }


    /// <summary>
    /// Polling Entry
    /// </summary>
    public class PollingEntry
    {
        /// <summary>
        /// Polling Entry
        /// </summary>
        public PollingEntry()
        {
            Next = DateTime.Now;
        }

        /// <summary>
        /// Polling Entry
        /// </summary>
        /// <param name="request"></param>
        public PollingEntry(AttachRequest request)
        {
            Next = DateTime.Now;
            AttachRequest = request;
        }

        /// <summary>
        /// Polling Entry
        /// </summary>
        /// <param name="request"></param>
        /// <param name="next"></param>
        public PollingEntry(AttachRequest request, DateTime next)
        {
            Next = next;
            AttachRequest = request;
        }

        /// <summary>
        /// The Next Scheduled Polling Event for this sensor
        /// </summary>
        public DateTime Next;

        /// <summary>
        /// The Timestamp when this polling entry was registered.
        /// </summary>
        public DateTime Timestamp;

        /// <summary>
        /// The Attach Request received from the sensor service
        /// </summary>
        public AttachRequest AttachRequest
        {
            get { return _attachRequest; }
            set 
            { 
                _attachRequest = value;
                if (_attachRequest != null)
                {
                    this.Timestamp = _attachRequest.Timestamp;
                }
            }
        }

        internal int PollingSuccessCount = 0;
        internal int PollingFailureCount = 0;
        internal DateTime StartedPolling;

        private AttachRequest _attachRequest = null;
    }

    /// <summary>
    /// Register a Sensor or Motor to the LEGO Brick before Attaching
    /// </summary>
    [DataContract, Description("Registers a Sensor or Motor to the LEGO Brick before Attaching.")]
    public class Registration
    {
        /// <summary>
        /// The Brick Connection Port
        /// </summary>
        [DataMember, Description("Identifies the Port or Connection for this device.")]
        [DataMemberConstructor(Order = 1)]
        public LegoNxtConnection Connection;

        /// <summary>
        /// The Device Type
        /// </summary>
        [DataMember, Description("Specifies the Type of the device being registered.")]
        [DataMemberConstructor(Order = 2)]
        public LegoDeviceType DeviceType;

        /// <summary>
        /// The Device Model
        /// </summary>
        [DataMember, Description("Specifies the Model of the device being registered.")]
        [DataMemberConstructor(Order = 3)]
        public string DeviceModel;

        /// <summary>
        /// The Device Contract
        /// </summary>
        [DataMember, Description("Specifies the DSS Service Contract of the Device being registered.")]
        [DataMemberConstructor(Order = 4)]
        public string DeviceContract;

        /// <summary>
        /// The Service URI. 
        /// Use: ServiceInfo.Service
        /// </summary>
        [DataMember, Description("Specifies the DSS Service URI of the Device being registered.")]
        [DataMemberConstructor(Order = 5)]
        public string ServiceUri;

        /// <summary>
        /// Identifies the user friendly name for the LEGO NXT Device.
        /// </summary>
        [DataMember, Description("Identifies the user friendly name for the LEGO NXT Device.")]
        [DataMemberConstructor(Order = 6)]
        public string Name;

        /// <summary>
        /// Subscription Uri
        /// </summary>
        [DataMember, Description("Identifies the Service URI of the subscribing LEGO NXT Device.")]
        public string SubscriberUri;

        /// <summary>
        /// The I2C Bus Address of the Ultrasonic or other I2C Sensor.
        /// </summary>
        [DataMember, Description("Specifies the I2C Bus Address of the Ultrasonic or other I2C Sensor.")]
        public byte I2CBusAddress = NxtCommon.DefaultI2CBusAddress;

        /// <summary>
        /// Attach a Sensor or Motor to the LEGO Brick
        /// </summary>
        public Registration()
        {
            I2CBusAddress = NxtCommon.DefaultI2CBusAddress;
        }

        /// <summary>
        /// Register a Sensor or Motor to the LEGO Brick before Attaching.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="deviceType"></param>
        /// <param name="deviceModel"></param>
        /// <param name="deviceContract"></param>
        /// <param name="serviceUri"></param>
        /// <param name="name"></param>
        public Registration(LegoNxtConnection connection, LegoDeviceType deviceType, string deviceModel, string deviceContract, string serviceUri, string name)
        {
            this.Connection = connection;
            this.DeviceType = deviceType;
            this.DeviceModel = deviceModel;
            this.DeviceContract = deviceContract;
            this.ServiceUri = serviceUri;
            this.Name = name;
        }

        ///// <summary>
        ///// Attach a Sensor or Motor to the LEGO Brick
        ///// </summary>
        //public Registration(LegoNxtPort port, string deviceType, string deviceContract, string serviceUri)
        //{
        //    this.Connection = new LegoNxtConnection(port);
        //    this.DeviceModel = deviceType;
        //    this.DeviceContract = deviceContract;
        //    this.ServiceUri = serviceUri;
        //}
        ///// <summary>
        ///// Attach a Sensor or Motor to the LEGO Brick
        ///// </summary>
        //public Registration(NxtMotorPort port, string deviceType, string deviceContract, string serviceUri)
        //{
        //    this.Connection = new LegoNxtConnection((LegoNxtPort)port);
        //    this.DeviceModel = deviceType;
        //    this.DeviceContract = deviceContract;
        //    this.ServiceUri = serviceUri;
        //}

        ///// <summary>
        ///// Attach a Sensor or Motor to the LEGO Brick
        ///// </summary>
        //public Registration(LegoNxtConnection connection, string deviceType, string deviceContract, string serviceUri)
        //{
        //    this.Connection = connection;
        //    this.DeviceModel = deviceType;
        //    this.DeviceContract = deviceContract;
        //    this.ServiceUri = serviceUri;
        //}

    }

    /// <summary>
    /// Detach a Sensor or Motor from the LEGO Brick
    /// </summary>
    [DataContract, Description("Detaches a Sensor or Motor from the LEGO Brick.")]
    public class DetachRequest
    {
        #region Constructors
        /// <summary>
        /// Detach a Sensor or Motor from the LEGO Brick
        /// </summary>
        public DetachRequest()
        {
        }
        /// <summary>
        /// Detach a Sensor or Motor from the LEGO Brick
        /// </summary>
        public DetachRequest(string serviceUri)
        {
            this.ServiceUri = serviceUri;
        }
        #endregion

        /// <summary>
        /// The Service URI of a LEGO NXT Device 
        /// </summary>
        [DataMember, Description("Specifies the Service URI of the service being detached.")]
        [DataMemberConstructor(Order = 1)]
        public string ServiceUri;
    }


    /// <summary>
    /// Adjust the Polling Frequency for a LEGO NXT Device.
    /// </summary>
    [DataContract, Description("Adjust the Polling Frequency for a LEGO NXT Device.")]
    public class AdjustPollingFrequencyRequest
    {
        #region Constructors
        /// <summary>
        /// Adjust the Polling Frequency for a LEGO NXT Device.
        /// </summary>
        public AdjustPollingFrequencyRequest()
        {
        }
        /// <summary>
        /// Adjust the Polling Frequency for a LEGO NXT Device.
        /// </summary>
        /// <param name="serviceUri"></param>
        /// <param name="pollingFrequencyMs"></param>
        public AdjustPollingFrequencyRequest(string serviceUri, int pollingFrequencyMs)
        {
            this.ServiceUri = serviceUri;
            this.PollingFrequencyMs = pollingFrequencyMs;
        }
        #endregion

        /// <summary>
        /// The Service URI of a LEGO NXT Device 
        /// </summary>
        [DataMember, Description("Specifies the Service URI of the service whose polling frequency is updated.")]
        [DataMemberConstructor(Order = 1)]
        public string ServiceUri;


        /// <summary>
        /// Indicates the new Polling Frequency in milliseconds. \n(-1 = disabled; 0 = Original setting; > 0 = ms)
        /// </summary>
        [DataMember, Description("Indicates the new Polling Frequency in milliseconds. \n(-1 = disabled; 0 = Original setting; > 0 = ms)")]
        [DataMemberConstructor(Order = 2)]
        public int PollingFrequencyMs;
    }

    /// <summary>
    /// The Adjusted Polling Frequency.
    /// </summary>
    [DataContract, Description("Indicates the adjusted polling frequency.")]
    public class AdjustPollingFrequencyResponse
    {
        /// <summary>
        /// The Adjusted Polling Frequency.
        /// </summary>
        public AdjustPollingFrequencyResponse(){}
        /// <summary>
        /// The Adjusted Polling Frequency.
        /// </summary>
        /// <param name="pollingFrequencyMs"></param>
        public AdjustPollingFrequencyResponse(int pollingFrequencyMs) { this.PollingFrequencyMs = pollingFrequencyMs; }

        /// <summary>
        /// Polling Freqency Milliseconds (> 0, -1 disabled)
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds. \n(-1 = disabled; > 0 = ms)")]
        [DataMemberConstructor(Order = 2)]
        public int PollingFrequencyMs;
    }

    /// <summary>
    /// Attach a Sensor or Motor to the LEGO Brick
    /// </summary>
    [DataContract, Description("Attaches a Sensor or Motor to the LEGO Brick.")]
    public class AttachRequest: SubscribeRequestType
    {
        /// <summary>
        /// Registration
        /// </summary>
        [DataMember, Description("Specifies the device registration.")]
        [DataMemberConstructor(Order = 1)]
        public Registration Registration;

        /// <summary>
        /// Sensor Initialization Sequence which is executed when the sensor is attached,
        /// and any time the LEGO NXT brick is reinitialized.
        /// </summary>
        [DataMember, Description("Specifies the sensor Initialization Sequence which is executed when the sensor is attached, \nand any time the LEGO NXT brick is reinitialized.")]
        public nxtcmd.NxtCommandSequence InitializationCommands;

        /// <summary>
        /// Commands which are executed for continuous polling of the sensor.
        /// </summary>
        /// <remarks>PollingFrequencyMs must be greater than zero.</remarks>
        [DataMember, Description("Specifies the Commands which are executed for continuous polling of the sensor.")]
        public nxtcmd.NxtCommandSequence PollingCommands;

        /// <summary>
        /// Timestamp
        /// </summary>
        [DataMember, Description("Indicates the most recent attach request or polling adjustment for this device.")]
        [Browsable(false)]
        public DateTime Timestamp;

        /// <summary>
        /// Attach a Sensor or Motor to the LEGO Brick
        /// </summary>
        public AttachRequest()
        {
        }

        /// <summary>
        /// Attach a Sensor or Motor to the LEGO Brick
        /// </summary>
        public AttachRequest(Registration registration)
        {
            this.Registration = registration;
        }

        /// <summary>
        /// The description of an Attach Request instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string description = string.Empty;
            if (this.Registration != null)
                return string.Format("{0} on {1}", this.Registration.DeviceModel, this.Registration.Connection);

            return base.ToString();
        }
    }

    /// <summary>
    /// Attach Response which identifies which port the sensor was bound to.
    /// </summary>
    [DataContract, Description("Identifies which port the sensor was bound to.")]
    public class AttachResponse
    {
        /// <summary>
        /// Connection
        /// </summary>
        [DataMember, Description("Identifies the port which was bound to the LEGO NXT Device.")]
        [DataMemberConstructor(Order = 1)]
        public LegoNxtConnection Connection;

        /// <summary>
        /// Device Model
        /// </summary>
        [DataMember, Description("Identifies the model of the device which was attached to the LEGO NXT Brick.")]
        [DataMemberConstructor(Order = 2)]
        public string DeviceModel;

        /// <summary>
        /// Attach a Sensor or Motor to the LEGO Brick
        /// </summary>
        public AttachResponse()
        {
        }

        /// <summary>
        /// Attach a Sensor or Motor to the LEGO Brick
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="deviceModel"></param>
        public AttachResponse(LegoNxtConnection connection, string deviceModel)
        {
            this.Connection = connection;
            this.DeviceModel = deviceModel;
        }

    }

    /// <summary>
    /// Configure the LEGO NXT Brick
    /// </summary>
    [DataContract, Description("Configures the LEGO NXT Brick.")]
    public class BrickConfiguration
    {
        /// <summary>
        /// Communications Serial Port
        /// </summary>
        [DataMember, Description("Specifies the Serial Port used to communicate with the LEGO NXT Brick.")]
        [DataMemberConstructor(Order = 1)]
        public int SerialPort;

        /// <summary>
        /// Communications Baud Rate
        /// </summary>
        [DataMember, Description("Specifies the Baud Rate (0 = default).")]
        [DataMemberConstructor(Order = 2)]
        public int BaudRate;

        /// <summary>
        /// Lego Connection Type
        /// </summary>
        [DataMember, Description("Identifies how the LEGO NXT Brick is connected.")]
        [DataMemberConstructor(Order = 3)]
        public LegoConnectionType ConnectionType;


        /// <summary>
        /// Specifies that the LEGO NXT Brick service will be displayed in a web browser.
        /// </summary>
        [DataMember(IsRequired=false), Description("Specifies that the LEGO NXT Brick service will be displayed in a web browser.")]
        [DataMemberConstructor(Order = 4)]
        public bool ShowInBrowser;

        /// <summary>
        /// Configure the LEGO NXT Brick
        /// </summary>
        public BrickConfiguration()
        {
        }

        /// <summary>
        /// Configure the LEGO NXT Brick
        /// </summary>
        public BrickConfiguration(int serialPort)
        {
            this.SerialPort = serialPort;
        }

        /// <summary>
        /// Configure the LEGO NXT Brick
        /// </summary>
        public BrickConfiguration(int serialPort, int baudRate, LegoConnectionType connectionType)
        {
            this.SerialPort = serialPort;
            this.BaudRate = baudRate;
            this.ConnectionType = connectionType;
        }

    }


    /// <summary>
    /// Disconnect from the LEGO NXT Brick Hardware.
    /// </summary>
    [DataContract, Description("Disconnects from the LEGO NXT Brick Hardware.")]
    public class DisconnectRequest
    {
    }


    /// <summary>
    /// A LEGO Response exception 
    /// </summary>
    [DataContract, Description("Indicates a failed LEGO Command Response.")]
    public class LegoResponseException : nxtcmd.LegoResponse
    {
        private string _errorMessage = null;
        private nxtcmd.LegoCommand _cmd = null;

        /// <summary>
        /// A LEGO Response exception 
        /// </summary>
        public LegoResponseException() 
        : base(3, LegoCommandCode.Undefined)
        {
            base.ErrorCode = LegoErrorCode.UndefinedError; 
        }

        /// <summary>
        /// A LEGO Response exception 
        /// </summary>
        public LegoResponseException(byte[] commandData)
            : base(3, LegoCommandCode.Undefined)
        {
            base.ErrorCode = LegoErrorCode.UndefinedError;
            CommandData = commandData;
        }

        /// <summary>
        /// A LEGO Response exception 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="ex"></param>
        public LegoResponseException(nxtcmd.LegoCommand cmd, Exception ex) 
            : base(3, cmd.LegoCommandCode)
        {
            base.ErrorCode = LegoErrorCode.UndefinedError;
            OriginalCommand = cmd;
            ErrorMessage = ex.ToString();
        }

        /// <summary>
        /// Calculate the length of the current packet.
        /// </summary>
        /// <returns></returns>
        private int PacketLength()
        {
            return 7 + OriginalCommandLength() + ErrorMessageLength();
        }

        /// <summary>
        /// Calculate the length of the error message.
        /// </summary>
        /// <returns></returns>
        private int ErrorMessageLength()
        {
            return (string.IsNullOrEmpty(_errorMessage)) ? 0 : _errorMessage.Length + 1;
        }

        /// <summary>
        /// Calculate the length of the original command.
        /// </summary>
        /// <returns></returns>
        private int OriginalCommandLength()
        {
            return (_cmd != null && _cmd.CommandData != null) ? _cmd.CommandData.Length : 0;
        }

        /// <summary>
        /// The underlying Command Data
        /// </summary>
        public override byte[] CommandData
        {
            get
            {
                return base.CommandData;
            }
            set
            {
                if (value == null || value.Length < 7)
                    throw new InvalidOperationException("Invalid LegoResponseException packet!");

                int originalCommandLength = NxtCommon.GetUShort(value, 3);
                int errorMessageLength = NxtCommon.GetUShort(value, 5);
                if ((7 + originalCommandLength + errorMessageLength) != value.Length)
                    throw new InvalidOperationException("Invalid LegoResponseException packet: Command and ErrorMessage Lengths do not match the packet size!");

                base.CommandData = value;
                if (originalCommandLength > 0)
                {
                    byte[] originalCommandData = new byte[originalCommandLength];
                    Buffer.BlockCopy(value, 7, originalCommandData, 0, originalCommandLength);
                    _cmd = new nxtcmd.LegoCommand(0, originalCommandData);
                }
                if (errorMessageLength > 0)
                {
                    _errorMessage = NxtCommon.DataToString(value, 7 + originalCommandLength, errorMessageLength);
                }
            }
        }

        /// <summary>
        /// LEGO Command
        /// </summary>
        [DataMember, Description("Identifies the original LEGO Command.")]
        public nxtcmd.LegoCommand OriginalCommand
        {
            get 
            {
                return _cmd;
            }
            set 
            {
                _cmd = value;
                SetCommandData();
            }
        }
        
        /// <summary>
        /// Error Message
        /// </summary>
        [DataMember, Description("Identifies the Runtime Exception.")]
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set 
            { 
                _errorMessage = value;
                SetCommandData();
            }
        }

        /// <summary>
        /// Set CommandData with the current values of ErrorMessage and OriginalCommand
        /// </summary>
        private void SetCommandData()
        {
            byte[] newBuffer;
            if (base.CommandData.Length == PacketLength())
            {
                newBuffer = base.CommandData;
            }
            else
            {
                newBuffer = new byte[PacketLength()];
                Buffer.BlockCopy(base.CommandData, 0, newBuffer, 0, 3);
            }
            NxtCommon.SetUShort(newBuffer, 3, OriginalCommandLength());
            NxtCommon.SetUShort(newBuffer, 5, ErrorMessageLength());
            if (OriginalCommandLength() > 0)
                Buffer.BlockCopy(_cmd.CommandData, 0, newBuffer, 7, _cmd.CommandData.Length);
            if (ErrorMessageLength() > 0)
                NxtCommon.SetStringToData(newBuffer, 7 + OriginalCommandLength(), _errorMessage);
        }
    }

    /// <summary>
    /// Internal Brick Status
    /// </summary>
    class InternalBrickStatus
    {
        /// <summary>
        /// The brick has been disconnected.
        /// </summary>
        public bool Disconnected;

        /// <summary>
        /// A Close request is pending.
        /// </summary>
        public bool ClosePending;
    }


    #region LEGO NXT Brick Operations

    /// <summary>
    /// NxtBrick Main Operations Port
    /// </summary>
    [ServicePort]
    public class NxtBrickOperations : PortSet
    {

        /// <summary>
        /// NxtBrick Main Operations Port
        /// </summary>
        public NxtBrickOperations(): base(
            typeof(DsspDefaultLookup),
            typeof(DsspDefaultDrop),
            typeof(Get),
            typeof(HttpGet),
            typeof(ConnectToHardware),
            typeof(DisconnectFromHardware),
            typeof(ReserveDevicePort),
            typeof(AttachAndSubscribe),
            typeof(Detach),
            typeof(AdjustPollingFrequency),
            typeof(SendLowSpeedCommand),
            typeof(SendNxtCommand),
            typeof(Subscribe),
            typeof(LegoSensorUpdate),
            typeof(PlayTone)
            )
        {
        }

        #region Implicit Operators
        /// <summary>
        /// Implicit Operator for Port of DsspDefaultLookup
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<DsspDefaultLookup>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<DsspDefaultLookup>)portSet[typeof(DsspDefaultLookup)];
        }
        /// <summary>
        /// Implicit Operator for Port of DsspDefaultDrop
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<DsspDefaultDrop>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<DsspDefaultDrop>)portSet[typeof(DsspDefaultDrop)];
        }
        /// <summary>
        /// Implicit Operator for Port of Get
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<Get>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<Get>)portSet[typeof(Get)];
        }
        /// <summary>
        /// Implicit Operator for Port of HttpGet
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<HttpGet>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<HttpGet>)portSet[typeof(HttpGet)];
        }
        /// <summary>
        /// Implicit Operator for Port of ConnectToHardware
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<ConnectToHardware>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<ConnectToHardware>)portSet[typeof(ConnectToHardware)];
        }
        /// <summary>
        /// Implicit Operator for Port of DisconnectFromHardware
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<DisconnectFromHardware>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<DisconnectFromHardware>)portSet[typeof(DisconnectFromHardware)];
        }
        /// <summary>
        /// Implicit Operator for Port of ReserveDevicePort
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<ReserveDevicePort>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<ReserveDevicePort>)portSet[typeof(ReserveDevicePort)];
        }
        /// <summary>
        /// Implicit Operator for Port of AttachAndSubscribe
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<AttachAndSubscribe>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<AttachAndSubscribe>)portSet[typeof(AttachAndSubscribe)];
        }
        /// <summary>
        /// Implicit Operator for Port of Detach
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<Detach>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<Detach>)portSet[typeof(Detach)];
        }
        /// <summary>
        /// Implicit Operator for Port of AdjustPollingFrequency
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<AdjustPollingFrequency>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<AdjustPollingFrequency>)portSet[typeof(AdjustPollingFrequency)];
        }
        /// <summary>
        /// Implicit Operator for Port of SendLowSpeedCommand
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<SendLowSpeedCommand>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<SendLowSpeedCommand>)portSet[typeof(SendLowSpeedCommand)];
        }
        /// <summary>
        /// Implicit Operator for Port of SendNxtCommand
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<SendNxtCommand>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<SendNxtCommand>)portSet[typeof(SendNxtCommand)];
        }
        /// <summary>
        /// Implicit Operator for Port of Subscribe
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<Subscribe>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<Subscribe>)portSet[typeof(Subscribe)];
        }
        /// <summary>
        /// Implicit Operator for Port of LegoSensorUpdate
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<LegoSensorUpdate>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<LegoSensorUpdate>)portSet[typeof(LegoSensorUpdate)];
        }

        /// <summary>
        /// Implicit Operator for Port of PlayTone
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<PlayTone>(NxtBrickOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<PlayTone>)portSet[typeof(PlayTone)];
        }

        #endregion

        #region Post Overrides
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
        /// Post(HttpGet)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(HttpGet item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(ConnectToHardware)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(ConnectToHardware item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(DisconnectFromHardware)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(DisconnectFromHardware item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(ReserveDevicePort)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(ReserveDevicePort item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(AttachAndSubscribe)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(AttachAndSubscribe item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(Detach)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(Detach item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(AdjustPollingFrequency)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(AdjustPollingFrequency item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(SendLowSpeedCommand)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(SendLowSpeedCommand item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(SendNxtCommand)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(SendNxtCommand item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(Subscribe)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(Subscribe item) { base.PostUnknownType(item); }
        /// <summary>
        /// Post(LegoSensorUpdate)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(LegoSensorUpdate item) { base.PostUnknownType(item); }

        /// <summary>
        /// Post(PlayTone)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Post(PlayTone item) { base.PostUnknownType(item); }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Post Dssp Default Lookup and return the response port.
        /// </summary>
        public virtual PortSet<LookupResponse, Fault> DsspDefaultLookup()
        {
            LookupRequestType body = new LookupRequestType();
            DsspDefaultLookup op = new DsspDefaultLookup(body);
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Dssp Default Lookup with body and return the response port.
        /// </summary>
        public virtual PortSet<LookupResponse, Fault> DsspDefaultLookup(LookupRequestType body)
        {
            DsspDefaultLookup op = new DsspDefaultLookup();
            op.Body = body ?? new LookupRequestType();
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Dssp Default Drop and return the response port.
        /// </summary>
        public virtual PortSet<DefaultDropResponseType, Fault> DsspDefaultDrop()
        {
            DropRequestType body = new DropRequestType();
            DsspDefaultDrop op = new DsspDefaultDrop(body);
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Dssp Default Drop with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultDropResponseType, Fault> DsspDefaultDrop(DropRequestType body)
        {
            DsspDefaultDrop op = new DsspDefaultDrop();
            op.Body = body ?? new DropRequestType();
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Get and return the response port.
        /// </summary>
        public virtual PortSet<NxtBrickState, Fault> Get()
        {
            GetRequestType body = new GetRequestType();
            Get op = new Get(body);
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Get with body and return the response port.
        /// </summary>
        public virtual PortSet<NxtBrickState, Fault> Get(GetRequestType body)
        {
            Get op = new Get();
            op.Body = body ?? new GetRequestType();
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Http Get and return the response port.
        /// </summary>
        public virtual PortSet<HttpResponseType, Fault> HttpGet()
        {
            HttpGetRequestType body = new HttpGetRequestType();
            HttpGet op = new HttpGet(body);
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Http Get with body and return the response port.
        /// </summary>
        public virtual PortSet<HttpResponseType, Fault> HttpGet(HttpGetRequestType body)
        {
            HttpGet op = new HttpGet();
            op.Body = body ?? new HttpGetRequestType();
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Connect To Hardware with parameters and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToHardware(int serialPort, int baudRate, LegoConnectionType connectionType)
        {
            BrickConfiguration body = new BrickConfiguration(serialPort, baudRate, connectionType);
            ConnectToHardware op = new ConnectToHardware(body);
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Connect To Hardware with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToHardware(BrickConfiguration body)
        {
            ConnectToHardware op = new ConnectToHardware();
            op.Body = body ?? new BrickConfiguration();
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Disconnect From Hardware and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> DisconnectFromHardware()
        {
            DisconnectFromHardware op = new DisconnectFromHardware(new DisconnectRequest());
            this.Post(op);
            return op.ResponsePort;
        }
        /// <summary>
        /// Post Disconnect From Hardware with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> DisconnectFromHardware(DisconnectRequest body)
        {
            DisconnectFromHardware op = new DisconnectFromHardware(body);
            this.Post(op);
            return op.ResponsePort;
        }
        /// <summary>
        /// Post Reserve Device Port with parameters and return the response port.
        /// </summary>
        public virtual PortSet<AttachResponse, Fault> ReserveDevicePort(LegoNxtConnection connection, LegoDeviceType deviceType, string deviceModel, string deviceContract, string serviceUri, string name)
        {
            Registration body = new Registration(connection, deviceType, deviceModel, deviceContract, serviceUri, name);
            ReserveDevicePort op = new ReserveDevicePort(body);
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Reserve Device Port with body and return the response port.
        /// </summary>
        public virtual PortSet<AttachResponse, Fault> ReserveDevicePort(Registration body)
        {
            ReserveDevicePort op = new ReserveDevicePort();
            op.Body = body ?? new Registration();
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Attach And Subscribe and return the response port.
        /// </summary>
        public virtual PortSet<AttachResponse, Fault> AttachAndSubscribe(IPort notificationPort)
        {
            AttachAndSubscribe op = new AttachAndSubscribe();
            op.Body = new AttachRequest();
            op.NotificationPort = notificationPort;
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Attach And Subscribe with body and return the response port.
        /// </summary>
        public virtual PortSet<AttachResponse, Fault> AttachAndSubscribe(AttachRequest body, IPort notificationPort)
        {
            AttachAndSubscribe op = new AttachAndSubscribe();
            op.Body = body ?? new AttachRequest();
            op.NotificationPort = notificationPort;
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Detach with parameters and return the response port.
        /// </summary>
        public virtual PortSet<DefaultSubmitResponseType, Fault> Detach(string serviceUri)
        {
            DetachRequest body = new DetachRequest(serviceUri);
            Detach op = new Detach(body);
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Detach with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultSubmitResponseType, Fault> Detach(DetachRequest body)
        {
            Detach op = new Detach();
            op.Body = body ?? new DetachRequest();
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post AdjustPollingFrequency with parameters and return the response port.
        /// </summary>
        /// <param name="serviceUri"></param>
        /// <param name="pollingFrequencyMs"></param>
        /// <returns></returns>
        public virtual PortSet<AdjustPollingFrequencyResponse, Fault> AdjustPollingFrequency(string serviceUri, int pollingFrequencyMs)
        {
            AdjustPollingFrequencyRequest body = new AdjustPollingFrequencyRequest(serviceUri, pollingFrequencyMs);
            AdjustPollingFrequency op = new AdjustPollingFrequency(body);
            this.Post(op);
            return op.ResponsePort;
        }
        /// <summary>
        /// Post AdjustPollingFrequency with body and return the response port.
        /// </summary>
        public virtual PortSet<AdjustPollingFrequencyResponse, Fault> AdjustPollingFrequency(AdjustPollingFrequencyRequest body)
        {
            AdjustPollingFrequency op = new AdjustPollingFrequency();
            op.Body = body ?? new AdjustPollingFrequencyRequest();
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Send Low Speed Command and return the response port.
        /// </summary>
        public virtual PortSet<nxtcmd.LegoResponse, Fault> SendLowSpeedCommand()
        {
            nxtcmd.LegoLSWrite body = new nxtcmd.LegoLSWrite();
            SendLowSpeedCommand op = new SendLowSpeedCommand(body);
            this.Post(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Send Low Speed Command with body and return the response port.
        /// </summary>
        public virtual PortSet<nxtcmd.LegoResponse, Fault> SendLowSpeedCommand(nxtcmd.LegoLSWrite body)
        {
            SendLowSpeedCommand op = new SendLowSpeedCommand();
            op.Body = body ?? new nxtcmd.LegoLSWrite();
            this.Post(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Send Nxt Command with body and return the response port.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public virtual PortSet<nxtcmd.LegoResponse, Fault> SendNxtCommand(nxtcmd.LegoCommand body)
        {
            SendNxtCommand op = new SendNxtCommand(body);
            this.Post(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Send Nxt Command with body and return the response port.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="requireResponse"></param>
        /// <returns></returns>
        public virtual PortSet<nxtcmd.LegoResponse, Fault> SendNxtCommand(nxtcmd.LegoCommand body, bool requireResponse)
        {
            SendNxtCommand op = new SendNxtCommand(body);
            op.Body.RequireResponse = requireResponse;
            this.Post(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Send Nxt Command with body and return the response port.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        public virtual PortSet<nxtcmd.LegoResponse, Fault> SendNxtCommand(nxtcmd.LegoCommand body, int tryCount)
        {
            SendNxtCommand op = new SendNxtCommand(body);
            op.Body.TryCount = tryCount;
            this.Post(op);
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
            this.Post(op);
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
            this.Post(op);
            return op.ResponsePort;

        }
        
        /// <summary>
        /// Post Play Tone with parameters and return the response port.
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.DefaultSubmitResponseType, Fault> PlayTone(int frequency, int duration)
        {
            PlayTone op = new PlayTone(frequency, duration);
            this.Post(op);
            return op.ResponsePort;
        }


        /// <summary>
        /// Post Play Tone with body and return the response port.
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.DefaultSubmitResponseType, Fault> PlayTone(Note body)
        {
            PlayTone op = new PlayTone();
            op.Body = body ?? new Note();
            this.Post(op);
            return op.ResponsePort;

        }
       

        #endregion

    }


    /// <summary>
    /// NxtBrick Get Operation
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Brick.")]
    public class Get : Get<GetRequestType, PortSet<NxtBrickState, Fault>>
    {
        /// <summary>
        /// NxtBrick Get Operation
        /// </summary>
        public Get()
        {
        }
        /// <summary>
        /// NxtBrick Get Operation
        /// </summary>
        public Get(GetRequestType body)
            :
                base(body)
        {
        }
        /// <summary>
        /// NxtBrick Get Operation
        /// </summary>
        public Get(GetRequestType body, PortSet<NxtBrickState, Fault> responsePort)
            :
                base(body, responsePort)
        {
        }
    }


    /// <summary>
    /// Indicates a period sensor was updated. Valid only for LEGO Device services
    /// </summary>
    [DisplayName("(User) InternalSensorUpdate")]
    [Description("Provides custom notifications which indicates a periodic sensor was updated.\n*** Valid only for third party LEGO Device services. ***")]
    public class LegoSensorUpdate : Update<nxtcmd.LegoResponse, PortSet<DefaultUpdateResponseType, Fault>> { }

    /// <summary>
    /// Play a tone on the internal LEGO NXT Speaker
    /// </summary>
    [Description("Plays a tone on the internal LEGO NXT Speaker.")]
    public class PlayTone : Submit<Note, PortSet<DefaultSubmitResponseType, Fault>> 
    {
        /// <summary>
        /// Play a tone on the internal LEGO NXT Speaker
        /// </summary>
        public PlayTone() { }

        /// <summary>
        /// Play a tone on the internal LEGO NXT Speaker
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="duration"></param>
        public PlayTone(int frequency, int duration)
        {
            this.Body = new Note(frequency, duration);
        }
    }

    /// <summary>
    /// Send a direct LEGO Command to the brick and wait for the response data
    /// </summary>
    [Description("Sends a direct LEGO Command to the brick and waits for the response data.")]
    [DisplayName("(User) SendRawCommand")]
    public class SendNxtCommand : Query<nxtcmd.LegoCommand, PortSet<nxtcmd.LegoResponse, Fault>>
    {
        /// <summary>
        /// Send a direct LEGO Command to the brick and wait for the response data
        /// </summary>
        public SendNxtCommand()
        {
        }
        /// <summary>
        /// Send a direct LEGO Command to the brick and wait for the response data
        /// </summary>
        public SendNxtCommand(nxtcmd.LegoCommand body)
            : base(body)
        {
        }
        /// <summary>
        /// Send a direct LEGO Command to the brick and wait for the response data
        /// </summary>
        public SendNxtCommand(nxtcmd.LegoCommand body, PortSet<nxtcmd.LegoResponse, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Send a direct LEGO Command to the brick and wait for the response data
    /// </summary>
    [Description("Sends a LEGO Command to the Low Speed interface and waits for the response data.")]
    [DisplayName("(User) SendRawLowSpeedCommand")]
    public class SendLowSpeedCommand : Query<nxtcmd.LegoLSWrite, PortSet<nxtcmd.LegoResponse, Fault>>
    {
        /// <summary>
        /// Send a LEGO Command to the Low Speed interface and wait for the response data
        /// </summary>
        public SendLowSpeedCommand()
        {
        }
        /// <summary>
        /// Send a LEGO Command to the Low Speed interface and wait for the response data
        /// </summary>
        public SendLowSpeedCommand(nxtcmd.LegoLSWrite body)
            : base(body)
        {
        }
        /// <summary>
        /// Send a LEGO Command to the Low Speed interface and wait for the response data
        /// </summary>
        public SendLowSpeedCommand(nxtcmd.LegoLSWrite body, PortSet<nxtcmd.LegoResponse, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Subscribe to LEGO NXT Brick Connection notifications
    /// </summary>
    [Description("Subscribes to LEGO NXT Brick Connection notifications.")]
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>>
    {
    }

    /// <summary>
    /// Configure the LEGO NXT Brick
    /// </summary>
    [Description("Connects to the LEGO NXT Brick Hardware.")]
    public class ConnectToHardware : Update<BrickConfiguration, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Configure the LEGO NXT Brick
        /// </summary>
        public ConnectToHardware()
        {
        }
        /// <summary>
        /// Configure the LEGO NXT Brick
        /// </summary>
        public ConnectToHardware(BrickConfiguration body)
            : base(body)
        {
        }
        /// <summary>
        /// Configure the LEGO NXT Brick
        /// </summary>
        public ConnectToHardware(BrickConfiguration body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            :
                base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Disconnect from the LEGO NXT Brick Hardware.
    /// </summary>
    [Description("Disconnects from the LEGO NXT Brick Hardware.")]
    public class DisconnectFromHardware : Update<DisconnectRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Disconnect from the LEGO NXT Brick Hardware.
        /// </summary>
        public DisconnectFromHardware()
            : base()
        {
        }

        /// <summary>
        /// Disconnect from the LEGO NXT Brick Hardware.
        /// </summary>
        public DisconnectFromHardware(DisconnectRequest body)
            : base(body)
        {
        }
        /// <summary>
        /// Disconnect from the LEGO NXT Brick Hardware.
        /// </summary>
        public DisconnectFromHardware(DisconnectRequest body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            :
                base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Detach a Sensor or Motor from the LEGO Brick
    /// </summary>
    [Description("Detaches a Sensor or Motor from the LEGO Brick\n*** Valid only for third party LEGO Device services. ***")]
    [DisplayName("(User) InternalDetachDevice")]
    public class Detach : Submit<DetachRequest, PortSet<DefaultSubmitResponseType, Fault>>
    {
        /// <summary>
        /// ( Internal) Detach a Sensor or Motor from the LEGO Brick
        /// Used by custom LEGO NXT sensor and actuator services.
        /// </summary>
        public Detach()
        {
        }
        /// <summary>
        /// ( Internal) Detach a Sensor or Motor from the LEGO Brick
        /// Used by custom LEGO NXT sensor and actuator services.
        /// </summary>
        public Detach(DetachRequest body)
            : base(body)
        {
        }
        /// <summary>
        /// ( Internal) Detach a Sensor or Motor from the LEGO Brick
        /// Used by custom LEGO NXT sensor and actuator services.
        /// </summary>
        public Detach(DetachRequest body, PortSet<DefaultSubmitResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }

    }

    /// <summary>
    /// AdjustPollingFrequency for a LEGO NXT device.
    /// </summary>
    [Description("Adjust the Polling Frequency of a Sensor or Motor.\n*** Valid only for third party LEGO Device services. ***")]
    [DisplayName("(User) InternalAdjustDevicePollingFrequency")]
    public class AdjustPollingFrequency : Submit<AdjustPollingFrequencyRequest, PortSet<AdjustPollingFrequencyResponse, Fault>>
    {
        /// <summary>
        /// (Internal) AdjustPollingFrequency for a LEGO NXT device.
        /// Used by custom LEGO NXT sensor and actuator services.
        /// </summary>
        public AdjustPollingFrequency()
        {
        }
        /// <summary>
        /// (Internal) AdjustPollingFrequency for a LEGO NXT device.
        /// Used by custom LEGO NXT sensor and actuator services.
        /// </summary>
        public AdjustPollingFrequency(AdjustPollingFrequencyRequest body)
            : base(body)
        {
        }
        /// <summary>
        /// (Internal) AdjustPollingFrequency for a LEGO NXT device.
        /// Used by custom LEGO NXT sensor and actuator services.
        /// </summary>
        public AdjustPollingFrequency(AdjustPollingFrequencyRequest body, PortSet<AdjustPollingFrequencyResponse, Fault> responsePort)
            : base(body, responsePort)
        {
        }

    }


    /// <summary>
    /// Attach a Sensor or Motor to the LEGO Brick
    /// and send notifications when sensor data is updated.
    /// </summary>
    [Description("Attaches a Sensor or Motor to the LEGO Brick\n*** Used by custom LEGO NXT sensor and actuator services. ***")]
    [DisplayName("(User) InternalAttachDevice")]
    public class AttachAndSubscribe : Subscribe<AttachRequest, PortSet<AttachResponse, Fault>>
    {
        /// <summary>
        /// Attach a Sensor or Motor to the LEGO Brick
        /// </summary>
        public AttachAndSubscribe()
        {
        }
        /// <summary>
        /// Attach a Sensor or Motor to the LEGO Brick
        /// </summary>
        public AttachAndSubscribe(AttachRequest body)
            :
                base(body)
        {
        }
        /// <summary>
        /// Attach a Sensor or Motor to the LEGO Brick
        /// </summary>
        public AttachAndSubscribe(AttachRequest body, PortSet<AttachResponse, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Reserve a port for the specified device
    /// </summary>
    [DisplayName("(User) InternalReserveDevicePort")]
    [Description("Reserves a port for the specified device\n*** Valid only for third party LEGO Device services. ***")]
    public class ReserveDevicePort : Submit<Registration, PortSet<AttachResponse, Fault>> 
    {
        /// <summary>
        /// Internal Reserve Device Port
        /// </summary>
        public ReserveDevicePort()
        {
        }
        /// <summary>
        /// Internal Reserve Device Port
        /// </summary>
        public ReserveDevicePort(Registration body)
            :
                base(body)
        {
        }
        /// <summary>
        /// Internal Reserve Device Port
        /// </summary>
        public ReserveDevicePort(Registration body, PortSet<AttachResponse, Fault> responsePort)
            : base(body, responsePort)
        {
        }

    }

    #endregion

    #region Contract

    /// <summary>
    /// NxtBrick Contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// The Dss Service contract
        /// </summary>
        [DataMember, Description("Identifies the unique DSS Contract for the Lego NXT Brick (v2) service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/brick.user.html";
    }

    /// <summary>
    /// Categories published by Microsoft to group LEGO services together.
    /// These categories are available for use by any Dss Service which works in conjunction with the LEGO services.
    /// </summary>
    [DataContract, Description("Identifies Categories published by Microsoft to group LEGO services together.")]
    public sealed class LegoCategories
    {
        /// <summary>
        /// Indicates that the service works specifically with LEGO RCX.
        /// </summary>
        [DataMember]
        [Description("Indicates that the service works specifically with LEGO RCX.")]
        public const string RCX = "http://schemas.microsoft.com/categories/robotics/lego/rcx.html";

        /// <summary>
        /// Indicates that the service works specifically with the 'LEGO(R) NXT Brick' service.
        /// </summary>
        [DataMember]
        [Description("Indicates that the service works specifically with the 'LEGO(R) NXT Brick' service.")]
        public const string NXT = "http://schemas.microsoft.com/categories/robotics/lego/nxt.html";
    }

    #endregion
}
