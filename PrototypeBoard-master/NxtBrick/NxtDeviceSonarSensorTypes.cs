//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceSonarSensorTypes.cs $ $Revision: 10 $
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


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.SonarSensor
{
    
    /// <summary>
    /// NxtUltrasonicSensor Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the NxtUltrasonicSensor service
        [DataMember, Description("Identifies the Unique Contract Identifier for the NxtUltrasonicSensor service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/sonarsensor.user.html";

        /// <summary>
        /// The Nxt Ultrasonic Sensor Device Type
        /// </summary>
        [DataMember, Description("Identifies the Nxt Ultrasonic Sensor Device Type.")]
        public const string DeviceModel = "UltrasonicSensor";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 500;

    }

    /// <summary>
    /// LEGO NXT Sonar Sensor State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Ultrasonic Sensor state.")]
    public class SonarSensorState 
    {
        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// The name of this Sonar Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Ultrasonic Sensor.")]
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
        /// The current sonar distance (cm) received from the Sonar Sensor
        /// </summary>
        [DataMember, Description("Indicates the current distance (cm) received from the Ultrasonic Sensor.")]
        [Browsable(false)]
        public int Distance;

        /// <summary>
        /// The time of the last sensor update
        /// </summary>
        [DataMember, Description("Indicates the time of the last sensor reading.")]
        [Browsable(false)]
        public DateTime TimeStamp;
    }

    /// <summary>
    /// Get the LEGO Sonar Sensor State
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Ultrasonic Sensor.")]
    public class Get : Get<GetRequestType, PortSet<SonarSensorState, Fault>>
    {
    }

    /// <summary>
    /// Get the LEGO Sonar Sensor State
    /// </summary>
    [Description("Indicates an update to the LEGO NXT Ultrasonic Sensor State.")]
    public class SonarSensorUpdate : Update<SonarSensorState, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }


    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate")]
    [Description("Connects the LEGO NXT Ultrasonic Sensor to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<SonarSensorConfig, PortSet<DefaultUpdateResponseType, Fault>>
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
            : base(new SonarSensorConfig(sensorPort))
        {
        }

    }

    /// <summary>
    /// LEGO NXT Sonar Sensor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Ultrasonic Sensor Configuration.")]
    public class SonarSensorConfig
    {
        /// <summary>
        /// LEGO NXT Sonar Sensor Configuration.
        /// </summary>
        public SonarSensorConfig() { }

        /// <summary>
        /// LEGO NXT Sonar Sensor Configuration.
        /// </summary>
        /// <param name="sensorPort"></param>
        public SonarSensorConfig(NxtSensorPort sensorPort)
        {
            this.SensorPort = sensorPort;
        }

        /// <summary>
        /// The name of this Sonar Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Ultrasonic Sensor.")]
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
    /// LEGO NXT Sonar Sensor Operations Port
    /// </summary>
    [ServicePort]
    public class UltrasonicSensorOperations : PortSet
    {
        /// <summary>
        /// LEGO NXT Sonar Sensor Operations Port
        /// </summary>
        public UltrasonicSensorOperations()
            : base(
                typeof(DsspDefaultLookup),
                typeof(DsspDefaultDrop),
                typeof(ConnectToBrick),
                typeof(dssphttp.HttpGet),
                typeof(Get),
                typeof(SonarSensorUpdate),     
                typeof(pxanalogsensor.ReliableSubscribe),
                typeof(pxanalogsensor.Subscribe)
            )
        { }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(SonarSensorConfig body)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = body ?? new SonarSensorConfig();
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(SonarSensorState state)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = new SonarSensorConfig(state.SensorPort);
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
