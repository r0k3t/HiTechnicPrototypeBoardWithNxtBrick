//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtCommands.cs $ $Revision: 21 $
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Dss.Core.Attributes;
using System.Text;
using System.Diagnostics;
using Microsoft.Ccr.Core;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Dss.Core;
using System.Xml.Serialization;
using System.IO;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands
{

    /// <summary>
    /// NxtBrick Contract class
    /// </summary>
    [DataContract]
    public sealed class Contract
    {
        /// <summary>
        /// The Dss Service contract
        /// </summary>
        [DataMember, Description("The unique DSS Service Contract for the NXT internal Commands.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/commands.user.html";
    }

    #region Lego Command and Response Base Classes

    /// <summary>
    /// The base type for all LEGO Commands
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Any command sequence which can be sent to the LEGO NXT Brick.")]
    [XmlRootAttribute("LegoCommand", Namespace = Contract.Identifier)]
    public class LegoCommand : ICloneable, IDssSerializable
    {
        #region Static Members
        /// <summary>
        /// Indicates a LEGO NXT Direct Command
        /// </summary>
        [DataMember, Description("Indicates a LEGO NXT Direct Command")]
        public const byte NxtDirectCommand = 0x00;

        /// <summary>
        /// Indicates a LEGO NXT System Command
        /// </summary>
        [DataMember, Description("Indicates a LEGO NXT System Command")]
        public const byte NxtSystemCommand = 0x01;
        #endregion

        #region Private Members

        private bool? _requireResponse = null;
        /// <summary>
        /// The Expected size of a required LegoResponse
        /// </summary>
        protected internal int internalExpectedResponseSize = 0;
        private int _tryCount = 1;

        /// <summary>
        /// The command buffer 
        /// </summary>
        protected internal byte[] internalCommandData = null;


        /// <summary>
        /// Extend the size of the CommandData.
        /// <remarks>Requires the 2 header bytes to be declared in the constructor.</remarks>
        /// </summary>
        /// <param name="newSize"></param>
        protected void ExtendCommandData(int newSize)
        {
            
            if (this.CommandData != null && this.CommandData.Length == newSize)
                return;

            byte[] header = this.CommandData;
            if (header == null || header.Length < 2)
                throw new InvalidOperationException("The LegoCommand must declare the first two header bytes in the constructor.");
            this.CommandData = new byte[newSize];
            System.Buffer.BlockCopy(header, 0, this.CommandData, 0, header.Length);
        }

        #endregion

        /// <summary>
        /// The expected response buffer size
        /// </summary>
        [DataMember, Description("The expected size of the LEGO Response buffer.")]
        public virtual int ExpectedResponseSize
        {
            get { return (RequireResponse) ? internalExpectedResponseSize : 0; }
            set { internalExpectedResponseSize = value; }
        }

        /// <summary>
        /// The LEGO Command data buffer
        /// </summary>
        [DataMember, Description("The LEGO Command buffer")]
        public virtual byte[] CommandData
        {
            get
            {
                if (internalCommandData == null)
                    return null;

                // Set the RequireResponse flag in the data packet
                this.internalCommandData[0] = (byte)(this.internalCommandData[0] & 0x7F | ((RequireResponse) ? 0x00 : 0x80));

                return internalCommandData;
            }
            set
            {
                byte[] priorData = internalCommandData;

                if (value == null || value.Length < 2)
                    throw new ArgumentOutOfRangeException("LegoCommand.CommandData must be at least 2 bytes");

                internalCommandData = value;
                if (internalCommandData != null && internalCommandData.Length >= 1)
                    _requireResponse = (internalCommandData[0] < 0x80);
                if (priorData != null
                    && priorData.Length >= 2
                    && (internalCommandData[0] != priorData[0] || internalCommandData[1] != priorData[1]))
                {
                    internalCommandData[0] = priorData[0];
                    internalCommandData[1] = priorData[1];
                }
            }
        }

        /// <summary>
        /// Determines whether a response will be sent from the LEGO NXT Brick.
        /// When no respone is required, the TryCount will always be 1.
        /// </summary>
        [DataMember, Description("Determines whether a response will be sent from the LEGO NXT Brick. \n"
            + "When no respone is required, the TryCount will always be 1.")]
        public virtual bool RequireResponse
        {
            get
            {
                if (_requireResponse != null)
                    return (bool)_requireResponse;

                return (internalExpectedResponseSize > 0);
            }

            set
            {
                _requireResponse = value;
                if (!value)
                    _tryCount = 1;
            }
        }

        /// <summary>
        /// Specifies how many times the command will be attempted (1-20).
        /// When TryCount is more than 1, a response will always be required.
        /// </summary>
        [DataMember, Description("Specifies how many times the command will be attempted (1-20). \n"
            + "When TryCount is more than 1, a response will always be required.")]
        public virtual int TryCount
        {
            get
            {
                return _tryCount;
            }

            set
            {
                if (value > 1)
                    _requireResponse = true;
                _tryCount = Math.Max(1, Math.Min(20, value));
            }
        }

        /// <summary>
        /// The time a command was sent or response was received
        /// </summary>
        [DataMember, Description("Indicates the time a command was sent or response was received.")]
        public DateTime TimeStamp;


        #region Constructors

        /// <summary>
        /// The base type for all LEGO Commands
        /// </summary>
        public LegoCommand()
        {
            this.CommandData = new byte[2];
            this.ExpectedResponseSize = 0;
        }

        /// <summary>
        /// The base type for all LEGO Commands
        /// </summary>
        /// <param name="expectedResponseSize"></param>
        /// <param name="commandData"></param>
        public LegoCommand(int expectedResponseSize, params byte[] commandData)
        {
            this.CommandData = commandData;
            this.ExpectedResponseSize = expectedResponseSize;
        }

        #endregion

        #region Helper Properties

        /// <summary>
        /// The LEGO Command Code
        /// </summary>
        public virtual LegoCommandCode LegoCommandCode
        {
            get { return (LegoCommandCode)this.internalCommandData[1]; }
            set { this.internalCommandData[1] = (byte)value; }
        }

        /// <summary>
        /// The type of LEGO Command (System, Direct, Response)
        /// </summary>
        public virtual LegoCommandType CommandType
        {
            get
            {
                if (this.internalCommandData == null)
                    return LegoCommandType.Response;

                return (LegoCommandType)(this.internalCommandData[0] & 0x03);
            }
            set
            {
                if (this.internalCommandData != null)
                {
                    this.internalCommandData[0] = (byte)((int)value & 0x7F | ((RequireResponse) ? 0x00 : 0x80));
                }
            }
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Construct a LEGO NXT Response packet for the specified request
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns>A Generic Lego Response</returns>
        /// <remarks>Override this method to return a custom response</remarks>
        public virtual LegoResponse GetResponse(byte[] responseData)
        {
            int expectedResponseSize = (this.RequireResponse) ? this.ExpectedResponseSize : 3;
            LegoResponse response = new LegoResponse(expectedResponseSize, this.LegoCommandCode, responseData);
            return response;
        }
        #endregion

        #region IDssSerializable

        /// <summary>
        /// Copy To Lego Command
        /// </summary>
        public virtual void CopyTo(IDssSerializable target)
        {
            LegoCommand typedTarget = target as LegoCommand;

            if (typedTarget == null)
                throw new ArgumentException("CopyTo({0}) requires type {0}", this.GetType().FullName);
            typedTarget.ExpectedResponseSize = this.ExpectedResponseSize;

            // copy System.Byte[] CommandData
            if (this.CommandData != null)
            {
                typedTarget.CommandData = new System.Byte[this.CommandData.GetLength(0)];
                System.Buffer.BlockCopy(this.CommandData, 0, typedTarget.CommandData, 0, this.CommandData.GetLength(0));
            }
            typedTarget.RequireResponse = this.RequireResponse;
            typedTarget.TryCount = this.TryCount;
            typedTarget.TimeStamp = this.TimeStamp;
        }
        /// <summary>
        /// Clone Lego Command
        /// </summary>
        public virtual object Clone()
        {
            LegoCommand target = new LegoCommand();

            target.ExpectedResponseSize = this.ExpectedResponseSize;

            // copy System.Byte[] CommandData
            if (this.CommandData != null)
            {
                target.CommandData = new System.Byte[this.CommandData.GetLength(0)];
                System.Buffer.BlockCopy(this.CommandData, 0, target.CommandData, 0, this.CommandData.GetLength(0));
            }
            target.RequireResponse = this.RequireResponse;
            target.TryCount = this.TryCount;
            target.TimeStamp = this.TimeStamp;
            return target;

        }
        /// <summary>
        /// Serialize Serialize
        /// </summary>
        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write(ExpectedResponseSize);

            if (CommandData == null) writer.Write((byte)0);
            else
            {
                // null flag
                writer.Write((byte)1);

                writer.Write(this.CommandData.Length);
                writer.Write(this.CommandData);
            }

            writer.Write(RequireResponse);
            writer.Write(TryCount);

            Microsoft.Dss.Services.Serializer.BinarySerializationHelper.SerializeDateTime(TimeStamp, writer);

        }
        /// <summary>
        /// Deserialize Deserialize
        /// </summary>
        public virtual object Deserialize(BinaryReader reader)
        {
            ExpectedResponseSize = reader.ReadInt32();

            if (reader.ReadByte() == 0) { }
            else
            {
                int count3833989529 = reader.ReadInt32();
                CommandData = reader.ReadBytes(count3833989529);
            } //nullable

            RequireResponse = reader.ReadBoolean();
            TryCount = reader.ReadInt32();

            TimeStamp = Microsoft.Dss.Services.Serializer.BinarySerializationHelper.DeserializeDateTime(reader);

            return this;

        }

        #endregion
    }


    /// <summary>
    /// The base type for all LEGO Responses
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("The default LEGO Command Response.")]
    [XmlRootAttribute("LegoResponse", Namespace = Contract.Identifier)]
    public class LegoResponse : LegoCommand
    {
        #region Private Members
        private LegoErrorCode _legoErrorCode = LegoErrorCode.UnknownStatus;
        #endregion

        #region Static Members
        /// <summary>
        /// The Lego Command Type for a reply
        /// </summary>
        [DataMember, Description("The Lego Command Type for a reply (response) packet from the LEGO NXT Brick.")]
        public const byte NxtResponse = 0x02;
        #endregion

        /// <summary>
        /// The Expected Lego Response Code
        /// </summary>
        [DataMember, Description("Identifies the expected LEGO Command Code for this Response.")]
        public LegoCommandCode ExpectedCommandCode;

        /// <summary>
        /// Indicates the error code returned
        /// </summary>
        [DataMember, Description("Indicates the error code returned.")]
        public LegoErrorCode ErrorCode
        {
            get
            {
                int ec;
                LegoErrorCode errorCode = _legoErrorCode;
                if (this.CommandData != null && this.CommandData.Length >= 3)
                {
                    try
                    {
                        errorCode = (LegoErrorCode)this.CommandData[2];
                        if (Int32.TryParse(errorCode.ToString(), out ec))
                            errorCode = LegoErrorCode.UnknownStatus;
                    }
                    catch
                    {
                        errorCode = LegoErrorCode.UnknownStatus;
                    }
                }
                return errorCode;
            }
            set
            {
                _legoErrorCode = value;

                if (this.CommandData != null && this.CommandData.Length >= 3)
                    this.CommandData[2] = (byte)_legoErrorCode;
            }
        }

        #region Remove [DataMember] from base class fields

        /// <summary>
        /// This is the response.
        /// </summary>
        public override bool RequireResponse
        {
            get { return false; }
            set { base.RequireResponse = false; }
        }

        /// <summary>
        /// The LEGO Response data buffer
        /// </summary>
        [Description("The LEGO Response Data buffer.")]
        public override byte[] CommandData
        {
            get { return internalCommandData; }
            set { internalCommandData = value; }
        }

        /// <summary>
        /// The Expected Response Size
        /// </summary>
        public override int ExpectedResponseSize
        {
            get
            {
                return internalExpectedResponseSize;
            }
            set
            {
                internalExpectedResponseSize = value;
            }
        }
        #endregion

        #region Constructors

        /// <summary>
        /// A LEGO Command response
        /// </summary>
        public LegoResponse()
        {
            base.TimeStamp = DateTime.Now;
        }

        /// <summary>
        /// Constructor used by inheriting members to initialize the LegoResponse.
        /// </summary>
        /// <param name="expectedResponseSize"></param>
        /// <param name="commandCode"></param>
        public LegoResponse(int expectedResponseSize, LegoCommandCode commandCode)
        {
            this.ExpectedResponseSize = expectedResponseSize;
            this.ExpectedCommandCode = commandCode;
            this.CommandData = null;
            base.TimeStamp = DateTime.Now;
        }

        /// <summary>
        /// Constructor used by inheriting members to initialize the LegoResponse with Data
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponse(byte[] responseData)
        {
            this.CommandData = responseData;
            this.ExpectedResponseSize = responseData.Length;
            this.ExpectedCommandCode = (LegoCommandCode)responseData[1];
            if (this.CommandData == null)
            {
                this.CommandData = new byte[3];
                this.CommandType = LegoCommandType.Response;
                this.CommandData[1] = (byte)this.ExpectedCommandCode;
                this.CommandData[2] = 0;
            }
            base.TimeStamp = DateTime.Now;
        }


        /// <summary>
        /// Constructor used by inheriting members to initialize the LegoResponse with Data
        /// </summary>
        /// <param name="expectedResponseSize"></param>
        /// <param name="commandCode"></param>
        /// <param name="responseData"></param>
        public LegoResponse(int expectedResponseSize, LegoCommandCode commandCode, byte[] responseData)
        {
            this.ExpectedResponseSize = expectedResponseSize;
            this.ExpectedCommandCode = commandCode;
            this.CommandData = responseData;
            if (this.CommandData == null)
            {
                this.CommandData = new byte[3];
                this.CommandType = LegoCommandType.Response;
                this.CommandData[1] = (byte)this.ExpectedCommandCode;
                this.CommandData[2] = 0;
            }
            base.TimeStamp = DateTime.Now;
        }


        /// <summary>
        /// LEGO Response Constructor which matches LEGOCommand initialization parameters
        /// </summary>
        /// <param name="expectedResponseSize"></param>
        /// <param name="responseData"></param>
        public LegoResponse(int expectedResponseSize, params byte[] responseData)
            : base(expectedResponseSize, responseData)
        {
            if (this.CommandData == null)
            {
                this.CommandData = new byte[3];
                this.CommandType = LegoCommandType.Response;
                this.CommandData[1] = (byte)this.ExpectedCommandCode;
                this.CommandData[2] = 0;
            }
            base.TimeStamp = DateTime.Now;
        }

        #endregion

        #region Helper Properties

        /// <summary>
        /// Has Response
        /// </summary>
        [Browsable(false)]
        protected bool HasResponse
        {
            get
            {
                return (this.CommandData != null)
                    && (this.CommandData.Length == this.ExpectedResponseSize)
                    && this.CommandData.Length > 0;
            }
        }

        /// <summary>
        /// The standard acknowledgement from a LEGO Command
        /// </summary>
        [Browsable(false)]
        public virtual bool Success
        {
            get
            {
                if (!HasResponse
                    || this.CommandData.Length < 3)
                    return false;

                return (this.CommandType == LegoCommandType.Response
                    && this.CommandData[1] == (byte)this.ExpectedCommandCode
                    && this.CommandData[2] == 0);
            }
        }

        #endregion

        #region IDssSerializable
        /// <summary>
        /// Copy To Lego Response
        /// </summary>
        public override void CopyTo(IDssSerializable target)
        {
            LegoResponse typedTarget = target as LegoResponse;

            if (typedTarget == null)
                throw new ArgumentException("CopyTo({0}) requires type {0}", this.GetType().FullName);
            base.CopyTo(typedTarget);
            typedTarget.ExpectedCommandCode = this.ExpectedCommandCode;
        }
        /// <summary>
        /// Clone Lego Response
        /// </summary>
        public override object Clone()
        {
            LegoResponse target = new LegoResponse();

            base.CopyTo(target);
            target.ExpectedCommandCode = this.ExpectedCommandCode;
            return target;

        }
        /// <summary>
        /// Serialize Serialize
        /// </summary>
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write((System.Byte)ExpectedCommandCode);

        }
        /// <summary>
        /// Deserialize Deserialize
        /// </summary>
        public override object Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ExpectedCommandCode = (LegoCommandCode)reader.ReadByte();

            return this;

        }
        #endregion

        /// <summary>
        /// Upcast a LegoResponse to it's proper type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ok"></param>
        /// <returns></returns>
        public static T Upcast<T>(LegoResponse ok)
            where T : LegoResponse, new()
        {
            T queryResponse = ok as T;
            if (queryResponse == null)
            {
                queryResponse = new T();
                queryResponse.CommandData = ok.CommandData;
            }
            return queryResponse;
        }
    }

    #endregion

    #region Lego Command Collections

    /// <summary>
    /// A sequence of LEGO Commands
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("Identifies an ordered sequence of LEGO Commands.")]
    [XmlRootAttribute("NxtCommandSequence", Namespace = Contract.Identifier)]
    public class NxtCommandSequence : ICloneable, IDssSerializable
    {
        /// <summary>
        /// The Commands which make up this sequence.
        /// </summary>
        [DataMember, Description("The Commands which make up this sequence.")]
        public List<LegoCommand> Commands = new List<LegoCommand>();

        /// <summary>
        /// Continue processing commands after an error 
        /// </summary>
        [DataMember, Description("Continue processing commands after an error.")]
        public bool ContinueOnError;

        /// <summary>
        /// Polling Freqency (ms)
        /// </summary>
        [DataMember, Description("Indicates the Polling Frequency in milliseconds (-1 = disabled).")]
        public int PollingFrequencyMs;

        /// <summary>
        /// Average Polling Freqency Milliseconds (read-only)
        /// </summary>
        [DataMember, Description("Average Polling Freqency Milliseconds (read-only)")]
        [Browsable(false)]
        public double AveragePollingFrequencyMs;

        /// <summary>
        /// The original value of the PollingFrequencyMs prior to any adjustment.
        /// </summary>
        [DataMember(IsRequired = false), Description("The original value of the PollingFrequencyMs.")]
        [Browsable(false)]
        public int OriginalPollingFrequencyMs;

        #region Constructors

        /// <summary>
        /// A sequence of LEGO Commands
        /// </summary>
        public NxtCommandSequence() { }

        /// <summary>
        /// Generate a Sequence of Initialization Commands
        /// </summary>
        /// <param name="cmds"></param>
        public NxtCommandSequence(params LegoCommand[] cmds)
        {
            if (cmds != null && cmds.Length > 0)
                Commands = new List<LegoCommand>(cmds);
            else
                Commands = null;
        }

        /// <summary>
        /// Generate a Sequence of Polling Commands
        /// </summary>
        /// <param name="pollingFrequencyMs"></param>
        /// <param name="cmds"></param>
        public NxtCommandSequence(int pollingFrequencyMs, params LegoCommand[] cmds)
        {
            OriginalPollingFrequencyMs = PollingFrequencyMs = pollingFrequencyMs;
            if (cmds != null && cmds.Length > 0)
                Commands = new List<LegoCommand>(cmds);
            else
                Commands = null;
        }

        #endregion

        #region IDssSerializable
        /// <summary>
        /// Copy To Nxt Command Sequence
        /// </summary>
        public virtual void CopyTo(IDssSerializable target)
        {
            NxtCommandSequence typedTarget = target as NxtCommandSequence;

            if (typedTarget == null)
                throw new ArgumentException("CopyTo({0}) requires type {0}", this.GetType().FullName);

            // copy IEnumerable System.Collections.Generic.List<LegoCommand> Commands
            if (this.Commands != null)
            {
                typedTarget.Commands = new System.Collections.Generic.List<LegoCommand>();
                foreach (LegoCommand elem in Commands)
                {
                    typedTarget.Commands.Add((elem == null) ? null : (LegoCommand)((IDssSerializable)elem).Clone());
                }
            }
            typedTarget.ContinueOnError = this.ContinueOnError;
            typedTarget.AveragePollingFrequencyMs = this.AveragePollingFrequencyMs;
            typedTarget.PollingFrequencyMs = this.PollingFrequencyMs;
            typedTarget.OriginalPollingFrequencyMs = this.OriginalPollingFrequencyMs;
        }
        /// <summary>
        /// Clone Nxt Command Sequence
        /// </summary>
        public virtual object Clone()
        {
            NxtCommandSequence target = new NxtCommandSequence();


            // copy IEnumerable System.Collections.Generic.List<LegoCommand> Commands
            if (this.Commands != null)
            {
                target.Commands = new System.Collections.Generic.List<LegoCommand>();
                foreach (LegoCommand elem in Commands)
                {
                    target.Commands.Add((elem == null) ? null : (LegoCommand)((IDssSerializable)elem).Clone());
                }
            }
            target.ContinueOnError = this.ContinueOnError;
            target.AveragePollingFrequencyMs = this.AveragePollingFrequencyMs;
            target.PollingFrequencyMs = this.PollingFrequencyMs;
            target.OriginalPollingFrequencyMs = this.OriginalPollingFrequencyMs;
            return target;

        }
        /// <summary>
        /// Serialize Serialize
        /// </summary>
        public virtual void Serialize(BinaryWriter writer)
        {
            if (Commands == null) writer.Write((byte)0);
            else
            {
                // null flag
                writer.Write((byte)1);

                writer.Write(this.Commands.Count);
                for (int index1156403025 = 0; index1156403025 < this.Commands.Count; index1156403025++)
                {
                    if (Commands[index1156403025] == null) writer.Write((byte)0);
                    else
                    {
                        // null flag
                        writer.Write((byte)1);

                        ((IDssSerializable)Commands[index1156403025]).Serialize(writer);
                    }
                }
            }

            writer.Write(ContinueOnError);
            writer.Write(AveragePollingFrequencyMs);
            writer.Write(PollingFrequencyMs);
            writer.Write(OriginalPollingFrequencyMs);

        }
        /// <summary>
        /// Deserialize Deserialize
        /// </summary>
        public virtual object Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() == 0) { }
            else
            {
                int count3138564271 = reader.ReadInt32();
                Commands = new System.Collections.Generic.List<LegoCommand>();
                for (int index1156403025 = 0; index1156403025 < count3138564271; index1156403025++)
                {
                    Commands.Add(null);
                    if (reader.ReadByte() == 0) { }
                    else
                    {
                        Commands[index1156403025] = (LegoCommand)((IDssSerializable)new LegoCommand()).Deserialize(reader);
                    } //nullable
                }
            } //nullable

            ContinueOnError = reader.ReadBoolean();
            AveragePollingFrequencyMs = reader.ReadDouble();
            PollingFrequencyMs = reader.ReadInt32();
            OriginalPollingFrequencyMs = reader.ReadInt32();

            return this;

        }
        #endregion

    }

    #endregion

    #region LEGO NXT API Commands

    #region LegoLSWrite

    /// <summary>
    /// LegoLSWrite
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: LSWrite.")]
    [XmlRootAttribute("LegoLSWrite", Namespace = Contract.Identifier)]
    public class LegoLSWrite : LegoCommand
    {
        private const int _headerSize = 5;
        private int _rxDataLength = -1;
        private NxtSensorPort _port = (NxtSensorPort)(-1);
        private byte[] _txData = null;
        private int _txDataLength = 0;

        /// <summary>
        /// Make sure CommandData is valid.
        /// </summary>
        private void ValidateCommandData()
        {
            int bufferSize = _headerSize + _txDataLength;
            if (this.CommandData.Length != bufferSize)
            {
                byte[] priorCmd = this.CommandData;
                this.CommandData = new byte[bufferSize];

                if (priorCmd == null || priorCmd.Length < _headerSize)
                {
                    LegoLSWrite temp = new LegoLSWrite();
                    priorCmd = temp.CommandData;
                }

                Buffer.BlockCopy(priorCmd, 0, this.CommandData, 0, priorCmd.Length);
            }
        }

        /// <summary>
        /// LegoLSWrite
        /// </summary>
        public LegoLSWrite()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.LSWrite, 0x00, 0x00, 0x00)
        {
            RequireResponse = true;
        }

        /// <summary>
        /// LegoLSWrite
        /// </summary>
        /// <param name="cmd"></param>
        public LegoLSWrite(LegoCommand cmd)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.LSWrite, 0x00, 0x00, 0x00)
        {
            this.CommandData = cmd.CommandData;
        }

        /// <summary>
        /// LegoLSWrite
        /// </summary>
        public LegoLSWrite(byte[] commandData)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.LSWrite, 0x00, 0x00, 0x00)
        {
            this.CommandData = commandData;
        }

        /// <summary>
        /// LegoLSWrite
        /// </summary>
        /// <param name="port"></param>
        /// <param name="txData"></param>
        /// <param name="rxDataLength"></param>
        public LegoLSWrite(NxtSensorPort port, byte[] txData, byte rxDataLength)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.LSWrite, 0x00, 0x00, 0x00)
        {
            this.Port = port;
            this.TXData = txData;
            this.ExpectedI2CResponseSize = rxDataLength;
        }

        /// <summary>
        /// The Low Speed Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            if (responseData == null || responseData.Length == 3)
                return base.GetResponse(responseData);
            return new LegoResponseLSRead(responseData);
        }


        /// <summary>
        /// The Sensor Port
        /// </summary>
        [DataMember, Description("The Port")]
        public NxtSensorPort Port
        {
            get
            {
                if ((int)_port == -1)
                    if (this.CommandData != null && this.CommandData.Length >= _headerSize)
                        _port = NxtCommon.GetNxtSensorPort(this.CommandData[2]);
                    else
                        return NxtSensorPort.NotConnected;

                return _port;
            }
            set
            {
                ValidateCommandData();
                _port = value;
                this.CommandData[2] = NxtCommon.PortNumber(_port);
            }
        }

        /// <summary>
        /// The transmitted data length.
        /// </summary>
        private int TXDataLength
        {
            get { return (this.CommandData == null) ? 0 : this.CommandData.Length - _headerSize; }
            set
            {
                if (value < 0 || value > 16)
                    throw new ArgumentOutOfRangeException("LSWrite(TxData) must be between 0 and 16");

                _txDataLength = value;
                ValidateCommandData();
                this.CommandData[3] = (byte)value;
            }
        }

        /// <summary>
        /// Expected Response Size
        /// Replaces RXDataLength from the LEGO Documentation
        /// </summary>
        [Description("The received data length expected from the I2C device.")]
        public int ExpectedI2CResponseSize
        {
            get
            {
                if (this.CommandData != null && this.CommandData.Length >= _headerSize)
                    _rxDataLength = this.CommandData[4];
                return Math.Max(0, _rxDataLength);
            }
            set
            {
                ValidateCommandData();
                _rxDataLength = value;
                this.CommandData[4] = (byte)_rxDataLength;
            }
        }

        /// <summary>
        /// The transmitted data
        /// </summary>
        [Description("The transmitted data.")]
        public byte[] TXData
        {
            get
            {
                if (_txData == null && TXDataLength > 0)
                {
                    _txData = new byte[TXDataLength];
                    System.Buffer.BlockCopy(this.CommandData, 5, _txData, 0, TXDataLength);
                }
                return _txData;
            }
            set
            {
                this.TXDataLength = (value == null) ? 0 : value.Length;
                _txData = value;
                if (_txData != null)
                    _txData.CopyTo(this.CommandData, 5);
            }
        }

        #region Hide underlying data members

        /// <summary>
        /// Command Data
        /// </summary>
        [Description("The LEGO Command buffer")]
        public override byte[] CommandData
        {
            get { return base.CommandData; }
            set
            {
                base.CommandData = value;

                // reset the internal variables
                _txData = null;
                _rxDataLength = -1;
                _port = (NxtSensorPort)(-1);
                _txDataLength = (value == null) ? 0 : Math.Max(0, value.Length - _headerSize);
            }
        }

        #endregion
    }


    #endregion

    #region LegoLSRead
    /// <summary>
    /// LEGO Command: LSRead.  I2C Low Speed Read
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: LSRead.  I2C Low Speed Read")]
    [XmlRootAttribute("LegoLSRead", Namespace = Contract.Identifier)]
    public class LegoLSRead : LegoCommand
    {
        /// <summary>
        /// LEGO NXT Low Speed (I2C) Read
        /// </summary>
        public LegoLSRead()
            : base(20, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.LSRead, 0x00)
        {
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO NXT Low Speed (I2C) Read
        /// </summary>
        /// <param name="port"></param>
        public LegoLSRead(NxtSensorPort port)
            : base(20, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.LSRead, 0x00)
        {
            base.RequireResponse = true;
            Port = port;
        }

        /// <summary>
        /// LEGO NXT Low Speed (I2C) Read
        /// </summary>
        public LegoLSRead(byte[] commandData)
            : base(20, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.LSRead, 0x00)
        {
            base.RequireResponse = true;
            this.CommandData = commandData;
        }

        /// <summary>
        /// The matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new LegoResponseLSRead(responseData);
        }

        /// <summary>
        /// LSRead has a fixed response size which can not be changed.
        /// Excess receive bytes are padded with zeroes.
        /// </summary>
        public override int ExpectedResponseSize
        {
            get
            {
                return 20;
            }
            set
            {
                base.ExpectedResponseSize = 20;
            }
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// 0,1,2,3
        /// </summary>
        [Description("The input port (0, 1, 2, or 3).")]
        public NxtSensorPort Port
        {
            get { return NxtCommon.GetNxtSensorPort(this.CommandData[2]); }
            set { this.CommandData[2] = NxtCommon.PortNumber(value); }
        }

        #region Hide underlying data members

        /// <summary>
        /// Command Data
        /// </summary>
        public override byte[] CommandData
        {
            get { return base.CommandData; }
            set { base.CommandData = value; }
        }

        #endregion
    }

    /// <summary>
    /// LEGO Response: LSRead.  I2C Low Speed Read.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: LSRead.  I2C Low Speed Read.")]
    [XmlRootAttribute("LegoResponseLSRead", Namespace = Contract.Identifier)]
    public class LegoResponseLSRead : LegoResponse
    {
        /// <summary>
        /// LEGO NXT Response: Low Speed (I2C) Read
        /// </summary>
        public LegoResponseLSRead()
            : base(20, LegoCommandCode.LSRead)
        {
        }

        /// <summary>
        /// LEGO NXT Response: Low Speed (I2C) Read
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseLSRead(byte[] responseData)
            : base(20, LegoCommandCode.LSRead, responseData) { }

        #region Hide base type DataMembers

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion

        /// <summary>
        /// The number of bytes read
        /// </summary>
        [DataMember, Description("The number of bytes read.")]
        public int BytesRead
        {
            get
            {
                if (CommandData != null && CommandData.Length >= 4)
                    return CommandData[3];
                return -1;
            }
            set
            {
                CommandData[3] = (byte)value;
            }
        }

        /// <summary>
        /// The received data
        /// </summary>
        [DataMember, Description("The received data.")]
        public byte[] RXData
        {
            get
            {
                if (BytesRead < 1 || (CommandData.Length < (4 + BytesRead)))
                    return new byte[0];

                byte[] rxdata = new byte[BytesRead];
                Buffer.BlockCopy(this.CommandData, 4, rxdata, 0, BytesRead);
                return rxdata;
            }
            set
            {
                BytesRead = Math.Min(16, value.Length);
                Buffer.BlockCopy(value, 0, this.CommandData, 4, BytesRead);
            }
        }
    }


    #endregion

    #region LegoSetInputMode
    /// <summary>
    /// LEGO Command: SetInputMode.
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: SetInputMode.")]
    [XmlRootAttribute("LegoSetInputMode", Namespace = Contract.Identifier)]
    public class LegoSetInputMode : LegoCommand
    {
        /// <summary>
        /// Range 0-3
        /// </summary>
        private NxtSensorPort _inputPort;
        private LegoSensorType _sensorType;
        private LegoSensorMode _sensorMode;

        /// <summary>
        /// LEGO NXT Command: Set Input Mode
        /// </summary>
        public LegoSetInputMode()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.SetInputMode, 0x00, 0x00, 0x00)
        {
        }

        /// <summary>
        /// LEGO NXT Command: Set Input Mode
        /// </summary>
        /// <param name="sensorPort"></param>
        /// <param name="sensorType"></param>
        /// <param name="sensorMode"></param>
        public LegoSetInputMode(NxtSensorPort sensorPort, LegoSensorType sensorType, LegoSensorMode sensorMode)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.SetInputMode, 0x00, 0x00, 0x00)
        {
            InputPort = sensorPort;
            SensorType = sensorType;
            SensorMode = sensorMode;
        }

        /// <summary>
        /// The LEGO NXT Sensor Port
        /// </summary>
        [Description("The input port on the NXT brick.")]
        public NxtSensorPort InputPort
        {
            get { return _inputPort; }
            set
            {
                _inputPort = value;
                this.CommandData[2] = NxtCommon.PortNumber(_inputPort);
            }
        }

        /// <summary>
        /// Sensor Type
        /// </summary>
        public LegoSensorType SensorType
        {
            get { return _sensorType; }
            set
            {
                _sensorType = value;
                this.CommandData[3] = (byte)_sensorType;
            }
        }

        /// <summary>
        /// Sensor Type
        /// </summary>
        [Description("The translation mode of the LEGO NXT sensor.")]
        public LegoSensorMode SensorMode
        {
            get { return _sensorMode; }
            set
            {
                _sensorMode = value;
                this.CommandData[4] = (byte)_sensorMode;
            }
        }
    }

    #endregion


    /// <summary>
    /// LEGO NXT Command: Get Battery Level
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: GetBatteryLevel.")]
    [XmlRootAttribute("LegoGetBatteryLevel", Namespace = Contract.Identifier)]
    public class LegoGetBatteryLevel : LegoCommand
    {
        /// <summary>
        /// LEGO NXT Command: Get Battery Level
        /// </summary>
        public LegoGetBatteryLevel()
            : base(5, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.GetBatteryLevel)
        { }

        /// <summary>
        /// The matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new LegoResponseGetBatteryLevel(responseData);
        }
    }


    /// <summary>
    /// LEGO NXT Response: Get Battery Level
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: GetBatteryLevel.")]
    [XmlRootAttribute("LegoResponseGetBatteryLevel", Namespace = Contract.Identifier)]
    public class LegoResponseGetBatteryLevel : LegoResponse
    {
        /// <summary>
        /// LEGO NXT Response: Get Battery Level
        /// </summary>
        public LegoResponseGetBatteryLevel()
            : base(5, LegoCommandCode.GetBatteryLevel)
        {
        }

        /// <summary>
        /// LEGO NXT Response: Get Battery Level
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseGetBatteryLevel(byte[] responseData)
            : base(5, LegoCommandCode.GetBatteryLevel, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// Voltage in Volts
        /// </summary>
        [DataMember, Description("Indicates the voltage (in Volts).")]
        public double Voltage
        {
            get { return (double)Millivolts / 1000.0; }
            set { Millivolts = (int)(value * 1000.0); }
        }

        /// <summary>
        /// Millivolts
        /// </summary>
        private int Millivolts
        {
            get { return (int)BitConverter.ToUInt16(this.CommandData, 3); }
            set { NxtCommon.SetUShort(this.CommandData, 3, value); }
        }

    }

    /// <summary>
    /// LEGO NXT Command: Get Device Info
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: GetDeviceInfo.")]
    [XmlRootAttribute("LegoGetDeviceInfo", Namespace = Contract.Identifier)]
    public class LegoGetDeviceInfo : LegoCommand
    {
        /// <summary>
        /// LEGO NXT Command: Get Device Info
        /// </summary>
        public LegoGetDeviceInfo()
            : base(33, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.GetDeviceInfo)
        {
        }

        /// <summary>
        /// The matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new LegoResponseGetDeviceInfo(responseData);
        }
    }

    /// <summary>
    /// LEGO NXT Response: Get Device Info
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: GetDeviceInfo.")]
    [XmlRootAttribute("LegoResponseGetDeviceInfo", Namespace = Contract.Identifier)]
    public class LegoResponseGetDeviceInfo : LegoResponse
    {
        /// <summary>
        /// LEGO NXT Response: Get Device Info
        /// </summary>
        public LegoResponseGetDeviceInfo()
            : base(33, LegoCommandCode.GetDeviceInfo) { }

        /// <summary>
        /// LEGO NXT Response: Get Device Info
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseGetDeviceInfo(byte[] responseData)
            : base(33, LegoCommandCode.GetDeviceInfo, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The descriptive name of the NXT brick
        /// </summary>
        [DataMember, Description("The descriptive name of the NXT brick.")]
        public string BrickName
        {
            get
            {
                if (this.CommandData == null || this.CommandData.Length < 33)
                    return string.Empty;

                return NxtCommon.DataToString(this.CommandData, 3, 15);
            }
            set
            {

                string newValue = value ?? string.Empty;
                if (newValue.Length > 14)
                    newValue = newValue.Substring(0, 14);

                if (this.CommandData == null || this.CommandData.Length < 33)
                {
                    byte[] oldData = this.CommandData;
                    this.CommandData = new byte[33];
                    if (oldData != null) oldData.CopyTo(this.CommandData, 0);
                }
                NxtCommon.StringToData(newValue, newValue.Length + 1).CopyTo(this.CommandData, 3);
            }
        }

        /// <summary>
        /// The Bluetooth address
        /// </summary>
        [DataMember, Description("The Bluetooth address.")]
        public string BluetoothAddress
        {
            get
            {
                if (this.CommandData == null || this.CommandData.Length < 33)
                    return string.Empty;

                StringBuilder sb = new StringBuilder();
                for (int ix = 18; ix < 25; ix++)
                    sb.Append(this.CommandData[ix].ToString() + ".");
                sb.Length--;
                return sb.ToString();

            }
            set
            {
                string[] values = value.Split('.');
                if (values.Length != 7)
                    throw new InvalidOperationException("Bluetooth address is not valid.");

                int ix = 18;
                foreach (string number in values)
                {
                    byte v;
                    if (byte.TryParse(number, out v))
                    {
                        this.CommandData[ix] = v;
                    }
                    ix++;
                }
            }
        }

        /// <summary>
        /// The Bluetooth signal strength
        /// </summary>
        [DataMember, Description("The Bluetooth signal strength.")]
        public long BluetoothSignalStrength
        {
            get
            {
                if (this.CommandData.Length >= 33)
                    return (long)BitConverter.ToUInt32(this.CommandData, 25);
                return -1;
            }
            set
            {
                if (this.CommandData.Length >= 33)
                    NxtCommon.SetUInt32(this.CommandData, 25, value);
            }
        }

        /// <summary>
        /// The amount of memory available
        /// </summary>
        [DataMember, Description("The amount of memory available.")]
        public long FreeMemory
        {
            get
            {
                if (this.CommandData.Length >= 33)
                    return (long)BitConverter.ToUInt32(this.CommandData, 29);
                return -1;
            }
            set
            {
                if (this.CommandData.Length >= 33)
                    NxtCommon.SetUInt32(this.CommandData, 29, value);
            }
        }

    }


    /// <summary>
    /// LEGO NXT Command: Get Firmware Version
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: GetFirmwareVersion.")]
    [XmlRootAttribute("LegoGetFirmwareVersion", Namespace = Contract.Identifier)]
    public class LegoGetFirmwareVersion : LegoCommand
    {
        /// <summary>
        /// LEGO NXT Command: Get Firmware Version
        /// </summary>
        public LegoGetFirmwareVersion()
            : base(7, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.GetFirmwareVersion)
        {
        }

        /// <summary>
        /// The matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new LegoResponseGetFirmwareVersion(responseData);
        }
    }

    /// <summary>
    /// LEGO NXT Response: Get Firmware Version
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: GetFirmwareVersion.")]
    [XmlRootAttribute("LegoResponseGetFirmwareVersion", Namespace = Contract.Identifier)]
    public class LegoResponseGetFirmwareVersion : LegoResponse
    {
        /// <summary>
        /// LEGO NXT Response: Get Firmware Version
        /// </summary>
        public LegoResponseGetFirmwareVersion()
            : base(7, LegoCommandCode.GetFirmwareVersion)
        {
        }

        /// <summary>
        /// LEGO NXT Response: Get Firmware Version
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseGetFirmwareVersion(byte[] responseData)
            : base(7, LegoCommandCode.GetFirmwareVersion, responseData)
        {
        }

        #region Hide base type DataMembers

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The minor protocol version number
        /// </summary>
        [DataMember, Description("The minor protocol version number.")]
        public int MinorProtocolVersion
        {
            get
            {
                if (CommandData.Length >= 4)
                    return CommandData[3];
                return -1;
            }
            set
            {
                if (CommandData.Length >= 4)
                    CommandData[3] = (byte)value;
            }
        }

        /// <summary>
        /// The major protocol version number
        /// </summary>
        [DataMember, Description("The major protocol version number.")]
        public int MajorProtocolVersion
        {
            get
            {
                if (CommandData.Length >= 5)
                    return CommandData[4];
                return -1;
            }
            set
            {
                if (CommandData.Length >= 5)
                    CommandData[4] = (byte)value;
            }
        }

        /// <summary>
        /// The minor firmware version number
        /// </summary>
        [DataMember, Description("The minor firmware version number.")]
        public int MinorFirmwareVersion
        {
            get
            {
                if (CommandData.Length >= 6)
                    return CommandData[5];
                return -1;
            }
            set
            {
                if (CommandData.Length >= 6)
                    CommandData[5] = (byte)value;
            }
        }

        /// <summary>
        /// The major firmware version number
        /// </summary>
        [DataMember, Description("The major firmware version number.")]
        public int MajorFirmwareVersion
        {
            get
            {
                if (CommandData.Length >= 7)
                    return CommandData[6];
                return -1;
            }
            set
            {
                if (CommandData.Length >= 7)
                    CommandData[6] = (byte)value;
            }
        }
    }


    #region LegoSetOutputState
    /// <summary>
    /// LEGO NXT Command: Set Output State
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: SetOutputState.")]
    [XmlRootAttribute("LegoSetOutputState", Namespace = Contract.Identifier)]
    public class LegoSetOutputState : LegoCommand
    {
        #region Constructors

        /// <summary>
        /// LEGO NXT Command: Set Output State
        /// </summary>
        public LegoSetOutputState()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.SetOutputState, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00) { }

        /// <summary>
        /// LEGO NXT Command: Set Output State
        /// </summary>
        /// <param name="motorPort"></param>
        /// <param name="powerSetPoint"></param>
        /// <param name="mode"></param>
        /// <param name="regulationMode"></param>
        /// <param name="turnRatio"></param>
        /// <param name="runState"></param>
        /// <param name="rotationLimit"></param>
        public LegoSetOutputState(NxtMotorPort motorPort,
                                  int powerSetPoint,
                                  LegoOutputMode mode,
                                  LegoRegulationMode regulationMode,
                                  int turnRatio,
                                  RunState runState,
                                  long rotationLimit)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.SetOutputState, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00)
        {
            this.MotorPort = motorPort;
            this.PowerSetPoint = powerSetPoint;
            this.Mode = mode;
            this.RegulationMode = regulationMode;
            this.TurnRatio = turnRatio;
            this.RunState = runState;
            this.EncoderLimit = rotationLimit;
        }

        #endregion

        /// <summary>
        /// The Motor Port
        /// </summary>
        [DataMember, Description("The output port on the NXT brick (or NxtMotorPort.AnyMotorPort for all three.)")]
        [DataMemberConstructor(Order = 1)]
        public NxtMotorPort MotorPort
        {
            get
            {

                if (CommandData.Length == 12)
                    return NxtCommon.GetNxtMotorPort(CommandData[2]);
                return NxtMotorPort.NotConnected;
            }
            set
            {
                this.CommandData[2] = NxtCommon.PortNumber(value);
            }
        }

        /// <summary>
        /// Power Setpoint (range -100 to +100)
        /// </summary>
        [DataMember, Description("The motor power setting (range -100 to +100).")]
        [DataMemberConstructor(Order = 2)]
        public int PowerSetPoint
        {
            get
            {
                if (CommandData.Length == 12)
                    return (int)NxtCommon.GetSByte(this.CommandData, 3);
                return -1;
            }
            set
            {
                NxtCommon.SetSByte(this.CommandData, 3, value);
            }
        }

        /// <summary>
        /// A limit on the number of motor rotations (360 per rotation).
        /// </summary>
        [DataMember, Description("A limit on the number of motor rotations (360 per rotation).")]
        [DataMemberConstructor(Order = 3)]
        public long EncoderLimit
        {
            get
            {
                if (CommandData.Length == 12)
                    return (long)(long)BitConverter.ToUInt32(this.CommandData, 8);
                return -1;
            }
            set
            {
                if (this.CommandData == null) this.CommandData = new byte[12];
                NxtCommon.SetUInt32(this.CommandData, 8, value);
            }
        }

        /// <summary>
        /// Mode
        /// </summary>
        [DataMember, Description("The NXT output mode.")]
        [DataMemberConstructor(Order = 4)]
        public LegoOutputMode Mode
        {
            get
            {
                if (CommandData.Length == 12)
                    return (LegoOutputMode)CommandData[4];
                return LegoOutputMode.Brake;
            }
            set
            {
                this.CommandData[4] = (byte)value;
            }
        }

        /// <summary>
        /// RunState
        /// </summary>
        [DataMember, Description("The Motor Run State")]
        [DataMemberConstructor(Order = 5)]
        public RunState RunState
        {
            get
            {
                if (CommandData.Length == 12)
                    return (RunState)CommandData[7];
                return RunState.Idle;
            }
            set
            {
                this.CommandData[7] = (byte)value;
            }
        }


        /// <summary>
        /// Lego Regulation Mode
        /// </summary>
        [DataMember, Description("The NXT regulation mode.")]
        [DataMemberConstructor(Order = 6)]
        public LegoRegulationMode RegulationMode
        {
            get
            {
                if (CommandData.Length == 12)
                    return (LegoRegulationMode)CommandData[5];
                return LegoRegulationMode.Idle;
            }
            set
            {
                this.CommandData[5] = (byte)value;
            }
        }

        /// <summary>
        /// The Motor Turn Ratio
        /// <remarks>(-100 - 100)</remarks>
        /// </summary>
        [DataMember, Description("The Motor Turn Ratio")]
        [DataMemberConstructor(Order = 7)]
        public int TurnRatio
        {
            get
            {
                if (CommandData.Length == 12)
                    return (int)NxtCommon.GetSByte(this.CommandData, 6);
                return -1;
            }
            set
            {
                NxtCommon.SetSByte(this.CommandData, 6, value);
            }
        }

        /// <summary>
        /// String representation of SetOutputState with parameters.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{7} SetOutputState(port={0},power={1},rotations={2},mode={3},runstate={4},regulation={5},turnratio={6})",
                this.MotorPort,
                this.PowerSetPoint,
                this.EncoderLimit,
                this.Mode,
                this.RunState,
                this.RegulationMode,
                this.TurnRatio,
                this.TimeStamp.ToString("HH:mm:ss.fffffff"));
        }
    }

    #endregion


    #region LegoGetButtonState
    /// <summary>
    /// LEGO NXT Command: Get Button State
    /// NOTE: 0x01, 0x94, 0x01, 0x00, 0x04, 0x00, 0x20, 0x00, 0x04, 0x00
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: GetButtonState.")]
    [XmlRootAttribute("LegoGetButtonState", Namespace = Contract.Identifier)]
    public class LegoGetButtonState : LegoCommand
    {
        /// <summary>
        /// LEGO NXT Command: Get Button State
        /// </summary>
        public LegoGetButtonState()
            : base(13, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.ReadIOMap, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00)
        {
            RequireResponse = true;

            // Set the Module
            NxtCommon.SetUInt32(this.CommandData, 2, 0x00040001);

            // Set the offset
            NxtCommon.SetUShort(this.CommandData, 6, 0x0020);

            // Set the number of bytes to read
            NxtCommon.SetUShort(this.CommandData, 8, 0x0004);
        }

        /// <summary>
        /// The matching LEGO NXT Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new LegoResponseGetButtonState(responseData);
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

    }

    /// <summary>
    /// LEGO NXT Response: Get Button State
    /// NOTE: Because this return package does not return the index of the button queried,
    /// something special will have to be done.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: GetButtonState.")]
    [XmlRootAttribute("LegoResponseGetButtonState", Namespace = Contract.Identifier)]
    public class LegoResponseGetButtonState : LegoResponse
    {
        /// <summary>
        /// LEGO NXT Response: Get Button State
        /// </summary>
        public LegoResponseGetButtonState()
            : base(13, LegoCommandCode.ReadIOMap) { }

        /// <summary>
        /// LEGO NXT Response: Get Button State
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseGetButtonState(byte[] responseData)
            : base(13, LegoCommandCode.ReadIOMap, responseData)
        {
        }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion

        /// <summary>
        /// Determine if the LEGO response packet was a request for Button state.
        /// </summary>
        public override bool Success
        {
            get
            {
                if (!base.Success)
                    return false;

                if (CommandData[2] != 0x00 // success
                    || CommandData[3] != 0x01 // offset byte 1
                    || CommandData[4] != 0x00 // offset byte 2
                    || CommandData[5] != 0x04 // offset byte 3
                    || CommandData[6] != 0x00 // offset byte 4
                    || CommandData[7] != 0x04 // data length byte 1
                    || CommandData[8] != 0x00 // data length byte 2
                   )
                    return false;

                return true;
            }
        }
        /// <summary>
        /// Determine if the LEGO response packet was a request for Button state.
        /// </summary>
        /// <param name="legoResponse"></param>
        /// <returns></returns>
        public static bool IsValidButtonStateResponse(LegoResponse legoResponse)
        {
            if ((legoResponse == null)
                || (legoResponse.CommandData == null)
                || legoResponse.CommandData.Length != 13
                || legoResponse.CommandType != LegoCommandType.Response // Lego Return Code
                || legoResponse.LegoCommandCode != LegoCommandCode.ReadIOMap
                || legoResponse.ErrorCode != LegoErrorCode.Success // success
                || legoResponse.CommandData[3] != 0x01 // offset byte 1
                || legoResponse.CommandData[4] != 0x00 // offset byte 2
                || legoResponse.CommandData[5] != 0x04 // offset byte 3
                || legoResponse.CommandData[6] != 0x00 // offset byte 4
                || legoResponse.CommandData[7] != 0x04 // data length byte 1
                || legoResponse.CommandData[8] != 0x00 // data length byte 2
                )
                return false;

            return true;
        }

        /// <summary>
        /// LEGO NXT Response: Get Button State
        /// </summary>
        /// <param name="right"></param>
        /// <param name="left"></param>
        /// <param name="enter"></param>
        /// <param name="cancel"></param>
        public LegoResponseGetButtonState(bool right, bool left, bool enter, bool cancel)
            : base(13, LegoCommandCode.ReadIOMap)
        {
            PressedRight = right;
            PressedLeft = left;
            PressedEnter = enter;
            PressedCancel = cancel;
        }


        /// <summary>
        /// The number of bytes read from IO Mapped data
        /// </summary>
        private int BytesRead
        {
            get { return NxtCommon.GetUShort(this.CommandData, 7); }
        }

        /// <summary>
        /// The IO Mapped Data which is returned
        /// </summary>
        private byte[] MappedData
        {
            get
            {
                int bytesRead = this.BytesRead;
                if (CommandData == null || bytesRead == 0 || CommandData.Length < (9 + bytesRead))
                    return null;

                byte[] mappedData = new byte[bytesRead];
                for (int ix = 0; ix < bytesRead; ix++)
                    mappedData[ix] = CommandData[ix + 9];

                return mappedData;
            }
        }


        /// <summary>
        /// Right Button is pressed
        /// </summary>
        [DataMember, Description("Indicates that the right button was pressed.")]
        [DataMemberConstructor(Order = 1)]
        public bool PressedRight
        {
            get
            {
                if (CommandData == null || CommandData.Length < 13)
                    return false;

                return (CommandData[10] & 0x80) == 0x80;
            }
            set
            {
                CommandData[10] = (byte)(value ? 0x80 : 0x00);
            }
        }

        /// <summary>
        /// Left button is pressed.
        /// </summary>
        [DataMember, Description("Indicates the left button was pressed.")]
        [DataMemberConstructor(Order = 2)]
        public bool PressedLeft
        {
            get
            {
                if (CommandData == null || CommandData.Length < 13)
                    return false;

                return (CommandData[11] & 0x80) == 0x80;
            }
            set
            {
                CommandData[11] = (byte)(value ? 0x80 : 0x00);
            }
        }

        /// <summary>
        /// Enter button is pressed
        /// </summary>
        [DataMember, Description("Indicates that the Enter button was pressed.")]
        [DataMemberConstructor(Order = 3)]
        public bool PressedEnter
        {
            get
            {
                if (CommandData == null || CommandData.Length < 13)
                    return false;

                return (CommandData[12] & 0x80) == 0x80;
            }
            set
            {
                CommandData[12] = (byte)(value ? 0x80 : 0x00);
            }
        }


        /// <summary>
        /// Cancel Button is pressed
        /// </summary>
        [DataMember, Description("Indicates that the Cancel button was pressed.")]
        [DataMemberConstructor(Order = 4)]
        public bool PressedCancel
        {
            get
            {
                if (CommandData == null || CommandData.Length < 13)
                    return false;

                return (CommandData[9] & 0x80) == 0x80;
            }
            set
            {
                CommandData[9] = (byte)(value ? 0x80 : 0x00);
            }
        }

    }

    #endregion


    #region LegoGetInputValues

    /// <summary>
    /// LEGO NXT Command: GetInputValues
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: GetInputValues.")]
    [XmlRootAttribute("LegoGetInputValues", Namespace = Contract.Identifier)]
    public class LegoGetInputValues : LegoCommand
    {
        /// <summary>
        /// Range 0-3
        /// </summary>
        private NxtSensorPort _inputPort;

        /// <summary>
        /// LEGO NXT Command: GetInputValues
        /// </summary>
        public LegoGetInputValues()
            : base(16, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.GetInputValues, 0x00)
        {
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO NXT Command: GetInputValues
        /// </summary>
        /// <param name="inputPort"></param>
        public LegoGetInputValues(NxtSensorPort inputPort)
            : base(16, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.GetInputValues, 0x00)
        {
            base.RequireResponse = true;
            InputPort = inputPort;
        }

        /// <summary>
        /// LEGO NXT Command: GetInputValues
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new LegoResponseGetInputValues(responseData);
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// 0,1,2,3
        /// </summary>
        [DataMember, Description("The input port on the NXT brick.")]
        public NxtSensorPort InputPort
        {
            get { return _inputPort; }
            set
            {
                _inputPort = value;
                this.CommandData[2] = NxtCommon.PortNumber(_inputPort);
            }
        }
    }

    /// <summary>
    /// LEGO NXT Command Response: GetInputValues
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: GetInputValues.")]
    [XmlRootAttribute("LegoResponseGetInputValues", Namespace = Contract.Identifier)]
    public class LegoResponseGetInputValues : LegoResponse
    {
        /// <summary>
        /// LEGO NXT Command Response: GetInputValues
        /// </summary>
        public LegoResponseGetInputValues()
            : base(16, LegoCommandCode.GetInputValues)
        {
        }

        /// <summary>
        /// LEGO NXT Command Response: GetInputValues
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseGetInputValues(byte[] responseData)
            : base(16, LegoCommandCode.GetInputValues, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The input port on the NXT brick.
        /// </summary>
        [DataMember, Description("The input port on the NXT brick")]
        public NxtSensorPort InputPort
        {
            get
            {
                if (CommandData.Length >= 4)
                    return NxtCommon.GetNxtSensorPort(CommandData[3]);

                return NxtSensorPort.NotConnected;
            }
            set
            {
                if (CommandData.Length >= 4)
                    CommandData[3] = NxtCommon.PortNumber(value);
            }
        }

        /// <summary>
        /// Success
        /// </summary>
        public override bool Success
        {
            get
            {
                if (!base.Success)
                    return false;

                // Check the Valid field
                if (CommandData.Length >= 5)
                    return (CommandData[4] == 0) ? false : true;

                return false;
            }
        }

        /// <summary>
        /// Is the specified sensor Calibrated?
        /// </summary>
        [DataMember, Description("Is the specified sensor Calibrated?")]
        public bool Calibrated
        {
            get
            {
                if (CommandData.Length >= 6)
                    return (CommandData[5] == 0) ? false : true;
                return false;
            }
            set
            {
                if (CommandData.Length >= 6)
                    CommandData[5] = (byte)((value) ? 1 : 0);
            }
        }

        /// <summary>
        /// The LEGO NXT Sensor Type as defined by LEGO
        /// </summary>
        [DataMember, Description("The LEGO NXT Sensor Type as defined by LEGO")]
        public LegoSensorType SensorType
        {
            get
            {
                if (CommandData.Length >= 7)
                    return (LegoSensorType)CommandData[6];

                return LegoSensorType.NoSensor;
            }
            set
            {
                if (CommandData.Length >= 7)
                    CommandData[6] = (byte)value;
            }
        }

        /// <summary>
        /// The Sensor Mode
        /// </summary>
        [DataMember, Description("The Sensor Mode")]
        public LegoSensorMode SensorMode
        {
            get
            {
                if (CommandData.Length >= 8)
                    return (LegoSensorMode)CommandData[7];
                return LegoSensorMode.RawMode;
            }
            set
            {
                if (CommandData.Length >= 8)
                    CommandData[7] = (byte)value;
            }
        }

        /// <summary>
        /// The raw reading from the sensor.
        /// </summary>
        [DataMember, Description("The raw reading from the sensor.")]
        public int RawValue
        {
            get { return (int)BitConverter.ToUInt16(CommandData, 8); }
            set { NxtCommon.SetUShort(CommandData, 8, value); }
        }

        /// <summary>
        /// The normalized reading from the sensor
        /// </summary>
        [DataMember, Description("The normalized reading from the sensor.")]
        public int NormalizedValue
        {
            get { return (int)BitConverter.ToUInt16(CommandData, 10); }
            set { NxtCommon.SetUShort(CommandData, 10, value); }
        }

        /// <summary>
        /// The scaled reading from the sensor
        /// </summary>
        [DataMember, Description("The scaled reading from the sensor.")]
        public int ScaledValue
        {
            get { return (int)BitConverter.ToInt16(CommandData, 12); }
            set { NxtCommon.SetShort(CommandData, 12, value); }
        }


        /// <summary>
        /// The calibrated reading from the sensor
        /// </summary>
        [DataMember, Description("The calibrated reading from the sensor.")]
        public int CalibratedValue
        {
            get { return (int)BitConverter.ToInt16(CommandData, 14); }
            set { NxtCommon.SetShort(CommandData, 14, value); }
        }
    }

    #endregion

    #region LegoGetOutputState

    /// <summary>
    /// LEGO NXT Command: GetOutputState
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: GetOutputState.")]
    [XmlRootAttribute("LegoGetOutputState", Namespace = Contract.Identifier)]
    public class LegoGetOutputState : LegoCommand
    {
        /// <summary>
        /// LEGO NXT Command: GetOutputState
        /// </summary>
        public LegoGetOutputState()
            : base(25, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.GetOutputState, 0x00)
        {
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO NXT Command: GetOutputState
        /// </summary>
        /// <param name="outputPort"></param>
        public LegoGetOutputState(NxtMotorPort outputPort)
            : base(25, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.GetOutputState, 0x00)
        {
            base.RequireResponse = true;
            OutputPort = outputPort;
        }

        /// <summary>
        /// The matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new LegoResponseGetOutputState(responseData);
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// 0, 1, 2
        /// </summary>
        [DataMember, Description("The NXT output port (0,1, or 2).")]
        public NxtMotorPort OutputPort
        {
            get
            {
                return NxtCommon.GetNxtMotorPort(this.CommandData[2]);
            }
            set
            {
                this.CommandData[2] = NxtCommon.PortNumber(value);
            }
        }
    }

    /// <summary>
    /// LEGO NXT Response: Get Output State
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: GetOutputState.")]
    [XmlRootAttribute("LegoResponseGetOutputState", Namespace = Contract.Identifier)]
    public class LegoResponseGetOutputState : LegoResponse
    {
        /// <summary>
        /// LEGO NXT Response: Get Output State
        /// </summary>
        public LegoResponseGetOutputState()
            : base(25, LegoCommandCode.GetOutputState) { }

        /// <summary>
        /// LEGO NXT Response: Get Output State
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseGetOutputState(byte[] responseData)
            : base(25, LegoCommandCode.GetOutputState, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The NXT Motor port
        /// </summary>
        [DataMember, Description("The NXT Motor port")]
        public NxtMotorPort MotorPort
        {
            get
            {
                if (CommandData.Length >= 4)
                    return NxtCommon.GetNxtMotorPort(CommandData[3]);
                return NxtMotorPort.NotConnected;
            }
            set
            {
                CommandData[3] = NxtCommon.PortNumber(value);
            }
        }

        /// <summary>
        /// The motor power setting (range -100 to +100)
        /// </summary>
        [DataMember, Description("The motor power setting (range -100 to +100).")]
        public int PowerSetPoint
        {
            get
            {
                if (CommandData.Length >= 5)
                    return (int)(sbyte)CommandData[4];
                return -1;
            }
            set
            {
                CommandData[4] = (byte)value;
            }
        }

        /// <summary>
        /// The NXT output mode
        /// </summary>
        [DataMember, Description("The NXT output mode.")]
        public LegoOutputMode Mode
        {
            get
            {
                if (CommandData.Length >= 6)
                    return (LegoOutputMode)CommandData[5];
                return LegoOutputMode.Brake;
            }
            set
            {
                CommandData[5] = (byte)value;
            }
        }

        /// <summary>
        /// The NXT regulation mode
        /// </summary>
        [DataMember, Description("The NXT regulation mode.")]
        public LegoRegulationMode RegulationMode
        {
            get
            {
                if (CommandData.Length >= 7)
                    return (LegoRegulationMode)CommandData[6];
                return LegoRegulationMode.Idle;
            }
            set
            {
                CommandData[6] = (byte)value;
            }
        }

        /// <summary>
        /// The Motor Turn Ratio
        /// <remarks>(-100 - 100)</remarks>
        /// </summary>
        [DataMember, Description("Motor Turn Ratio")]
        public int TurnRatio
        {
            get
            {
                if (CommandData.Length >= 8)
                    return CommandData[7];
                return -1;
            }
            set
            {
                CommandData[7] = (byte)value;
            }
        }

        /// <summary>
        /// The Motor running state
        /// </summary>
        [DataMember, Description("The Motor running state")]
        public RunState RunState
        {
            get
            {
                if (CommandData.Length >= 9)
                    return (RunState)CommandData[8];
                return RunState.Idle;
            }
            set
            {
                CommandData[8] = (byte)value;
            }
        }

        /// <summary>
        /// A limit on the number of motor rotations (360 per rotation).
        /// </summary>
        [DataMember, Description("The Motor Encoder Limit (360 per rotation).")]
        public long EncoderLimit
        {
            get { return (long)BitConverter.ToUInt32(CommandData, 9); }
            set
            {
                NxtCommon.SetUInt32(CommandData, 9, value);
            }
        }

        /// <summary>
        /// The Motor Encoder Count (360 per rotation).
        /// </summary>
        [DataMember, Description("The Motor Encoder Count (360 per rotation).")]
        public int EncoderCount
        {
            get { return BitConverter.ToInt32(CommandData, 13); }
            set { NxtCommon.SetUInt32(CommandData, 13, value); }
        }

        /// <summary>
        /// The Motor Block Tachometer Count (360 per rotation).
        /// </summary>
        [DataMember, Description("The Motor Block Tachometer Count (360 per rotation).")]
        public int BlockTachoCount
        {
            get { return BitConverter.ToInt32(CommandData, 17); }
            set { NxtCommon.SetUInt32(CommandData, 17, value); }
        }

        /// <summary>
        /// The Resettable Encoder Count (360 per rotation).
        /// </summary>
        [DataMember, Description("The Resettable Encoder Count (360 per rotation).")]
        public long ResettableCount
        {
            get { return (long)BitConverter.ToInt32(CommandData, 21); }
            set { NxtCommon.SetUInt32(CommandData, 21, value); }
        }

        /// <summary>
        /// String representation of GetOutputState with parameters.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{10} GetOutputState(port={0},power={1},rotations={2},mode={3},runstate={4},regulation={5},turnratio={6},encoder={7},block={8},resettable={9})",
                this.MotorPort,
                this.PowerSetPoint,
                this.EncoderLimit,
                this.Mode,
                this.RunState,
                this.RegulationMode,
                this.TurnRatio,
                this.EncoderCount,
                this.BlockTachoCount,
                this.ResettableCount,
                this.TimeStamp.ToString("HH:mm:ss.fffffff"));
        }
    }


    #endregion

    #region LegoPlayTone

    /// <summary>
    /// Play a tone on the NXT
    /// </summary>
    /// <remarks>Standard return package.</remarks>    
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: PlayTone.")]
    [XmlRootAttribute("LegoPlayTone", Namespace = Contract.Identifier)]
    public class LegoPlayTone : LegoCommand
    {
        /// <summary>
        /// Play a tone on the NXT
        /// </summary>
        public LegoPlayTone()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.PlayTone, 0x00, 0x00, 0x00, 0x00) { }

        /// <summary>
        /// Play a tone on the NXT
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="duration"></param>
        public LegoPlayTone(int frequency, int duration)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.PlayTone, 0x00, 0x00, 0x00, 0x00)
        {
            this.Frequency = frequency;
            this.Duration = duration;
        }


        /// <summary>
        /// 200 - 14000 Hz
        /// </summary>
        [DataMember, Description("The frequency of the note.")]
        [DataMemberConstructor(Order = 1)]
        public int Frequency
        {
            get
            {
                if (this.CommandData == null || this.CommandData.Length < 6)
                    return 0;
                return NxtCommon.GetUShort(this.CommandData, 2);
            }
            set
            {
                ushort frequency = (ushort)Math.Min(Math.Max(200, value), 14000);
                if (this.CommandData == null) this.CommandData = new byte[6];
                NxtCommon.SetUShort(this.CommandData, 2, frequency);
            }
        }

        /// <summary>
        /// Duration to play tome in ms
        /// </summary>
        [DataMember, Description("The duration to play the note (in ms).")]
        [DataMemberConstructor(Order = 2)]
        public int Duration
        {
            get
            {
                if (CommandData == null || CommandData.Length < 6)
                    return 0;
                return NxtCommon.GetUShort(this.CommandData, 4);
            }

            set
            {
                ushort duration = (ushort)Math.Max(1, Math.Min(30000, value));
                if (this.CommandData == null) this.CommandData = new byte[6];
                NxtCommon.SetUShort(this.CommandData, 4, duration);
            }
        }
    }

    #endregion

    #region LegoLSGetStatus

    /// <summary>
    /// LEGO NXT Command: Low Speed (I2C) Get Status
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: LSGetStatus.")]
    [XmlRootAttribute("LegoLSGetStatus", Namespace = Contract.Identifier)]
    public class LegoLSGetStatus : LegoCommand
    {
        /// <summary>
        /// LEGO NXT Command: Low Speed (I2C) Get Status
        /// </summary>
        public LegoLSGetStatus()
            : base(4, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.LSGetStatus, 0x00)
        {
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO NXT Command: Low Speed (I2C) Get Status
        /// </summary>
        /// <param name="port"></param>
        public LegoLSGetStatus(NxtSensorPort port)
            : base(4, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.LSGetStatus, 0x00)
        {
            base.RequireResponse = true;
            Port = port;
        }

        /// <summary>
        /// The Matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new LegoResponseLSGetStatus(responseData);
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// The Sensor Port to Query Status
        /// </summary>
        [DataMember, Description("The Sensor Port to Query Status")]
        public NxtSensorPort Port
        {
            get { return NxtCommon.GetNxtSensorPort(this.CommandData[2]); }
            set { this.CommandData[2] = NxtCommon.PortNumber(value); }
        }
    }

    /// <summary>
    /// LEGO NXT Response: Low Speed (I2C) Get Status
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: LSGetStatus.")]
    [XmlRootAttribute("LegoResponseLSGetStatus", Namespace = Contract.Identifier)]
    public class LegoResponseLSGetStatus : LegoResponse
    {

        /// <summary>
        /// LEGO NXT Response: Low Speed (I2C) Get Status
        /// </summary>
        public LegoResponseLSGetStatus()
            : base(4, LegoCommandCode.LSGetStatus) { }

        /// <summary>
        /// LEGO NXT Response: Low Speed (I2C) Get Status
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseLSGetStatus(byte[] responseData)
            : base(4, LegoCommandCode.LSGetStatus, responseData) { }

        #region Hide base type DataMembers

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The number of bytes ready to read
        /// </summary>
        [DataMember, Description("The number of bytes ready to read")]
        public int BytesReady
        {
            get
            {
                if (CommandData.Length >= 4)
                    return (int)CommandData[3];
                return -1;
            }
            set
            {
                CommandData[3] = (byte)value;
            }
        }
    }


    #endregion

    #region ReadI2CSensorType (LSWrite/LSRead)
    /// <summary>
    /// LEGO Read Sensor Type
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: LSRead(SensorType).")]
    [XmlRootAttribute("I2CReadSensorType", Namespace = Contract.Identifier)]
    public class I2CReadSensorType : LegoLSWrite
    {
        /// <summary>
        /// LEGO Read Sensor Type
        /// </summary>
        public I2CReadSensorType()
            : base()
        {
            base.TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x08 };
            base.ExpectedI2CResponseSize = 16;
            base.Port = 0;
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Read Sensor Type
        /// </summary>
        /// <param name="port"></param>
        public I2CReadSensorType(NxtSensorPort port)
            : base()
        {
            base.TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x08 };
            base.ExpectedI2CResponseSize = 15;
            base.Port = port;
            base.RequireResponse = true;
        }

        /// <summary>
        /// The matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new I2CResponseSensorType(responseData);
        }

    }

    /// <summary>
    /// LegoResponse: I2C Sensor Type
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: LSRead(SensorType).")]
    [XmlRootAttribute("I2CResponseSensorType", Namespace = Contract.Identifier)]
    public class I2CResponseSensorType : LegoResponse
    {
        /// <summary>
        /// LegoResponse: I2C Sensor Type
        /// </summary>
        public I2CResponseSensorType()
            : base(20, LegoCommandCode.LSRead)
        {
        }

        /// <summary>
        /// LegoResponse: I2C Sensor Type
        /// </summary>
        /// <param name="responseData"></param>
        public I2CResponseSensorType(byte[] responseData)
            : base(20, LegoCommandCode.LSRead, responseData) { }

        #region Hide base type DataMembers

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The Sensor Manufacturer
        /// </summary>
        [DataMember, Description("The Sensor Manufacturer.")]
        public string Manufacturer
        {
            get
            {
                if (CommandData == null || CommandData.Length < 12)
                    return null;
                return Encoding.ASCII.GetString(base.CommandData, 3, 10).TrimEnd('\0', ' ', '?');
            }
            set
            {
            }
        }


        /// <summary>
        /// The Sensor Type
        /// </summary>
        [DataMember, Description("The Sensor Type.")]
        public string SensorType
        {
            get
            {
                if (CommandData == null || CommandData.Length < 20)
                    return null;
                return Encoding.ASCII.GetString(base.CommandData, 13, 7).TrimEnd('\0', ' ', '?');
            }
            set
            {
            }
        }
    }
    #endregion

    #region Send Command To I2C Address

    /// <summary>
    /// Read LEGO Sonar Sensor Data
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: LSWrite(address, cmd).")]
    [XmlRootAttribute("I2CSendCommand", Namespace = Contract.Identifier)]
    public class I2CSendCommand : LegoLSWrite
    {
        /// <summary>
        /// Send an I2C command to the specified address
        /// </summary>
        public I2CSendCommand()
            : base()
        {
            base.TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, 0x41 };
            base.ExpectedI2CResponseSize = 0;
            base.Port = 0;
            RequireResponse = true;
        }

        /// <summary>
        /// Send an I2C command to the specified address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="address"></param>
        public I2CSendCommand(NxtSensorPort port, int address)
            : base()
        {
            TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, (byte)address };
            ExpectedI2CResponseSize = 0;
            Port = port;
            RequireResponse = true;
        }

        /// <summary>
        /// Send an I2C command to the specified address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="address"></param>
        /// <param name="i2CBusAddress"></param>
        public I2CSendCommand(NxtSensorPort port, int address, int i2CBusAddress)
            : base()
        {
            TXData = new byte[] { (byte)i2CBusAddress, (byte)address };
            ExpectedI2CResponseSize = 0;
            Port = port;
            RequireResponse = true;
        }

        /// <summary>
        /// Command Address
        /// </summary>
        public int Address
        {
            get
            {
                if (TXData == null || TXData.Length < 2)
                    return 0;
                return TXData[1];
            }
            set
            {
                if (TXData == null)
                {
                    TXData = new byte[2];
                    TXData[0] = NxtCommon.DefaultI2CBusAddress;
                }
                TXData[1] = (byte)value;
            }
        }

        /// <summary>
        /// The I2C Bus Address which identifies the sensor on the I2C Bus
        /// <remarks>This is usually 0x02 (NxtCommon.DefaultI2CBusAddress) for most devices</remarks>
        /// </summary>
        public int I2CBusAddress
        {
            get
            {
                if (TXData == null || TXData.Length < 2)
                    return NxtCommon.DefaultI2CBusAddress;
                return TXData[0];
            }
            set
            {
                if (TXData == null)
                    TXData = new byte[2];
                TXData[0] = (byte)value;
            }
        }

    }

    #endregion

    #region Read Sonar Sensor Data


    /// <summary>
    /// UltraSonic Variables
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("UltraSonic Variables")]
    public enum UltraSonicPacket
    {
        /// <summary>
        /// Factory Zero
        /// </summary>
        ByteEight = 0x08,
        /// <summary>
        /// Factory Zero
        /// </summary>
        FactoryZero = 0x11,
        /// <summary>
        /// ContinuousMeasurementInterval
        /// </summary>
        ContinuousMeasurementInterval = 0x40,
        /// <summary>
        /// CommandState
        /// </summary>
        CommandState = 0x41,
        /// <summary>
        /// ReadMeasurement0
        /// </summary>
        ReadMeasurement1 = 0x42,
    }

    /// <summary>
    /// Read LEGO Sonar Sensor Data
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: LSRead(port, UltrasonicSensor).")]
    [XmlRootAttribute("I2CReadSonarSensor", Namespace = Contract.Identifier)]
    public class I2CReadSonarSensor : LegoLSWrite
    {

        /// <summary>
        /// HiTechnic Read Compass Sensor Data
        /// </summary>
        /// <param name="ultraSonicVariable"></param>
        public I2CReadSonarSensor(UltraSonicPacket ultraSonicVariable)
            : base()
        {
            base.TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, (byte)ultraSonicVariable };
            base.ExpectedI2CResponseSize = 1;
            base.Port = 0;
        }

        /// <summary>
        /// HiTechnic Read Compass Sensor Data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ultraSonicVariable"></param>
        public I2CReadSonarSensor(NxtSensorPort port, UltraSonicPacket ultraSonicVariable)
            : base()
        {
            TXData = new byte[] { NxtCommon.DefaultI2CBusAddress, (byte)ultraSonicVariable };
            ExpectedI2CResponseSize = 1;
            Port = port;
        }

        /// <summary>
        /// The matching LEGO Response
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        public override LegoResponse GetResponse(byte[] responseData)
        {
            return new I2CResponseSonarSensor(responseData);
        }


    }


    /// <summary>
    /// LegoResponse: I2C UltraSonic Sensor 
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: LSRead(port, UltrasonicSensor).")]
    [XmlRootAttribute("I2CResponseSonarSensor", Namespace = Contract.Identifier)]
    public class I2CResponseSonarSensor : LegoResponse
    {
        /// <summary>
        /// LegoResponse: I2C Sensor Type
        /// </summary>
        public I2CResponseSonarSensor()
            : base(20, LegoCommandCode.LSRead)
        {
        }

        /// <summary>
        /// LegoResponse: I2C UltraSonic Sensor 
        /// </summary>
        /// <param name="responseData"></param>
        public I2CResponseSonarSensor(byte[] responseData)
            : base(20, LegoCommandCode.LSRead, responseData) { }

        /// <summary>
        /// UltraSonic Sensor Variable
        /// </summary>
        [DataMember, Description("Ultrasonic Sensor Variable")]
        public int UltraSonicVariable
        {
            get
            {
                if (!Success)
                    return -1;

                return (int)this.CommandData[4];
            }
            set
            {
                this.CommandData[4] = (byte)value;
            }
        }
    }

    #endregion

    #region LegoResetMotorPosition

    /// <summary>
    /// Reset Motor Position
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: ResetMotorPosition.")]
    [XmlRootAttribute("LegoResetMotorPosition", Namespace = Contract.Identifier)]
    public class LegoResetMotorPosition : LegoCommand
    {
        /// <summary>
        /// Reset Motor Position
        /// </summary>
        public LegoResetMotorPosition()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.ResetMotorPosition, 0x00, 0x00) { }

        /// <summary>
        /// Reset Motor Position
        /// </summary>
        /// <param name="outputPort"></param>
        /// <param name="relative"></param>
        public LegoResetMotorPosition(NxtMotorPort outputPort, bool relative)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.ResetMotorPosition, 0x00, 0x00)
        {
            this.OutputPort = outputPort;
            this.Relative = relative;
        }

        /// <summary>
        /// Output Port 0-2
        /// </summary>
        [DataMember, Description("The NXT Motor Port")]
        [DataMemberConstructor(Order = 1)]
        public NxtMotorPort OutputPort
        {
            get
            {
                if (this.CommandData == null)
                    return NxtMotorPort.NotConnected;
                return NxtCommon.GetNxtMotorPort(this.CommandData[2]);
            }
            set
            {
                if (this.CommandData == null)
                    this.CommandData = new byte[4];

                this.CommandData[2] = NxtCommon.PortNumber(value);
            }
        }

        /// <summary>
        /// Position relative to last movement or absolute?
        /// </summary>
        [DataMember, Description("Identifies whether the position is relative to the last movement.")]
        [DataMemberConstructor(Order = 2)]
        public bool Relative
        {
            get
            {
                if (this.CommandData == null || this.CommandData.Length < 4)
                    return false;
                return (this.CommandData[3] != 0);
            }
            set
            {
                if (this.CommandData == null) this.CommandData = new byte[4];
                this.CommandData[3] = (byte)((value) ? 1 : 0);
            }
        }
    }


    #endregion

    #region LegoBootCommand
    /// <summary>
    /// LEGO Command: USB Command to Reset the LEGO Brick.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: USB Command to Reset the LEGO Brick.")]
    [XmlRootAttribute("LegoBootCommand", Namespace = Contract.Identifier)]
    public class LegoBootCommand : LegoCommand
    {
        /// <summary>
        /// USB Command to Reset the LEGO Brick.
        /// </summary>
        public LegoBootCommand()
            : base(7, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.BootCommand)
        {
            base.ExtendCommandData(21);
            NxtCommon.SetStringToData(CommandData, 2, "Let's dance: SAMBA", 19);
        }
    }

    /// <summary>
    /// LEGO Response: BootCommand
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: BootCommand")]
    [XmlRootAttribute("LegoResponseBootCommand", Namespace = Contract.Identifier)]
    public class LegoResponseBootCommand : LegoResponse
    {
        /// <summary>
        /// LEGO Response: BootCommand
        /// </summary>
        public LegoResponseBootCommand()
            : base(7, LegoCommandCode.BootCommand)
        {
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Response: BootCommand
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseBootCommand(byte[] responseData)
            : base(7, LegoCommandCode.BootCommand, responseData)
        {
            base.RequireResponse = true;
        }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion

        /// <summary>
        /// LEGO NXT Boot Acknowledgement.
        /// </summary>
        [DataMember, Description("LEGO NXT Boot Acknowledgement")]
        public string Message
        {
            get
            {
                if (CommandData == null || CommandData.Length < ExpectedResponseSize)
                    return string.Empty;

                return NxtCommon.DataToString(CommandData, 3);
            }
            set
            {
                if (CommandData == null || CommandData.Length < ExpectedResponseSize)
                {
                    byte[] oldData = CommandData;
                    CommandData = new byte[ExpectedResponseSize];
                    if (oldData != null) oldData.CopyTo(CommandData, 0);
                }
                NxtCommon.SetStringToData(CommandData, 3, value, 4);
            }
        }

    }

    #endregion

    #region LegoClose
    /// <summary>
    /// LEGO Command: Close a file handle.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: Close a file handle.")]
    [XmlRootAttribute("LegoClose", Namespace = Contract.Identifier)]
    public class LegoClose : LegoCommand
    {

        /// <summary>
        /// Close a file handle.
        /// </summary>
        public LegoClose()
            : base(4, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.Close, 0x00)
        { base.RequireResponse = true; }

        /// <summary>
        /// Close a file handle.
        /// </summary>
        /// <param name="handle"></param>
        public LegoClose(int handle)
            : base(4, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.Close, 0x00)
        {
            base.RequireResponse = true;
            this.Handle = handle;
        }

        /// <summary>
        /// The handle to the file.
        /// </summary>
        [DataMember, Description("The handle to the file.")]
        public int Handle
        {
            get { return (int)this.CommandData[2]; }
            set
            {
                this.CommandData[2] = (byte)value;
            }
        }
    }

    /// <summary>
    /// LEGO Response: Close
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: Close.")]
    [XmlRootAttribute("LegoResponseClose", Namespace = Contract.Identifier)]
    public class LegoResponseClose : LegoResponse
    {
        /// <summary>
        /// LEGO Response: Close.
        /// </summary>
        public LegoResponseClose()
            : base(4, LegoCommandCode.Close) { }

        /// <summary>
        /// LEGO Response: Close.
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseClose(byte[] responseData)
            : base(4, LegoCommandCode.Close, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length == ExpectedResponseSize)
                    return CommandData[3];
                return -1;
            }
            set
            {
                if (CommandData != null && CommandData.Length == 4)
                {
                    CommandData[3] = (byte)value;
                }
            }
        }

    }
    #endregion

    #region LegoDelete
    /// <summary>
    /// LEGO Command: Delete
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: Delete.")]
    [XmlRootAttribute("LegoDelete", Namespace = Contract.Identifier)]
    public class LegoDelete : LegoCommand
    {

        private string _fileName = string.Empty;

        /// <summary>
        /// LEGO Command: Delete
        /// </summary>
        public LegoDelete()
            : base(23, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.Delete)
        {
            ExtendCommandData(22);
            base.RequireResponse = true;
        }


        /// <summary>
        /// LEGO Command: Delete
        /// </summary>
        /// <param name="fileName"></param>
        public LegoDelete(string fileName)
            : base(23, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.Delete)
        {
            base.RequireResponse = true;
            ExtendCommandData(22);
            this.FileName = fileName;
        }

        /// <summary>
        /// The name of the file to be deleted.
        /// </summary>
        [DataMember, Description("The name of the file to be deleted.")]
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    _fileName = NxtCommon.DataToString(this.CommandData, 2, 20);

                return _fileName;
            }
            set
            {
                _fileName = value;
                NxtCommon.SetStringToData(this.CommandData, 2, _fileName, 20);
            }
        }

    }

    /// <summary>
    /// LEGO Response: Delete
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: Delete.")]
    [XmlRootAttribute("LegoResponseDelete", Namespace = Contract.Identifier)]
    public class LegoResponseDelete : LegoResponse
    {
        /// <summary>
        /// LEGO Response: Delete
        /// </summary>
        public LegoResponseDelete()
            : base(23, LegoCommandCode.Delete) { }

        /// <summary>
        /// LEGO Response: Delete
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseDelete(byte[] responseData)
            : base(23, LegoCommandCode.Delete, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The name of the file.
        /// </summary>
        [DataMember, Description("The name of the file.")]
        public string FileName
        {
            get
            {
                if (CommandData.Length < this.ExpectedResponseSize)
                    return string.Empty;

                return NxtCommon.DataToString(CommandData, 3, 20);
            }
            set
            {
                if (CommandData == null || CommandData.Length < this.ExpectedResponseSize)
                {
                    byte[] oldData = CommandData;
                    CommandData = new byte[this.ExpectedResponseSize];
                    if (oldData != null) oldData.CopyTo(CommandData, 0);
                }
                NxtCommon.SetStringToData(this.CommandData, 3, value, 20);
            }
        }
    }

    #endregion

    #region LegoStartProgram
    /// <summary>
    /// LEGO Command: Starts a program on the NXT.
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: Starts a program on the NXT.")]
    [XmlRootAttribute("LegoStartProgram", Namespace = Contract.Identifier)]
    public class LegoStartProgram : LegoCommand
    {
        private string _fileName = string.Empty;

        /// <summary>
        /// Starts a program on the NXT.
        /// </summary>
        public LegoStartProgram()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.StartProgram)
        {
            base.ExtendCommandData(22);
        }

        /// <summary>
        /// Starts a program on the NXT.
        /// </summary>
        /// <param name="fileName"></param>
        public LegoStartProgram(string fileName)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.StartProgram)
        {
            base.ExtendCommandData(22);
            this.FileName = fileName;
        }

        /// <summary>
        /// The name of the file to be started.
        /// </summary>
        [DataMember, Description("The name of the file to be started.")]
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    _fileName = NxtCommon.DataToString(this.CommandData, 2, 20);
                return _fileName;
            }
            set
            {
                _fileName = value;
                NxtCommon.SetStringToData(this.CommandData, 2, _fileName, 20);
            }
        }
    }

    #endregion

    #region LegoPlaySoundFile
    /// <summary>
    /// LEGO Command: Play a sound file
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: Play a sound file.")]
    [XmlRootAttribute("LegoPlaySoundFile", Namespace = Contract.Identifier)]
    public class LegoPlaySoundFile : LegoCommand
    {
        private string _fileName = string.Empty;
        private bool? _loop = null;

        /// <summary>
        /// Play a sound file
        /// </summary>
        public LegoPlaySoundFile()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.PlaySoundFile)
        {
            ExtendCommandData(23);
        }

        /// <summary>
        /// Play a sound file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="loop"></param>
        public LegoPlaySoundFile(string fileName, bool loop)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.PlaySoundFile)
        {
            ExtendCommandData(23);
            this.FileName = fileName;
        }

        /// <summary>
        /// The name of the sound file to be played.
        /// </summary>
        [DataMember, Description("The name of the sound file to be played.")]
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    _fileName = NxtCommon.DataToString(this.CommandData, 3, 20);
                return _fileName;
            }
            set
            {
                _fileName = value;
                NxtCommon.SetStringToData(this.CommandData, 3, _fileName, 20);
            }
        }

        /// <summary>
        /// Repeat the sound file
        /// </summary>
        [DataMember, Description("Repeat the sound file")]
        public bool Loop
        {
            get
            {
                if (_loop == null)
                    _loop = (this.CommandData != null && this.CommandData.Length >= 3 && this.CommandData[2] != 0);
                return (bool)_loop;
            }
            set
            {
                _loop = value;
                this.CommandData[2] = (byte)((value) ? 1 : 0);
            }
        }
    }


    #endregion

    #region LegoStopSoundPlayback

    /// <summary>
    /// Stop sound playback on the NXT.
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: StopSoundPlayback \nStop sound playback on the NXT.")]
    [XmlRootAttribute("LegoStopSoundPlayback", Namespace = Contract.Identifier)]
    public class LegoStopSoundPlayback : LegoCommand
    {
        /// <summary>
        /// Stop sound playback on the NXT.
        /// </summary>
        public LegoStopSoundPlayback()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.StopSoundPlayback) { }

    }

    #endregion

    #region LegoStopProgram

    /// <summary>
    /// LEGO Command: Stop a program on the NXT.
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: Stop a program on the NXT.")]
    [XmlRootAttribute("LegoStopProgram", Namespace = Contract.Identifier)]
    public class LegoStopProgram : LegoCommand
    {
        /// <summary>
        /// LEGO Command: Stop a program on the NXT.
        /// </summary>
        public LegoStopProgram()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.StopProgram)
        {
            // Default return a status
            base.RequireResponse = true;
        }

    }

    #endregion

    #region LegoFindFirst

    /// <summary>
    /// LEGO Command: FindFirst
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: FindFirst.")]
    [XmlRootAttribute("LegoFindFirst", Namespace = Contract.Identifier)]
    public class LegoFindFirst : LegoCommand
    {
        private string _fileName = string.Empty;

        /// <summary>
        /// LEGO Command: FindFirst
        /// </summary>
        public LegoFindFirst()
            : base(28, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.FindFirst)
        {
            base.ExtendCommandData(22);
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Command: FindFirst
        /// </summary>
        /// <param name="fileName"></param>
        public LegoFindFirst(string fileName)
            : base(28, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.FindFirst, 0)
        {
            base.ExtendCommandData(22);
            base.RequireResponse = true;
            this.FileName = fileName;
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// The name of the file.
        /// </summary>
        [DataMember, Description("The name of the file.")]
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    _fileName = NxtCommon.DataToString(this.CommandData, 2);
                return _fileName;
            }
            set
            {
                _fileName = value;
                NxtCommon.SetStringToData(this.CommandData, 2, _fileName, 20);
            }
        }

    }

    /// <summary>
    /// LEGO Response: Find First.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: Find First.")]
    [XmlRootAttribute("LegoResponseFindFirst", Namespace = Contract.Identifier)]
    public class LegoResponseFindFirst : LegoResponse
    {
        /// <summary>
        /// LEGO Response: Find First.
        /// </summary>
        public LegoResponseFindFirst()
            : base(28, LegoCommandCode.FindFirst)
        {
        }

        /// <summary>
        /// LEGO Response: Find First.
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseFindFirst(byte[] responseData)
            : base(28, LegoCommandCode.FindFirst, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion

        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length == this.ExpectedResponseSize)
                    return CommandData[3];
                return -1;
            }
            set
            {
                if (CommandData != null && CommandData.Length == this.ExpectedResponseSize)
                    CommandData[3] = (byte)value;
            }

        }

        /// <summary>
        /// The name of the file.
        /// </summary>
        [DataMember, Description("The name of the file.")]
        public string FileName
        {
            get
            {
                if (CommandData == null || CommandData.Length != this.ExpectedResponseSize)
                    return string.Empty;

                return NxtCommon.DataToString(CommandData, 4, 20);
            }
            set
            {
                string newFilename = value;
                if (value.Length > 19)
                    newFilename = value.Substring(0, 19);

                if (CommandData == null || CommandData.Length < this.ExpectedResponseSize)
                {
                    byte[] oldData = CommandData;
                    CommandData = new byte[this.ExpectedResponseSize];
                    if (oldData != null) oldData.CopyTo(CommandData, 0);
                }
                NxtCommon.SetStringToData(this.CommandData, 4, value, 20);
            }
        }

        /// <summary>
        /// The size of the file.
        /// </summary>
        [DataMember, Description("The size of the file.")]
        public long FileSize
        {
            get
            {
                if (CommandData.Length == this.ExpectedResponseSize)
                    return (long)BitConverter.ToUInt32(CommandData, 24);
                return -1;
            }
            set
            {
                if (CommandData.Length == this.ExpectedResponseSize)
                    NxtCommon.SetUInt32(CommandData, 24, value);
            }
        }

    }
    #endregion

    #region LegoFindNext

    /// <summary>
    /// LEGO Command: FindNext
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: FindNext.")]
    [XmlRootAttribute("LegoFindNext", Namespace = Contract.Identifier)]
    public class LegoFindNext : LegoCommand
    {
        /// <summary>
        /// LEGO Command: FindNext
        /// </summary>
        public LegoFindNext()
            : base(28, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.FindNext, 0x00)
        {
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Command: FindNext
        /// </summary>
        /// <param name="handle"></param>
        public LegoFindNext(int handle)
            : base(28, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.FindNext, 0)
        {
            base.RequireResponse = true;
            this.Handle = handle;
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// The handle from LegoResponseFirst or LegoResponseNxt.
        /// </summary>
        [DataMember, Description("The handle from LegoResponseFindFirst or LegoResponseFindNxt.")]
        public int Handle
        {
            get
            {
                return (int)this.CommandData[2];
            }
            set
            {
                this.CommandData[2] = (byte)value;
            }
        }

    }

    /// <summary>
    /// LEGO Response: Find Next.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: Find Next.")]
    [XmlRootAttribute("LegoResponseFindNext", Namespace = Contract.Identifier)]
    public class LegoResponseFindNext : LegoResponse
    {
        /// <summary>
        /// LEGO Response: Find Next.
        /// </summary>
        public LegoResponseFindNext()
            : base(28, LegoCommandCode.FindNext)
        {
        }

        /// <summary>
        /// LEGO Response: Find Next.
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseFindNext(byte[] responseData)
            : base(28, LegoCommandCode.FindNext, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion

        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length == this.ExpectedResponseSize)
                    return CommandData[3];
                return -1;
            }
            set
            {
                if (CommandData != null && CommandData.Length == this.ExpectedResponseSize)
                    CommandData[3] = (byte)value;
            }

        }

        /// <summary>
        /// The name of the file.
        /// </summary>
        [DataMember, Description("The name of the file.")]
        public string FileName
        {
            get
            {
                if (CommandData == null || CommandData.Length != this.ExpectedResponseSize)
                    return string.Empty;

                return NxtCommon.DataToString(CommandData, 4, 20);
            }
            set
            {
                string newFilename = value;
                if (value.Length > 19)
                    newFilename = value.Substring(0, 19);

                if (CommandData == null || CommandData.Length < this.ExpectedResponseSize)
                {
                    byte[] oldData = CommandData;
                    CommandData = new byte[this.ExpectedResponseSize];
                    if (oldData != null) oldData.CopyTo(CommandData, 0);
                }
                NxtCommon.SetStringToData(this.CommandData, 4, value, 20);
            }
        }

        /// <summary>
        /// The size of the file.
        /// </summary>
        [DataMember, Description("The size of the file.")]
        public long FileSize
        {
            get
            {
                if (CommandData.Length == this.ExpectedResponseSize)
                    return (long)BitConverter.ToUInt32(CommandData, 24);
                return -1;
            }
            set
            {
                if (CommandData.Length == this.ExpectedResponseSize)
                    NxtCommon.SetUInt32(CommandData, 24, value);
            }
        }

    }
    #endregion

    #region LegoGetCurrentProgramName
    /// <summary>
    /// LEGO Command: GetCurrentProgramName
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: GetCurrentProgramName.")]
    [XmlRootAttribute("LegoGetCurrentProgramName", Namespace = Contract.Identifier)]
    public class LegoGetCurrentProgramName : LegoCommand
    {
        /// <summary>
        /// LEGO Command: GetCurrentProgramName
        /// </summary>
        public LegoGetCurrentProgramName()
            : base(23, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.GetCurrentProgramName)
        {
            base.RequireResponse = true;
        }


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }
    }

    /// <summary>
    /// LEGO Response: GetCurrentProgramName
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: Get Current Program Name.")]
    [XmlRootAttribute("LegoResponseGetCurrentProgramName", Namespace = Contract.Identifier)]
    public class LegoResponseGetCurrentProgramName : LegoResponse
    {
        /// <summary>
        /// LEGO Response: GetCurrentProgramName
        /// </summary>
        public LegoResponseGetCurrentProgramName()
            : base(23, LegoCommandCode.GetCurrentProgramName) { }

        /// <summary>
        /// LEGO Response: GetCurrentProgramName
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseGetCurrentProgramName(byte[] responseData)
            : base(23, LegoCommandCode.GetCurrentProgramName, responseData) { }


        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion

        /// <summary>
        /// The name of the file.
        /// </summary>
        [DataMember, Description("The name of the file.")]
        public string FileName
        {
            get
            {
                if (CommandData == null || CommandData.Length < this.ExpectedResponseSize)
                    return string.Empty;

                return NxtCommon.DataToString(CommandData, 3, 20);
            }
            set
            {
                if (CommandData == null || CommandData.Length != this.ExpectedResponseSize)
                {
                    byte[] oldData = CommandData;
                    CommandData = new byte[this.ExpectedResponseSize];
                    if (oldData != null) oldData.CopyTo(CommandData, 0);
                }
                NxtCommon.SetStringToData(this.CommandData, 3, value, 20);
            }
        }

    }

    #endregion

    #region LegoOpenWrite
    /// <summary>
    /// LEGO Command: OpenWrite
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: OpenWrite.")]
    [XmlRootAttribute("LegoOpenWrite", Namespace = Contract.Identifier)]
    public class LegoOpenWrite : LegoCommand
    {
        private string _fileName;

        /// <summary>
        /// LEGO Command: OpenWrite
        /// </summary>
        public LegoOpenWrite()
            : base(4, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.OpenWrite)
        {
            base.ExtendCommandData(26);
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Command: OpenWrite
        /// </summary>
        /// <param name="file"></param>
        /// <param name="size"></param>
        public LegoOpenWrite(string file, int size)
            : base(4, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.OpenWrite)
        {
            base.ExtendCommandData(26);
            base.RequireResponse = true;
            this.FileName = file;
            this.FileSize = size;
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// The name of the file to be opened for writing.
        /// </summary>
        [DataMember, Description("The name of the file to be opened for writing.")]
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    _fileName = NxtCommon.DataToString(this.CommandData, 2, 20);
                return _fileName;
            }
            set
            {
                _fileName = value;
                if (this.CommandData == null) this.CommandData = new byte[26];
                NxtCommon.SetStringToData(this.CommandData, 2, _fileName, 20);
            }
        }

        /// <summary>
        /// The size of the file.
        /// </summary>
        [DataMember, Description("The size of the file.")]
        public int FileSize
        {
            get
            {
                return (int)System.BitConverter.ToUInt32(this.CommandData, 22);
            }
            set
            {
                if (this.CommandData == null) this.CommandData = new byte[26];
                uint fileSize = (UInt32)value;
                NxtCommon.SetUInt32(this.CommandData, 22, fileSize);
            }
        }
    }

    /// <summary>
    /// LEGO Response: OpenWrite.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: OpenWrite.")]
    [XmlRootAttribute("LegoResponseOpenWrite", Namespace = Contract.Identifier)]
    public class LegoResponseOpenWrite : LegoResponse
    {
        /// <summary>
        /// LEGO Command: OpenWrite
        /// </summary>
        public LegoResponseOpenWrite()
            : base(4, LegoCommandCode.OpenWrite) { }

        /// <summary>
        /// LEGO Command: OpenWrite
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseOpenWrite(byte[] responseData)
            : base(4, LegoCommandCode.OpenWrite, responseData) { }


        #region Hide base type DataMembers

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length == this.ExpectedResponseSize)
                    return CommandData[3];
                return -1;
            }
            set
            {
                CommandData[3] = (byte)value;
            }
        }

    }
    #endregion

    #region LegoOpenWriteLinear
    /// <summary>
    /// LEGO Command: OpenWriteLinear.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: OpenWriteLinear.")]
    [XmlRootAttribute("LegoOpenWriteLinear", Namespace = Contract.Identifier)]
    public class LegoOpenWriteLinear : LegoCommand
    {
        private string _fileName;
        private UInt32 _fileSize;

        /// <summary>
        /// LEGO Command: OpenWriteLinear
        /// </summary>
        public LegoOpenWriteLinear()
            : base(4, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.OpenWriteLinear)
        {
            base.ExtendCommandData(26);
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Command: OpenWriteLinear
        /// </summary>
        /// <param name="file"></param>
        /// <param name="size"></param>
        public LegoOpenWriteLinear(string file, int size)
            : base(4, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.OpenWriteLinear)
        {
            base.ExtendCommandData(26);
            base.RequireResponse = true;
            this.FileName = file;
            this.FileSize = size;
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [DataMember, Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// The name of the file.
        /// </summary>
        [DataMember, Description("The name of the file.")]
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    _fileName = NxtCommon.DataToString(this.CommandData, 2, 20);
                return _fileName;
            }
            set
            {
                _fileName = value;
                if (this.CommandData == null) this.CommandData = new byte[26];
                NxtCommon.SetStringToData(this.CommandData, 2, _fileName, 20);
            }
        }

        /// <summary>
        /// The size of the file.
        /// </summary>
        [DataMember, Description("The size of the file.")]
        public int FileSize
        {
            get
            {
                return (int)System.BitConverter.ToUInt32(this.CommandData, 22);
            }
            set
            {
                _fileSize = (UInt32)value;
                if (this.CommandData == null) this.CommandData = new byte[26];
                NxtCommon.SetUInt32(this.CommandData, 22, _fileSize);
            }
        }


    }

    /// <summary>
    /// LEGO Response: OpenWriteLinear.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: OpenWriteLinear.")]
    [XmlRootAttribute("LegoResponseOpenWriteLinear", Namespace = Contract.Identifier)]
    public class LegoResponseOpenWriteLinear : LegoResponse
    {
        /// <summary>
        /// LEGO Response: OpenWriteLinear
        /// </summary>
        public LegoResponseOpenWriteLinear()
            : base(4, LegoCommandCode.OpenWriteLinear) { }

        /// <summary>
        /// LEGO Response: OpenWriteLinear
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseOpenWriteLinear(byte[] responseData)
            : base(4, LegoCommandCode.OpenWriteLinear, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length == this.ExpectedResponseSize)
                    return CommandData[3];
                return -1;
            }
            set
            {
                CommandData[3] = (byte)value;
            }

        }

    }
    #endregion

    #region LegoOpenWriteData
    /// <summary>
    /// LEGO Command: OpenWriteData.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: OpenWriteData.")]
    [XmlRootAttribute("LegoOpenWriteData", Namespace = Contract.Identifier)]
    public class LegoOpenWriteData : LegoCommand
    {
        private string _fileName;

        /// <summary>
        /// LEGO Command: OpenWrite
        /// </summary>
        public LegoOpenWriteData()
            : base(4, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.OpenWriteData)
        {
            base.ExtendCommandData(26);
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Command: OpenWrite
        /// </summary>
        /// <param name="file"></param>
        /// <param name="size"></param>
        public LegoOpenWriteData(string file, int size)
            : base(4, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.OpenWriteData)
        {
            base.ExtendCommandData(26);
            base.RequireResponse = true;
            this.FileName = file;
            this.FileSize = size;
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// The name of the file to be opened for writing.
        /// </summary>
        [DataMember, Description("The name of the file to be opened for writing.")]
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    _fileName = NxtCommon.DataToString(this.CommandData, 2, 20);
                return _fileName;
            }
            set
            {
                _fileName = value;
                if (this.CommandData == null) this.CommandData = new byte[26];
                NxtCommon.SetStringToData(this.CommandData, 2, _fileName, 20);
            }
        }

        /// <summary>
        /// The size of the file.
        /// </summary>
        [DataMember, Description("The size of the file.")]
        public int FileSize
        {
            get
            {
                return (int)System.BitConverter.ToUInt32(this.CommandData, 22);
            }
            set
            {
                if (this.CommandData == null) this.CommandData = new byte[26];
                uint fileSize = (UInt32)value;
                NxtCommon.SetUInt32(this.CommandData, 22, fileSize);
            }
        }
    }

    /// <summary>
    /// LEGO Response: OpenWriteData.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: OpenWriteData.")]
    [XmlRootAttribute("LegoResponseOpenWriteData", Namespace = Contract.Identifier)]
    public class LegoResponseOpenWriteData : LegoResponse
    {
        /// <summary>
        /// LEGO Command: OpenWrite
        /// </summary>
        public LegoResponseOpenWriteData()
            : base(4, LegoCommandCode.OpenWriteData) { }

        /// <summary>
        /// LEGO Command: OpenWrite
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseOpenWriteData(byte[] responseData)
            : base(4, LegoCommandCode.OpenWriteData, responseData) { }


        #region Hide base type DataMembers

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length == this.ExpectedResponseSize)
                    return CommandData[3];
                return -1;
            }
            set
            {
                CommandData[3] = (byte)value;
            }
        }

    }
    #endregion

    #region LegoWrite

    /// <summary>
    /// LEGO Command: Write.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: Write.")]
    [XmlRootAttribute("LegoWrite", Namespace = Contract.Identifier)]
    public class LegoWrite : LegoCommand
    {
        private byte[] _writeData;

        /// <summary>
        /// LEGO Command: Write
        /// </summary>
        public LegoWrite()
            : base(6, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.Write, 0x00)
        {
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Command: Write
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="writeData"></param>
        public LegoWrite(int handle, byte[] writeData)
            : base(6, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.Write, 0x00)
        {
            base.RequireResponse = true;
            this.Handle = handle;
            this.WriteData = writeData;
        }

        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get { return (int)this.CommandData[2]; }
            set
            {
                this.CommandData[2] = (byte)value;
            }
        }

        /// <summary>
        /// The data to be written.
        /// </summary>
        [DataMember, Description("The data to be written.")]
        public byte[] WriteData
        {
            get
            {
                if (_writeData == null && CommandData != null)
                    _writeData = new byte[CommandData.Length - 3];

                if (CommandData != null && CommandData.Length > 3)
                    System.Buffer.BlockCopy(CommandData, 3, _writeData, 0, _writeData.Length);

                return _writeData;
            }
            set
            {
                _writeData = value;
                base.ExtendCommandData(_writeData.Length + 3);
                System.Buffer.BlockCopy(_writeData, 0, this.CommandData, 3, _writeData.Length);
            }
        }


    }

    /// <summary>
    /// LEGO Response: Write.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: Write.")]
    [XmlRootAttribute("LegoResponseWrite", Namespace = Contract.Identifier)]
    public class LegoResponseWrite : LegoResponse
    {
        /// <summary>
        /// LEGO Response: Write.
        /// </summary>
        public LegoResponseWrite()
            : base(6, LegoCommandCode.Write) { }

        /// <summary>
        /// LEGO Response: Write.
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseWrite(byte[] responseData)
            : base(6, LegoCommandCode.Write, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion

        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length == ExpectedResponseSize)
                    return CommandData[3];
                return -1;
            }
            set
            {
                CommandData[3] = (byte)value;
            }
        }

        /// <summary>
        /// The number of bytes written.
        /// </summary>
        [DataMember, Description("The number of bytes written.")]
        public int BytesWritten
        {
            get
            {
                if (CommandData != null && CommandData.Length == ExpectedResponseSize)
                    return (int)BitConverter.ToUInt16(CommandData, 4);
                return -1;
            }
            set { NxtCommon.SetUShort(CommandData, 4, value); }
        }

    }
    #endregion

    #region LegoOpenRead

    /// <summary>
    /// LEGO Command: OpenRead.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: OpenRead.")]
    [XmlRootAttribute("LegoOpenRead", Namespace = Contract.Identifier)]
    public class LegoOpenRead : LegoCommand
    {
        private string _fileName = string.Empty;

        /// <summary>
        /// LEGO Command: OpenRead
        /// </summary>
        public LegoOpenRead()
            : base(8, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.OpenRead)
        {
            ExtendCommandData(22);
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Command: OpenRead
        /// </summary>
        /// <param name="fileName"></param>
        public LegoOpenRead(string fileName)
            : base(8, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.OpenRead)
        {
            ExtendCommandData(22);
            base.RequireResponse = true;
            this.FileName = fileName;
        }


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// The name of the file opened for reading.
        /// </summary>
        [DataMember, Description("Specifies the name of the file opened for reading.")]
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    _fileName = NxtCommon.DataToString(this.CommandData, 2, 20);
                return _fileName;
            }
            set
            {
                _fileName = value;
                if (this.CommandData == null) this.CommandData = new byte[26];
                NxtCommon.SetStringToData(this.CommandData, 2, _fileName, 20);
            }
        }

    }

    /// <summary>
    /// LEGO Response: OpenRead.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: OpenRead.")]
    [XmlRootAttribute("LegoResponseOpenRead", Namespace = Contract.Identifier)]
    public class LegoResponseOpenRead : LegoResponse
    {
        /// <summary>
        /// LEGO Response: OpenRead
        /// </summary>
        public LegoResponseOpenRead()
            : base(8, LegoCommandCode.OpenRead) { }

        /// <summary>
        /// LEGO Response: OpenRead
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseOpenRead(byte[] responseData)
            : base(8, LegoCommandCode.OpenRead, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion

        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length == ExpectedResponseSize)
                    return CommandData[3];
                return -1;
            }
            set
            {
                CommandData[3] = (byte)value;
            }
        }

        /// <summary>
        /// The size of the file.
        /// </summary>
        [DataMember, Description("The size of the file.")]
        public long FileSize
        {
            get
            {
                if (CommandData.Length == ExpectedResponseSize)
                    return (long)BitConverter.ToUInt32(CommandData, 4);
                return -1;
            }
            set
            {
                NxtCommon.SetUInt32(CommandData, 4, value);
            }
        }

    }
    #endregion

    #region LegoRead
    /// <summary>
    /// LEGO Command: Read.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: Read.")]
    [XmlRootAttribute("LegoRead", Namespace = Contract.Identifier)]
    public class LegoRead : LegoCommand
    {
        private int _bytesToRead;

        /// <summary>
        /// LEGO Command: Read
        /// </summary>
        public LegoRead()
            : base(5, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.Read, 0, 0, 0)
        {
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Command: Read
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="bytesToRead"></param>
        public LegoRead(int handle, int bytesToRead)
            : base(5, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.Read, 0, 0, 0)
        {
            base.RequireResponse = true;
            this.Handle = handle;
            this.BytesToRead = bytesToRead;
        }

        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length >= 3)
                    return CommandData[2];
                return -1;
            }
            set
            {
                CommandData[2] = (byte)value;
            }
        }

        /// <summary>
        /// The number of bytes to read.
        /// </summary>
        [DataMember, Description("The number of bytes to read.")]
        public int BytesToRead
        {
            get
            {
                _bytesToRead = NxtCommon.GetUShort(this.CommandData, 3);
                return (int)_bytesToRead;
            }
            set
            {
                _bytesToRead = value;
                ExpectedResponseSize = _bytesToRead + 6;
                if (this.CommandData == null) this.CommandData = new byte[5];
                NxtCommon.SetUShort(this.CommandData, 3, _bytesToRead);
            }
        }
    }

    /// <summary>
    /// LEGO Response: Read.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: Read.")]
    [XmlRootAttribute("LegoResponseRead", Namespace = Contract.Identifier)]
    public class LegoResponseRead : LegoResponse
    {
        /// <summary>
        /// LEGO Response: Read
        /// </summary>
        public LegoResponseRead()
            : base(7, LegoCommandCode.Read) { }

        /// <summary>
        /// LEGO Response: Read
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseRead(byte[] responseData)
            : base(responseData.Length, LegoCommandCode.Read, responseData)
        {
            this.CommandData = responseData;
            this.ExpectedResponseSize = responseData.Length;
        }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The handle to the data.
        /// </summary>
        [DataMember, Description("The handle to the data.")]
        public int Handle
        {
            get
            {
                if (CommandData != null && CommandData.Length >= 4)
                    return CommandData[3];
                return -1;
            }
            set
            {
                CommandData[3] = (byte)value;
            }

        }

        /// <summary>
        /// The number of bytes read.
        /// </summary>
        [DataMember, Description("The number of bytes read.")]
        public int BytesRead
        {
            get
            {
                if (CommandData != null && CommandData.Length >= 6)
                    return (int)BitConverter.ToUInt16(CommandData, 4);
                return -1;
            }
            set
            {
                NxtCommon.SetUShort(CommandData, 4, value);
                ExpectedResponseSize = value + 6;
            }
        }

        /// <summary>
        /// The data read.
        /// </summary>
        [DataMember, Description("The data read.")]
        public byte[] ReadData
        {
            get
            {
                if (CommandData != null && CommandData.Length == ExpectedResponseSize)
                {
                    byte[] r = new byte[ExpectedResponseSize];
                    System.Buffer.BlockCopy(this.CommandData, 6, r, 0, ExpectedResponseSize);
                    return r;
                }
                return null;
            }
            set
            {
                ExtendCommandData(6 + value.Length);
                System.Buffer.BlockCopy(value, 0, this.CommandData, 6, value.Length);
            }
        }

    }

    #endregion

    #region LegoMessageRead

    /// <summary>
    /// LEGO Command: MessageRead.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: MessageRead.")]
    [XmlRootAttribute("LegoMessageRead", Namespace = Contract.Identifier)]
    public class LegoMessageRead : LegoCommand
    {
        private bool _remove;

        /// <summary>
        /// LEGO Command: MessageRead
        /// </summary>
        public LegoMessageRead()
            : base(64, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.MessageRead, 0x00, 0x00, 0x00)
        {
            base.RequireResponse = true;
        }

        /// <summary>
        /// LEGO Command: MessageRead
        /// </summary>
        /// <param name="remoteInbox"></param>
        /// <param name="localInbox"></param>
        /// <param name="remove"></param>
        public LegoMessageRead(int remoteInbox, int localInbox, bool remove)
            : base(64, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.MessageRead, 0x00, 0x00, 0x00)
        {
            base.RequireResponse = true;
            this.RemoteInbox = remoteInbox;
            this.LocalInbox = localInbox;
            this.Remove = remove;
        }


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        /// <summary>
        /// Remove Inbox 0-9
        /// </summary>
        [DataMember, Description("The remote communication port (0-9).")]
        public int RemoteInbox
        {
            get { return (int)this.CommandData[2]; }
            set
            {
                if (this.CommandData == null) this.CommandData = new byte[5];
                this.CommandData[2] = (byte)value;
            }
        }

        /// <summary>
        /// Local Inbox 0-9
        /// </summary>
        [DataMember, Description("The local communication port (0-9).")]
        public int LocalInbox
        {
            get { return (int)this.CommandData[3]; }
            set
            {
                if (this.CommandData == null) this.CommandData = new byte[5];
                this.CommandData[3] = (byte)value;
            }
        }

        /// <summary>
        /// Clear message from remote inbox
        /// </summary>
        [DataMember, Description("Identifies whether the message has been removed.")]
        public bool Remove
        {
            get
            {
                if (this.CommandData == null || this.CommandData.Length < 5)
                    return false;

                return (this.CommandData[4] != 0);
            }
            set
            {
                _remove = value;
                if (this.CommandData == null) this.CommandData = new byte[5];
                this.CommandData[4] = (byte)((value) ? 1 : 0);
            }
        }
    }

    /// <summary>
    /// LEGO Response: MessageRead.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: MessageRead.")]
    [XmlRootAttribute("LegoResponseMessageRead", Namespace = Contract.Identifier)]
    public class LegoResponseMessageRead : LegoResponse
    {
        /// <summary>
        /// LEGO Response: MessageRead
        /// </summary>
        public LegoResponseMessageRead()
            : base(64, LegoCommandCode.MessageRead) { }

        /// <summary>
        /// LEGO Response: MessageRead
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseMessageRead(byte[] responseData)
            : base(64, LegoCommandCode.MessageRead, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion

        /// <summary>
        /// The local communication port.
        /// </summary>
        [DataMember, Description("The local communication port.")]
        public int LocalInbox
        {
            get
            {
                if (CommandData != null && CommandData.Length == ExpectedResponseSize)
                    return (int)this.CommandData[3];
                return -1;
            }
            set
            {
                CommandData[3] = (byte)value;
            }
        }

        /// <summary>
        /// The size of the message.
        /// </summary>
        [DataMember, Description("The size of the message.")]
        public int MessageSize
        {
            get
            {
                if (CommandData != null && CommandData.Length >= 5)
                    return (int)this.CommandData[4];
                return -1;
            }
            set
            {
                CommandData[4] = (byte)value;
            }
        }

        /// <summary>
        /// The message data read.
        /// </summary>
        [DataMember, Description("The message data read.")]
        public byte[] MessageReadData
        {
            get
            {
                if (CommandData != null && CommandData.Length == ExpectedResponseSize && MessageSize <= 59)
                {
                    byte[] r = new byte[MessageSize];
                    System.Buffer.BlockCopy(this.CommandData, 5, r, 0, MessageSize);
                    return r;
                }
                return null;
            }
            set
            {
                System.Buffer.BlockCopy(value, 0, this.CommandData, 5, Math.Min(value.Length, 59));
            }
        }

        /// <summary>
        /// The Text version of the Message
        /// </summary>
        public string Message
        {
            get
            {
                byte[] messageReadData = this.MessageReadData;
                if (messageReadData == null || messageReadData.Length == 0)
                    return null;

                return Encoding.ASCII.GetString(messageReadData).TrimEnd('\0', ' ', '?');
            }
        }

    }


    #endregion

    #region LegoMessageWrite

    /// <summary>
    /// LEGO Command: MessageWrite.  Send a message to the NXT that the NXT can read with a message block.
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: MessageWrite.  Send a message to the NXT that the NXT can read with a message block.")]
    [XmlRootAttribute("LegoMessageWrite", Namespace = Contract.Identifier)]
    public class LegoMessageWrite : LegoCommand
    {
        private byte[] _messageData = null;

        /// <summary>
        /// Send a message to the NXT that the NXT can read with a message block.
        /// </summary>
        public LegoMessageWrite()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.MessageWrite, 0x00, 0x00)
        {
        }

        /// <summary>
        /// Send a message to the NXT that the NXT can read with a message block.
        /// </summary>
        /// <param name="inbox"></param>
        /// <param name="messageData"></param>
        public LegoMessageWrite(int inbox, byte[] messageData)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.MessageWrite, (byte)inbox, Math.Min((byte)messageData.Length, (byte)59))
        {
            this.Inbox = inbox;
            this.MessageData = messageData;
        }

        /// <summary>
        /// Send a message to the NXT that the NXT can read with a message block.
        /// </summary>
        /// <param name="inbox"></param>
        /// <param name="message"></param>
        public LegoMessageWrite(int inbox, string message)
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.MessageWrite, 0x00, 0x00)
        {
            this.Inbox = inbox;
            this.MessageDataString = message;
        }


        /// <summary>
        /// LEGO NXT Inbox where message should be delivered
        /// </summary>
        [DataMember, Description("LEGO NXT Inbox where message should be delivered")]
        [DataMemberConstructor(Order = 1)]
        public int Inbox
        {
            get { return (int)this.CommandData[2]; }
            set { this.CommandData[2] = (byte)value; }
        }

        /// <summary>
        /// The size of the message to be written (0-60).
        /// </summary>
        [DataMember, Description("The size of the message to be written (1-58).")]
        public int MessageSize
        {
            get { return (int)this.CommandData[3]; }
            set
            {
                if (value < 1 || value > 58)
                    throw new ArgumentOutOfRangeException("MessageSize must be between 1 and 58 bytes");

                this.CommandData[3] = (byte)value;
            }
        }

        /// <summary>
        /// The Message Data
        /// </summary>
        public byte[] MessageData
        {
            get
            {
                if (MessageSize < 1 || MessageSize > 58)
                    return null;
                if (_messageData == null || _messageData.Length != MessageSize)
                {
                    _messageData = new byte[MessageSize];
                    System.Buffer.BlockCopy(CommandData, 4, _messageData, 0, MessageSize);
                }
                return _messageData;
            }
            set
            {
                int length = (value == null) ? 0 : value.Length;

                if (length == 0)
                    throw new ArgumentOutOfRangeException("MessageData must be at least 1 byte.");

                if (length > 58)
                    throw new ArgumentOutOfRangeException("MessageData must be no larger than 58 bytes.");

                _messageData = value;
                base.ExtendCommandData(length + 4);
                MessageSize = length;
                System.Buffer.BlockCopy(value, 0, this.CommandData, 4, length);
            }
        }

        /// <summary>
        /// Expose the message data as a string.
        /// </summary>
        [DataMember, Description("The message data.")]
        [DataMemberConstructor(Order = 2)]
        public string MessageDataString
        {
            get
            {
                if (MessageData == null || _messageData.Length < 2)
                    return string.Empty;

                return NxtCommon.DataToString(CommandData, 4);
            }

            set
            {
                MessageData = NxtCommon.StringToData(value, value.Length + 1);
            }
        }

    }

    #endregion

    #region LegoKeepAlive

    /// <summary>
    /// LEGO Command: KeepAlive.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: KeepAlive.")]
    [XmlRootAttribute("LegoKeepAlive", Namespace = Contract.Identifier)]
    public class LegoKeepAlive : LegoCommand
    {
        /// <summary>
        /// LEGO Command: KeepAlive.
        /// </summary>
        public LegoKeepAlive()
            : base(7, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.KeepAlive)
        {
            base.RequireResponse = true;
        }
    }

    /// <summary>
    /// LEGO Response: KeepAlive.
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Response: KeepAlive.")]
    [XmlRootAttribute("LegoResponseKeepAlive", Namespace = Contract.Identifier)]
    public class LegoResponseKeepAlive : LegoResponse
    {
        /// <summary>
        /// LEGO Response: KeepAlive.
        /// </summary>
        public LegoResponseKeepAlive()
            : base(7, LegoCommandCode.KeepAlive)
        {
        }

        /// <summary>
        /// LEGO Response: KeepAlive.
        /// </summary>
        /// <param name="responseData"></param>
        public LegoResponseKeepAlive(byte[] responseData)
            : base(7, LegoCommandCode.KeepAlive, responseData) { }

        #region Hide base type DataMembers


        /// <summary>
        /// Hide RequireResponse from proxy and always set it to true.
        /// </summary>
        [Description("Identifies whether to send an acknowledgement back on a command request.")]
        public override bool RequireResponse
        {
            get { return true; }
            set { base.RequireResponse = true; }
        }

        #endregion


        /// <summary>
        /// The number of milliseconds between KeepAlive messages
        /// </summary>
        [DataMember, Description("The number of milliseconds between KeepAlive messages.")]
        public long SleepTimeMilliseconds
        {
            get
            {
                if (CommandData.Length == ExpectedResponseSize)
                    return (long)BitConverter.ToUInt32(CommandData, 3);
                return -1;
            }
            set
            {
                if (CommandData.Length >= ExpectedResponseSize)
                    NxtCommon.SetUInt32(CommandData, 3, value);
            }
        }

    }


    #endregion

    #region LegoSetBrickName
    /// <summary>
    /// LEGO Command: SetBrickName.
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: SetBrickName.")]
    [XmlRootAttribute("LegoSetBrickName", Namespace = Contract.Identifier)]
    public class LegoSetBrickName : LegoCommand
    {
        private string _name = string.Empty;

        /// <summary>
        /// LEGO Command: SetBrickName.
        /// </summary>
        public LegoSetBrickName()
            : base(3, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.SetBrickName)
        {
            ExtendCommandData(18);
        }

        /// <summary>
        /// LEGO Command: SetBrickName.
        /// </summary>
        /// <param name="name"></param>
        public LegoSetBrickName(string name)
            : base(3, LegoCommand.NxtSystemCommand, (byte)LegoCommandCode.SetBrickName)
        {
            ExtendCommandData(18);
            this.Name = name;
        }

        /// <summary>
        /// The descriptive identifier for the NXT brick.
        /// </summary>
        [DataMember, Description("The descriptive identifier for the NXT brick.")]
        [DataMemberConstructor(Order = 1)]
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                    _name = NxtCommon.DataToString(this.CommandData, 2, 16);

                return _name;
            }
            set
            {
                _name = value;
                NxtCommon.SetStringToData(this.CommandData, 2, _name, 16);
            }
        }

    }

    #endregion

    #region LegoResetInputScaledValue

    /// <summary>
    /// Reset Motor Position
    /// <remarks>Standard return package.</remarks>
    /// </summary>
    [DataContract(ExcludeFromProxy = true), Description("LEGO Command: ResetInputScaledValue.")]
    [XmlRootAttribute("LegoResetInputScaledValue", Namespace = Contract.Identifier)]
    public class LegoResetInputScaledValue : LegoCommand
    {
        /// <summary>
        /// LEGO Command: ResetInputScaledValue.
        /// </summary>
        public LegoResetInputScaledValue()
            : base(3, LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.ResetInputScaledValue, 0x00) { }

        /// <summary>
        /// LEGO Command: ResetInputScaledValue.
        /// </summary>
        /// <param name="inputPort"></param>
        public LegoResetInputScaledValue(NxtSensorPort inputPort)
            : base(LegoCommand.NxtDirectCommand, (byte)LegoCommandCode.ResetInputScaledValue, 0x00)
        {
            this.InputPort = inputPort;
        }


        /// <summary>
        /// Input Port 0-3
        /// </summary>
        [DataMember, Description("The input port on the NXT brick.")]
        [DataMemberConstructor(Order = 1)]
        public NxtSensorPort InputPort
        {
            get { return NxtCommon.GetNxtSensorPort(this.CommandData[2]); }
            set { this.CommandData[2] = NxtCommon.PortNumber(value); }
        }

    }

    #endregion


    #endregion

}
