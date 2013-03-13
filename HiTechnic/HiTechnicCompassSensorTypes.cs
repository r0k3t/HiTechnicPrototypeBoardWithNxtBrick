//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: HiTechnicCompassSensorTypes.cs $ $Revision: 17 $
//-----------------------------------------------------------------------

using System.Diagnostics;
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
using pxanalogsensor = Microsoft.Robotics.Services.AnalogSensor.Proxy;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;


namespace Microsoft.Robotics.Services.Sample.HiTechnic.Compass
{
    
    /// <summary>
    /// HiTechnic Compass Sensor Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique DSS Contract Identifier for the HiTechnic Compass Sensor service.
        [DataMember, Description("Identifies the Unique DSS Contract Identifier for the HiTechnic Compass Sensor service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/hitechnic/nxt/compasssensor.user.html";

        /// <summary>
        /// The I2C Vendor code
        /// </summary>
        [DataMember, Description("Identifies the I2C Vendor code.")]
        public const string Vendor = "HiTechnc";

        /// <summary>
        /// The I2C Device Model
        /// </summary>
        [Description("Identifies the I2C Device Model.")]
        public const string DeviceModel = "Compass";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 500;

    }

    /// <summary>
    /// HiTechnic Compass State
    /// </summary>
    [DataContract, Description("Specifies the HiTechnic Compass Sensor state.")]
    public class CompassSensorState 
    {
        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// The name of this Compass Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the HiTechnic Compass Sensor.")]
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
        /// The current compass heading received from the Compass Sensor
        /// </summary>
        [DataMember, Description("Indicates the current compass heading received from the Compass Sensor.")]
        [Browsable(false)]
        public CompassReading Heading;

    }

    /// <summary>
    /// The current compass heading received from the Compass Sensor
    /// </summary>
    [DataContract, Description("Indicates the current compass heading received from the Compass Sensor.")]
    public class CompassReading
    {

        /// <summary>
        /// The current compass heading (degrees) received from the Compass Sensor.
        /// </summary>
        [DataMember, Description("Indicates the current compass heading (degrees) received from the Compass Sensor.")]
        [Browsable(false)]
        public double Degrees;

        /// <summary>
        /// The time of the last sensor update
        /// </summary>
        [DataMember, Description("Indicates the time of the last sensor reading.")]
        [Browsable(false)]
        public DateTime TimeStamp;
    }

    /// <summary>
    /// Get the HiTechnic Compass Sensor State
    /// </summary>
    [Description("Gets the current state of the HiTechnic Compass sensor.")]
    public class Get : Get<GetRequestType, PortSet<CompassSensorState, Fault>>
    {
    }

    /// <summary>
    /// Get the HiTechnic Compass Sensor State
    /// </summary>
    [Description("Indicates an update to the HiTechnic Compass sensor.")]
    public class CompassSensorUpdate : Update<CompassReading, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }

    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate")]
    [Description("Connects the HiTechnic Compass Sensor to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<CompassConfig, PortSet<DefaultUpdateResponseType, Fault>>
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
            : base(new CompassConfig(sensorPort))
        {
        }

    }

    /// <summary>
    /// HiTechnic Compass Sensor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the HiTechnic Compass Sensor Configuration.")]
    public class CompassConfig
    {
        /// <summary>
        /// HiTechnic Compass Sensor Configuration.
        /// </summary>
        public CompassConfig() { }

        /// <summary>
        /// HiTechnic Compass Sensor Configuration.
        /// </summary>
        /// <param name="sensorPort"></param>
        public CompassConfig(NxtSensorPort sensorPort)
        {
            this.SensorPort = sensorPort;
        }

        /// <summary>
        /// The name of this Compass Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the HiTechnic Compass Sensor.")]
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
    /// HiTechnic Compass Sensor Operations Port
    /// </summary>
    [ServicePort]
    public class CompassSensorOperations : PortSet
    {
        /// <summary>
        /// Compass Sensor Operations Port
        /// </summary>
        public CompassSensorOperations()
            : base(
                typeof(DsspDefaultLookup),
                typeof(DsspDefaultDrop),
                typeof(ConnectToBrick),
                typeof(dssphttp.HttpGet),
                typeof(Get),
                typeof(CompassSensorUpdate),     
                typeof(pxanalogsensor.ReliableSubscribe),
                typeof(pxanalogsensor.Subscribe)
            )
        { }


        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(CompassConfig body)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = body ?? new CompassConfig();
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(CompassSensorState state)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = new CompassConfig(state.SensorPort);
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
    /// HiTechnic Read Compass Sensor Data
    /// </summary>
    [DataContract, Description("Reads the I2C HiTechnic Compass sensor.")]
    public class I2CReadHiTechnicCompassSensor : nxtcmd.LegoLSWrite
    {
        /// <summary>
        /// HiTechnic Read Compass Sensor Data
        /// </summary>
        public I2CReadHiTechnicCompassSensor()
            : base()
        {
            base.TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x41 };
            base.ExpectedI2CResponseSize = 5;
            base.Port = 0;
        }

        /// <summary>
        /// HiTechnic Read Compass Sensor Data
        /// </summary>
        /// <param name="port"></param>
        public I2CReadHiTechnicCompassSensor(NxtSensorPort port)
            : base()
        {
            TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x08 };
            this.ExpectedI2CResponseSize = 4;
            Port = port;
        }

        /// <summary>
        /// The matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public nxtcmd.LegoResponse GetResponse(byte[] responseData)
        {
            return new I2CResponseHiTechnicCompassSensor(responseData);
        }


    }

    /// <summary>
    /// LegoResponse: I2C Sensor Type
    /// </summary>
    [DataContract, Description("Reads the HiTechnic I2C Compass sensor.")]
    public class I2CResponseHiTechnicCompassSensor : nxtcmd.LegoResponse
    {
        /// <summary>
        /// LegoResponse: I2C Sensor Type
        /// </summary>
        public I2CResponseHiTechnicCompassSensor()
            : base(20, LegoCommandCode.LSRead) { }

        /// <summary>
        /// LegoResponse: I2C Sensor Type
        /// </summary>
        /// <param name="responseData"></param>
        public I2CResponseHiTechnicCompassSensor(byte[] responseData)
            : base(20, LegoCommandCode.LSRead, responseData) { }

        /// <summary>
        /// HiTechnic Compass Mode
        /// </summary>
        [DataMember, Description("Specifies the HiTechnic Compass Mode.")]
        public CompassMode ModeControl
        {
            get
            {
                if (CommandData == null || CommandData.Length < 5)
                    return CompassMode.Unknown;
                
                CompassMode mode = (CompassMode)CommandData[4];
                switch (mode)
                {
                    case CompassMode.Calibration:
                    case CompassMode.CalibrationFailed:
                    case CompassMode.Sensor:
                        return mode;
                }

                return CompassMode.Unknown;
            }
            set
            {
                CommandData[4] = (byte)value;
            }
        }


        /// <summary>
        /// Compass Heading
        /// </summary>
        [DataMember, Description("Indicates the Compass Heading.")]
        public int Heading
        {
            get
            {
                if (!Success)
                    return -1;

                return (int)NxtCommon.GetUShort(this.CommandData, 7);
            }
            set
            {
            }
        }

        /// <summary>
        /// Validate the Compass Mode
        /// </summary>
        public override bool Success
        {
            get
            {
                return (base.Success && ModeControl != CompassMode.Unknown);
            }
        }
    }

    /// <summary>
    /// HiTechnic Compass Mode
    /// </summary>
    [DataContract, Description("Specifies the HiTechnic Compass Mode.")]
    public enum CompassMode
    {
        /// <summary>
        /// The Compass is in Sensor Mode
        /// </summary>
        Sensor = 0x00,
        /// <summary>
        /// The Compass failed Calibration
        /// </summary>
        CalibrationFailed = 0x02,
        /// <summary>
        /// The Compass is in Calibration Mode
        /// </summary>
        Calibration = 0x43,
        /// <summary>
        /// The Compass is in an Unknown Mode
        /// </summary>
        Unknown = 0xFF,
    }
}
