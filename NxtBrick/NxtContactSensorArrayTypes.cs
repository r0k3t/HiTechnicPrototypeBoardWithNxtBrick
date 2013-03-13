//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtContactSensorArrayTypes.cs $ $Revision: 6 $
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;
using dssphttp = Microsoft.Dss.Core.DsspHttp;
using nxtcommon = Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.ContactSensorArray
{
    /// <summary>
    /// Runtime configuration of the ContactSensorArray.
    /// </summary>
    [DataContract, Description("Runtime configuration of the ContactSensorArray.")]
    public class PortConfiguration
    {
        #region Constructors and Methods

        /// <summary>
        /// Translate an analog value to a ContactSensor.Pressed value.
        /// </summary>
        /// <param name="analogValue"></param>
        /// <returns></returns>
        internal bool Pressed(double analogValue)
        {
            if (this.SuccessRangeMax >= this.SuccessRangeMin)
                return (analogValue >= this.SuccessRangeMin && analogValue <= this.SuccessRangeMax);

            // This is an exclusion range.
            return (analogValue <= this.SuccessRangeMax || analogValue >= this.SuccessRangeMin);
        }


        /// <summary>
        /// Runtime configuration of the ContactSensorArray.
        /// </summary>
        public PortConfiguration()
        {
        }

        /// <summary>
        /// Runtime configuration of the ContactSensorArray.
        /// </summary>
        /// <param name="hardwareIdentifier"></param>
        /// <param name="name"></param>
        /// <param name="successRangeMin"></param>
        /// <param name="successRangeMax"></param>
        public PortConfiguration(int hardwareIdentifier, string name, double successRangeMin, double successRangeMax) 
        {
            this.HardwareIdentifier = hardwareIdentifier;
            this.Name = name;
            this.SuccessRangeMin = successRangeMin;
            this.SuccessRangeMax = successRangeMax;
        }
        #endregion

        /// <summary>
        /// Indicates the Hardware Identifier.
        /// </summary>
        [DataMember, Description("Indicates the Hardware Identifier (1-4).")]
        public int HardwareIdentifier;
        /// <summary>
        /// Indicates the Sensor Name.
        /// </summary>
        [DataMember, Description("Indicates the Sensor Name.")]
        public string Name;
        /// <summary>
        /// Specifies the minimum value of the success range for this type of sensor.
        /// </summary>
        [DataMember, Description("Specifies the minimum value of the success range for this type of sensor.")]
        public double SuccessRangeMin;
        /// <summary>
        /// Specifies the maximum value of the success range for this type of sensor.
        /// </summary>
        [DataMember, Description("Specifies the maximum value of the success range for this type of sensor.")]
        public double SuccessRangeMax;

        /// <summary>
        /// "Specifies the Service URI of the analog sensor service for this sensor."
        /// </summary>
        [DataMember, Description("Specifies the Service URI of the analog sensor service for this sensor.")]
        public string AnalogSensorServiceUri;

        /// <summary>
        /// Indicates the current state of the Contact Sensor.
        /// </summary>
        [DataMember, Description("Indicates the current state of the Contact Sensor.")]
        public bool Contact;
    }

    /// <summary>
    /// Identifies a Sensor Range on the specified Sensor Port.
    /// </summary>
    [DataContract, Description("Identifies a Sensor Range on the specified Sensor Port.")]
    public class SensorRange
    {
        private nxtcommon.NxtSensorPort _sensorPort;

        /// <summary>
        /// Identifies the Sensor Port.
        /// </summary>
        [DataMember, Description("Identifies the Sensor Port.")]
        public nxtcommon.NxtSensorPort SensorPort
        {
            get { return _sensorPort; }
            set 
            {
                if (value != NxtSensorPort.NotConnected)
                    _sensorPort = value; 
            }
        }

        /// <summary>
        /// Identifies the Contact Sensor Hardware Identifier.
        /// </summary>
        [DataMember, Description("Identifies the Contact Sensor Hardware Identifier.")]
        [DataMemberConstructor(Order = 1)]
        public int HardwareIdentifier
        {
            get 
            {
                return NxtCommon.HardwareIdentifier(this.SensorPort);
            }
            set 
            {
                if (value != 0)
                    SensorPort = NxtCommon.GetNxtSensorPortFromHardwareIdentifier(value);
            }
        }

        /// <summary>
        /// Identifies the NXT Sensor Model.
        /// </summary>
        [DataMember, Description("Identifies the NXT Sensor Model.")]
        [DataMemberConstructor(Order = 2)]
        public string Model;

        /// <summary>
        /// Identifies the NXT Sensor Name.
        /// </summary>
        [DataMember, Description("Identifies the NXT Sensor Name.")]
        [DataMemberConstructor(Order = 3)]
        public string SensorName;

        /// <summary>
        /// Identifies the name of this Sensor Range.
        /// </summary>
        [DataMember, Description("Identifies the name of this Sensor Range.")]
        [DataMemberConstructor(Order = 4)]
        public string RangeName;

        #region Constructors and Methods

        /// <summary>
        /// Identifies a Sensor Range on the specified Sensor Port.
        /// </summary>
        public SensorRange()
        {
        }

        /// <summary>
        /// Identifies a Sensor Range on the specified Sensor Port.
        /// </summary>
        /// <param name="hardwareIdentifier"></param>
        /// <param name="model"></param>
        /// <param name="sensorName"></param>
        /// <param name="rangeName"></param>
        public SensorRange(int hardwareIdentifier, string model, string sensorName, string rangeName) 
        {
            _sensorPort = NxtCommon.GetNxtSensorPortFromHardwareIdentifier(hardwareIdentifier);
            Model = model;
            SensorName = sensorName;
            RangeName = rangeName;
        }

        /// <summary>
        /// Generate a unique Contact Sensor Name which is derrived from the Sensor Port, Model, Range Name and Sensor Name.
        /// </summary>
        public string ContactSensorName
        {
            get
            {
                string connection = (string.IsNullOrEmpty(SensorName)) ? SensorPort.ToString() : SensorName;
                return string.Format("{0}.{1} on {2}", Model, RangeName, connection);
            }
        }

        #endregion
    }

    /// <summary>
    /// Configures the minimum and maximum values of an analog sensor to translate the
    /// analog value to ContactSensor.Pressed.
    /// </summary>
    [DataContract, Description("Configures the minimum and maximum values of an analog sensor to translate the analog value to ContactSensor.Pressed.")]
    public class SensorConfiguration
    {
        /// <summary>
        /// Specifies the user friendly range name.
        /// </summary>
        [DataMember, Description("Specifies the user friendly range name.")]
        [DataMemberConstructor(Order = 1)]
        public string RangeName;

        /// <summary>
        /// Identifies the device model for which this range applies.
        /// </summary>
        [DataMember, Description("Identifies the device model for which this range applies (Required).")]
        [DataMemberConstructor(Order = 2)]
        public string DeviceModel;

        /// <summary>
        /// Optionally Identifies the user friendly device name to which this range applies.
        /// </summary>
        [DataMember, Description("Optionally Identifies the user friendly device name to which this range applies.")]
        [DataMemberConstructor(Order = 3)]
        public string DeviceName;

        /// <summary>
        /// Specifies the minimum value of the success range for this type of sensor.
        /// </summary>
        [DataMember, Description("Specifies the minimum value of the success range for this type of sensor.")]
        [DataMemberConstructor(Order = 4)]
        public double SuccessRangeMin;
        /// <summary>
        /// Specifies the maximum value of the success range for this type of sensor.
        /// </summary>
        [DataMember, Description("Specifies the maximum value of the success range for this type of sensor.")]
        [DataMemberConstructor(Order = 5)]
        public double SuccessRangeMax;

        #region Constructors
        /// <summary>
        /// Configures the minimum and maximum values of an analog sensor to translate the
        /// analog value to ContactSensor.Pressed.
        /// </summary>
        public SensorConfiguration()
        {
        }
        /// <summary>
        /// Configures the minimum and maximum values of an analog sensor to translate the
        /// analog value to ContactSensor.Pressed.
        /// </summary>
        /// <param name="rangeName"></param>
        /// <param name="deviceModel"></param>
        /// <param name="deviceName"></param>
        /// <param name="successRangeMin"></param>
        /// <param name="successRangeMax"></param>
        public SensorConfiguration(string rangeName, string deviceModel, string deviceName, double successRangeMin, double successRangeMax)
        {
            this.RangeName = rangeName;
            this.DeviceModel = deviceModel;
            this.DeviceName = deviceName;
            this.SuccessRangeMin = successRangeMin;
            this.SuccessRangeMax = successRangeMax;
        }
        #endregion
    }

    /// <summary>
    /// Resets the configuration of the LEGO NXT Contact Sensor Array.
    /// </summary>
    [DataContract, Description("Resets the configuration of the LEGO NXT Contact Sensor Array.")]
    public class ResetConfigurationRequest
    {
    }

    /// <summary>
    /// Configures the minimum and maximum Contact Sensor range for any LEGO NXT device which implements the Generice Analog Sensor contract.
    /// </summary>
    [DataContract, Description("Configures the minimum and maximum Contact Sensor range for any LEGO NXT device which implements the Generice Analog Sensor contract.")]
    public class NxtContactSensorArrayState
    {
        /// <summary>
        /// Configures the minimum and maximum Contact Sensor range for any LEGO NXT device which implements the Generice Analog Sensor contract.
        /// </summary>
        [DataMember, Description("Configures the minimum and maximum Contact Sensor range for any LEGO NXT device which implements the Generice Analog Sensor contract.")]
        [DataMemberConstructor(Order=1)]
        public List<SensorConfiguration> SensorConfiguration;

        /// <summary>
        /// Indicates the Runtime configuration of the Contact Sensor Array.
        /// </summary>
        [DataMember, Description("Indicates the Runtime configuration of the Contact Sensor Array.")]
        [Browsable(false)]
        public Dictionary<SensorRange, PortConfiguration> RuntimeConfiguration = null;
    }

    /// <summary>
    /// NxtContactSensorArray Main Operations Port
    /// </summary>
    [ServicePort()]
    public class NxtContactSensorArrayOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, dssphttp.HttpGet, ConfigureDevice, ResetConfiguration>
    {
    }

    /// <summary>
    /// NxtContactSensorArray Get Operation
    /// </summary>
    [Description("Returns the configuration state for the LEGO NXT Contact Sensor Array.")]
    public class Get : Get<GetRequestType, PortSet<NxtContactSensorArrayState, Fault>>
    {
        /// <summary>
        /// NxtContactSensorArray Get Operation
        /// </summary>
        public Get()
        {
        }
        /// <summary>
        /// NxtContactSensorArray Get Operation
        /// </summary>
        public Get(Microsoft.Dss.ServiceModel.Dssp.GetRequestType body)
            :
                base(body)
        {
        }
        /// <summary>
        /// NxtContactSensorArray Get Operation
        /// </summary>
        public Get(Microsoft.Dss.ServiceModel.Dssp.GetRequestType body, Microsoft.Ccr.Core.PortSet<NxtContactSensorArrayState, W3C.Soap.Fault> responsePort)
            :
                base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Configures a LEGO NXT Device to be interpreted as a Contact Sensor.
    /// The specified Device must implement the Generic Analog Sensor contract 
    /// in order to be used by the Contact Sensor Array service.
    /// </summary>
    [Description("Configures LEGO NXT Devices which implement the Generic Analog Sensor contract to be interpreted as Contact Sensors.")]
    public class ConfigureDevice : Upsert<SensorConfiguration, PortSet<DefaultUpsertResponseType, Fault>> { }

    /// <summary>
    /// Resets the configuration of the LEGO NXT Contact Sensor Array.
    /// </summary>
    [Description("Resets the configuration of the LEGO NXT Contact Sensor Array.")]
    public class ResetConfiguration : Delete<ResetConfigurationRequest, PortSet<DefaultDeleteResponseType, Fault>> { }

    /// <summary>
    /// Standard NXT Devices which support the generic Analog Sensor contract.
    /// </summary>
    [DataContract, Description("Standard NXT Devices which support the generic Analog Sensor contract.")]
    public enum StandardNxtDevices
    {
        /// <summary>
        /// The Device Model of the LEGO NXT Touch Sensor.
        /// </summary>
        TouchSensor,
        /// <summary>
        /// The Device Model of the LEGO NXT Light Sensor.
        /// </summary>
        LightSensor,
        /// <summary>
        /// The Device Model of the LEGO NXT Sound Sensor.
        /// </summary>
        SoundSensor,
        /// <summary>
        /// The Device Model of the LEGO NXT Ultrasonic Sensor.
        /// </summary>
        UltrasonicSensor,
        /// <summary>
        /// The Device Model of the HiTechnic Compass Sensor.
        /// </summary>
        Compass,
        /// <summary>
        /// The Device Model of the MindSensors Compass Sensor.
        /// </summary>
        CMPS,
    }

    #region Contract

    /// <summary>
    /// ContactSensorArray Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the ContactSensorArray service
        [DataMember()]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/10/contactsensorarray.user.html";
    }

    #endregion
}
