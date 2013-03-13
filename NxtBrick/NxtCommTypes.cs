//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtCommTypes.cs $ $Revision: 14 $
//-----------------------------------------------------------------------

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using System;
using System.Collections.Generic;
using W3C.Soap;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using System.ComponentModel;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using System.IO.Ports;


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Comm
{
    
    /// <summary>
    /// LegoComm Main Operations Port
    /// </summary>
    [ServicePort]
    public class LegoCommOperations : PortSet
    {
        /// <summary>
        /// LegoComm Main Operations Port
        /// </summary>
        public LegoCommOperations()
            : base(
            typeof(DsspDefaultLookup),
            typeof(DsspDefaultDrop),
            typeof(Get),
            typeof(Open),
            typeof(Close),
            typeof(SendCommand),
            typeof(Subscribe),
            typeof(ConnectionUpdate))
        {
        }

        #region Helper Methods

        /// <summary>
        /// Required Lookup request body type
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.LookupResponse, Fault> DsspDefaultLookup()
        {
            Microsoft.Dss.ServiceModel.Dssp.LookupRequestType body = new Microsoft.Dss.ServiceModel.Dssp.LookupRequestType();
            Microsoft.Dss.ServiceModel.Dssp.DsspDefaultLookup op = new Microsoft.Dss.ServiceModel.Dssp.DsspDefaultLookup(body);
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Dssp Default Lookup and return the response port.
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.LookupResponse, Fault> DsspDefaultLookup(Microsoft.Dss.ServiceModel.Dssp.LookupRequestType body)
        {
            Microsoft.Dss.ServiceModel.Dssp.DsspDefaultLookup op = new Microsoft.Dss.ServiceModel.Dssp.DsspDefaultLookup();
            op.Body = body ?? new Microsoft.Dss.ServiceModel.Dssp.LookupRequestType();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// A request to drop the service.
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.DefaultDropResponseType, Fault> DsspDefaultDrop()
        {
            Microsoft.Dss.ServiceModel.Dssp.DropRequestType body = new Microsoft.Dss.ServiceModel.Dssp.DropRequestType();
            Microsoft.Dss.ServiceModel.Dssp.DsspDefaultDrop op = new Microsoft.Dss.ServiceModel.Dssp.DsspDefaultDrop(body);
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Dssp Default Drop and return the response port.
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.DefaultDropResponseType, Fault> DsspDefaultDrop(Microsoft.Dss.ServiceModel.Dssp.DropRequestType body)
        {
            Microsoft.Dss.ServiceModel.Dssp.DsspDefaultDrop op = new Microsoft.Dss.ServiceModel.Dssp.DsspDefaultDrop();
            op.Body = body ?? new Microsoft.Dss.ServiceModel.Dssp.DropRequestType();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Required Get body type
        /// </summary>
        public virtual PortSet<NxtCommState, Fault> Get()
        {
            Microsoft.Dss.ServiceModel.Dssp.GetRequestType body = new Microsoft.Dss.ServiceModel.Dssp.GetRequestType();
            Get op = new Get(body);
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Get and return the response port.
        /// </summary>
        public virtual PortSet<NxtCommState, Fault> Get(Microsoft.Dss.ServiceModel.Dssp.GetRequestType body)
        {
            Get op = new Get();
            op.Body = body ?? new Microsoft.Dss.ServiceModel.Dssp.GetRequestType();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Open a connection to the LEGO NXT Hardware
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.DefaultSubmitResponseType, Fault> Open()
        {
            Open op = new Open();
            op.Body = new OpenRequest();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Brick Open and return the response port.
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.DefaultSubmitResponseType, Fault> Open(OpenRequest body)
        {
            Open op = new Open();
            op.Body = body ?? new OpenRequest();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Close the connection to the LEGO NXT Hardware
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.DefaultSubmitResponseType, Fault> Close()
        {
            Close op = new Close();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Brick Close and return the response port.
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.DefaultSubmitResponseType, Fault> Close(CloseRequest body)
        {
            Close op = new Close();
            op.Body = body ?? new CloseRequest();
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Lego Command
        /// </summary>
        public virtual PortSet<LegoResponse, Fault> SendCommand(int expectedResponseSize, byte[] commandData)
        {
            SendCommand op = new SendCommand();
            op.Body = new SendCommandRequest(new LegoCommand(expectedResponseSize, commandData));
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Brick SendCommand and return the response port.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public virtual PortSet<LegoResponse, Fault> SendCommand(LegoCommand cmd)
        {
            SendCommand op = new SendCommand();
            op.Body = new SendCommandRequest(cmd);
            this.PostUnknownType(op);
            return op.ResponsePort;

        }

        /// <summary>
        /// Post Brick Send Command and return the response port.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="priorityRequest"></param>
        /// <returns></returns>
        public virtual PortSet<LegoResponse, Fault> SendCommand(LegoCommand cmd, bool priorityRequest)
        {
            SendCommand op = new SendCommand();
            op.Body = new SendCommandRequest(cmd, priorityRequest);
            this.PostUnknownType(op);
            return op.ResponsePort;
        }

        /// <summary>
        /// Post Subscribe and return the response port.
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.SubscribeResponseType, Fault> Subscribe(IPort notificationPort)
        {
            Subscribe op = new Subscribe();
            op.Body = new Microsoft.Dss.ServiceModel.Dssp.SubscribeRequestType();
            op.NotificationPort = notificationPort;
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        /// <summary>
        /// Post Subscribe with body and return the response port.
        /// </summary>
        public virtual PortSet<Microsoft.Dss.ServiceModel.Dssp.SubscribeResponseType, Fault> Subscribe(Microsoft.Dss.ServiceModel.Dssp.SubscribeRequestType body, IPort notificationPort)
        {
            Subscribe op = new Subscribe();
            op.Body = body ?? new Microsoft.Dss.ServiceModel.Dssp.SubscribeRequestType();
            op.NotificationPort = notificationPort;
            this.PostUnknownType(op);
            return op.ResponsePort;

        }
        #endregion

        #region Implicit Operators

        /// <summary>
        /// Implicit Operator for Port of Open
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<Open>(LegoCommOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<Open>)portSet[typeof(Open)];
        }
        /// <summary>
        /// Implicit Operator for Port of Close
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<Close>(LegoCommOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<Close>)portSet[typeof(Close)];
        }
        /// <summary>
        /// Implicit Operator for Port of SendCommand
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<SendCommand>(LegoCommOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<SendCommand>)portSet[typeof(SendCommand)];
        }
        /// <summary>
        /// Implicit Operator for Port of Subscribe
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<Subscribe>(LegoCommOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<Subscribe>)portSet[typeof(Subscribe)];
        }
        /// <summary>
        /// Implicit Operator for Port of ConnectionUpdate
        /// </summary>
        /// <param name="portSet"></param>
        /// <returns></returns>
        public static implicit operator Port<ConnectionUpdate>(LegoCommOperations portSet)
        {
            if (portSet == null) return null;
            return (Port<ConnectionUpdate>)portSet[typeof(ConnectionUpdate)];
        }
        #endregion
    }

    #region Data Contracts

    /// <summary>
    /// The LegoComm State
    /// </summary>
    [DataContract, Description("Specifies the LEGO Communications state.")]
    public class NxtCommState
    {
        /// <summary>
        /// Connect using Bluetooth
        /// </summary>
        [DataMember, Description("Connects using Bluetooth.")]
        public bool ConnectOverBluetooth;

        /// <summary>
        /// Is the connection currently active?
        /// </summary>
        [DataMember, Description("Identifies if the service is currently open and connected to a LEGO NXT brick.")]
        public bool Connected;

        /// <summary>
        /// Communication Statistics by LEGO Command.
        /// </summary>
        [DataMember, Description("Communication Statistics by LEGO Command.")]
        public Dictionary<LegoCommandCode, LegoCommandStat> RuntimeStatistics;
    }

    /// <summary>
    /// Communication statistics by LEGO Command.
    /// </summary>
    [DataContract, Description("Communication statistics by LEGO Command.")]
    public class LegoCommandStat
    {
        /// <summary>
        /// Communication statistics by LEGO Command.
        /// </summary>
        public LegoCommandStat() { }
        /// <summary>
        /// Communication statistics by LEGO Command.
        /// </summary>
        /// <param name="commandCode"></param>
        public LegoCommandStat(LegoCommandCode commandCode) { CommandCode = commandCode; }

        /// <summary>
        /// The LEGO Command Code
        /// </summary>
        [DataMember, Description("Identifies the LEGO Command Code.")]
        public LegoCommandCode CommandCode;
        
        /// <summary>
        /// Total Milliseconds
        /// </summary>
        [DataMember, Description("Total Microseconds.")]
        public double TotalMicroseconds;

        /// <summary>
        /// Number of times this command has been measured.
        /// </summary>
        [DataMember, Description("Number of times this command has been measured.")]
        public long Count;

        /// <summary>
        /// Average Microseconds (�sec).
        /// </summary>
        [DataMember, Description("Average Microseconds (�sec).")]
        public double AverageMicroseconds;


        /// <summary>
        /// The quickest command completion (Microseconds).
        /// </summary>
        [DataMember, Description("The quickest command completion (Microseconds).")]
        public double MinimumMicroseconds;
    }

    /// <summary>
    /// Open a connection to the LEGO NXT Hardware
    /// </summary>
    [DataContract, Description("Open a connection to the LEGO NXT Hardware.")]
    public class OpenRequest
    {
        /// <summary>
        /// Open a connection to the LEGO NXT Hardware
        /// </summary>
        public OpenRequest() { }

        /// <summary>
        /// Open a connection to the LEGO NXT Hardware
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="baudRate"></param>
        /// <param name="connectionType"></param>
        public OpenRequest(int serialPort, int baudRate, LegoConnectionType connectionType)
        {
            this.SerialPort = serialPort;
            this.BaudRate = baudRate;
            this.ConnectionType = connectionType;
        }

        /// <summary>
        /// Serial Port
        /// </summary>
        [DataMember, Description("Indicates the Serial Communications Port.")]
        public int SerialPort;

        /// <summary>
        /// Baud Rate (0 = default)
        /// </summary>
        [DataMember, Description("Identifies the Baud Rate (0 = default).")]
        public int BaudRate;

        /// <summary>
        /// Connection Type (USB or Bluetooth)
        /// </summary>
        [DataMember, Description("Identifies the Connection Type (USB or Bluetooth).\n(USB is not currently supported).")]
        public LegoConnectionType ConnectionType;
    }

    /// <summary>
    /// Close the connection to the LEGO NXT Hardware
    /// </summary>
    [DataContract, Description("Closes the connection to the LEGO NXT Hardware.")]
    public class CloseRequest
    {
    }

    /// <summary>
    /// Send Lego Command Request
    /// </summary>
    [DataContract, Description("Sends Lego Command Requests.")]
    public class SendCommandRequest
    {
        /// <summary>
        /// Lego Command Request
        /// </summary>
        public SendCommandRequest()
        {
        }

        /// <summary>
        /// Lego Command Request
        /// </summary>
        /// <param name="legoCommand"></param>
        public SendCommandRequest(LegoCommand legoCommand)
        {
            this.LegoCommand = legoCommand;
        }

        /// <summary>
        /// Lego Command Request
        /// </summary>
        /// <param name="legoCommand"></param>
        /// <param name="priorityRequest"></param>
        public SendCommandRequest(LegoCommand legoCommand, bool priorityRequest)
        {
            this.LegoCommand = legoCommand;
            this.PriorityRequest = priorityRequest;
        }

        /// <summary>
        /// The LEGO Command
        /// </summary>
        [DataMember]
        public LegoCommand LegoCommand;

        /// <summary>
        /// Process in the Priority Queue
        /// </summary>
        [DataMember]
        public bool PriorityRequest;

        /// <summary>
        /// Internal Stopwatch
        /// </summary>
        internal System.Diagnostics.Stopwatch Stopwatch;

        /// <summary>
        /// The minimum time expected for this response to be returned.
        /// </summary>
        internal TimeSpan MinExpectedTimeSpan;

        /// <summary>
        /// Internal Response Port
        /// </summary>
        internal PortSet<EmptyValue, LegoResponse, Fault> InternalResponsePort;
    }

    /// <summary>
    /// Indicates an update to the Connection Status.
    /// </summary>
    [DataContract, Description("Indicates an update to the Connection Status.")]
    public class ConnectionStatus
    {
        /// <summary>
        /// The current connection status.
        /// </summary>
        [DataMember, Description("Identifies if the service is currently open and connected to a LEGO NXT brick.")]
        public bool Connected;

    }

    #endregion

    #region Operation Types
    
    /// <summary>
    /// LegoComm Get Operation
    /// </summary>
    [Description("Gets the current LEGO NXT Communications state.")]
    public class Get : Get<GetRequestType, PortSet<NxtCommState, Fault>>
    {
        /// <summary>
        /// LegoComm Get Operation
        /// </summary>
        public Get()
        {
        }
        /// <summary>
        /// LegoComm Get Operation
        /// </summary>
        public Get(Microsoft.Dss.ServiceModel.Dssp.GetRequestType body) : 
                base(body)
        {
        }
        /// <summary>
        /// LegoComm Get Operation
        /// </summary>
        public Get(Microsoft.Dss.ServiceModel.Dssp.GetRequestType body, Microsoft.Ccr.Core.PortSet<NxtCommState,W3C.Soap.Fault> responsePort) : 
                base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// Open communication to the LEGO NXT Hardware
    /// </summary>
    [Description("Open communication to the LEGO NXT Hardware.")]
    public class Open : Submit<OpenRequest, PortSet<DefaultSubmitResponseType, Fault>> { }

    /// <summary>
    /// Close connection to the LEGO NXT Hardware
    /// </summary>
    [Description("Close connection to the LEGO NXT Hardware.")]
    public class Close : Submit<CloseRequest, PortSet<DefaultSubmitResponseType, Fault>> { }

    /// <summary>
    /// Send a command to the LEGO NXT Brick
    /// </summary>
    [Description("Send a LEGO Command to the LEGO NXT Hardware.")]
    public class SendCommand : Submit<SendCommandRequest, PortSet<LegoResponse, Fault>> { }

    /// <summary>
    /// Subscribe to ConnectionUpdate Notifications.
    /// </summary>
    [Description("Subscribe to ConnectionUpdate Notifications.")]
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>> { }

    /// <summary>
    /// Indicates a change to the connection status.
    /// </summary>
    [Description("Indicates a change to the Connection Status.")]
    public class ConnectionUpdate : Update<ConnectionStatus, PortSet<DefaultUpdateResponseType, Fault>> 
    {
        /// <summary>
        /// Indicates a change to the connection status.
        /// </summary>
        public ConnectionUpdate() { }

        /// <summary>
        /// Indicates a change to the connection status.
        /// </summary>
        /// <param name="connected"></param>
        public ConnectionUpdate(bool connected) 
        {
            this.Body.Connected = connected;
        }

    }

    #endregion

    #region Private Data Types

    /// <summary>
    /// Wait for a response on the serial port
    /// </summary>
    class GetResponse
    {
        /// <summary>
        /// A static instance of GetResponse
        /// </summary>
        private static GetResponse _staticInstance = new GetResponse();

        /// <summary>
        /// Return a stance instance of GetResponse
        /// </summary>
        public static GetResponse Instance
        {
            get { return _staticInstance; }
        }
    }

    class CommState
    {
        /// <summary>
        /// A connection to the Serial Port
        /// </summary>
        public SerialPort SerialPort = new SerialPort();

        /// <summary>
        /// Keep track of how many consecutive priority requests have been handled.
        /// </summary>
        public int ConsecutivePriorityRequests = 0;

        public Stack<SendCommandRequest> PendingRequests = new Stack<SendCommandRequest>();

        public int ConsecutiveReadTimeouts = 0;
    }

    #endregion

    /// <summary>
    /// LegoComm Contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// The Dss Service contract
        /// </summary>
        [DataMember, Description("The DSS Contract for the internal LEGO NXT Communications service.")]
        public const String Identifier = "http://schemas.microsoft.com/robotics/2007/07/lego/nxt/comm.user.html";
    }

}
