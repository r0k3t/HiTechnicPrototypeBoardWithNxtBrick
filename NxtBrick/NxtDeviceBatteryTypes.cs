//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceBatteryTypes.cs $ $Revision: 15 $
//-----------------------------------------------------------------------

using Microsoft.Dss.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using pxbattery = Microsoft.Robotics.Services.Battery.Proxy;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Ccr.Core;
using W3C.Soap;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Battery
{
    
    /// <summary>
    /// LegoNxtBattery Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the LegoNxtBattery service
        [DataMember, Description("Identifies the unique DSS Contract Identifier for the Lego NXT Battery (v2).")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/battery.user.html";

        /// <summary>
        /// The LEGO NXT Battery Device Type
        /// </summary>
        [DataMember, Description("Identifies the device model.")]
        public const string DeviceModel = "Battery";

        /// <summary>
        /// Default Battery Polling (Seconds)
        /// </summary>
        [DataMember, Description("Specifies the default Battery Polling (Seconds).")]
        public const int DefaultBatteryPollingSeconds = 15;
    }

    /// <summary>
    /// Battery State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT battery\'s state.")]
    public class BatteryState 
    {
        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired=false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        private int _batteryPollingSeconds = 15;
        /// <summary>
        /// Battery Polling Seconds (0 = default)
        /// </summary>
        [DataMember, Description("Identifies the frequency in seconds to poll for the battery voltage. \n(0 = default)")]
        [DisplayName("(User) PollingFrequencySeconds")]
        public int BatteryPollingSeconds
        {
            get { return _batteryPollingSeconds; }
            set { _batteryPollingSeconds = value; }
        }

        /// <summary>
        /// Full battery power
        /// </summary>
        [DataMember, Description("Identifies the power setting at which the battery is fully charged. \n(Suggested 9.0 volts)")]
        [Browsable(false)]
        public Double MaxVoltage;

        /// <summary>
        /// Critical battery voltage
        /// </summary>
        [DataMember, Description("Indicates the battery voltage at which operation may be impaired. \n(Suggested 5.8 volts)")]
        public Double CriticalBatteryVoltage;

        /// <summary>
        /// Minimum battery voltage;
        /// </summary>
        [DataMember, Description("Indicates the minimum battery voltage. \n(Suggested 5.0 volts)")]
        [Browsable(false)]
        public Double MinVoltage;

        /// <summary>
        /// Percentage of remaining battery power
        /// between 0.0 and 1.0
        /// </summary>
        [DataMember, Description("Indicates the percentage of battery power remaining. \n(0.00 - 1.00)")]
        [Browsable(false)]
        public Double PercentBatteryPower;

        /// <summary>
        /// Current Battery Voltage
        /// </summary>
        [DataMember, Description("Indicates the current battery voltage.")]
        [Browsable(false)]
        public Double CurrentBatteryVoltage;

        /// <summary>
        /// The time of the last battery reading
        /// </summary>
        [DataMember, Description("Indicates the time of the last battery reading.")]
        [Browsable(false)]
        public DateTime TimeStamp;

        /// <summary>
        /// Copy To the generic battery state
        /// </summary>
        /// <param name="genericBattery"></param>
        public pxbattery.BatteryState SyncGenericState(ref pxbattery.BatteryState genericBattery)
        {
            genericBattery.MaxBatteryPower = MaxVoltage;
            genericBattery.PercentBatteryPower = PercentBatteryPower;
            genericBattery.PercentCriticalBattery = CriticalBatteryVoltage / MaxVoltage;
            return genericBattery;
        }

        /// <summary>
        /// Copy From the generic battery state
        /// </summary>
        /// <param name="genericBattery"></param>
        public void CopyFrom(pxbattery.BatteryState genericBattery)
        {
            MaxVoltage = genericBattery.MaxBatteryPower;
            CriticalBatteryVoltage = MaxVoltage * genericBattery.PercentCriticalBattery;
            PercentBatteryPower = genericBattery.PercentBatteryPower;
            CurrentBatteryVoltage = MaxVoltage * PercentBatteryPower;
        }
    }


    /// <summary>
    /// Specifies the LEGO NXT battery\'s configuration.
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT battery\'s configuration.")]
    public class ConfigureBatteryRequest
    {
        /// <summary>
        /// Battery Polling Seconds (0 = default)
        /// </summary>
        [DataMember, Description("Identifies the frequency in seconds to poll for the battery voltage. \n(0 = default)")]
        public int PollingFrequencySeconds;

        /// <summary>
        /// Full battery power
        /// </summary>
        [DataMember, Description("Identifies the power setting at which the battery is fully charged. \n(Suggested 9.0 volts)")]
        public Double MaxVoltage;

        /// <summary>
        /// Critical battery voltage
        /// </summary>
        [DataMember, Description("Indicates the battery voltage at which operation may be impaired. \n(Suggested 5.8 volts)")]
        public Double CriticalBatteryVoltage;

        /// <summary>
        /// Minimum battery voltage;
        /// </summary>
        [DataMember, Description("Indicates the minimum battery voltage. \n(Suggested 5.0 volts)")]
        public Double MinVoltage;

    }


    /// <summary>
    /// Battery Operations Port
    /// </summary>
    [ServicePort]
    public class BatteryOperations : PortSet<
        DsspDefaultLookup,
        Get,
        HttpGet,
        ConfigureBattery>
    {
    }

    /// <summary>
    /// Get Operation
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Battery.")]
    public class Get : Get<GetRequestType, PortSet<BatteryState, Fault>> { }

    /// <summary>
    /// Replace Operation
    /// </summary>
    [Description("Sets the configuration of the LEGO NXT Battery.")]
    public class ConfigureBattery : Update<ConfigureBatteryRequest, PortSet<DefaultUpdateResponseType, Fault>> { }

}
