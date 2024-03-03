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
using System.Diagnostics;
using System.Threading;

namespace AndroidDebugBridge
{
    public class AndroidDebugBridgeStream : IDisposable
    {
        private bool Disposed = false;

        private bool ReceivedOK = false;
        public bool IsClosed = false;

        private readonly uint LocalIdentifier;
        private readonly string OpenString;
        private readonly AndroidDebugBridgeTransport Transport;

        internal uint RemoteIdentifier = 0;

        public event EventHandler<byte[]>? DataReceived;
        public event EventHandler? DataClosed;

        internal AndroidDebugBridgeStream(AndroidDebugBridgeTransport Transport, uint LocalIdentifier, string OpenString)
        {
            this.LocalIdentifier = LocalIdentifier;
            this.OpenString = OpenString;
            this.Transport = Transport;
        }

        internal void Open()
        {
            Transport.SendMessage(AndroidDebugBridgeMessage.GetOpenMessage(LocalIdentifier, OpenString));
        }

        private void Close()
        {
            if (!IsClosed)
            {
                IsClosed = true;

                DataClosed?.Invoke(this, EventArgs.Empty);

                Transport.SendMessage(AndroidDebugBridgeMessage.GetCloseMessage(LocalIdentifier, LocalIdentifier));
                //WaitForAcknowledgement();
            }
        }

        public void Write(byte[] buffer)
        {
            if (!IsClosed)
            {
                Transport.SendMessage(AndroidDebugBridgeMessage.GetWriteMessage(LocalIdentifier, LocalIdentifier, buffer));
                WaitForAcknowledgement();
            }
        }

        internal bool HandleIncomingMessage(AndroidDebugBridgeMessage incomingMessage)
        {
            switch (incomingMessage.CommandIdentifier)
            {
                case AndroidDebugBridgeCommands.OKAY:
                    {
                        ReceivedOK = true;
                        return true;
                    }
                case AndroidDebugBridgeCommands.WRTE:
                    {
                        DataReceived?.Invoke(this, incomingMessage.Payload!);
                        return true;
                    }
                case AndroidDebugBridgeCommands.CLSE:
                    {
                        if (!IsClosed)
                        {
                            // < CLSE - Done (here)
                            // > OKAY
                            // > CLSE
                            // < CLSE

                            IsClosed = true;

                            DataClosed?.Invoke(this, EventArgs.Empty);

                            // Send an OKAY back
                            SendAcknowledgement();

                            // Close ourselves too
                            Transport.SendMessage(AndroidDebugBridgeMessage.GetCloseMessage(LocalIdentifier, LocalIdentifier));

                            // Another CLSE will be sent from the device but we removed ourselves (DataClosed) so we won't treat it.
                        }
                        else
                        {
                            // > CLSE - Done
                            // < OKAY - Done
                            // < CLSE - Done (here)
                            // > CLSE

                            // Close ourselves too
                            Transport.SendMessage(AndroidDebugBridgeMessage.GetCloseMessage(LocalIdentifier, LocalIdentifier));

                            // Remove ourselves
                            DataClosed?.Invoke(this, EventArgs.Empty);
                        }

                        return true;
                    }
            }

            return false;
        }

        public void SendAcknowledgement()
        {
            Transport.SendMessage(AndroidDebugBridgeMessage.GetReadyMessage(LocalIdentifier, LocalIdentifier));
        }

        public void WaitForAcknowledgement()
        {
            if (!IsClosed)
            {
                Debug.WriteLine("Entering WaitForAcknowledgement Loop!");

                while (!ReceivedOK)
                {
                    Thread.Sleep(100);
                }
                Debug.WriteLine("Leaving WaitForAcknowledgement Loop!");

                ReceivedOK = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AndroidDebugBridgeStream()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                // Other disposables
            }

            // Clean unmanaged resources here.
            Close();

            Disposed = true;
        }
    }
}