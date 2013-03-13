//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtComm.cs $ $Revision: 21 $
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.IO.Ports;
using System.Text;
using W3C.Soap;
using System.IO;

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using submgr = Microsoft.Dss.Services.SubscriptionManager;

namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.Comm
{
    
    /// <summary>
    /// Implementation class for LegoComm.
    /// All communication with the LEGO Hardware is done through this service.
    /// This service is started automatically by the LEGO NXT Brick service.
    /// </summary>    
    /// <remarks>The LEGO Communications service calls to the serial port and may block a thread
    /// The ActivationSettings attribute with ShareDispatch = false makes the runtime
    /// dedicate a dispatcher thread pool just for this service.</remarks>
    [ActivationSettings(ShareDispatcher = false, ExecutionUnitsPerDispatcher = 1)]
    [DisplayName("(User) LegoNxtComm")]
    [Description("Provides access to the LEGO� MINDSTORMS� NXT Communications Service which is responsible for all communications with the LEGO NXT Hardware. \n"
        + "*** INTERNAL - DO NOT START THIS SERVICE DIRECTLY ***")]
    [Contract(Contract.Identifier)]
    [DssCategory(LegoCategories.NXT)]
    [DssCategory(DssCategories.Infrastructure)]
    public class LegoCommService : DsspServiceBase
    {
        /// <summary>
        /// Lego Communications Service State
        /// </summary>
        [InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.Comm.config.xml")]
        private NxtCommState _state = new NxtCommState();
        
        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/comm", AllowMultipleInstances = true)]
        private LegoCommOperations _mainPort = new LegoCommOperations();

        /// <summary>
        /// Subscription manager partner for Ultrasonic Sensor
        /// </summary>
        [Partner(Partners.SubscriptionManagerString, Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        submgr.SubscriptionManagerPort _subMgrPort = new submgr.SubscriptionManagerPort();

        #region Private Members

        /// <summary>
        /// Buffer used for bluetooth header communications
        /// </summary>
        private byte[] buffer;

        /// <summary>
        /// A connection to the Serial Port
        /// </summary>
        private CommState _commState = new CommState();

        /// <summary>
        /// Port for processing standard requests which expect a response.
        /// </summary>
        private Port<SendCommand> _legoRequestResponsePort = new Port<SendCommand>();
        /// <summary>
        /// Port for processing Priority requests which expect a response.
        /// </summary>
        private Port<SendCommand> _legoPriorityRequestResponsePort = new Port<SendCommand>();

        #region Low-level Communications Ports

        /// <summary>
        /// Port for sending data to the low-level communications.
        /// </summary>
        private Port<SendCommand> _commSendImmediatePort = new Port<SendCommand>();

        /// <summary>
        /// Port for opening and closing the low-level communications.
        /// </summary>
        private PortSet<Open, Close> _commOpenClosePort = new PortSet<Open, Close>();

        /// <summary>
        /// Port which indicates there is receive data pending on the low-level communications.
        /// </summary>
        private Port<GetResponse> _commGetResponsePort = new Port<GetResponse>();

        #endregion

        #region Timing and Statistics
        private Stack<System.Diagnostics.Stopwatch> _stopwatches = new Stack<System.Diagnostics.Stopwatch>();
        private List<CodeTimer> _CodeTimerStats = new List<CodeTimer>();
        private int _saveInterval = 0;
        struct CodeTimer
        {
            public CodeTimer(LegoCommandCode commandCode, double uSec)
            {
                CommandCode = commandCode;
                USec = uSec;
            }
            public LegoCommandCode CommandCode;
            public double USec;
        }
        #endregion

        #endregion


        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public LegoCommService(DsspServiceCreationPort creationPort) 
            : base(creationPort)
        {
        }

        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            ValidateState();
            base.Start();

            Activate(new Interleave(
                new ExclusiveReceiverGroup(
                    Arbiter.Receive<Close>(true, _commOpenClosePort, CommCloseHandler),
                    Arbiter.ReceiveWithIterator<Open>(true, _commOpenClosePort, CommOpenHandler),
                    Arbiter.ReceiveWithIterator(true, _commSendImmediatePort, CommSendImmediateHandler),
                    Arbiter.ReceiveWithIterator(true, _commGetResponsePort, CommGetResponseHandler)
                ), new ConcurrentReceiverGroup()));
        }

        #region Main Operation Port Handlers

        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetHandler(Get get)
        {
            LogInfo(LogGroups.Console, "Processing Communication Stats");
            ProcessCommunicationStats();

            get.ResponsePort.Post(_state);
            SaveState(_state);
            yield break;
        }

        /// <summary>
        /// Open a serial port.
        /// </summary>
        /// <param name="open"></param>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void OpenHandler(Open open)
        {
            _commOpenClosePort.Post(open);
        }

        /// <summary>
        /// Close the connection to a serial port.
        /// </summary>
        /// <param name="close"></param>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual void CloseHandler(Close close)
        {
            _commOpenClosePort.Post(close);
        }

        /// <summary>
        /// Issue a command directly to the serial port
        /// </summary>
        /// <param name="sendCommand"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SendCommandHandler(SendCommand sendCommand)
        {


            if (buffer == null || _commState.SerialPort == null || !_commState.SerialPort.IsOpen)
            {
                sendCommand.ResponsePort.Post(Fault.FromException(new InvalidOperationException("Not Connected")));
                yield break;
            }
            if (sendCommand.Body == null || sendCommand.Body.LegoCommand == null)
            {
                sendCommand.ResponsePort.Post(Fault.FromException(new InvalidOperationException("Invalid LEGO Command")));
                yield break;
            }

            // Dispatch again to one of three different ports:
            // If no response is required, send the command immediately
            // Otherwise queue into a low or high priority queue to
            // be handled one command/response at a time.
            if (!sendCommand.Body.LegoCommand.RequireResponse)
                _commSendImmediatePort.Post(sendCommand);
            else if (sendCommand.Body.PriorityRequest)
                _legoPriorityRequestResponsePort.Post(sendCommand);
            else
                _legoRequestResponsePort.Post(sendCommand);

            yield break;
        }

        /// <summary>
        /// Drop Handler
        /// </summary>
        /// <param name="drop"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Teardown)]
        public virtual IEnumerator<ITask> DropHandler(DsspDefaultDrop drop)
        {
            // Close the serial port
            Close close = new Close();
            _commOpenClosePort.Post(close);
            yield return Arbiter.Choice(close.ResponsePort,
                EmptyHandler<DefaultSubmitResponseType>,
                EmptyHandler<Fault>);

            drop.ResponsePort.Post(DefaultDropResponseType.Instance);
            yield break;
        }

        /// <summary>
        /// Subscribe Handler
        /// </summary>
        /// <param name="subscribe"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> SubscribeHandler(Subscribe subscribe)
        {
            base.SubscribeHelper(_subMgrPort, subscribe.Body, subscribe.ResponsePort);
            yield break;
        }

        /// <summary>
        /// ConnectionUpdate is a notification
        /// </summary>
        /// <param name="update"></param>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual void UpdateHandler(ConnectionUpdate update)
        {
            throw new InvalidOperationException("ConnectionUpdate is a notification and not valid for external requests.");
        }

        #endregion

        /// <summary>
        /// Handle Requests with Responses
        /// </summary>
        /// <param name="sendCommand"></param>
        /// <returns></returns>
        private IEnumerator<ITask> RequestResponseHandler(SendCommand sendCommand)
        {
            // Keep track of consecutive priority requests.
            if (sendCommand.Body.PriorityRequest)
                _commState.ConsecutivePriorityRequests++;
            else
                _commState.ConsecutivePriorityRequests = 0;

            try
            {
                Fault faultResponse = null;

                // Process Priority Request

                // Set up an intermediate response port.
                sendCommand.Body.InternalResponsePort = new PortSet<EmptyValue, LegoResponse, Fault>();
                sendCommand.Body.Stopwatch = GetStopwatch();

                // Send the command to the serial port
                _commSendImmediatePort.Post(sendCommand);

                // Wait for the data to be sent
                yield return Arbiter.Choice(
                    Arbiter.Receive<EmptyValue>(false, sendCommand.Body.InternalResponsePort, EmptyHandler<EmptyValue>),
                    Arbiter.Receive<Fault>(false, sendCommand.Body.InternalResponsePort,
                    delegate(Fault fault)
                    {
                        faultResponse = fault;
                    }));

                // early exit
                if (faultResponse != null)
                {
                    RetireStopwatch(ref sendCommand.Body.Stopwatch);
                    sendCommand.ResponsePort.Post(faultResponse);
                    yield break;
                }

                // wait for the response
                yield return Arbiter.Choice(
                    Arbiter.Receive<LegoResponse>(false, sendCommand.Body.InternalResponsePort,
                        delegate(LegoResponse response)
                        {
                            //////Debugger.Break();
                            sendCommand.ResponsePort.Post(response);
                        }),
                    Arbiter.Receive<Fault>(false, sendCommand.Body.InternalResponsePort,
                        delegate(Fault fault)
                        {
                            sendCommand.ResponsePort.Post(fault);
                        }));

                RetireStopwatch(ref sendCommand.Body.Stopwatch);

                yield break;

            }
            finally
            {
                // Decide which request queue is next.
                ActivateNextRequest();
            }
        }

        #region Low Level Communications Handlers

        /// <summary>
        /// Wait for a response on the serial port.
        /// </summary>
        /// <param name="getResponse"></param>
        /// <returns></returns>
        private IEnumerator<ITask> CommGetResponseHandler(GetResponse getResponse)
        {
            
            string errorMessage = null;
            SendCommandRequest cmdRequest = _commState.PendingRequests.Peek();
            if (cmdRequest == null)
                yield break;
            
            #region If the data isn't ready yet, wait a little and try again.
            if (_commState.SerialPort.BytesToRead == 0)
            {
                TimeSpan elapsed = (cmdRequest.Stopwatch == null) ? TimeSpan.MinValue : cmdRequest.Stopwatch.Elapsed;
                TimeSpan remaining;
                if (elapsed != TimeSpan.MinValue 
                    && elapsed < cmdRequest.MinExpectedTimeSpan)
                {
                    // Wait until the minimum expected time for this command.
                    remaining = cmdRequest.MinExpectedTimeSpan.Subtract(elapsed);
                }
                else if (elapsed.TotalSeconds < 1.0)
                {
                    // No data yet, wait 3 milliseconds
                    remaining = new TimeSpan(0, 0, 0, 0, 3);
                }
                else
                {
                    // Timeout has occurred
                    // Remove from the pending list
                    _commState.PendingRequests.Pop();

                    _commState.ConsecutiveReadTimeouts++;
                    if (_commState.ConsecutiveReadTimeouts > 5)
                    {
                        // Several read timeouts in a row means the serial port is no longer connected.
                        _state.Connected = false;
                        SendNotification<ConnectionUpdate>(_subMgrPort, new ConnectionUpdate(_state.Connected));
                    }

                    errorMessage = "Timeout receiving data from the LEGO NXT.";

                    if (cmdRequest.InternalResponsePort != null)
                    {
                        Fault faultResponse = Fault.FromException(new IOException(errorMessage));
                        cmdRequest.InternalResponsePort.Post(faultResponse);
                    }
                    else
                    {
                        LogError(errorMessage);
                    }
                    yield break;
                }

                // Leave the exclusive handler, 
                // but wake up in a little bit and try again.
                Activate(Arbiter.Receive(false, TimeoutPort(remaining),
                    delegate(DateTime timeout)
                    {
                        _commGetResponsePort.Post(GetResponse.Instance);
                    }));
                yield break;

            }

            // When data starts to come in, clear the read timeout counter.
            if (_commState.ConsecutiveReadTimeouts > 0)
                _commState.ConsecutiveReadTimeouts = 0;

            #endregion

            // See if there is data on the serial port.
            // if not, post back to _commGetResponsePort
            // otherwise read it and send it back
            LegoCommand cmd = cmdRequest.LegoCommand;
            bool resetComm = false;
            try
            {
                int packetSize = cmd.ExpectedResponseSize;
                if (_state.ConnectOverBluetooth)
                {
                    packetSize = _commState.SerialPort.ReadByte() + (_commState.SerialPort.ReadByte() * 256);
                    if (packetSize != cmd.ExpectedResponseSize)
                    {
                        errorMessage = "Bluetooth header does not match the expected LEGO Command response size.";
                        resetComm = true;
                    }
                }

                // Read the data and get a response packet.
                byte[] receiveData = new byte[packetSize];
                _commState.SerialPort.Read(receiveData, 0, packetSize);

                LogInfo("Received Data -------------------------------------");
                for (int i = 0; i < receiveData.Length; i++)
                    LogInfo(receiveData[i].ToString(CultureInfo.InvariantCulture));

                #region Timing Stats
                if (cmdRequest.Stopwatch != null)
                {
                    cmdRequest.Stopwatch.Stop();
                    _CodeTimerStats.Add(new CodeTimer(cmd.LegoCommandCode, cmdRequest.Stopwatch.Elapsed.TotalMilliseconds * 1000.0));
                    if (_CodeTimerStats.Count > 20)
                        ProcessCommunicationStats();
                }
                #endregion

                LegoResponse legoReceive = cmd.GetResponse(receiveData);

                byte commandType = (byte)(receiveData[0] & 0x7F);

                // Is this a valid starting type?
                if (commandType != 0x00 && commandType != 0x01 && commandType != 0x02)
                {
                    errorMessage = string.Format("Invalid LEGO response command: {0}", commandType);
                    resetComm = true;
                }
                else  
                {
                    // Data is received successfully
                    // Remove from the pending list
                    _commState.PendingRequests.Pop();

                    if (!_state.Connected)
                    {
                        _state.Connected = true;
                        SendNotification<ConnectionUpdate>(_subMgrPort, new ConnectionUpdate(_state.Connected));
                    }

                    if (cmdRequest.InternalResponsePort != null)
                        cmdRequest.InternalResponsePort.Post(legoReceive);

                    yield break;
                }
            }
            catch (ArgumentException ex)
            {
                resetComm = true;
                if (ex.Message == "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.")
                    errorMessage = "A connection error occured which may be caused by an invalid Baud Rate";
                else
                    errorMessage = "A connection error occured while accessing the LEGO NXT serial port";
            }
            catch (TimeoutException)
            {
                // Ignore timeouts for now.

                errorMessage = "Timeout reading from LEGO NXT brick";
                resetComm = true;
            }
            catch (IOException ex)
            {
                errorMessage = string.Format("Error reading from the serial port in NxtComm(): {0}", ex);
                resetComm = true;
            }
            catch (Exception ex)
            {
                errorMessage = string.Format("Error reading from the serial port in NxtComm(): {0}", ex);
                resetComm = true;
            }

            // Some error has occurred
            // Remove from the pending list
            _commState.PendingRequests.Pop();

            if (resetComm)
            {
                // A serious error occurred
                LogInfo(LogGroups.Console, "Resetting LEGO Serial Port");

                // Wait for remaining bytes
                yield return Arbiter.Receive(false, TimeoutPort(300), delegate(DateTime timeout) { });

                // Clear the serial port buffer
                _commState.SerialPort.DiscardInBuffer();
            }

            if (string.IsNullOrEmpty(errorMessage))
                errorMessage = "Invalid or missing response data from LEGO NXT.";

            if (cmdRequest.InternalResponsePort != null)
            {
                Fault faultResponse = Fault.FromException(new IOException(errorMessage));
                cmdRequest.InternalResponsePort.Post(faultResponse);
            }
            else
            {
                LogError(errorMessage);
            }

            yield break;
        }

        /// <summary>
        /// Send data immediately to the serial port.
        /// </summary>
        /// <param name="sendCommand"></param>
        /// <returns></returns>
        private IEnumerator<ITask> CommSendImmediateHandler(SendCommand sendCommand)
        {
           
            Fault fault = null;
            if (_commState.SerialPort == null
                || !_commState.SerialPort.IsOpen)
            {
                fault = Fault.FromException(new NullReferenceException("The Lego Serial Port is disconnected."));
            }
            else if (sendCommand == null
                || sendCommand.Body == null
                || sendCommand.Body.LegoCommand == null
                || sendCommand.Body.LegoCommand.CommandData == null
                || sendCommand.Body.LegoCommand.CommandData.Length == 0)
            {
                fault = Fault.FromException(new NullReferenceException("Invalid Lego Command"));
            }

            if (fault != null)
            {
                if (sendCommand.Body.InternalResponsePort != null)
                    sendCommand.Body.InternalResponsePort.Post(fault);
                else if (sendCommand.ResponsePort != null)
                    sendCommand.ResponsePort.Post(fault);

                yield break;
            }
            byte[] data = sendCommand.Body.LegoCommand.CommandData;
            LogInfo("Sending Data -------------------------------------");
            for (int i = 0; i < data.Length; i++)
                LogInfo(data[i].ToString(CultureInfo.InvariantCulture));
            int packetLength = data.Length;

            try
            {
                if ((sendCommand.Body.LegoCommand.RequireResponse)
                    && (sendCommand.Body.Stopwatch != null))
                {
                    sendCommand.Body.Stopwatch.Start();
                }

                if (_state.ConnectOverBluetooth)
                {
                    // Add the bluetooth packet length 
                    buffer[0] = (byte)(packetLength % 256);
                    buffer[1] = (byte)(packetLength / 256);

                    // Write the bluetooth header
                    _commState.SerialPort.Write(buffer, 0, 2);
                }

                _commState.SerialPort.Write(data, 0, packetLength);

                if (sendCommand.Body.LegoCommand.RequireResponse)
                {
                    // calculate the minimum expected response time for this command.
                    sendCommand.Body.MinExpectedTimeSpan = CalcMinWaitTimeSpan(sendCommand.Body.LegoCommand.LegoCommandCode);
                    _commState.PendingRequests.Push(sendCommand.Body);
                    if (_commState.PendingRequests.Count == 1)
                    {
                        //Debugger.Break();
                        _commGetResponsePort.Post(GetResponse.Instance);
                    }
                        

                    // Signal intermediate completion
                    if (sendCommand.Body.InternalResponsePort != null)
                        sendCommand.Body.InternalResponsePort.Post(EmptyValue.SharedInstance);
                }
                else
                {
                    // Send an empty success response.
                    LegoResponse response = sendCommand.Body.LegoCommand.GetResponse(null);
                    sendCommand.ResponsePort.Post(response);
                }
            }
            catch (Exception ex)
            {
                if (ex is System.TimeoutException)
                {
                    // A write timeout means the serial port is no longer connected.
                    _state.Connected = false;
                    SendNotification<ConnectionUpdate>(_subMgrPort, new ConnectionUpdate(_state.Connected));
                }

                fault = Fault.FromException(ex);
                if (sendCommand.Body.LegoCommand.RequireResponse)
                {
                    if (sendCommand.Body.InternalResponsePort != null)
                        sendCommand.Body.InternalResponsePort.Post(fault);
                }
                else // respond directly to the source
                {
                    sendCommand.ResponsePort.Post(fault);
                }
            }
        }

        /// <summary>
        /// Close the serial port
        /// </summary>
        /// <param name="close"></param>
        private void CommCloseHandler(Close close)
        {
            try
            {
                if (_commState.SerialPort != null)
                {
                    if (_commState.SerialPort.IsOpen)
                    {
                        _commState.SerialPort.Close();
                        _state.Connected = false;
                        SendNotification<ConnectionUpdate>(_subMgrPort, new ConnectionUpdate(_state.Connected));
                    }
                }

                #region Respond to all pending requests and responses 

                Fault closingFault = Fault.FromException(new IOException("Connection is closing"));
                SendCommand pendingCommand;

                // respond to pending priority requests
                while (_legoPriorityRequestResponsePort.ItemCount > 0)
                {
                    _legoPriorityRequestResponsePort.Test(out pendingCommand);
                    if (pendingCommand != null && pendingCommand.ResponsePort != null)
                        pendingCommand.ResponsePort.Post(closingFault);
                }

                // respond to pending standard requests
                while (_legoRequestResponsePort.ItemCount > 0)
                {
                    _legoRequestResponsePort.Test(out pendingCommand);
                    if (pendingCommand != null && pendingCommand.ResponsePort != null)
                        pendingCommand.ResponsePort.Post(closingFault);
                }

                // respond to pending responses
                while (_commState.PendingRequests.Count > 0)
                {
                    SendCommandRequest pendingRequest = _commState.PendingRequests.Pop();
                    if (pendingRequest.InternalResponsePort != null)
                        pendingRequest.InternalResponsePort.Post(closingFault);
                }
                #endregion

                _commState.ConsecutivePriorityRequests = 0;

                if (close != null && close.ResponsePort != null)
                    close.ResponsePort.Post(DefaultSubmitResponseType.Instance);
            }
            catch (Exception ex)
            {
                if (close != null && close.ResponsePort != null)
                    close.ResponsePort.Post(Fault.FromException(ex));
            }
        }

        /// <summary>
        /// Open the Serial Port
        /// </summary>
        /// <param name="open"></param>
        private IEnumerator<ITask> CommOpenHandler(Open open)
        {
            //////Debugger.Break();
            Fault responseFault = null;
            bool connected = false;

            try
            {
                if (buffer == null)
                    buffer = new byte[2];

                if (open.Body.BaudRate < 1200)
                    open.Body.BaudRate = 115200;

                _state.ConnectOverBluetooth = (open.Body.ConnectionType == LegoConnectionType.Bluetooth);

                CloseSerialPort();

                if (open.Body.SerialPort > 0)
                {
                    string portName = "COM" + open.Body.SerialPort.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                    if (_commState.SerialPort != null)
                    {
                        try
                        {
                            _commState.SerialPort.Dispose();
                        }
                        catch(Exception ex)
                        {
                            LogError(ex);
                        }
                        yield return Arbiter.Receive(false, TimeoutPort(300), EmptyHandler<DateTime>);
                        _commState.SerialPort = null;
                    }

                    try
                    {
                        _commState.SerialPort = new SerialPort(portName, open.Body.BaudRate);
                        _commState.SerialPort.Encoding = Encoding.Default;
                        _commState.SerialPort.Parity = Parity.None;
                        _commState.SerialPort.DataBits = 8;
                        _commState.SerialPort.StopBits = StopBits.One;
                        _commState.SerialPort.WriteTimeout = 2000;
                        _commState.SerialPort.ReadTimeout = 2000;
                        _commState.SerialPort.Handshake = Handshake.RequestToSend;

                        int tryCount = 0;
                        Fault fault = null;
                        while (!_commState.SerialPort.IsOpen && tryCount < 4)
                        {
                            try
                            {
                                tryCount++;
                                _commState.SerialPort.Open();
                            }
                            catch (Exception ex)
                            {
                                fault = Fault.FromException(ex);
                            }
                        }
                        if (_commState.SerialPort.IsOpen)
                        {
                            connected = true;

                            // Wait for a request/response
                            ActivateNextRequest();
                        }
                        else
                        {
                            LogError(LogGroups.Console, "Invalid Serial Port", fault);
                            responseFault = fault;
                        }
                    }
                    catch (Exception ex)
                    {
                        responseFault = Fault.FromException(ex);
                    }

                }
                else
                {
                    responseFault = Fault.FromException(new ArgumentException("The LEGO NXT Serial Port is not specified"));
                }
            }
            finally
            {
                if (connected)
                {
                    open.ResponsePort.Post(DefaultSubmitResponseType.Instance);
                }
                else
                {
                    if (responseFault == null)
                        responseFault = Fault.FromException(new ArgumentException("Failed to connect to the LEGO NXT Serial Port."));

                    open.ResponsePort.Post(responseFault);
                }

            }
            yield break;
        }

        #endregion

        #region Subroutines

        /// <summary>
        /// Ready for the next request.
        /// Decide if it should be a priority request, 
        /// a standard request, or either of the two.
        /// </summary>
        private void ActivateNextRequest()
        {
            // Decide which request queue is next.
            if (_commState.ConsecutivePriorityRequests >= 2
                && _legoRequestResponsePort.ItemCount > 0)
            {
                // We've processed at least two priority requests
                // Take the next standard request
                Activate(Arbiter.ReceiveWithIterator(false, _legoRequestResponsePort, RequestResponseHandler));
            }
            else if (_legoPriorityRequestResponsePort.ItemCount > 0)
            {
                // There is a priority request waiting, take it now.
                Activate(Arbiter.ReceiveWithIterator(false, _legoPriorityRequestResponsePort, RequestResponseHandler));
            }
            else
            {
                // take the next available request
                Activate(Arbiter.Choice(
                    Arbiter.ReceiveWithIterator(false, _legoPriorityRequestResponsePort, RequestResponseHandler),
                    Arbiter.ReceiveWithIterator(false, _legoRequestResponsePort, RequestResponseHandler)));
            }
        }

        /// <summary>
        /// Close the serial port immediately
        /// </summary>
        /// <remarks>
        /// WARNING: This may only be called from an exclusive low-level communication handler!
        /// </remarks>
        private void CloseSerialPort()
        {
            try
            {
                CommCloseHandler(null);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        #region Timing and Statistics

        /// <summary>
        /// Determine how long we should wait before reading a command response on the serial port.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private TimeSpan CalcMinWaitTimeSpan(LegoCommandCode code)
        {
            // If this command hasn't been run before, get a baseline.
            if (!_state.RuntimeStatistics.ContainsKey(code))
                return new TimeSpan((long)1);

            long ticks = (long)(_state.RuntimeStatistics[code].MinimumMicroseconds * 10.0);
            long ms = ticks / 10000;

            if (ms < 5)  // min -1/2 ms
                return new TimeSpan(ticks - 5000);

            if (ms < 20) // min -1 ms
                return new TimeSpan(ticks - 10000);

            // min -5 ms
            return new TimeSpan(ticks - 50000);
        }


        /// <summary>
        /// Get a stopwatch from the pool.
        /// </summary>
        /// <returns></returns>
        private System.Diagnostics.Stopwatch GetStopwatch()
        {
            System.Diagnostics.Stopwatch stopwatch;
            lock (_stopwatches)
            {
                if (_stopwatches.Count == 0)
                    stopwatch = new System.Diagnostics.Stopwatch();
                else
                    stopwatch = _stopwatches.Pop();
            }
            return stopwatch;
        }

        /// <summary>
        /// Return a stopwatch to the pool.
        /// </summary>
        /// <param name="stopwatch"></param>
        private void RetireStopwatch(ref System.Diagnostics.Stopwatch stopwatch)
        {
            if (stopwatch != null)
            {
                lock (_stopwatches)
                {
                    stopwatch.Reset();
                    _stopwatches.Push(stopwatch);
                }
                stopwatch = null;
            }
        }


        /// <summary>
        /// Process pending communication stats.
        /// </summary>
        private void ProcessCommunicationStats()
        {
            List<CodeTimer> processStats;
            lock (_CodeTimerStats)
            {
                processStats = new List<CodeTimer>(_CodeTimerStats);
                _CodeTimerStats.Clear();
            }
            foreach (CodeTimer ct in processStats)
            {
                LegoCommandStat stat;
                if (!_state.RuntimeStatistics.ContainsKey(ct.CommandCode))
                    _state.RuntimeStatistics[ct.CommandCode] = new LegoCommandStat(ct.CommandCode);

                stat = _state.RuntimeStatistics[ct.CommandCode];
                stat.Count++;
                stat.TotalMicroseconds += ct.USec;
                stat.AverageMicroseconds = stat.TotalMicroseconds / stat.Count;
                if (stat.MinimumMicroseconds == 0 || stat.MinimumMicroseconds > ct.USec)
                    stat.MinimumMicroseconds = ct.USec;
            }
            _saveInterval++;
            if (_saveInterval > 30)
            {
                SaveState(_state);
                _saveInterval = 0;
            }

        }

        #endregion

        /// <summary>
        /// Validate Startup State
        /// </summary>
        private void ValidateState()
        {
            if (_state == null)
                _state = new NxtCommState();
            if (_state.RuntimeStatistics == null)
                _state.RuntimeStatistics = new Dictionary<LegoCommandCode, LegoCommandStat>();
            _state.Connected = false;
        }

        #endregion
    }
}
