using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Ccr.Core;
using W3C.Soap;

using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;

using pxbrick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick.Proxy;
using Microsoft.Dss.Core.DsspHttp;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;

namespace Microsoft.Robotics.Services.Sample.HiTechnic.PrototypeBoard
{
    /// <summary>
    /// HiTechnic Accelerometer Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique DSS Contract Identifier for the HiTechnic Acceleration Sensor service
        [DataMember, Description("Identifies the Unique DSS Contract Identifier for the HiTechnic Prototype Board service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2012/10/hitechnic/nxt/prototypeboard.user.html";

        /// <summary>
        /// The I2C Vendor code
        /// </summary>
        [DataMember, Description("Identifies the I2C Vendor code.")]
        public const string Vendor = "HiTechnc";

        /// <summary>
        /// The Accelerometer Sensor Device Type
        /// </summary>
        [DataMember, Description("Identifies the I2C Device Model.")]
        public const string DeviceModel = "SuperPr";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 500;

    }

    /// <summary>
    /// HiTechnic prototype board State
    /// </summary>
    [DataContract, Description("Specifies the HiTechnic Prototype Board state.")]
    public class PrototypeBoardState
    {
        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// The name of this Prototype Board instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the HiTechnic Accelerometer Sensor.")]
        public string Name;

        /// <summary>
        /// The Manufaturer infomation stored at 0x08
        /// </summary>
        [DataMember, Description("The Manufaturer infomation stored at 0x08")]
        public string ManufactureInfo;

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

    }

    /// <summary>
    /// Get the HiTechnic Accelerometer Sensor State
    /// </summary>
    [Description("Gets the current state of the HiTechnic prototype board sensor.")]
    public class Get : Get<GetRequestType, PortSet<PrototypeBoardState, Fault>>
    {
    }


    
    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate")]
    [Description("Connects the HiTechnic Accelerometer Sensor to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<PrototypeBoardConfig, PortSet<DefaultUpdateResponseType, Fault>>
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
        public ConnectToBrick(NxtSensorPort sensorPort) : base(new PrototypeBoardConfig(sensorPort))
        {
        }
    }

    /// <summary>
    /// Set the LED status
    /// </summary>
    [DisplayName("ReadFromI2cAddress")]
    [Description("Read from a specified address on the board")]
    public class ReadFromI2cAddress : Update<ReadConfig, PortSet<DefaultUpdateResponseType, Fault>>
    {

    }




    /// <summary>
    /// Set the LED status
    /// </summary>
    [DisplayName("(User) Set LED")]
    [Description("Sets the status of the onboard LED (Off / Red / Blue / Red and Blue)")]
    public class SetLed : Update<LedConfig, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }

    /// <summary>
    /// Set LED status
    /// </summary>
    [DataContract, Description("Set LED status")]
    public class LedConfig
    {
        /// <summary>
        /// LED status
        /// </summary>
        [DataMember, Description("Led state")]
        public LedStatus Status { get; set; }
    }

    /// <summary>
    /// ReadConfiguration
    /// </summary>
    [DataContract, Description("Values to read from board")]
    public class ReadConfig
    {
        /// <summary>
        /// TxData
        /// </summary>
        [DataMember, Description("TxData")]
        public byte[] TxData { get; set; }
        /// <summary>
        /// ExpectedResponseSize
        /// </summary>
        [DataMember, Description("ExpectedResponseSize")]
        public int ExpectedResponseSize { get; set; }
    }

    /// <summary>
    /// I2cReadResponse
    /// </summary>
    [DataContract, Description("I2cReadResponse")]
    public class I2cReadResponse
    {
        /// <summary>
        /// Response
        /// </summary>
        [DataMember]
        public byte[] Response { get; set; }
    }

    /// <summary>
    /// HiTechnic Accelerometer Sensor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the HiTechnic prototype board Configuration.")]
    public class PrototypeBoardConfig
    {
        /// <summary>
        /// HiTechnic prototype board Configuration.
        /// </summary>
        public PrototypeBoardConfig() { }

        /// <summary>
        /// HiTechnic prototype board Configuration.
        /// </summary>
        /// <param name="sensorPort"></param>
        public PrototypeBoardConfig(NxtSensorPort sensorPort)
        {
            this.SensorPort = sensorPort;
        }

        /// <summary>
        /// The name of this prototype board instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the HiTechnic prototype board.")]
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
    /// HiTechnic Accelerometer Sensor Operations Port
    /// </summary>
    [ServicePort]
    public class PrototypeBoardOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, ConnectToBrick, SetLed, ReadFromI2cAddress>
    {

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(PrototypeBoardConfig body)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = body ?? new PrototypeBoardConfig();
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(PrototypeBoardState state)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = new PrototypeBoardConfig(state.SensorPort);
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
    [DataContract, Description("Reads the I2C HiTechnic prototype board.")]
    public class I2CReadHiTechnicPrototypeBoard : nxtcmd.LegoLSWrite
    {
        /// <summary>
        /// I2CReadHiTechnicPrototypeBoard
        /// </summary>
        public I2CReadHiTechnicPrototypeBoard() : base()
        {
            base.TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x41 };
            base.ExpectedI2CResponseSize = 5;
            base.Port = 0;
        }

        /// <summary>
        /// I2CReadHiTechnicPrototypeBoard
        /// </summary>
        /// <param name="port"></param>
        public I2CReadHiTechnicPrototypeBoard(NxtSensorPort port) : base()
        {
            base.TXData = new byte[] { 0x10, 0x8 };
            base.ExpectedI2CResponseSize = 15;
            base.Port = port;
        }

        /// <summary>
        /// The matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public nxtcmd.LegoResponse GetResponse(byte[] responseData)
        {
            return new nxtcmd.LegoResponse(responseData);
        }
    }

    /// <summary>
    /// LegoResponse: I2C Sensor Type
    /// </summary>
    [DataContract, Description("Reads the HiTechnic I2C prototype board.")]
    public class I2CResponseHiTechnicPrototypeBoard : nxtcmd.LegoResponse
    {
        /// <summary>
        /// I2CResponseHiTechnicPrototypeBoard
        /// </summary>
        /// <param name="responseData"></param>
        public I2CResponseHiTechnicPrototypeBoard(byte[] responseData)
            : base(20, LegoCommandCode.LSRead, responseData) { }

        /// <summary>
        /// I2CResponseHiTechnicPrototypeBoard
        /// </summary>
        public I2CResponseHiTechnicPrototypeBoard()
            : base(20, LegoCommandCode.LSRead) { }
    }

    /// <summary>
    /// LedStatus control
    /// </summary>
    [DataContract, Description("Set the onboard LED")]
    public enum LedStatus
    {
        /// <summary>
        /// Off
        /// </summary>
        Off = 0,
        /// <summary>
        /// Red
        /// </summary>
        Red = 1,
        /// <summary>
        /// Blue
        /// </summary>
        Blue = 2,
        /// <summary>
        /// Read and Blue
        /// </summary>
        RedAndBlue = 3
    }
}
