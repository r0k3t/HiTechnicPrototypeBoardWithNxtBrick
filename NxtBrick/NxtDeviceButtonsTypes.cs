//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtDeviceButtonsTypes.cs $ $Revision: 11 $
//-----------------------------------------------------------------------

using Microsoft.Dss.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Ccr.Core;
using W3C.Soap;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Buttons
{
    
    /// <summary>
    /// LegoNxtButtons Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the LegoNxtButtons service
        [DataMember, Description("Identifies the unique DSS Contract Identifier for the Lego NXT Buttons (v2).")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/buttons.user.html";

        /// <summary>
        /// The LEGO NXT Buttons Device Type
        /// </summary>
        [DataMember, Description("Identifies the device model.")]
        public const string DeviceModel = "Buttons";

        /// <summary>
        /// Default Polling Frequency (ms)
        /// </summary>
        [DataMember, Description("Specifies the default Polling Frequency (ms).")]
        public const int DefaultPollingFrequencyMs = 100;

    }

    /// <summary>
    /// Buttons State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT buttons\'s state.")]
    public class ButtonState 
    {
        /// <summary>
        /// Is the Sensor connected to a LEGO Brick?
        /// </summary>
        [DataMember(IsRequired = false), Description("Indicates a connection to the LEGO NXT Brick Service.")]
        [Browsable(false)]
        public bool Connected;

        /// <summary>
        /// The frequency in ms to poll for the buttons (0 = default)
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds (0 = default).")]
        public int PollingFrequencyMs;

        /// <summary>
        /// Identifies the most recent state of the LEGO NXT Buttons.
        /// </summary>
        [DataMember, Description("Identifies the most recent state of the LEGO NXT Buttons.")]
        [Browsable(false)]
        public NxtButtonReadings Buttons;

        /// <summary>
        /// Return the greater of two DateTime values
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private DateTime MaxDateTime(DateTime first, DateTime second)
        {
            if (first > second)
                return first;
            return second;
        }
    }

    /// <summary>
    /// Nxt Button Readings
    /// </summary>
    [DataContract, Description("Indicates the current state of the LEGO NXT built-in Buttons.")]
    public class NxtButtonReadings
    {
        /// <summary>
        /// Nxt Buttons
        /// </summary>
        public NxtButtonReadings() { }

        /// <summary>
        /// Nxt Buttons
        /// </summary>
        public NxtButtonReadings(bool pressedRight, bool pressedLeft, bool pressedEnter, bool pressedCancel, DateTime timeStamp)
        {
            this.PressedRight = pressedRight;
            this.PressedLeft = pressedLeft;
            this.PressedEnter = pressedEnter;
            this.PressedCancel = pressedCancel;
            this.TimeStamp = timeStamp;
        }

        /// <summary>
        /// Right Button is pressed
        /// </summary>
        [DataMember, Description("Indicates that the right button was pressed.")]
        [DataMemberConstructor(Order = 1)]
        public bool PressedRight;

        /// <summary>
        /// Left button is pressed.
        /// </summary>
        [DataMember, Description("Indicates the left button was pressed.")]
        [DataMemberConstructor(Order = 2)]
        public bool PressedLeft;

        /// <summary>
        /// Enter button is pressed
        /// </summary>
        [DataMember, Description("Indicates that the Enter button was pressed.")]
        [DataMemberConstructor(Order = 3)]
        public bool PressedEnter;


        /// <summary>
        /// Cancel Button is pressed
        /// </summary>
        [DataMember, Description("Indicates that the Cancel button was pressed.")]
        [DataMemberConstructor(Order = 4)]
        public bool PressedCancel;

        /// <summary>
        /// The time of the last buttons reading
        /// </summary>
        [DataMember, Description("Indicates the time of the last buttons reading.")]
        [Browsable(false)]
        public DateTime TimeStamp;

    }

    /// <summary>
    /// Indicates one or more LEGO NXT buttons have been pressed or released
    /// </summary>
    [Description("Indicates one or more LEGO NXT buttons have been pressed or released.")]
    public class ButtonsUpdate : Update<NxtButtonReadings, PortSet<DefaultUpdateResponseType, Fault>>
    {
        /// <summary>
        /// Indicates one or more LEGO NXT buttons have been pressed or released
        /// </summary>
        public ButtonsUpdate()
        {
        }
        /// <summary>
        /// Indicates one or more LEGO NXT buttons have been pressed or released
        /// </summary>
        public ButtonsUpdate(NxtButtonReadings body)
            :
                base(body)
        {
        }
        /// <summary>
        /// Indicates one or more LEGO NXT buttons have been pressed or released
        /// </summary>
        public ButtonsUpdate(NxtButtonReadings body, PortSet<DefaultUpdateResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }


    /// <summary>
    /// Subscribes to Button updates.
    /// </summary>
    [Description("Subscribes to Button updates.")]
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>> { }


    /// <summary>
    /// Button Operations Port
    /// </summary>
    [ServicePort]
    public class ButtonOperations : PortSet<
        DsspDefaultLookup,
        DsspDefaultDrop,
        Get,
        HttpGet,
        ButtonsUpdate,
        Subscribe>
    {
    }

    /// <summary>
    /// Get Operation
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Buttons.")]
    public class Get : Get<GetRequestType, PortSet<ButtonState, Fault>> { }

}
