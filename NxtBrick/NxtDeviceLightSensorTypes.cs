//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceLightSensorTypes.cs $ $Revision: 12 $
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


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.LightSensor
{
    
    /// <summary>
    /// LegoNxtLightSensor Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the LegoNxtLightSensor service
        [DataMember, Description("Identifies the Unique Contract Identifier for the NxtLightSensor service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/lightsensor.user.html";

        /// <summary>
        /// The Light Sensor Device Type
        /// </summary>
        [DataMember, Description("Identifies the Light Sensor Device Type.")]
        public const string DeviceModel = "LightSensor";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 500;

    }

    /// <summary>
    /// LEGO NXT Light Sensor State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Light Sensor state.")]
    public class LightSensorState
    {
        private bool _spotlightOn;

        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// The name of this Light Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Light Sensor.")]
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
        /// Indicates the current state of the light sensor spotlight
        /// </summary>
        [DataMember, Description("Indicates the current state of the light sensor spotlight.")]
        [DisplayName("(User) IsOn")]
        public bool SpotlightOn{get{return _spotlightOn;} set{_spotlightOn=value;} }

        /// <summary>
        /// The intensity of the current light reading
        /// </summary>
        [DataMember, Description("Indicates the intensity of the current light reading.")]
        [Browsable(false)]
        public int Intensity;

        /// <summary>
        /// The time of the last sensor update
        /// </summary>
        [DataMember, Description("Indicates the time of the last sensor reading.")]
        [Browsable(false)]
        public DateTime TimeStamp;
    }

    /// <summary>
    /// Request the spotlight to be turned on or off.
    /// </summary>
    [DataContract, Description("Requests the spotlight to be turned on or off.")]
    public class SpotlightRequest
    {
        private bool _spotlightOn;

        /// <summary>
        /// The requested state of the spotlight on the LEGO NXT light sensor
        /// </summary>
        [DataMember, Description("Specifies the requested state of the spotlight on the LEGO NXT light sensor.")]
        [DataMemberConstructor(Order = 1)]
        [DisplayName("(User) IsOn")]
        public bool SpotlightOn 
        { 
            get { return _spotlightOn; } 
            set { _spotlightOn = value; } 
        }

        /// <summary>
        /// Request the spotlight to be turned on or off.
        /// </summary>
        public SpotlightRequest() { }

        /// <summary>
        /// Request the spotlight to be turned on or off.
        /// </summary>
        /// <param name="spotlightOn"></param>
        public SpotlightRequest(bool spotlightOn) { this.SpotlightOn = spotlightOn; }
    }


    /// <summary>
    /// Turn the LEGO LightSensor spotlight on or off.
    /// </summary>
    [Description("Turns the LEGO LightSensor spotlight on or off (or indicates that the spotlight has been turned on or off).")]
    [DisplayName("(User) SpotlightUpdate")]
    public class Spotlight : Update<SpotlightRequest, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Turn the LEGO LightSensor spotlight on or off.
        /// </summary>
        public Spotlight()
        {
        }

        /// <summary>
        /// Turn the LEGO LightSensor spotlight on or off.
        /// </summary>
        public Spotlight(SpotlightRequest body)
            : base(body)
        {
        }

        /// <summary>
        /// Turn the LEGO LightSensor spotlight on or off.
        /// </summary>
        /// <param name="spotlightOn"></param>
        public Spotlight(bool spotlightOn)
        {
            base.Body = new SpotlightRequest(spotlightOn);
        }

        /// <summary>
        /// Turn the LEGO LightSensor spotlight on or off.
        /// </summary>
        public Spotlight(SpotlightRequest body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }


    /// <summary>
    /// Get the LEGO Light Sensor State
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Light Sensor.")]
    public class Get : Get<GetRequestType, PortSet<LightSensorState, Fault>>
    {
    }

    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate")]
    [Description("Connects the LEGO NXT Light Sensor to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<LightSensorConfig, PortSet<DefaultUpdateResponseType, Fault>>
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
            : base(new LightSensorConfig(sensorPort))
        {
        }

    }

    /// <summary>
    /// LEGO NXT Light Sensor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Light Sensor Configuration.")]
    public class LightSensorConfig
    {
        /// <summary>
        /// LEGO NXT Light Sensor Configuration.
        /// </summary>
        public LightSensorConfig() { }

        /// <summary>
        /// LEGO NXT Light Sensor Configuration.
        /// </summary>
        /// <param name="sensorPort"></param>
        public LightSensorConfig(NxtSensorPort sensorPort)
        {
            this.SensorPort = sensorPort;
        }

        /// <summary>
        /// The name of this Light Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Light Sensor.")]
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


        /// <summary>
        /// Indicates the initial state of the light sensor spotlight
        /// </summary>
        [DataMember, Description("Indicates the initial state of the light sensor spotlight.")]
        [DisplayName("(User) IsOn")]
        public bool SpotlightOn { get { return _spotlightOn; } set { _spotlightOn = value; } }
        private bool _spotlightOn;
    }

    /// <summary>
    /// Replace the LEGO Light Sensor State
    /// </summary>
    [Description("Replaces the current state of the LEGO NXT Light Sensor.")]
    public class Replace : Replace<LightSensorState, PortSet<DefaultReplaceResponseType, Fault>>
    {
    }

    /// <summary>
    /// Subscribe to LEGO NXT Light Sensor notifications
    /// </summary>
    [Description("Subscribes to LEGO NXT Light Sensor notifications.")]
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>>
    {
    }

    /// <summary>
    /// LEGO NXT Light Sensor Operations
    /// </summary>
    [ServicePort]
    public class LightSensorOperations : PortSet
    {
        /// <summary>
        /// LEGO NXT Light Sensor Operations
        /// </summary>
        public LightSensorOperations()
            : base(
                typeof(DsspDefaultLookup),
                typeof(DsspDefaultDrop),
                typeof(Spotlight),
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
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(LightSensorConfig body)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = body ?? new LightSensorConfig();
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(LightSensorState state)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = new LightSensorConfig(state.SensorPort);
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
}
