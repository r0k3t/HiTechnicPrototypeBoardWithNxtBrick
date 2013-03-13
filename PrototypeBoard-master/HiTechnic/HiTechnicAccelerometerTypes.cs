//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: HiTechnicAccelerometerTypes.cs $ $Revision: 19 $
//-----------------------------------------------------------------------

using Microsoft.Dss.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Ccr.Core;
using W3C.Soap;

using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;

using pxbrick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick.Proxy;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;


namespace Microsoft.Robotics.Services.Sample.HiTechnic.Accelerometer
{
    
    /// <summary>
    /// HiTechnic Accelerometer Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique DSS Contract Identifier for the HiTechnic Acceleration Sensor service
        [DataMember, Description("Identifies the Unique DSS Contract Identifier for the HiTechnic Acceleration Sensor service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/hitechnic/nxt/accelerometer.user.html";

        /// <summary>
        /// The I2C Vendor code
        /// </summary>
        [DataMember, Description("Identifies the I2C Vendor code.")]
        public const string Vendor = "HiTechnc";

        /// <summary>
        /// The Accelerometer Sensor Device Type
        /// </summary>
        [DataMember, Description("Identifies the I2C Device Model.")]
        public const string DeviceModel = "Accel.";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 500;

    }

    /// <summary>
    /// HiTechnic Accelerometer State
    /// </summary>
    [DataContract, Description("Specifies the HiTechnic Acceleration Sensor state.")]
    public class AccelerometerState 
    {
        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// The name of this Accelerometer Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the HiTechnic Accelerometer Sensor.")]
        public string Name;

        /// <summary>
        /// LEGO NXT Sensor Port
        /// </summary>
        [DataMember, Description("Specifies the LEGO NXT Sensor Port.")]
        public NxtSensorPort SensorPort;

        /// <summary>
        /// Polling Freqency (ms)
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds (0 = default).")]
        public int PollingFrequencyMs;

        /// <summary>
        /// Setting ZeroOffset with the initial Tilt values of the robot
        /// will calibrate the acceleration sensor to an initial pose of X=0, Y=0, Z=0.
        /// </summary>
        [DataMember, Description("Calibrates the initial readings of the Accelerometer. \n"
        + "Setting ZeroOffset with the initial Tilt values of the robot \n"
        + "will calibrate the acceleration sensor to an initial pose of X=0, Y=0, Z=0.")]
        public AccelerometerReading ZeroOffset;

        /// <summary>
        /// The current tilt readings received from the Accelerometer Sensor
        /// </summary>
        [DataMember, Description("Indicates the current tilt readings received from the Acceleration Sensor.")]
        [Browsable(false)]
        public AccelerometerReading Tilt;

    }

    /// <summary>
    /// Get the HiTechnic Accelerometer Sensor State
    /// </summary>
    [Description("Gets the current state of the HiTechnic Accelerometer sensor.")]
    public class Get : Get<GetRequestType, PortSet<AccelerometerState, Fault>>
    {
    }

    /// <summary>
    /// Get the HiTechnic Accelerometer Sensor State
    /// </summary>
    [Description("Indicates an update to the HiTechnic Accelerometer sensor.")]
    public class AccelerometerUpdate : Update<AccelerometerReading, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }


    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate")]
    [Description("Connects the HiTechnic Accelerometer Sensor to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<AccelerometerConfig, PortSet<DefaultUpdateResponseType, Fault>>
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
        /// <param name="sensorPort"></param>
        public ConnectToBrick(NxtSensorPort sensorPort)
            : base(new AccelerometerConfig(sensorPort))
        {
        }

    }

    /// <summary>
    /// HiTechnic Accelerometer Sensor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the HiTechnic Accelerometer Sensor Configuration.")]
    public class AccelerometerConfig
    {
        /// <summary>
        /// HiTechnic Accelerometer Sensor Configuration.
        /// </summary>
        public AccelerometerConfig() { }

        /// <summary>
        /// HiTechnic Accelerometer Sensor Configuration.
        /// </summary>
        /// <param name="sensorPort"></param>
        public AccelerometerConfig(NxtSensorPort sensorPort)
        {
            this.SensorPort = sensorPort;
        }

        /// <summary>
        /// The name of this Accelerometer Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the HiTechnic Accelerometer Sensor.")]
        public string Name;

        /// <summary>
        /// LEGO NXT Sensor Port
        /// </summary>
        [DataMember, Description("Specifies the LEGO NXT Sensor Port.")]
        [DataMemberConstructor]
        public NxtSensorPort SensorPort;

        /// <summary>
        /// Polling Freqency (ms)
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds (0 = default).")]
        public int PollingFrequencyMs;

    }

    /// <summary>
    /// Subscribe to the HiTechnic Acceleration Sensor
    /// </summary>
    [Description("Subscribes to the HiTechnic Acceleration Sensor.")]
    public class ReliableSubscribe : Subscribe<ReliableSubscribeRequestType, DsspResponsePort<SubscribeResponseType>, AccelerometerOperations> { }

    /// <summary>
    /// Subscribe to the HiTechnic Acceleration Sensor
    /// </summary>
    [Description("Subscribes to the HiTechnic Acceleration Sensor.")]
    public class Subscribe : Subscribe<SubscribeRequestType, DsspResponsePort<SubscribeResponseType>, AccelerometerOperations> { }

    /// <summary>
    /// HiTechnic Accelerometer Sensor Operations Port
    /// </summary>
    [ServicePort]
    public class AccelerometerOperations : PortSet
    {
        /// <summary>
        /// Accelerometer Sensor Operations Port
        /// </summary>
        public AccelerometerOperations()
            : base(
                typeof(DsspDefaultLookup),
                typeof(DsspDefaultDrop),
                typeof(ConnectToBrick),
                typeof(dssphttp.HttpGet),
                typeof(Get),
                typeof(AccelerometerUpdate),     
                typeof(ReliableSubscribe),
                typeof(Subscribe)
            )
        { }




        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(AccelerometerConfig body)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = body ?? new AccelerometerConfig();
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(AccelerometerState state)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = new AccelerometerConfig(state.SensorPort);
            op.Body.Name = state.Name;
            op.Body.PollingFrequencyMs = state.PollingFrequencyMs;

            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        /// <param name="sensorPort"></param>
        /// <returns></returns>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(NxtSensorPort sensorPort)
        {
            ConnectToBrick op = new ConnectToBrick(sensorPort);
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

    }




    /// <summary>
    /// HiTechnic Read Accelerometer Sensor Data
    /// </summary>
    [DataContract, Description("Reads the I2C HiTechnic Acceleration sensor.")]
    public class I2CReadHiTechnicAccelerationSensor : nxtcmd.LegoLSWrite
    {
        /// <summary>
        /// HiTechnic Read Accelerometer Sensor Data
        /// </summary>
        public I2CReadHiTechnicAccelerationSensor()
            : base()
        {
            base.TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x42 };
            base.ExpectedI2CResponseSize = 6;
            base.Port = 0;
        }

        /// <summary>
        /// HiTechnic Read Accelerometer Sensor Data
        /// </summary>
        /// <param name="port"></param>
        public I2CReadHiTechnicAccelerationSensor(NxtSensorPort port)
            : base()
        {
            TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x42 };
            ExpectedI2CResponseSize = 6;
            Port = port;
        }

        /// <summary>
        /// The matching LegoResponse
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public nxtcmd.LegoResponse GetResponse(byte[] responseData)
        {
            return new I2CResponseHiTechnicAccelerationSensor(responseData);
        }


    }

    /// <summary>
    /// LegoResponse: I2C Sensor Type
    /// </summary>
    [DataContract, Description("Indicates the Acceleration sensor reading.")]
    public class I2CResponseHiTechnicAccelerationSensor : nxtcmd.LegoResponse
    {
        /// <summary>
        /// LegoResponse: I2C Sensor Type
        /// </summary>
        public I2CResponseHiTechnicAccelerationSensor()
            : base(20, LegoCommandCode.LSRead)
        {
        }

        /// <summary>
        /// LegoResponse: I2C Sensor Type
        /// </summary>
        /// <param name="responseData"></param>
        public I2CResponseHiTechnicAccelerationSensor(byte[] responseData)
            : base(20, LegoCommandCode.LSRead, responseData) { }


        /// <summary>
        /// Calculate a signed 10-bit integer
        /// </summary>
        /// <param name="low2"></param>
        /// <param name="high8"></param>
        /// <returns></returns>
        private int Int10(byte high8, byte low2)
        {
            int raw = ((int)high8) * 4 + (int)low2;
            if (raw >= 512)
                return raw - 1024;
            return raw;
        }

        /// <summary>
        /// Set low and high bytes for a 10-bit integer
        /// </summary>
        /// <param name="source"></param>
        /// <param name="high8"></param>
        /// <param name="low2"></param>
        private void SetInt10(int source, ref byte high8, ref byte low2)
        {
            if (source < 0)
                source += 1024;

            high8 = (byte)(source >> 2);
            low2 =  (byte)(source & 3);
        }

        /// <summary>
        /// X Axis 
        /// </summary>
        [DataMember, Description("Indicates the X Axis.")]
        public int X
        {
            get
            {
                if (CommandData == null || CommandData.Length < 10)
                    return 0;

                return Int10(CommandData[4], CommandData[7]);
            }
            set
            {
                SetInt10(value, ref CommandData[4], ref CommandData[7]);
            }
        }


        /// <summary>
        /// Y Axis 
        /// </summary>
        [DataMember, Description("Indicates the Y Axis.")]
        public int Y
        {
            get
            {
                if (CommandData == null || CommandData.Length < 10)
                    return 0;

                return Int10(CommandData[5], CommandData[8]);
            }
            set
            {
                SetInt10(value, ref CommandData[5], ref CommandData[8]);
            }
        }


        /// <summary>
        /// Z Axis 
        /// </summary>
        [DataMember, Description("Indicates the Z Axis.")]
        public int Z
        {
            get
            {
                if (CommandData == null || CommandData.Length < 10)
                    return 0;

                return Int10(CommandData[6], CommandData[9]);
            }
            set
            {
                SetInt10(value, ref CommandData[6], ref CommandData[9]);
            }
        }

        /// <summary>
        /// Valid Accelerometer Response Packet
        /// </summary>
        public override bool Success
        {
            get
            {
                return base.Success
                    && this.CommandData[7] <= 3
                    && this.CommandData[8] <= 3
                    && this.CommandData[9] <= 3;
            }
        }

    }

    /// <summary>
    /// HiTechnic Accelerometer Mode
    /// </summary>
    [DataContract, Description("Specifies the HiTechnic Acceleration Sensor Mode.")]
    public enum AccelerometerMode
    {
        /// <summary>
        /// The Accelerometer is in Sensor Mode
        /// </summary>
        Sensor = 0x00,
        /// <summary>
        /// The Accelerometer failed Calibration
        /// </summary>
        CalibrationFailed = 0x02,
        /// <summary>
        /// The Accelerometer is in Calibration Mode
        /// </summary>
        Calibration = 0x43,
        /// <summary>
        /// The Accelerometer is in an Unknown Mode
        /// </summary>
        Unknown = 0xFF,
    }
}
