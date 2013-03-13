//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtIOTypes.cs $ $Revision: 11 $
//-----------------------------------------------------------------------

using Microsoft.Dss.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Ccr.Core;
using W3C.Soap;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.IO
{
    
    /// <summary>
    /// LegoNxtIO Contract
    /// </summary>
    public sealed class Contract
    {
        /// The Unique Contract Identifier for the LegoNxtIO service
        [DataMember, Description("Identifies the unique DSS Contract Identifier for the Lego NXT IO Service (v2).")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/io.user.html";
    }

    /// <summary>
    /// NXT IO State
    /// </summary>
    [DataContract, Description("Specifies the LEGO NXT Input Output service state.")]
    public class NxtIOState 
    {
        /// <summary>
        /// The results of the last query.
        /// </summary>
        [DataMember, Description("Indicates the results of the last query.")]
        [Browsable(false)]
        public ResponseFiles ResponseFiles;
    }

    #region QueryFiles

    /// <summary>
    /// Search for one or more files on the LEGO NXT Brick.
    /// </summary>
    [Description("Searches for one or more files on the LEGO NXT Brick.")]
    public class QueryFiles : Query<QueryFilesRequest, PortSet<ResponseFiles, Fault>>
    {
    }

    /// <summary>
    /// The name of the files to search for on the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Requests to search for a file on the LEGO NXT Brick.")]
    public class QueryFilesRequest
    {
        /// <summary>
        /// The filename to search for. (use '*' as a wild card)
        /// </summary>
        [DataMember, Description("Specifies the filename to search for. (use '*' as a wild card)")]
        public string Filespec;

    }

    /// <summary>
    /// The files found on the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Identifies the files found on the LEGO NXT Brick.")]
    public class ResponseFiles
    {
        /// <summary>
        /// Files Found.
        /// </summary>
        [DataMember, Description("Indicates the files which were found.")]
        public List<LegoFile> Files;
    }

    /// <summary>
    /// A LEGO file
    /// </summary>
    [DataContract, Description("Specifies a LEGO file.")]
    public class LegoFile
    {
        /// <summary>
        /// The LEGO (15.3) filename.
        /// </summary>
        [DataMember, Description("Specifies the name of the file. (Filenames are limited to 15 characters and a three character extension.)")]
        public string FileName;

        /// <summary>
        /// The size of the file.
        /// </summary>
        [DataMember, Description("Identifies the size of the file.")]
        public int FileSize;

        /// <summary>
        /// A LEGO file
        /// </summary>
        public LegoFile() { }
        /// <summary>
        /// A LEGO file
        /// </summary>
        /// <param name="fileName"></param>
        public LegoFile(string fileName) { this.FileName = fileName; }
        /// <summary>
        /// A LEGO file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        public LegoFile(string fileName, int fileSize) { this.FileName = fileName; this.FileSize = fileSize; }
    }

    #endregion

    #region QueryRunningProgram

    /// <summary>
    /// Query for any program running on the LEGO NXT Brick.
    /// </summary>
    [Description("Queries for any program running on the LEGO NXT Brick.")]
    public class QueryRunningProgram : Query<QueryRunningLegoProgramRequest, PortSet<RunningProgramResponse, Fault>> { }

    /// <summary>
    /// Query for any program running on the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Queries for any program running on the LEGO NXT Brick.")]
    public class QueryRunningLegoProgramRequest{ }

    /// <summary>
    /// Indicates the program which is currently running on the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Indicates the program which is currently running on the LEGO NXT Brick.")]
    public class RunningProgramResponse
    {
        /// <summary>
        /// The name of the running LEGO NXT program.
        /// </summary>
        [DataMember, Description("Identifies the name of the running LEGO NXT program.")]
        public string Program;
    }

    #endregion

    #region StopProgram

    /// <summary>
    /// Stop any program running on the LEGO NXT Brick.
    /// </summary>
    [Description("Stops any program running on the LEGO NXT Brick.")]
    public class StopProgram : Submit<StopLegoProgramRequest, PortSet<DefaultSubmitResponseType, Fault>> { }

    /// <summary>
    /// Stop any program running on the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Stops any program running on the LEGO NXT Brick.")]
    public class StopLegoProgramRequest
    {
    }

    #endregion

    #region StartProgram

    /// <summary>
    /// Start running a program on the LEGO NXT Brick.
    /// </summary>
    [Description("Starts running a program on the LEGO NXT Brick.")]
    public class StartProgram : Submit<StartLegoProgramRequest, PortSet<DefaultSubmitResponseType, Fault>> { }

    /// <summary>
    /// Start a program on the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Starts a program on the LEGO NXT Brick.")]
    public class StartLegoProgramRequest
    {
        /// <summary>
        /// The name of the program to start.
        /// </summary>
        [DataMember, Description("Specifies the name of the program to start.")]
        public string Program;

    }

    #endregion

    #region DeleteFile

    /// <summary>
    /// Delete a file on the LEGO NXT Brick.
    /// </summary>
    [Description("Deletes a file on the LEGO NXT Brick.")]
    public class DeleteFile : Submit<DeleteFileRequest, PortSet<DefaultSubmitResponseType, Fault>>
    {
    }

    /// <summary>
    /// Delete a file on the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Deletes a file on the LEGO NXT Brick.")]
    public class DeleteFileRequest
    {
        /// <summary>
        /// The name of the file to delete from the LEGO NXT Brick.
        /// </summary>
        [DataMember, Description("Indicates the filename to search and delete on the LEGO NXT Brick. (use '*' as a wild card.)")]
        public string FileName;

    }

    #endregion

    #region CopyFileToBrick

    /// <summary>
    /// Send a file to the LEGO NXT Brick.
    /// </summary>
    [Description("Sends a file to the LEGO NXT Brick.")]
    public class CopyFileToBrick : Submit<SendFileRequest, PortSet<DefaultSubmitResponseType, Fault>> { }

    /// <summary>
    /// Send a file to the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Sends a file to the LEGO NXT Brick.")]
    public class SendFileRequest
    {
        /// <summary>
        /// The fully qualified location of the file to send.
        /// </summary>
        [DataMember, Description("Specifies the fully qualified location of the file to send.")]
        public Uri FileLocation;


        /// <summary>
        /// The file data to send to the LEGO NXT Brick.
        /// </summary>
        [DataMember, Description("Specifies the file data to send to the LEGO NXT Brick.")]
        public byte[] FileData;


        /// <summary>
        /// The name of the file when it is sent to the LEGO NXT.
        /// </summary>
        [DataMember, Description("Specifies the name of the file when it is saved to the LEGO NXT.")]
        public string FileName;

        /// <summary>
        /// The name of the file when it is sent to the LEGO NXT.
        /// </summary>
        [DataMember, Description("Specifies whether to replace an existing file.")]
        public bool ReplaceExistingFile;

    }

    #endregion


    #region SetBrickName

    /// <summary>
    /// Change the Name of the LEGO NXT Brick.
    /// </summary>
    [Description("Changes the Name of the LEGO NXT Brick.")]
    public class SetBrickName : Submit<SetBrickNameRequest, PortSet<DefaultSubmitResponseType, Fault>> { }

    /// <summary>
    /// Change the Name of the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Changes the Name of the LEGO NXT Brick.")]
    public class SetBrickNameRequest
    {
        /// <summary>
        /// The new name of the LEGO NXT Brick.
        /// </summary>
        [DataMember, Description("Specifies the new name of the LEGO NXT Brick.")]
        public string BrickName;

    }

    #endregion

    #region QueryBrickName

    /// <summary>
    /// Query for the name of the LEGO NXT Brick.
    /// </summary>
    [Description("Queries for the name of the LEGO NXT Brick.")]
    public class QueryBrickName : Query<QueryBrickNameRequest, PortSet<BrickNameResponse, Fault>> { }

    /// <summary>
    /// Query for the name of the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Queries for the name of the LEGO NXT Brick.")]
    public class QueryBrickNameRequest { }

    /// <summary>
    /// Indicates the name of the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Indicates the name of the LEGO NXT Brick.")]
    public class BrickNameResponse
    {
        /// <summary>
        /// The name of the LEGO NXT Brick.
        /// </summary>
        [DataMember, Description("Specifies the name of the LEGO NXT Brick.")]
        public string BrickName;
    }

    #endregion

    #region Send and Receive BluetoothMessages

    /// <summary>
    /// Send a Bluetooth Message to the LEGO NXT Brick.
    /// </summary>
    [Description("Sends a Bluetooth Message to the LEGO NXT Brick.")]
    public class SendBluetoothMessage : Submit<BluetoothMessage, PortSet<DefaultSubmitResponseType, Fault>> { }

    /// <summary>
    /// Receive a Bluetooth Message from the LEGO NXT Brick.
    /// </summary>
    [Description("Receives a Bluetooth Message from the LEGO NXT Brick.")]
    public class ReceiveBluetoothMessage : Query<ReceiveBluetoothMessageRequest, PortSet<BluetoothMessage, Fault>> { }

    /// <summary>
    /// Receive a Bluetooth Message from the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Receives a Bluetooth Message from the LEGO NXT Brick.")]
    public class ReceiveBluetoothMessageRequest
    {
        /// <summary>
        /// Specifies the mailbox to check for a message (1-10).
        /// </summary>
        [DataMember, Description("Specifies the mailbox to check for a message (1-10).")]
        [DataMemberConstructor(Order = 1)]
        public int Mailbox;

    }

    /// <summary>
    /// Specifies a Bluetooth Message which may be sent to or received from the LEGO NXT Brick.
    /// </summary>
    [DataContract, Description("Specifies a Bluetooth Message which may be sent to or received from the LEGO NXT Brick.")]
    public class BluetoothMessage
    {
        /// <summary>
        /// Indicates the mailbox where the message is delivered (1-10).
        /// </summary>
        [DataMember, Description("Indicates the mailbox where the message is delivered (1-10).")]
        [DataMemberConstructor(Order = 1)]
        public int Mailbox;

        /// <summary>
        /// Indicates the message which is sent to or received from the LEGO NXT Brick.
        /// </summary>
        [DataMember, Description("Indicates the message which is sent to or received from the LEGO NXT Brick.")]
        [DataMemberConstructor(Order = 2)]
        public string Message;

    }

    #endregion

    /// <summary>
    /// LEGO IO Operations Port
    /// </summary>
    [ServicePort]
    public class NxtBrickOnboardOperations : PortSet<
        DsspDefaultLookup,
        Get,
        HttpGet,
        QueryFiles,
        DeleteFile,
        CopyFileToBrick,
        StopProgram,
        StartProgram,
        QueryRunningProgram,
        SetBrickName,
        QueryBrickName,
        SendBluetoothMessage,
        ReceiveBluetoothMessage>
    {
    }

    /// <summary>
    /// Get Operation
    /// </summary>
    [Description("Gets the current state of the LEGO NXT Input Output Service.")]
    public class Get : Get<GetRequestType, PortSet<NxtIOState, Fault>> { }


}
