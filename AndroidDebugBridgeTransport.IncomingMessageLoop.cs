/*
* MIT License
* 
* Copyright (c) 2024 The DuoWOA authors
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AndroidDebugBridge
{
    public partial class AndroidDebugBridgeTransport
    {
        private uint LocalId = 0;

        private readonly List<AndroidDebugBridgeStream> Streams = new();

        public AndroidDebugBridgeStream OpenStream(string OpenString)
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot open a stream on a device with no accepted connection!");
            }

            AndroidDebugBridgeStream stream = new(this, ++LocalId, OpenString);
            lock (Streams)
            {
                Streams.Add(stream);
            }

            stream.Open();

            stream.DataClosed += (object? sender, EventArgs args) =>
            {
                lock (Streams)
                {
                    _ = Streams.Remove((sender as AndroidDebugBridgeStream)!);
                }
            };

            return stream;
        }

        private void IncomingMessageLoop()
        {
            try
            {
                ReadMessageAsync((AndroidDebugBridgeMessage incomingMessage) =>
                {
                    HandleIncomingMessage(incomingMessage);
                    IncomingMessageLoop();
                }, (Exception ex) =>
                {
                    lock (Streams)
                    {
                        foreach (AndroidDebugBridgeStream stream in Streams.ToArray())
                        {
                            stream.HandleIncomingException(ex);
                        }
                    }
                }, VerifyCrc: false);
            }
            catch { }
        }

        private void HandleIncomingMessage(AndroidDebugBridgeMessage incomingMessage)
        {
            //Debug.WriteLine($"< new AndroidDebugBridgeMessage(AndroidDebugBridgeCommands.{incomingMessage.CommandIdentifier}, 0x{incomingMessage.FirstArgument:X8}, 0x{incomingMessage.FirstArgument:X8}, );");

            if (incomingMessage.CommandIdentifier is not AndroidDebugBridgeCommands.CNXN and not AndroidDebugBridgeCommands.AUTH)
            {
                bool HandledExternally = false;
                lock (Streams)
                {
                    foreach (AndroidDebugBridgeStream stream in Streams.ToArray())
                    {
                        if (stream.RemoteIdentifier != 0 && stream.RemoteIdentifier == incomingMessage.FirstArgument)
                        {
                            HandledExternally = stream.HandleIncomingMessage(incomingMessage);
                            break;
                        }
                        else if (stream.RemoteIdentifier == 0)
                        {
                            stream.RemoteIdentifier = incomingMessage.FirstArgument;
                            HandledExternally = stream.HandleIncomingMessage(incomingMessage);
                        }
                    }
                }

                if (HandledExternally)
                {
                    return;
                }
            }

            switch (incomingMessage.CommandIdentifier)
            {
                case AndroidDebugBridgeCommands.CNXN:
                    {
                        PhoneSupportedProtocolVersion = incomingMessage.FirstArgument;

                        string ConnectionString = Encoding.UTF8.GetString(incomingMessage.Payload!);

                        PhoneConnectionEnvironment = ConnectionString.Split("::")[0];

                        Dictionary<string, string> ConnectionVariables = new();

                        foreach (string variable in ConnectionString.Split("::")[1].Split(';'))
                        {
                            string[] variableParts = variable.Split('=');

                            if (variableParts[0] == "features")
                            {
                                PhoneConnectionFeatures = variableParts[1].Split(',');
                            }
                            else
                            {
                                ConnectionVariables.Add(variableParts[0], variableParts[1]);
                            }
                        }

                        PhoneConnectionVariables = ConnectionVariables;

                        IsConnected = true;
                        new Thread(() => OnConnectionEstablished?.Invoke(this, EventArgs.Empty)).Start();

                        break;
                    }
                case AndroidDebugBridgeCommands.AUTH:
                    {
                        if (incomingMessage.FirstArgument == 1)
                        {
                            // Real ADB does this if already accepted once
                            //
                            // -> AUTH (type 2) + Signed Token with RSA Private Key
                            //
                            // If ok:
                            //   <- CNXN + System Information
                            //
                            // If not:
                            //   <- AUTH (type 1) + Token
                            //   -> AUTH (type 3) + RSA Public Key
                            //   <- CNXN + System Information

                            // Token: 1
                            // Signature: 2
                            // RSA Public: 3

                            // -> AUTH (type 3) + Public Key
                            byte[] PublicKey = GetAdbPublicKeyPayload();
                            AndroidDebugBridgeMessage AuthType3 = AndroidDebugBridgeMessage.GetAuthMessage(3, PublicKey);
                            SendMessage(AuthType3);
                        }

                        break;
                    }
            }
        }
    }
}