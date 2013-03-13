//-----------------------------------------------------------------------
//  This file is part of Microsoft Robotics Developer Studio Code Samples.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  $File: NxtIO.cs $ $Revision: 12 $
//-----------------------------------------------------------------------

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using W3C.Soap;

using dssphttp = Microsoft.Dss.Core.DsspHttp;
using brick = Microsoft.Robotics.Services.Sample.Lego.Nxt.Brick;
using submgr = Microsoft.Dss.Services.SubscriptionManager;
using nxtcmd = Microsoft.Robotics.Services.Sample.Lego.Nxt.Commands;
using Microsoft.Robotics.Services.Sample.Lego.Nxt.Common;
using Microsoft.Dss.Core.DsspHttp;


namespace Microsoft.Robotics.Services.Sample.Lego.Nxt.IO
{
    
    /// <summary>
    /// Lego NXT Battery Service
    /// </summary>
    [Contract(Contract.Identifier)]
    [Description("Provides access to programs and files on the LEGO� MINDSTORMS� NXT Brick (v2).")]
    [DisplayName("(User) Lego NXT\u200b Brick I/O (v2)")]
    [DssCategory(brick.LegoCategories.NXT)]
    public class NxtBrickIO : DsspServiceBase
    {
        /// <summary>
        /// _state
        /// </summary>
        //[InitialStatePartner(Optional = true, ServiceUri = ServicePaths.Store + "/Lego.Nxt.v2.IO.config.xml")]
        private NxtIOState _state = new NxtIOState();

        /// <summary>
        /// _main Port
        /// </summary>
        [ServicePort("/lego/nxt/brick/io", AllowMultipleInstances=true)]
        private NxtBrickOnboardOperations _mainPort = new NxtBrickOnboardOperations();

        /// <summary>
        /// Partner with the LEGO NXT Brick
        /// </summary>
        [Partner("brick",
            Contract = brick.Contract.Identifier,
            CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate,
            Optional = false)]
        private brick.NxtBrickOperations _legoBrickPort = new brick.NxtBrickOperations();

        /// <summary>
        /// Default Service Constructor
        /// </summary>
        public NxtBrickIO(DsspServiceCreationPort creationPort) : 
                base(creationPort)
        {
        }

        /// <summary>
        /// Service Start
        /// </summary>
        protected override void Start()
        {
            InitializeState();
            base.Start();
        }

        /// <summary>
        /// Initializate and Validate the state
        /// </summary>
        private void InitializeState()
        {
            if (_state == null)
                _state = new NxtIOState();
        }


        #region Main Port Handlers

        /// <summary>
        /// Get Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> GetHandler(Get get)
        {
            get.ResponsePort.Post(_state);
            yield break;
        }

        /// <summary>
        /// HttpGet Handler
        /// </summary>
        /// <param name="get"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> HttpGetHandler(HttpGet get)
        {
            get.ResponsePort.Post(new dssphttp.HttpResponseType(_state));
            yield break;
        }

        /// <summary>
        /// QueryFiles Handler
        /// </summary>
        /// <param name="queryFiles"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public virtual IEnumerator<ITask> QueryFilesHandler(QueryFiles queryFiles)
        {
            ResponseFiles response = new ResponseFiles();
            response.Files = new List<LegoFile>();
            bool done = false;
            int handle = 0;

            nxtcmd.LegoCommand cmd = new nxtcmd.LegoFindFirst(queryFiles.Body.Filespec);
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                delegate(nxtcmd.LegoResponse ok)
                {
                    nxtcmd.LegoResponseFindFirst ffResponse = nxtcmd.LegoResponse.Upcast<nxtcmd.LegoResponseFindFirst>(ok); 
                    if (ffResponse.Success)
                    {
                        response.Files.Add(new LegoFile(ffResponse.FileName, (int)ffResponse.FileSize));
                        handle = ffResponse.Handle;
                    }
                    else
                        done = true;
                },
                delegate(Fault fault)
                {
                    done = true;
                });

            while (!done)
            {
                cmd = new nxtcmd.LegoFindNext(handle);
                yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                    delegate(nxtcmd.LegoResponse ok)
                    {
                        nxtcmd.LegoResponseFindNext fnResponse = nxtcmd.LegoResponse.Upcast<nxtcmd.LegoResponseFindNext>(ok); 
                        if (fnResponse.Success)
                        {
                            response.Files.Add(new LegoFile(fnResponse.FileName, (int)fnResponse.FileSize));
                            handle = fnResponse.Handle;
                        }
                        else
                            done = true;
                    },
                    delegate(Fault fault)
                    {
                        done = true;
                    });
            }

            _state.ResponseFiles = response;
            queryFiles.ResponsePort.Post(response);
            yield break;
        }



        /// <summary>
        /// SendFile Handler
        /// </summary>
        /// <param name="sendFile"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SendFileHandler(CopyFileToBrick sendFile)
        {
            Fault faultResponse = null;
            bool fileExists = false;
            bool done = false;

            #region See if the file is already on the LEGO NXT.

            nxtcmd.LegoCommand cmd = new nxtcmd.LegoFindFirst(sendFile.Body.FileName);
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                delegate(nxtcmd.LegoResponse ok)
                {
                    nxtcmd.LegoResponseFindFirst ffResponse = nxtcmd.LegoResponse.Upcast<nxtcmd.LegoResponseFindFirst>(ok); 
                    if (ffResponse.Success && (ffResponse.FileName == sendFile.Body.FileName))
                    {
                        fileExists = true;
                        bool fileMatches = (sendFile.Body.FileData != null && sendFile.Body.FileData.Length == ffResponse.FileSize);
                        if (!sendFile.Body.ReplaceExistingFile)
                        {
                            string msg = ((fileMatches) ? "A matching file" : "A different file with the same name") + " already exists on the LEGO NXT Brick.";
                            sendFile.ResponsePort.Post(Fault.FromException(new System.IO.IOException(msg)));
                            done = true;
                        }
                    }
                },
                delegate(Fault fault)
                {
                    sendFile.ResponsePort.Post(fault);
                    done = true;
                });

            #endregion

            if (done)
                yield break;

            if (fileExists)
            {
                #region Remove the existing file

                cmd = new nxtcmd.LegoDelete(sendFile.Body.FileName);
                yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                    EmptyHandler<nxtcmd.LegoResponse>,
                    delegate(Fault fault)
                    {
                        done = true;
                        sendFile.ResponsePort.Post(fault);
                    });
                #endregion
            }

            if (done)
                yield break;


            #region Upload the file to the Brick.

            byte[] fileData = sendFile.Body.FileData;
            string fileName = sendFile.Body.FileName;
            if (fileData != null)
            {
                int pgmLength = fileData.Length;
                int handle = -1;
                LogInfo(LogGroups.Console, "Downloading " + fileName + " to LEGO NXT.");

                PortSet<nxtcmd.LegoResponse, Fault> responsePort = new PortSet<nxtcmd.LegoResponse, Fault>();
                done = false;
                int trial = 0;
                while (trial < 5 && !done)
                {
                    nxtcmd.LegoOpenWriteLinear openWrite = new nxtcmd.LegoOpenWriteLinear(fileName, pgmLength);
                    responsePort = _legoBrickPort.SendNxtCommand(openWrite);
                    yield return Arbiter.Choice(
                        Arbiter.Receive<nxtcmd.LegoResponse>(false, responsePort,
                            delegate(nxtcmd.LegoResponse response)
                            {
                                nxtcmd.LegoResponseOpenWriteLinear responseOpenWriteLinear = new nxtcmd.LegoResponseOpenWriteLinear(response.CommandData);
                                if (response.ErrorCode == LegoErrorCode.Success || response.ErrorCode == LegoErrorCode.FileExists)
                                {
                                    handle = responseOpenWriteLinear.Handle;
                                    done = true;
                                }
                                else if (response.ErrorCode == LegoErrorCode.NoSpace)
                                {
                                    faultResponse = Fault.FromException(new System.IO.IOException("Out of space on LEGO NXT Brick.\nPlease remove one or more LEGO NXT programs on the NXT Brick."));
                                    trial = 999;
                                }
                                else
                                {
                                    faultResponse = Fault.FromException(new System.IO.IOException("Error preparing to upload " + fileName + " file to the LEGO NXT: " + response.ErrorCode.ToString()));
                                }
                            }),
                        Arbiter.Receive<Fault>(false, responsePort,
                            delegate(Fault fault)
                            {
                                faultResponse = fault;
                            }));

                    trial++;
                }

                if (!done)
                {
                    if (faultResponse == null)
                        faultResponse = Fault.FromException(new System.IO.IOException("Failed to create the new file"));
                    sendFile.ResponsePort.Post(faultResponse);
                    yield break;
                }

                done = false;
                trial = 0;
                while (trial < 5 && !done)
                {
                    nxtcmd.LegoWrite legoWrite = new nxtcmd.LegoWrite(handle, fileData);
                    responsePort = _legoBrickPort.SendNxtCommand(legoWrite);
                    yield return Arbiter.Choice(
                        Arbiter.Receive<nxtcmd.LegoResponse>(false, responsePort,
                            delegate(nxtcmd.LegoResponse response)
                            {
                                nxtcmd.LegoResponseWrite responseWrite = new nxtcmd.LegoResponseWrite(response.CommandData);
                                if (response.ErrorCode == LegoErrorCode.Success)
                                {
                                    if (pgmLength != responseWrite.BytesWritten)
                                        LogWarning(LogGroups.Console, "Warning: " + fileName + " file length on LEGO NXT does not match the PC.");
                                    done = true;
                                }
                                else
                                {
                                    faultResponse = Fault.FromException(new System.IO.IOException("Error sending " + fileName + " file to the LEGO NXT: " + response.ErrorCode.ToString()));
                                    LogError(faultResponse);
                                }
                            }),
                        Arbiter.Receive<Fault>(false, responsePort,
                            delegate(Fault fault)
                            {
                                faultResponse = fault;
                                LogError(LogGroups.Console, "Timed out sending " + fileName + " file to the LEGO NXT");
                            }));

                    trial++;
                }

                if (!done)
                {
                    if (faultResponse == null)
                        faultResponse = Fault.FromException(new System.IO.IOException("Failed to write to the new file,"));
                    sendFile.ResponsePort.Post(faultResponse);

                    yield break;
                }


                // Now Close the Write Buffer.
                done = false;

                nxtcmd.LegoClose legoClose = new nxtcmd.LegoClose(handle);
                legoClose.TryCount = 5;
                responsePort = _legoBrickPort.SendNxtCommand(legoClose);
                yield return Arbiter.Choice(
                    Arbiter.Receive<nxtcmd.LegoResponse>(false, responsePort,
                        delegate(nxtcmd.LegoResponse response)
                        {
                            if (response.ErrorCode == LegoErrorCode.Success)
                            {
                                sendFile.ResponsePort.Post(DefaultSubmitResponseType.Instance);
                                done = true;
                            }
                            else
                            {
                                faultResponse = Fault.FromException(new System.IO.IOException("Error closing " + fileName + " file on the LEGO NXT: " + response.ErrorCode.ToString()));
                                LogError(faultResponse);
                            }
                        }),
                    Arbiter.Receive<Fault>(false, responsePort,
                        delegate(Fault fault)
                        {
                            faultResponse = fault;
                            LogError(LogGroups.Console, "Timed out closing file during SendFile.");
                        }));

            }
            else
            {
                faultResponse = Fault.FromException(new ArgumentNullException("The source file was not provided"));
            }

            if (done)
                yield break;

            if (faultResponse == null)
                faultResponse = Fault.FromException(new System.IO.IOException("Failed to write to the new file,"));

            sendFile.ResponsePort.Post(faultResponse);

            #endregion

            yield break;
        }


        /// <summary>
        /// StopLegoProgram Handler
        /// </summary>
        /// <param name="stopLegoProgram"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> StopLegoProgramHandler(StopProgram stopLegoProgram)
        {
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(new nxtcmd.LegoStopProgram()),
                delegate(nxtcmd.LegoResponse ok)
                {
                    stopLegoProgram.ResponsePort.Post(DefaultSubmitResponseType.Instance);
                },
                delegate(Fault fault)
                {
                    stopLegoProgram.ResponsePort.Post(fault);
                });

            yield break;
        }

        
        /// <summary>
        /// StartLegoProgram Handler
        /// </summary>
        /// <param name="startLegoProgram"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> StartLegoProgramHandler(StartProgram startLegoProgram)
        {
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(
                new nxtcmd.LegoStartProgram(startLegoProgram.Body.Program)),
                delegate(nxtcmd.LegoResponse ok)
                {
                    startLegoProgram.ResponsePort.Post(DefaultSubmitResponseType.Instance);
                },
                delegate(Fault fault)
                {
                    startLegoProgram.ResponsePort.Post(fault);
                });
            yield break;
        }

        
        /// <summary>
        /// QueryRunningLegoProgram Handler
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> QueryRunningLegoProgramHandler(QueryRunningProgram query)
        {
            RunningProgramResponse response = new RunningProgramResponse();

            nxtcmd.LegoGetCurrentProgramName cmd = new nxtcmd.LegoGetCurrentProgramName();
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                delegate(nxtcmd.LegoResponse ok)
                {
                    nxtcmd.LegoResponseGetCurrentProgramName queryResponse = nxtcmd.LegoResponse.Upcast<nxtcmd.LegoResponseGetCurrentProgramName>(ok);
                    if (queryResponse.Success)
                    {
                        response.Program = queryResponse.FileName;
                        query.ResponsePort.Post(response);
                    }
                    else
                    {
                        query.ResponsePort.Post(Fault.FromException(new System.IO.FileNotFoundException(queryResponse.ErrorCode.ToString())));
                    }
                },
                delegate(Fault fault)
                {
                    query.ResponsePort.Post(fault);
                });

            yield break;
        }
        
        /// <summary>
        /// DeleteFile Handler
        /// </summary>
        /// <param name="deleteFile"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> DeleteFileHandler(DeleteFile deleteFile)
        {

            nxtcmd.LegoDelete cmd = new nxtcmd.LegoDelete(deleteFile.Body.FileName);
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                delegate(nxtcmd.LegoResponse ok)
                {
                    if (ok.Success || ok.ErrorCode == LegoErrorCode.FileNotFound)
                    {
                        deleteFile.ResponsePort.Post(DefaultSubmitResponseType.Instance);
                    }
                    else
                    {
                        deleteFile.ResponsePort.Post(Fault.FromException(new System.IO.IOException(ok.ErrorCode.ToString())));
                    }
                },
                delegate(Fault fault)
                {
                    deleteFile.ResponsePort.Post(fault);
                });

            yield break;
        }

        /// <summary>
        /// SetBrickName Handler
        /// </summary>
        /// <param name="setBrickName"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SetBrickNameHandler(SetBrickName setBrickName)
        {
            bool success = false;
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(
                new nxtcmd.LegoSetBrickName(setBrickName.Body.BrickName)),
                delegate(nxtcmd.LegoResponse ok)
                {
                    success = true;
                },
                delegate(Fault fault)
                {
                    setBrickName.ResponsePort.Post(fault);
                });

            if (success)
            {
                // Wait for the name to be written.
                yield return Arbiter.Receive<DateTime>(false, TimeoutPort(100), EmptyHandler<DateTime>);
                setBrickName.ResponsePort.Post(DefaultSubmitResponseType.Instance);
            }
            yield break;
        }


        /// <summary>
        /// QueryBrickName Handler
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> QueryBrickNameHandler(QueryBrickName query)
        {
            BrickNameResponse response = new BrickNameResponse();

            nxtcmd.LegoGetDeviceInfo cmd = new nxtcmd.LegoGetDeviceInfo();
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                delegate(nxtcmd.LegoResponse ok)
                {
                    nxtcmd.LegoResponseGetDeviceInfo queryResponse = nxtcmd.LegoResponse.Upcast<nxtcmd.LegoResponseGetDeviceInfo>(ok);
                    if (queryResponse.Success)
                    {
                        response.BrickName = queryResponse.BrickName;
                        query.ResponsePort.Post(response);
                    }
                    else
                    {
                        query.ResponsePort.Post(Fault.FromException(new System.IO.FileNotFoundException(queryResponse.ErrorCode.ToString())));
                    }
                },
                delegate(Fault fault)
                {
                    query.ResponsePort.Post(fault);
                });

            yield break;
        }

        /// <summary>
        /// SendBluetoothMessage Handler
        /// </summary>
        /// <param name="sendBluetoothMessage"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> SendBluetoothMessageHandler(SendBluetoothMessage sendBluetoothMessage)
        {
            int inbox = Math.Max(1, Math.Min(10, sendBluetoothMessage.Body.Mailbox)) - 1;
            nxtcmd.LegoMessageWrite cmd = new nxtcmd.LegoMessageWrite(inbox, sendBluetoothMessage.Body.Message);
            cmd.RequireResponse = true;
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                delegate(nxtcmd.LegoResponse ok)
                {
                    sendBluetoothMessage.ResponsePort.Post(DefaultSubmitResponseType.Instance);
                },
                delegate(Fault fault)
                {
                    sendBluetoothMessage.ResponsePort.Post(fault);
                });

            yield break;
        }


        /// <summary>
        /// ReceiveBluetoothMessage Handler
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual IEnumerator<ITask> ReceiveBluetoothMessageHandler(ReceiveBluetoothMessage query)
        {
            BluetoothMessage response = new BluetoothMessage();
            int inbox = Math.Max(1, Math.Min(10, query.Body.Mailbox)) - 1;

            nxtcmd.LegoMessageRead cmd = new nxtcmd.LegoMessageRead(inbox + 10, inbox, true);
            yield return Arbiter.Choice(_legoBrickPort.SendNxtCommand(cmd),
                delegate(nxtcmd.LegoResponse ok)
                {
                    nxtcmd.LegoResponseMessageRead queryResponse = nxtcmd.LegoResponse.Upcast<nxtcmd.LegoResponseMessageRead>(ok); 
                    if (queryResponse.Success)
                    {
                        response.Mailbox = query.Body.Mailbox;
                        response.Message = queryResponse.Message;
                        query.ResponsePort.Post(response);
                    }
                    else
                    {
                        query.ResponsePort.Post(Fault.FromException(new System.IO.IOException(queryResponse.ErrorCode.ToString())));
                    }
                },
                delegate(Fault fault)
                {
                    query.ResponsePort.Post(fault);
                });

            yield break;
        }
     

        #endregion
    }
}
