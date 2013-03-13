//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: MindSensorsAccelerometerTypes.cs $ $Revision: 19 $
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


namespace Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer
{
    
    /// <summary>
    /// MindSensors Accelerometer Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique DSS Contract Identifier for the MindSensors Acceleration Sensor service.
        [DataMember, Description("Identifies the Unique DSS Contract Identifier for the MindSensors Acceleration Sensor service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/mindsensors/nxt/accelerometer.user.html";

        /// <summary>
        /// The I2C Vendor code
        /// </summary>
        [DataMember, Description("Identifies the I2C Vendor code.")]
        public const string Vendor = "mndsnsrs";

        /// <summary>
        /// I2C Device Model: Generic Accelerometer Sensor 
        /// </summary>
        [DataMember, Description("Identifies the I2C models for the subclass of MindSensors Acceleration Sensors.")]
        public const string DeviceModel = "ACL3X2g,ACL3X3g,ACL2X2g,ACL2X3g";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 500;

    }

    /// <summary>
    /// MindSensors Accelerometer State
    /// </summary>
    [DataContract, Description("Specifies the MindSensors Acceleration Sensor state.")]
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
        [DataMember, Description("Specifies a user friendly name for the MindSensors Accelerometer Sensor.")]
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
        [DataMember, Description("Calibrates the initial reading of the acceleration sensor. \n"
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
    /// Get the MindSensors Accelerometer Sensor State
    /// </summary>
    [Description("Gets the current state of the MindSensors Accelerometer sensor.")]
    public class Get : Get<GetRequestType, PortSet<AccelerometerState, Fault>>
    {
    }

    /// <summary>
    /// Get the MindSensors Accelerometer Sensor State
    /// </summary>
    [Description("Indicates an update to the MindSensors Accelerometer sensor.")]
    public class AccelerometerUpdate : Update<AccelerometerReading, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }


    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate")]
    [Description("Connects the MindSensors Accelerometer Sensor to be plugged into the NXT Brick.")]
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
    /// MindSensors Accelerometer Sensor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the MindSensors Accelerometer Sensor Configuration.")]
    public class AccelerometerConfig
    {
        /// <summary>
        /// MindSensors Accelerometer Sensor Configuration.
        /// </summary>
        public AccelerometerConfig() { }

        /// <summary>
        /// MindSensors Accelerometer Sensor Configuration.
        /// </summary>
        /// <param name="sensorPort"></param>
        public AccelerometerConfig(NxtSensorPort sensorPort)
        {
            this.SensorPort = sensorPort;
        }

        /// <summary>
        /// The name of this Accelerometer Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the MindSensors Accelerometer Sensor.")]
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
    /// Subscribe to the MindSensors Acceleration Sensor
    /// </summary>
    [Description("Subscribes to the MindSensors Acceleration Sensor.")]
    public class ReliableSubscribe : Subscribe<ReliableSubscribeRequestType, DsspResponsePort<SubscribeResponseType>, AccelerometerOperations> { }

    /// <summary>
    /// Subscribe to the MindSensors Acceleration Sensor
    /// </summary>
    [Description("Subscribes to the MindSensors Acceleration Sensor.")]
    public class Subscribe : Subscribe<SubscribeRequestType, DsspResponsePort<SubscribeResponseType>, AccelerometerOperations> { }

    /// <summary>
    /// MindSensors Accelerometer Sensor Operations Port
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
    /// MindSensors Read Accelerometer Sensor Data
    /// </summary>
    [DataContract, Description("Reads the MindSensors I2C Acceleration Sensor.")]
    public class I2CReadMindSensorsAccelerationSensor : nxtcmd.LegoLSWrite
    {
        /// <summary>
        /// MindSensors Read Accelerometer Sensor Data
        /// </summary>
        public I2CReadMindSensorsAccelerationSensor()
            : base()
        {
            base.TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x42 };
            base.ExpectedI2CResponseSize = 9;
            base.Port = 0;
        }

        /// <summary>
        /// MindSensors Read Accelerometer Sensor Data
        /// </summary>
        /// <param name="port"></param>
        public I2CReadMindSensorsAccelerationSensor(NxtSensorPort port)
            : base()
        {
            TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x42 };
            ExpectedI2CResponseSize = 9;
            Port = port;
        }

        /// <summary>
        /// The matching LegoResponse
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public nxtcmd.LegoResponse GetResponse(byte[] responseData)
        {
            return new I2CResponseMindSensorsAccelerationSensor(responseData);
        }


    }

    /// <summary>
    /// LegoResponse: I2C Sensor Type
    /// </summary>
    [DataContract, Description("Indicates the MindSensors Acceleration Sensor reading.")]
    public class I2CResponseMindSensorsAccelerationSensor : nxtcmd.LegoResponse
    {
        /// <summary>
        /// LegoResponse: I2C Sensor Type
        /// </summary>
        public I2CResponseMindSensorsAccelerationSensor()
            : base(20, LegoCommandCode.LSRead)
        {
        }

        /// <summary>
        /// LegoResponse: I2C Sensor Type
        /// </summary>
        /// <param name="responseData"></param>
        public I2CResponseMindSensorsAccelerationSensor(byte[] responseData)
            : base(20, LegoCommandCode.LSRead, responseData) { }


        /// <summary>
        /// X Axis 
        /// </summary>
        [DataMember, Description("Indicates the X Axis.")]
        public int X
        {
            get
            {
                if (CommandData == null || CommandData.Length < 9)
                    return 0;

                return (int)CommandData[4];
            }
            set
            {
                CommandData[4] = (byte)value;
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
                if (CommandData == null || CommandData.Length < 9)
                    return 0;

                return (int)CommandData[5];
            }
            set
            {
                CommandData[5] = (byte)value;
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
                if (CommandData == null || CommandData.Length < 9)
                    return 0;

                return (int)CommandData[6];
            }
            set
            {
                CommandData[6] = (byte)value;
            }
        }

    }

   
}
