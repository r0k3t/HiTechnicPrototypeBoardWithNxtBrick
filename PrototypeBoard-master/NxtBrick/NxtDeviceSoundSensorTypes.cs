//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceSoundSensorTypes.cs $ $Revision: 11 $
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


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.SoundSensor
{
    
    /// <summary>
    /// LegoNxtSoundSensor Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the NxtSoundSensor service
        [DataMember, Description("Identifies the Unique Contract Identifier for the NxtSoundSensor service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/soundsensor.user.html";

        /// <summary>
        /// The NxtSoundSensor Device Type
        /// </summary>
        [DataMember, Description("Identifies the NxtSoundSensor Device Type.")]
        public const string DeviceModel = "SoundSensor";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 500;

    }
    
    /// <summary>
    /// LEGO NXT Sound Sensor State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Sound Sensor state.")]
    public class SoundSensorState 
    {
        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// Name
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Sound Sensor.")]
        public string Name;

        /// <summary>
        /// Sensor Port
        /// </summary>
        [DataMember, Description("Specifies the LEGO NXT Sensor Port.")]
        public NxtSensorPort SensorPort;

        /// <summary>
        /// Polling Freqency (ms)
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds (0 = default).")]
        public int PollingFrequencyMs;

        /// <summary>
        /// The current sound intensity received from the Sound Sensor.
        /// </summary>
        [DataMember, Description("Indicates the current sound intensity received from the Sound Sensor.")]
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
    /// Get the LEGO Sound Sensor State
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Sound Sensor.")]
    public class Get : Get<GetRequestType, PortSet<SoundSensorState, Fault>>
    {
    }

    /// <summary>
    /// Indicates an update to the LEGO NXT Sound Sensor State.
    /// </summary>
    [Description("Indicates an update to the LEGO NXT Sound Sensor State.")]
    public class SoundSensorUpdate : Update<SoundSensorState, PortSet<DefaultUpdateResponseType, Fault>>
    {
    }


    /// <summary>
    /// Configure Device Connection
    /// </summary>
    [DisplayName("(User) ConnectionUpdate")]
    [Description("Connects the LEGO NXT Sound Sensor to be plugged into the NXT Brick.")]
    public class ConnectToBrick : Update<SoundSensorConfig, PortSet<DefaultUpdateResponseType, Fault>>
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
            : base(new SoundSensorConfig(sensorPort))
        {
        }

    }

    /// <summary>
    /// LEGO NXT Sound Sensor Configuration.
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Sound Sensor Configuration.")]
    public class SoundSensorConfig
    {
        /// <summary>
        /// LEGO NXT Sound Sensor Configuration.
        /// </summary>
        public SoundSensorConfig() { }

        /// <summary>
        /// LEGO NXT Sound Sensor Configuration.
        /// </summary>
        /// <param name="sensorPort"></param>
        public SoundSensorConfig(NxtSensorPort sensorPort)
        {
            this.SensorPort = sensorPort;
        }

        /// <summary>
        /// The name of this Sound Sensor instance
        /// </summary>
        [DataMember, Description("Specifies a user friendly name for the LEGO NXT Sound Sensor.")]
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
    /// Sound Sensor Operations Port
    /// </summary>
    [ServicePort]
    public class SoundSensorOperations : PortSet
    {
        /// <summary>
        /// Sound Sensor Operations Port
        /// </summary>
        public SoundSensorOperations()
            : base(
                typeof(DsspDefaultLookup),
                typeof(DsspDefaultDrop),
                typeof(ConnectToBrick),
                typeof(dssphttp.HttpGet),
                typeof(Get),
                typeof(SoundSensorUpdate),     
                typeof(pxanalogsensor.ReliableSubscribe),
                typeof(pxanalogsensor.Subscribe)
            )
        { }


        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(SoundSensorConfig body)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = body ?? new SoundSensorConfig();
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Configure Sensor Connection with body and return the response port.
        /// </summary>
        public virtual PortSet<DefaultUpdateResponseType, Fault> ConnectToBrick(SoundSensorState state)
        {
            ConnectToBrick op = new ConnectToBrick();
            op.Body = new SoundSensorConfig(state.SensorPort);
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
