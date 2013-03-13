//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceTouchSensorTypes.cs $ $Revision: 14 $
//-----------------------------------------------------------------------

using Microsoft.Dss.Core.Attributes;
using System;
using System.Collections.Generic;
using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using contactsensor = Microsoft.Robotics.Services.ContactSensor.Proxy;
using System.ComponentModel;
using System.Xml.Serialization;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Ccr.Core;
using W3C.Soap;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using Microsoft.Dss.Core.Utilities;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.TouchSensor
{
    
    /// <summary>
    /// LegoNxtTouchSensor Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the NxtTouchSensor service
        [DataMember, Description("Identifies the Unique Contract Identifier for the NxtTouchSensor service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/touchsensor.user.html";

        /// <summary>
        /// The NxtTouchSensor Device Type
        /// </summary>
        [DataMember, Description("Identifies the NxtTouchSensor Device Type.")]
        public const string DeviceModel = "TouchSensor";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 100;

    }

    /// <summary>
    /// LEGO NXT Touch Sensor State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Touch Sensor state.")]
    public class TouchSensorState 
    {
        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// The name of this Touch Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Touch Sensor.")]
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
        /// The current state of the active Touch Sensor
        /// </summary>
        [DataMember, Description("Indicates the current state of the active Touch Sensor.")]
        [Browsable(false)]
        public bool TouchSensorOn;

        /// <summary>
        /// The time of the last sensor update
        /// </summary>
        [DataMember, Description("Indicates the time of the last sensor reading.")]
        [Browsable(false)]
        public DateTime TimeStamp;
    }

    /// <summary>
    /// Get the LEGO Touch Sensor State
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Touch Sensor.")]
    public class Get : Get<GetRequestType, PortSet<TouchSensorState, Fault>>
    {
    }

    /// <summary>
    /// Get the LEGO Touch Sensor State
    /// </summary>
    [Description("Indicates an update to the LEGO NXT Touch Sensor State.")]
    public class TouchSensorUpdate : Update<TouchSensorState, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }


    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate")]
    [Description("Connects the LEGO NXT Touch Sensor to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<TouchSensorConfig, PortSet<DefaultUpdateResponseType, Fault>>
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
            : base(new TouchSensorConfig(sensorPort))
        {
        }

    }

    /// <summary>
    /// LEGO NXT Touch Sensor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Touch Sensor Configuration.")]
    public class TouchSensorConfig
    {
        /// <summary>
        /// LEGO NXT Touch Sensor Configuration.
        /// </summary>
        public TouchSensorConfig() { }

        /// <summary>
        /// LEGO NXT Touch Sensor Configuration.
        /// </summary>
        /// <param name="sensorPort"></param>
        public TouchSensorConfig(NxtSensorPort sensorPort)
        {
            this.SensorPort = sensorPort;
        }

        /// <summary>
        /// The name of this Touch Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Touch Sensor.")]
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
    /// LEGO NXT Touch Sensor Operations Port
    /// </summary>
    [ServicePort]
    public class TouchSensorOperations : PortSet
    {
        /// <summary>
        /// LEGO NXT Touch Sensor Operations Port
        /// </summary>
        public TouchSensorOperations()
            : base(
                typeof(DsspDefaultLookup),
                typeof(DsspDefaultDrop),
                typeof(ConnectToBrick),
                typeof(dssphttp.HttpGet),
                typeof(Get),
                typeof(TouchSensorUpdate),
                typeof(contactsensor.ReliableSubscribe),
                typeof(contactsensor.Subscribe)
            )
        { }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(TouchSensorConfig body)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = body ?? new TouchSensorConfig();
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(TouchSensorState state)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = new TouchSensorConfig(state.SensorPort);
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
