//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtCommonTypes.cs $ $Revision: 30 $
//-----------------------------------------------------------------------

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using W3C.Soap;

using common = Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using System.Text;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Common
{

    /// <summary>
    /// LEGO Helper Class
    /// </summary>
    public class NxtCommon
    {
        /// <summary>
        /// The default I2C Bus Address of the Ultrasonic or other I2C Sensor.
        /// </summary>
        [DataMember, Description("The default I2C Bus Address of the Ultrasonic or other I2C Sensor.")]
        public const byte DefaultI2CBusAddress = 0x02;

        /// <summary>
        /// Translate an 8-bit integer (0-3) to NxtSensorPort
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static NxtSensorPort GetNxtSensorPort(byte port)
        {
            switch (port)
            {
                case 0:
                    return NxtSensorPort.Sensor1;
                case 1:
                    return NxtSensorPort.Sensor2;
                case 2:
                    return NxtSensorPort.Sensor3;
                case 3:
                    return NxtSensorPort.Sensor4;
                default:
                    return NxtSensorPort.NotConnected;
            }
        }

        /// <summary>
        /// Translate an 8-bit integer (0-2) to NxtMotorPort
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static NxtMotorPort GetNxtMotorPort(byte port)
        {
            switch (port)
            {
                case 0:
                    return NxtMotorPort.MotorA;
                case 1:
                    return NxtMotorPort.MotorB;
                case 2:
                    return NxtMotorPort.MotorC;
                default:
                    return NxtMotorPort.NotConnected;
            }
        }

        /// <summary>
        /// Translate a HardwareIdentifier integer (1-4) to NxtSensorPort
        /// </summary>
        /// <param name="hardwareIdentifier"></param>
        /// <returns></returns>
        public static NxtSensorPort GetNxtSensorPortFromHardwareIdentifier(int hardwareIdentifier)
        {
            return GetNxtSensorPort((byte)(hardwareIdentifier - 1));
        }

        /// <summary>
        /// Translate a HardwareIdentifier integer (1-3) to NxtMotorPort.
        /// </summary>
        /// <param name="hardwareIdentifier"></param>
        /// <returns></returns>
        public static NxtMotorPort GetNxtMotorPortFromHardwareIdentifier(int hardwareIdentifier)
        {
            return GetNxtMotorPort((byte)(hardwareIdentifier - 1));
        }

        /// <summary>
        /// Get the integer representation of a NxtSensorPort (zero based).
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static byte PortNumber(NxtSensorPort port)
        {
            switch (port)
            {
                case NxtSensorPort.Sensor1:
                    return 0;

                case NxtSensorPort.Sensor2:
                    return 1;

                case NxtSensorPort.Sensor3:
                    return 2;

                case NxtSensorPort.Sensor4:
                    return 3;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Get the integer representation of a NxtMotorPort (0-2).
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static byte PortNumber(NxtMotorPort port)
        {
            switch (port)
            {
                case NxtMotorPort.MotorA:
                    return 0;

                case NxtMotorPort.MotorB:
                    return 1;

                case NxtMotorPort.MotorC:
                    return 2;

                case NxtMotorPort.AnyMotorPort:
                    return 255;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// Get the integer representation of a LegoNxtPort (zero based).
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static byte PortNumber(LegoNxtPort port)
        {
            switch (port)
            {
                case LegoNxtPort.A:
                case LegoNxtPort.EncoderA:
                case LegoNxtPort.MotorA:
                case LegoNxtPort.Sensor1:
                    return 0;

                case LegoNxtPort.B:
                case LegoNxtPort.EncoderB:
                case LegoNxtPort.MotorB:
                case LegoNxtPort.Sensor2:
                    return 1;

                case LegoNxtPort.C:
                case LegoNxtPort.EncoderC:
                case LegoNxtPort.MotorC:
                case LegoNxtPort.Sensor3:
                    return 2;

                case LegoNxtPort.Sensor4:
                    return 3;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Get the HardwareIdentifier representation of a NxtSensorPort (1-4).
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static int HardwareIdentifier(NxtSensorPort port)
        {
            return ((int)(PortNumber(port) + 1));
        }

        /// <summary>
        /// Get the HardwareIdentifier representation of a NxtMotorPort (1-3).
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static int HardwareIdentifier(NxtMotorPort port)
        {
            return ((int)(PortNumber(port) + 1));
        }

        /// <summary>
        /// Get the HardwareIdentifier representation of a LegoNxtPort (1-4).
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static int HardwareIdentifier(LegoNxtPort port)
        {
            return ((int)(PortNumber(port) + 1));
        }


        /// <summary>
        /// Convert string to lego data packet.
        /// If string is longer than maxlength, 
        /// the leftmost portion of the string 
        /// will be retained.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bufferLength">Maximum length including null terminator</param>
        /// <returns></returns>
        public static byte[] StringToData(string value, int bufferLength)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (value.Length >= bufferLength)
                value = value.Substring(0, (bufferLength - 1));

            byte[] result = new byte[bufferLength];

            System.Text.Encoding.ASCII.GetBytes(value, 0, value.Length, result, 0);
            return result;
        }

        /// <summary>
        /// Compare two byte arrays.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool ByteArrayIsEqual(byte[] first, byte[] second)
        {
            if (first == null && second == null)
                return true;
            if (first == null || second == null)
                return false;
            if (first.Length != second.Length)
                return false;
            for (int ix = 0; ix < first.Length; ix++)
                if (first[ix] != second[ix])
                    return false;
            return true;
        }

        /// <summary>
        /// Set String to Byte Array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetStringToData(byte[] data, int startPosition, string value)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (startPosition >= data.Length)
                throw new ArgumentOutOfRangeException("startPosition");

            if (value == null)
                throw new ArgumentNullException("value");

            return SetStringToData(data, startPosition, value, value.Length);
        }

        /// <summary>
        /// Set String to Byte Array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <param name="value"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static bool SetStringToData(byte[] data, int startPosition, string value, int length)
        {
            // Do we have room in the buffer?
            if (data == null)
                throw new ArgumentNullException("data");

            if (startPosition >= data.Length)
                throw new ArgumentOutOfRangeException("startPosition");

            // is length too long?
            if (length + startPosition > data.Length)
                throw new ArgumentOutOfRangeException("length");

            if (value == null)
                throw new ArgumentNullException("value");

            //shorten string if too long
            if (value.Length >= length)
                value = value.Substring(0, length);

            //write string
            int ix = startPosition;
            foreach (char c in value)
                data[ix++] = (byte)c;

            //fill up to length will null
            while (ix < length)
                data[ix++] = 0;

            return true;
        }

        /// <summary>
        /// Convert Byte Array to String
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <returns></returns>
        public static string DataToString(byte[] data, int startPosition)
        {
            return DataToString(data, startPosition, data.Length - startPosition);
        }

        /// <summary>
        /// Convert Byte Array to String
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <param name="maximumLength"></param>
        /// <returns></returns>
        public static string DataToString(byte[] data, int startPosition, int maximumLength)
        {
            int length = 0;
            while (startPosition + length < maximumLength && data[startPosition + length] != 0)
                length++;
            return System.Text.Encoding.ASCII.GetString(data, startPosition, length);
        }

        /// <summary>
        /// Retrieve a 32-bit integer from a Byte Array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int DataToInt(byte[] data, int startPosition, int length)
        {
            return int.Parse(System.Text.Encoding.ASCII.GetString(data, startPosition, length), System.Globalization.NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Retrieve a signed byte  from a Byte Array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <returns></returns>
        public static int GetSByte(byte[] data, int startPosition)
        {
            // Do we have room in the buffer?
            if (data == null)
                throw new ArgumentNullException("data");

            if (startPosition >= data.Length)
                throw new ArgumentOutOfRangeException("startPosition");

            if (data[startPosition] >= 128)
            {
                int temp = (byte)data[startPosition] ^ (byte)0xFF;
                temp += 1;
                return -temp;
            }
            else
                return data[startPosition];
        }

        /// <summary>
        /// Set a Signed byte to an offset within a byte array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetSByte(byte[] data, int startPosition, int value)
        {
            // Do we have room in the buffer?
            if (data == null)
                throw new ArgumentNullException("data");

            if (startPosition > data.Length - 1)
                throw new ArgumentOutOfRangeException("startPosition");

            if (value < 0)
                data[startPosition] = (byte)value;
            else
                data[startPosition] = (byte)value;
            return true;
        }

        /// <summary>
        /// Set a 16 bit unsigned integer in little-endian format
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetUShort(byte[] data, int startPosition, int value)
        {
            // Do we have room in the buffer?
            if (data == null)
                throw new ArgumentNullException("data");

            if (startPosition > data.Length - 2)
                throw new ArgumentOutOfRangeException("startPosition");

            ushort uValue = (ushort)value;

            data[startPosition] = (byte)(uValue & 0xFF); //LSB is first
            data[startPosition + 1] = (byte)(uValue >> 8);
            return true;
        }

        /// <summary>
        /// Get a 16 bit unsigned little-endian integer from data and startPosition.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <returns></returns>
        public static int GetUShort(byte[] data, int startPosition)
        {
            if (data == null || data.Length < (startPosition + 2))
                return 0;

            return data[startPosition] + (data[startPosition + 1] * 0x100);
        }

        /// <summary>
        /// Set a signed 2-byte short integer
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetShort(byte[] data, int startPosition, int value)
        {
            // Do we have room in the buffer?
            if (data == null)
                throw new ArgumentNullException("data");

            if (startPosition > data.Length - 2)
                throw new ArgumentOutOfRangeException("startPosition");

            data[startPosition] = (byte)(value & 0xFF); //LSB is first
            data[startPosition + 1] = (byte)(value >> 8);
            return true;
        }

        /// <summary>
        /// Set unsigned 32 bit integeger in little-endian format
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startPosition"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetUInt32(byte[] data, int startPosition, long value)
        {
            // Do we have room in the buffer?
            if (data == null)
                throw new ArgumentNullException("data");

            if (startPosition > data.Length - 4)
                throw new ArgumentOutOfRangeException("startPosition");

            UInt32 uValue = (UInt32)Math.Abs(value);

            data[startPosition] = (byte)(uValue & 0xFF);
            data[startPosition + 1] = (byte)((uValue >> 8) & 0xFF);
            data[startPosition + 2] = (byte)((uValue >> 16) & 0xFF);
            data[startPosition + 3] = (byte)((uValue >> 24) & 0xFF);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="originalData"></param>
        /// <param name="startPosition">can be > origdata.Length</param>
        /// <param name="dataToAppend"></param>
        /// <returns></returns>
        public static byte[] AppendData(byte[] originalData, int startPosition, byte[] dataToAppend)
        {
            //error check
            if (originalData == null)
                originalData = new byte[1];

            if (dataToAppend == null)
                return originalData;

            //copy over data to new array
            if (startPosition + dataToAppend.Length > originalData.Length)
            {
                byte[] newarray = new byte[startPosition + dataToAppend.Length];
                for (int i = 0; i < originalData.Length; i++)
                    newarray[i] = originalData[i];
                originalData = newarray;
            }

            //write data
            int ix = startPosition;
            foreach (byte b in dataToAppend)
                originalData[ix++] = b;

            return originalData;
        }


        /// <summary>
        /// Get an error message from a LEGO status code
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static string GetErrorMessage(int status)
        {
            switch (status)
            {
                case 0x00:
                    return "Success";
                case 0x81:
                    return "No more handles";
                case 0x82:
                    return "No Space";
                case 0x83:
                    return "No more files";
                case 0x84:
                    return "End of file expected";
                case 0x85:
                    return "End of file";
                case 0x86:
                    return "Not linear file";
                case 0x87:
                    return "File not found";
                case 0x88:
                    return "Handle already closed";
                case 0x89:
                    return "No linear space";
                case 0x8A:
                    return "Undefined error";
                case 0x8B:
                    return "File is buisy";
                case 0x8C:
                    return "No write buffers";
                case 0x8D:
                    return "Append not possible";
                case 0x8E:
                    return "File is full";
                case 0x8F:
                    return "File exists";
                case 0x90:
                    return "Module not found";
                case 0x91:
                    return "Out of boundary";
                case 0x92:
                    return "Illegal file name";
                case 0x93:
                    return "Illegal handle";
                default:
                    return "Unknown error code: " + status.ToString();
            }
        }

    }

    #region New Generic Contract Definitions

    /// <summary>
    /// The current X, Y, Z accelerometer readings
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Records the X, Y, and Z axis of an acceleration sensor.")]
    [XmlRootAttribute("AccelerometerReading", Namespace = Contract.Identifier)]
    public class AccelerometerReading : ICloneable, IDssSerializable
    {
        private Double _x;
        private Double _y;
        private Double _z;
        private DateTime _timeStamp;
        /// <summary>
        /// The current accelerometer X Axis
        /// </summary>
        [DataMember, Description("The X Axis")]
        public Double X
        {
            get
            {
                return this._x;
            }
            set
            {
                this._x = value;
            }
        }
        /// <summary>
        /// The current accelerometer Y Axis
        /// </summary>
        [DataMember, Description("The Y Axis")]
        public Double Y
        {
            get
            {
                return this._y;
            }
            set
            {
                this._y = value;
            }
        }
        /// <summary>
        /// The current accelerometer Z Axis
        /// </summary>
        [DataMember, Description("The Z Axis")]
        public Double Z
        {
            get
            {
                return this._z;
            }
            set
            {
                this._z = value;
            }
        }
        /// <summary>
        /// The time of the last sensor update
        /// </summary>
        [DataMember, Description("The time associated with this reading.")]
        [Browsable(false)]
        public DateTime TimeStamp
        {
            get
            {
                return this._timeStamp;
            }
            set
            {
                this._timeStamp = value;
            }
        }
        /// <summary>
        /// Copy To Accelerometer Reading
        /// </summary>
        public virtual void CopyTo(IDssSerializable target)
        {
            AccelerometerReading typedTarget = target as AccelerometerReading;

            if (typedTarget == null)
                throw new ArgumentException("CopyTo({0}) requires type {0}", this.GetType().FullName);
            typedTarget.X = this.X;
            typedTarget.Y = this.Y;
            typedTarget.Z = this.Z;
            typedTarget.TimeStamp = this.TimeStamp;
        }
        /// <summary>
        /// Clone Accelerometer Reading
        /// </summary>
        public virtual object Clone()
        {
            AccelerometerReading target = new AccelerometerReading();

            target.X = this.X;
            target.Y = this.Y;
            target.Z = this.Z;
            target.TimeStamp = this.TimeStamp;
            return target;

        }
        /// <summary>
        /// Serialize Serialize
        /// </summary>
        public virtual void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(X);

            writer.Write(Y);

            writer.Write(Z);

            Microsoft.Dss.Services.Serializer.BinarySerializationHelper.SerializeDateTime(TimeStamp, writer);

        }
        /// <summary>
        /// Deserialize Deserialize
        /// </summary>
        public virtual object Deserialize(System.IO.BinaryReader reader)
        {
            X = reader.ReadDouble();

            Y = reader.ReadDouble();

            Z = reader.ReadDouble();

            TimeStamp = Microsoft.Dss.Services.Serializer.BinarySerializationHelper.DeserializeDateTime(reader);

            return this;

        }
    }

    #endregion

    #region Common Operations

    /// <summary>
    /// A connection to the LEGO NXT Brick
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Defines a connection to the LEGO NXT Brick.")]
    [XmlRootAttribute("LegoNxtConnection", Namespace = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/brick.user.html")]
    public class LegoNxtConnection : ICloneable, IDssSerializable
    {
        private common.LegoNxtPort _port;
        private String _portOverride;

        #region Constructors
        /// <summary>
        /// A connection to the LEGO NXT Brick
        /// </summary>
        public LegoNxtConnection()
        {
        }
        /// <summary>
        /// A connection to the LEGO NXT Brick
        /// </summary>
        public LegoNxtConnection(LegoNxtPort port)
        {
            this._port = port;
        }
        #endregion

        /// <summary>
        /// LEGO NXT Port
        /// </summary>
        [DataMember, Description("Identifies the NXT Port associated with this Connection.")]
        [DataMemberConstructorAttribute(Order = 1)]
        public common.LegoNxtPort Port
        {
            get
            {
                return this._port;
            }
            set
            {
                this._port = value;
            }
        }
        /// <summary>
        /// The optional Port Override.
        /// </summary>
        [DataMember, Description("Identifies optional connection information.")]
        public String PortOverride
        {
            get
            {
                return this._portOverride;
            }
            set
            {
                this._portOverride = value;
            }
        }

        #region IDssSerializable

        /// <summary>
        /// Copy To Lego NXT Connection
        /// </summary>
        public virtual void CopyTo(IDssSerializable target)
        {
            LegoNxtConnection typedTarget = target as LegoNxtConnection;

            if (typedTarget == null)
                throw new ArgumentException("CopyTo({0}) requires type {0}", this.GetType().FullName);
            typedTarget.Port = this.Port;
            typedTarget.PortOverride = this.PortOverride;
        }
        /// <summary>
        /// Clone Lego NXT Connection
        /// </summary>
        public virtual object Clone()
        {
            LegoNxtConnection target = new LegoNxtConnection();

            target.Port = this.Port;
            target.PortOverride = this.PortOverride;
            return target;

        }
        /// <summary>
        /// Serialize Serialize
        /// </summary>
        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write((Int32)Port);

            if (PortOverride == null) writer.Write((byte)0);
            else
            {
                // null flag
                writer.Write((byte)1);

                writer.Write(PortOverride);
            }

        }
        /// <summary>
        /// Deserialize Deserialize
        /// </summary>
        public virtual object Deserialize(BinaryReader reader)
        {
            Port = (LegoNxtPort)reader.ReadInt32();

            if (reader.ReadByte() == 0) { }
            else
            {
                PortOverride = reader.ReadString();
            } //nullable

            return this;

        }

        #endregion

        /// <summary>
        /// The Display name of the LEGO NXT Connection
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string display = this.Port.ToString();
            if ((this.Port & LegoNxtPort.PortOverride) == LegoNxtPort.PortOverride && !string.IsNullOrEmpty(this.PortOverride))
                display += ": " + this.PortOverride;
            return display;
        }
    }

    #endregion

    #region Public LEGO NXT Enums

    /// <summary>
    /// The LEGO Connection Type
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("The connection type to the LEGO NXT Brick (USB or Bluetooth)\nUSB is not currently supported.")]
    [XmlRootAttribute("LegoConnectionType", Namespace = Contract.Identifier)]
    public enum LegoConnectionType
    {
        /// <summary>
        /// Bluetooth
        /// </summary>
        Bluetooth = 0,
        ///// <summary>
        ///// USB
        ///// </summary>
        //USB = 1,
    }


    /// <summary>
    /// Identifies the type of LEGO Command (System, Direct, Response)
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Identifies the type of LEGO Command (System, Direct, Response)")]
    [XmlRootAttribute("LegoCommandType", Namespace = Contract.Identifier)]
    public enum LegoCommandType
    {
        /// <summary>
        /// LEGO System Command
        /// </summary>
        System = 0x01,
        /// <summary>
        /// LEGO Direct Command
        /// </summary>
        Direct = 0x00,
        /// <summary>
        /// LEGO Command Response
        /// </summary>
        Response = 0x02,
    }

    /// <summary>
    /// The type of Device
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Identifies the type of the LEGO device.")]
    [XmlRootAttribute("LegoDeviceType", Namespace = Contract.Identifier)]
    public enum LegoDeviceType
    {
        /// <summary>
        /// Unknown or not specified
        /// </summary>
        Unknown = 0x00,
        /// <summary>
        /// An internal or built-in device
        /// <example>internal battery</example>
        /// </summary>
        Internal = 0x01,
        /// <summary>
        /// An Actuator
        /// <example>Motor</example>
        /// </summary>
        Actuator = 0x02,
        /// <summary>
        /// An analog sensor
        /// <example>Touch Sensor</example>
        /// </summary>
        [Description("An analog sensor")]
        AnalogSensor = 0x04,
        /// <summary>
        /// An I2C sensor
        /// <example>Sonar Sensor</example>
        /// </summary>
        [Description("An I2C sensor")]
        DigitalSensor = 0x08,

        /// <summary>
        /// An Aggregation service which works with multiple NXT devices.
        /// <example>internal battery</example>
        /// </summary>
        [Description("An Aggregation service which works with multiple NXT devices.")]
        Aggregation = 0x10,

    }

    /// <summary>
    /// LEGO NXT Motor Port
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Identifies a LEGO Motor Port (A,B,C).")]
    [XmlRootAttribute("NxtMotorPort", Namespace = Contract.Identifier)]
    public enum NxtMotorPort
    {
        /// <summary>
        /// Not Connected
        /// </summary>
        NotConnected = LegoNxtPort.NotConnected,

        /// <summary>
        /// Motor A
        /// </summary>
        MotorA = LegoNxtPort.MotorA,

        /// <summary>
        /// Motor B
        /// </summary>
        MotorB = LegoNxtPort.MotorB,

        /// <summary>
        /// Motor C
        /// </summary>
        MotorC = LegoNxtPort.MotorC,

        /// <summary>
        /// Any Motor Port
        /// </summary>
        AnyMotorPort = LegoNxtPort.AnyMotorPort,
    }

    /// <summary>
    /// LEGO NXT Sensor Port
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Identifies a LEGO NXT Sensor Port.")]
    [XmlRootAttribute("NxtSensorPort", Namespace = Contract.Identifier)]
    public enum NxtSensorPort
    {

        /// <summary>
        /// Not Connected
        /// </summary>
        NotConnected = LegoNxtPort.NotConnected,

        /// <summary>
        /// Sensor 1
        /// </summary>
        Sensor1 = LegoNxtPort.Sensor1,

        /// <summary>
        /// Sensor 2
        /// </summary>
        Sensor2 = LegoNxtPort.Sensor2,

        /// <summary>
        /// Sensor 3
        /// </summary>
        Sensor3 = LegoNxtPort.Sensor3,

        /// <summary>
        /// Sensor 4
        /// </summary>
        Sensor4 = LegoNxtPort.Sensor4,

        /// <summary>
        /// Any Sensor Port
        /// </summary>
        AnySensorPort = LegoNxtPort.AnySensorPort,
    }

    /// <summary>
    /// Motor Stop State
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Describes how a motor should stop.")]
    [XmlRootAttribute("MotorStopState", Namespace = Contract.Identifier)]
    public enum MotorStopState
    {
        /// <summary>
        /// Brake
        /// </summary>
        Brake = 0,
        /// <summary>
        /// Coast
        /// </summary>
        Coast,
        /// <summary>
        /// Default Stop State
        /// </summary>
        Default = 0,
    }

    /// <summary>
    /// The LEGO Nxt Port
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Describes all input and output connections on the LEGO NXT brick.")]
    [Flags]
    [XmlRootAttribute("LegoNxtPort", Namespace = Contract.Identifier)]
    public enum LegoNxtPort
    {
        /// <summary>
        /// Not Connected
        /// </summary>
        NotConnected = 0,

        /// <summary>
        /// Motor A
        /// </summary>
        MotorA = 0x00000001,
        /// <summary>
        /// Encoder A
        /// </summary>
        EncoderA = 0x00000002,
        /// <summary>
        /// Motor and Encoder A
        /// </summary>
        A = 0x00000003,

        /// <summary>
        /// Motor B
        /// </summary>
        MotorB = 0x00000004,
        /// <summary>
        /// Encoder B
        /// </summary>
        EncoderB = 0x00000008,
        /// <summary>
        /// Motor and Encoder B
        /// </summary>
        B = 0x0000000C,

        /// <summary>
        /// Motor C
        /// </summary>
        MotorC = 0x00000010,
        /// <summary>
        /// Encoder C
        /// </summary>
        EncoderC = 0x00000020,
        /// <summary>
        /// Motor and Encoder C
        /// </summary>
        C = 0x00000030,

        /// <summary>
        /// Any Motor Port
        /// </summary>
        AnyMotorPort = A | B | C,

        /// <summary>
        /// Sensor 1
        /// </summary>
        Sensor1 = 0x00000100,

        /// <summary>
        /// Sensor 2
        /// </summary>
        Sensor2 = 0x00000200,

        /// <summary>
        /// Sensor 3
        /// </summary>
        Sensor3 = 0x00000300,

        /// <summary>
        /// Sensor 4
        /// </summary>
        Sensor4 = 0x00000400,

        /// <summary>
        /// Any Sensor 
        /// </summary>
        AnySensorPort = Sensor1 | Sensor2 | Sensor3 | Sensor4,

        /// <summary>
        /// LEGO NXT Brick Display
        /// </summary>
        Display = 0x00001000,

        /// <summary>
        /// Internal Brick Speaker
        /// </summary>
        Speaker = 0x00002000,

        /// <summary>
        /// Brick Buttons
        /// </summary>
        Buttons = 0x00003000,

        /// <summary>
        /// Brick Battery
        /// </summary>
        Battery = 0x00004000,

        /// <summary>
        /// A LEGO NXT Aggregation Service
        /// </summary>
        Aggregation = 0x00008000,

        /// <summary>
        /// Port Override 
        /// </summary>
        PortOverride = 0x00010000,
    }

    /// <summary>
    /// LEGO NXT Commands
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Identifies an internal LEGO NXT Command Code.")]
    [XmlRootAttribute("LegoCommandCode", Namespace = Contract.Identifier)]
    public enum LegoCommandCode : byte
    {
        /// <summary>
        /// Start Program
        /// </summary>
        StartProgram = 0x00,
        /// <summary>
        /// Stop Program
        /// </summary>
        StopProgram = 0x01,
        /// <summary>
        /// Play a Sound file
        /// </summary>
        PlaySoundFile = 0x02,
        /// <summary>
        /// Play a Tone
        /// </summary>
        PlayTone = 0x03,
        /// <summary>
        /// Set Output State
        /// </summary>
        SetOutputState = 0x04,
        /// <summary>
        /// Set Input Mode
        /// </summary>
        SetInputMode = 0x05,
        /// <summary>
        /// Get Output State
        /// </summary>
        GetOutputState = 0x06,
        /// <summary>
        /// Get Input Mode
        /// </summary>
        GetInputValues = 0x07,
        /// <summary>
        /// Reset Input Scaled Value
        /// </summary>
        ResetInputScaledValue = 0x08,
        /// <summary>
        /// Message Write
        /// </summary>
        MessageWrite = 0x09,
        /// <summary>
        /// Reset Motor Position
        /// </summary>
        ResetMotorPosition = 0x0A,
        /// <summary>
        /// Get Battery Level
        /// </summary>
        GetBatteryLevel = 0x0B,
        /// <summary>
        /// Stop Sound Playback
        /// </summary>
        StopSoundPlayback = 0x0C,
        /// <summary>
        /// Keep Alive
        /// </summary>
        KeepAlive = 0x0D,
        /// <summary>
        /// Low Speed (I2C) Get Status
        /// </summary>
        LSGetStatus = 0x0E,
        /// <summary>
        /// Low Speed (I2C) Write
        /// </summary>
        LSWrite = 0x0F,
        /// <summary>
        /// Low Speed (I2C) Read
        /// </summary>
        LSRead = 0x10,
        /// <summary>
        /// Get Current Program Name
        /// </summary>
        GetCurrentProgramName = 0x11,
        /// <summary>
        /// Message Read
        /// </summary>
        MessageRead = 0x13,
        /// <summary>
        /// Open Read
        /// </summary>
        OpenRead = 0x80,
        /// <summary>
        /// Open Write
        /// </summary>
        OpenWrite = 0x81,
        /// <summary>
        /// Read
        /// </summary>
        Read = 0x82,
        /// <summary>
        /// Write
        /// </summary>
        Write = 0x83,
        /// <summary>
        /// Close
        /// </summary>
        Close = 0x84,
        /// <summary>
        /// Delete
        /// </summary>
        Delete = 0x85,
        /// <summary>
        /// Find First
        /// </summary>
        FindFirst = 0x86,
        /// <summary>
        /// Find Next
        /// </summary>
        FindNext = 0x87,
        /// <summary>
        /// Get Firmware Version
        /// </summary>
        GetFirmwareVersion = 0x88,
        /// <summary>
        /// OpenWriteLinear
        /// </summary>
        OpenWriteLinear = 0x89,
        /// <summary>
        /// OpenReadLinear
        /// </summary>
        OpenReadLinear = 0x8A,
        /// <summary>
        /// OpenWriteData
        /// </summary>
        OpenWriteData = 0x8B,
        /// <summary>
        /// OpenAppendData 
        /// </summary>
        OpenAppendData = 0x8C,
        /// <summary>
        /// RequestFirstModule 
        /// </summary>
        RequestFirstModule = 0x90,
        /// <summary>
        /// RequestNextModule 
        /// </summary>
        RequestNextModule = 0x91,
        /// <summary>
        /// CloseModuleHandle 
        /// </summary>
        CloseModuleHandle = 0x92,
        /// <summary>
        /// ReadIOMap 
        /// </summary>
        ReadIOMap = 0x94,
        /// <summary>
        /// WriteIOMap 
        /// </summary>
        WriteIOMap = 0x95,
        /// <summary>
        /// BootCommand 
        /// </summary>
        BootCommand = 0x97,
        /// <summary>
        /// SetBrickName
        /// </summary>
        SetBrickName = 0x98,
        /// <summary>
        /// GetDeviceInfo
        /// </summary>
        GetDeviceInfo = 0x9B,
        /// <summary>
        /// DeleteUserFlash
        /// </summary>
        DeleteUserFlash = 0xA0,
        /// <summary>
        /// PollCommandLength
        /// </summary>
        PollCommandLength = 0xA1,
        /// <summary>
        /// PollCommand
        /// </summary>
        PollCommand = 0xA2,
        /// <summary>
        /// BluetoothFactoryReset
        /// </summary>
        BluetoothFactoryReset = 0xA4,
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined = 0xFF,
    }

    /// <summary>
    /// Error codes returned by the LEGO NXT Brick
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Identifies an Error code returned by the LEGO NXT brick.")]
    [XmlRootAttribute("LegoErrorCode", Namespace = Contract.Identifier)]
    public enum LegoErrorCode : byte
    {
        /// <summary>
        /// Success
        /// </summary>
        Success = 0x00,
        /// <summary>
        /// UnknownStatus
        /// </summary>
        UnknownStatus = 0x01,
        /// <summary>
        /// Exception
        /// </summary>
        Exception = 0x02,
        /// <summary>
        /// PendingCommunicationTransactionInProgress
        /// </summary>
        PendingCommunicationTransactionInProgress = 0x20,
        /// <summary>
        /// SpecifiedMailboxQueueIsEmpty
        /// </summary>
        SpecifiedMailboxQueueIsEmpty = 0x40,
        /// <summary>
        /// NoMoreHandles
        /// </summary>
        NoMoreHandles = 0x81,
        /// <summary>
        /// NoSpace
        /// </summary>
        NoSpace = 0x82,
        /// <summary>
        /// NoMoreFiles
        /// </summary>
        NoMoreFiles = 0x83,
        /// <summary>
        /// EndOfFileExpected
        /// </summary>
        EndOfFileExpected = 0x84,
        /// <summary>
        /// EndOfFile
        /// </summary>
        EndOfFile = 0x85,
        /// <summary>
        /// NotALinearFile
        /// </summary>
        NotALinearFile = 0x86,
        /// <summary>
        /// FileNotFound
        /// </summary>
        FileNotFound = 0x87,
        /// <summary>
        /// HandleAllreadyClosed
        /// </summary>
        HandleAllreadyClosed = 0x88,
        /// <summary>
        /// NoLinearSpace
        /// </summary>
        NoLinearSpace = 0x89,
        /// <summary>
        /// UndefinedError
        /// </summary>
        UndefinedError = 0x8A,
        /// <summary>
        /// FileIsBusy
        /// </summary>
        FileIsBusy = 0x8B,
        /// <summary>
        /// NoWriteBuffers
        /// </summary>
        NoWriteBuffers = 0x8C,
        /// <summary>
        /// AppendNotPossible
        /// </summary>
        AppendNotPossible = 0x8D,
        /// <summary>
        /// FileIsFull
        /// </summary>
        FileIsFull = 0x8E,
        /// <summary>
        /// FileExists
        /// </summary>
        FileExists = 0x8F,
        /// <summary>
        /// ModuleNotFound
        /// </summary>
        ModuleNotFound = 0x90,
        /// <summary>
        /// OutOfBoundary
        /// </summary>
        OutOfBoundary = 0x91,
        /// <summary>
        /// IllegalFileName
        /// </summary>
        IllegalFileName = 0x92,
        /// <summary>
        /// IllegalHandle
        /// </summary>
        IllegalHandle = 0x93,
        /// <summary>
        /// RequestFailed_FileNotFound
        /// </summary>
        RequestFailed_FileNotFound = 0xBD,
        /// <summary>
        /// UnknownCommandOpcode
        /// </summary>
        UnknownCommandOpcode = 0xBE,
        /// <summary>
        /// InsanePacket
        /// </summary>
        InsanePacket = 0xBF,
        /// <summary>
        /// Data Contains Out-Of-Range Values
        /// </summary>
        DataContains_OutOfRange_Values = 0xC0,
        /// <summary>
        /// CommunicationBusError
        /// </summary>
        CommunicationBusError = 0xDD,
        /// <summary>
        /// NoFreeMemoryInCommunicationBuffer
        /// </summary>
        NoFreeMemoryInCommunicationBuffer = 0xDE,
        /// <summary>
        /// SpecifiedChannelOrConnectionIsNotValid
        /// </summary>
        SpecifiedChannelOrConnectionIsNotValid = 0xDF,
        /// <summary>
        /// SpecifiedChannelOrConnectionIsNotConfiguredOrBusy
        /// </summary>
        SpecifiedChannelOrConnectionIsNotConfiguredOrBusy = 0xE0,
        /// <summary>
        /// NoActiveProgram
        /// </summary>
        NoActiveProgram = 0xEC,
        /// <summary>
        /// IllegalSizeSpecified
        /// </summary>
        IllegalSizeSpecified = 0xED,
        /// <summary>
        /// IllegalMailboxQueueIdSpecified
        /// </summary>
        IllegalMailboxQueueIdSpecified = 0xEE,
        /// <summary>
        /// AttemptedToAccessInvalidFieldOfAStructure
        /// </summary>
        AttemptedToAccessInvalidFieldOfAStructure = 0xEF,
        /// <summary>
        /// BadInputOrOutputSpecified
        /// </summary>
        BadInputOrOutputSpecified = 0xF0,
        /// <summary>
        /// InsufficientMemoryAvailable
        /// </summary>
        InsufficientMemoryAvailable = 0xFB,
        /// <summary>
        /// BadArguments
        /// </summary>
        BadArguments = 0xFF,
    }

    /// <summary>
    /// The NXT output mode
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Specifies the NXT motor output mode.")]
    [XmlRootAttribute("LegoOutputMode", Namespace = Contract.Identifier)]
    [Flags]
    public enum LegoOutputMode
    {
        /// <summary>
        /// No Output Mode
        /// </summary>
        NotSpecified = 0x00,

        /// <summary>
        /// Turn on the specified motor
        /// </summary>
        MotorOn = 0x01,

        /// <summary>
        /// Use run/brake instead of run/float in PWM
        /// </summary>
        Brake = 0x02,

        /// <summary>
        /// Turns on the regulation
        /// </summary>
        Regulated = 0x04,

        /// <summary>
        /// Regulated Power
        /// </summary>
        PowerRegulated = MotorOn | Regulated,

        /// <summary>
        /// Active Braking
        /// </summary>
        PowerBrake = MotorOn | Regulated | Brake,
    }

    /// <summary>
    /// Motor regulation mode
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Specifies the NXT Motor regulation mode.")]
    [XmlRootAttribute("LegoRegulationMode", Namespace = Contract.Identifier)]
    public enum LegoRegulationMode
    {
        /// <summary>
        /// No regulation will be enabled
        /// </summary>
        [Description("No regulation will be enabled")]
        Idle = 0x00,

        /// <summary>
        /// Motors will be powered individually.
        /// </summary>
        [Description("Motors will be powered individually.")]
        Individual = 0x01,

        /// <summary>
        /// Synchronization will be enabled (Needs enabled on two outputs)
        /// </summary>
        [Description("Synchronization will be enabled (Needs enabled on two outputs)")]
        Synchronized = 0x02,
    }


    /// <summary>
    /// The motor running state
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Specifies the NXT Motor running state.")]
    [XmlRootAttribute("LegoRunState", Namespace = Contract.Identifier)]
    public enum RunState
    {
        /// <summary>
        /// Idle
        /// </summary>
        [Description("Idle")]
        Idle = 0x00,
        /// <summary>
        /// Ramp Up to the specified Power
        /// </summary>
        [Description("Ramp Up to the specified Power")]
        RampUp = 0x10,
        /// <summary>
        /// Run at a Constant Power
        /// </summary>
        [Description("Run at a Constant Power")]
        Constant = 0x20,
        /// <summary>
        /// Ramp Down from the specified Power
        /// </summary>
        [Description("Ramp Down from the specified Power")]
        RampDown = 0x40,
    }

    /// <summary>
    /// The Sensor Type
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Identifies the type of LEGO NXT Sensor.")]
    [XmlRootAttribute("LegoSensorType", Namespace = Contract.Identifier)]
    public enum LegoSensorType
    {
        /// <summary>
        /// No sensor attached
        /// </summary>
        NoSensor = 0x00,
        /// <summary>
        /// NXT or RCX touch sensor
        /// </summary>
        Switch = 0x01,
        /// <summary>
        /// RCX temperature sensor
        /// </summary>
        Temperature = 0x02,
        /// <summary>
        /// RCX light sensor
        /// </summary>
        Reflection = 0x03,
        /// <summary>
        /// RCX encoder
        /// </summary>
        Angle = 0x04,
        /// <summary>
        /// NXT light sensor (with spotlight)
        /// </summary>
        LightActive = 0x05,
        /// <summary>
        /// NXT light sensor (without spotlight)
        /// </summary>
        LightInactive = 0x06,
        /// <summary>
        /// NXT sound sensor (dB scaling)
        /// </summary>
        SoundDb = 0x07,
        /// <summary>
        /// NXT sound sensor (dBA scaling)
        /// </summary>
        SoundDba = 0x08,
        /// <summary>
        /// Custom
        /// </summary>
        Custom = 0x09,
        /// <summary>
        /// I2C Sensor
        /// </summary>
        I2C = 0x0A,
        /// <summary>
        /// I2C 9Volt
        /// </summary>
        I2C_9V = 0x0B, //previously LowSpeed9V
        /// <summary>
        /// I2C High speed?  Unused
        /// </summary>
        HighSpeed = 0x0C,
        /// <summary>
        /// NXT Color sensor in Color mode
        /// </summary>
        ColorFull = 0x0D,
        /// <summary>
        /// NXT Color sensor in light sensor mode with red light
        /// </summary>
        ColorRed = 0x0E,
        /// <summary>
        /// NXT Color sensor in light sensor mode with green light
        /// </summary>
        ColorGreen = 0x0F,
        /// <summary>
        /// NXT Color sensor in light sensor mode with blue light
        /// </summary>
        ColorBlue = 0x10,
        /// <summary>
        /// NXT Color sensor in light sensor mode with no light
        /// </summary>
        ColorNone = 0x11,
        /// <summary>
        /// Total number of types
        /// </summary>
        NumberOfSensorTypes = 0x12,
    }


    /// <summary>
    /// The translation mode of the LEGO NXT sensor.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("The translation mode of the LEGO NXT sensor.")]
    [XmlRootAttribute("LegoSensorMode", Namespace = Contract.Identifier)]
    public enum LegoSensorMode
    {
        /// <summary>
        /// RawMode
        /// </summary>
        RawMode = 0x00,
        /// <summary>
        /// BooleanMode
        /// </summary>
        BooleanMode = 0x20,
        /// <summary>
        /// TransitionCountMode
        /// </summary>
        TransitionCountMode = 0x40,
        /// <summary>
        /// PeriodCounterMode
        /// </summary>
        PeriodCounterMode = 0x60,
        /// <summary>
        /// PercentFullScaleMode
        /// </summary>
        PercentFullScaleMode = 0x80,
        /// <summary>
        /// CelsiusMode
        /// </summary>
        CelsiusMode = 0xA0,
        /// <summary>
        /// FahrenheitMode
        /// </summary>
        FahrenheitMode = 0xC0,
        /// <summary>
        /// AngleStepsMode
        /// </summary>
        AngleStepsMode = 0xE0,
        /// <summary>
        /// SlopeMask
        /// </summary>
        SlopeMask = 0x1F,
        /// <summary>
        /// ModeMask
        /// </summary>
        ModeMask = 0xE0,
    }

    #endregion

    #region Contract

    /// <summary>
    /// NxtCommon Contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// The Dss Service contract
        /// </summary>
        [DataMember, Description("The unique DSS Service Contract for the LEGO NXT Common library.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/common.user.html";
    }
    #endregion
}
