//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceColorSensorTypes.cs $ $Revision: 1 $
//-----------------------------------------------------------------------

using Microsoft.Dss.Core.Attributes;
using System;
using System.Collections.Generic;
using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using pxanalogsensor = Microsoft.Robotics.Services.AnalogSensor.Proxy;
using System.ComponentModel;
using System.Xml.Serialization;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Ccr.Core;
using W3C.Soap;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.ColorSensor
{
    
    /// <summary>
    /// LegoNxtColorSensor Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the LegoNxtColorSensor service
        [DataMember, Description("Identifies the Unique Contract Identifier for the LEGO NXT Color Sensor service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2010/03/lego/nxt/colorsensor.user.html";

        /// <summary>
        /// The Color Sensor Device Type
        /// </summary>
        [DataMember, Description("Identifies the Color Sensor Device Type.")]
        public const string DeviceModel = "ColorSensor";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 500;

    }

    /// <summary>
    /// LEGO NXT Color Sensor State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Color Sensor state.")]
    public class ColorSensorState
    {
        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// The name of this Color Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Color Sensor.")]
        public string Name;

        /// <summary>
        /// LEGO NXT Sensor Port
        /// </summary>
        [DataMember, Description("Specifies the LEGO NXT Sensor Port.")]
        public NxtSensorPort SensorPort;

        /// <summary>
        /// Polling Frequency in milliseconds
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds (0 = default).")]
        public int PollingFrequencyMs;

        /// <summary>
        /// The Mode that the sensor is in (See ColorSensorMode enum)
        /// </summary>
        [DataMember, Description("Indicates the Mode for the Color Sensor (See ColorSensorMode enum).")]
        public ColorSensorMode SensorMode;

        /// <summary>
        /// The current light reading (intensity 0-1023) or Color Number (1-6) in Color mode
        /// </summary>
        [DataMember, Description("Indicates the current light reading (intensity 0-1023) or Color Number (1-6) in Color mode.")]
        [Browsable(false)]
        public int Reading;

        /// <summary>
        /// The time of the last sensor update
        /// </summary>
        [DataMember, Description("Indicates the time of the last sensor reading.")]
        [Browsable(false)]
        public DateTime TimeStamp;
    }

    /// <summary>
    /// Request the Color Sensor to change Mode.
    /// </summary>
    [DataContract, Description("Requests the Color Sensor to change Modes.")]
    public class ModeRequest
    {
        private ColorSensorMode _mode;

        /// <summary>
        /// The requested Mode of the LEGO NXT Color sensor
        /// </summary>
        [DataMember, Description("Specifies the requested Mode of the LEGO NXT Color sensor.")]
        [DataMemberConstructor(Order = 1)]
        [DisplayName("(User) Mode")]
        public ColorSensorMode Mode
        { 
            get { return _mode; } 
            set { _mode = value; } 
        }

        /// <summary>
        /// Request the Mode to be changed.
        /// </summary>
        public ModeRequest() { }

        /// <summary>
        /// Request the Mode to be changed.
        /// </summary>
        /// <param name="mode">Sensor mode - See ColorSensorMode enum.</param>
        public ModeRequest(ColorSensorMode mode) { this.Mode = mode; }
    }


    /// <summary>
    /// Set the LEGO Color Sensor Mode.
    /// </summary>
    [Description("Set the LEGO Color Sensor Mode. (Also used in notifications).")]
    [DisplayName("(User) ModeUpdate")]
    public class SetMode : Update<ModeRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Set the LEGO Color Sensor Mode - Default constructor.
        /// </summary>
        public SetMode()
        {
        }

        /// <summary>
        /// Set the LEGO Color Sensor Mode.
        /// </summary>
        /// <param name="body">An initialized ModeRequest</param>
        public SetMode(ModeRequest body)
            : base(body)
        {
        }

        /// <summary>
        /// Set the LEGO Color Sensor Mode.
        /// </summary>
        /// <param name="mode">Sensor mode - Select from ColorSensorMode enum</param>
        public SetMode(ColorSensorMode mode)
        {
            base.Body = new ModeRequest(mode);
        }

        /// <summary>
        /// Set the LEGO Color Sensor Mode.
        /// </summary>
        /// <param name="body">An initialized ModeRequest</param>
        /// <param name="responsePort">Response Port</param>
        public SetMode(ModeRequest body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }


    /// <summary>
    /// Get the LEGO Color Sensor State
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Color Sensor.")]
    public class Get : Get<GetRequestType, PortSet<ColorSensorState, Fault>>
    {
    }

    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate (Internal use only)")]
    [Description("Connects the LEGO NXT Color Sensor to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<ColorSensorConfig, PortSet<DefaultUpdateResponseType, Fault>>
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
            : base(new ColorSensorConfig(sensorPort))
        {
        }

    }

    /// <summary>
    /// LEGO NXT Color Sensor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Color Sensor Configuration.")]
    public class ColorSensorConfig
    {
        /// <summary>
        /// LEGO NXT Color Sensor Configuration - Default constructor.
        /// </summary>
        public ColorSensorConfig() { }

        /// <summary>
        /// LEGO NXT Color Sensor Configuration.
        /// </summary>
        /// <param name="sensorPort"></param>
        public ColorSensorConfig(NxtSensorPort sensorPort)
        {
            this.SensorPort = sensorPort;
        }

        /// <summary>
        /// The name of this Color Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Color Sensor.")]
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
        [DataMember, Description("Indicates the Polling Frequency in milliseconds (0 = use default).")]
        public int PollingFrequencyMs;

        /// <summary>
        /// The Mode that the sensor is in
        /// </summary>
        [DataMember, Description("Indicates the mode for the Color Sensor (See ColorSensorMode enum).")]
        public ColorSensorMode SensorMode;

    }

    /// <summary>
    /// Replace the LEGO Color Sensor State
    /// </summary>
    [Description("Replaces the current state of the LEGO NXT Color Sensor.")]
    public class Replace : Replace<ColorSensorState, PortSet<DefaultReplaceResponseType, Fault>>
    {
    }

    /// <summary>
    /// Subscribe to LEGO NXT Color Sensor notifications
    /// </summary>
    [Description("Subscribes to LEGO NXT Color Sensor notifications.")]
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>>
    {
    }

    /// <summary>
    /// LEGO NXT Color Sensor Operations
    /// </summary>
    [ServicePort]
    public class ColorSensorOperations : PortSet
    {
        /// <summary>
        /// LEGO NXT Color Sensor Operations
        /// </summary>
        public ColorSensorOperations()
            : base(
                typeof(DsspDefaultLookup),
                typeof(DsspDefaultDrop),
                typeof(SetMode),
                typeof(ConnectToBrick),
                typeof(dssphttp.HttpGet),
                typeof(Get),
                typeof(Subscribe),
                typeof(Replace)
            )
        { }


        /// <summary>
        /// Add Types with an inherited PortSet
        /// </summary>
        /// <param name="types"></param>
        protected void AddTypes(params Type[] types)
        {
            List<Type> newTypes = null;
            List<IPort> newPortsTable = new List<IPort>();

            if (types != null && types.Length > 0)
            {
                newTypes = new List<Type>(types);
                foreach (Type t in types)
                {
                    Type portType = typeof(Port<>).MakeGenericType(t);
                    newPortsTable.Add((IPort)Activator.CreateInstance(portType));
                }
            }
            else
                newTypes = new List<Type>();

            if (base.Types != null && base.Types.Length > 0)
            {
                int iy = 0;
                foreach (Type t in base.Types)
                {
                    if (ValidateType(t, newTypes))
                    {
                        newTypes.Add(t);
                        newPortsTable.Add(base.PortsTable[iy]);
                    }
                    iy++;
                }
            }

            base.PortsTable = newPortsTable.ToArray();
            base.Types = newTypes.ToArray();
        }

        private bool ValidateType(Type newType, List<Type> types)
        {
            if (types.Contains(newType))
                return false;
            Type requestType = null;
            if (newType.BaseType.IsGenericType)
                requestType = newType.BaseType.GetGenericArguments()[0];

            foreach (Type t in types)
            {
                if (newType.IsInstanceOfType(t))
                    return false;
                if (t.BaseType.IsGenericType && t.BaseType.GetGenericArguments()[0].Equals(requestType))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        /// <param name="body">An initialized ColorSensorConfig</param>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(ColorSensorConfig body)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = body ?? new ColorSensorConfig();
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        /// <param name="state">Color Sensor State to use for initialization</param>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(ColorSensorState state)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = new ColorSensorConfig(state.SensorPort);
            op.Body.Name = state.Name;
            op.Body.PollingFrequencyMs = state.PollingFrequencyMs;
            op.Body.SensorMode = state.SensorMode;

            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        /// <param name="sensorPort">The sensor port to connect to</param>
        /// <returns></returns>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(NxtSensorPort sensorPort)
        {
            ConnectToBrick op = new ConnectToBrick(sensorPort);
            this.PostUnknownType(op);
            return op.ResponsePort;
        }
    }

    /// <summary>
    /// Color Sensor Mode
    /// </summary>
    /// <remarks>The mode determines which LED(s) are turned on.</remarks>
    [DataContract, Description("Specifies the NXT Color Sensor Mode.")]
    public enum ColorSensorMode
    {
        /// <summary>
        /// The Color Sensor is in Color Mode with all LEDs On (Default)
        /// </summary>
        /// <remarks>The Readings range from 1-6 for standard LEGO brick colors.</remarks>
        Color = 0x00,
        /// <summary>
        /// The Color Sensor is in Red Mode (Red LED On)
        /// </summary>
        Red = 0x01,
        /// <summary>
        /// The Color Sensor is in Green Mode (Green LEG On)
        /// </summary>
        Green = 0x02,
        /// <summary>
        /// The Color Sensor is in Blue Mode (Blue LED On)
        /// </summary>
        Blue = 0x03,
        /// <summary>
        /// The Color Sensor is in Light Sensor Mode (All LEDs Off)
        /// </summary>
        None = 0x04,
        /// <summary>
        /// The Color Sensor is in an Unknown Mode
        /// </summary>
        Unknown = 0xFF
    }

    /// <summary>
    /// Color Numbers
    /// </summary>
    /// <remarks>These codes are returned as Readings when the sensor is in Color mode.
    /// The Readings range from 1-6 for standard LEGO brick colors.</remarks>
    [DataContract, Description("Specifies the codes for the Color Number (from 1 to 6).")]
    public enum ColorNumber
    {
        /// <summary>
        /// The Color is Unknown
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Black or nothing in range (no color detected)
        /// </summary>
        Black = 1,
        /// <summary>
        /// Blue
        /// </summary>
        Blue = 2,
        /// <summary>
        /// Green
        /// </summary>
        Green = 3,
        /// <summary>
        /// Yellow
        /// </summary>
        Yellow = 4,
        /// <summary>
        /// Red
        /// </summary>
        Red = 5,
        /// <summary>
        /// White (or light is too bright to detect a color)
        /// </summary>
        White = 6
    }
}
