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
using MadWizard.WinUSBNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AndroidDebugBridge
{
    public partial class AndroidDebugBridgeTransport : IDisposable
    {
        private bool Disposed = false;
        public event EventHandler? OnConnectionEstablished = null;
        public string DevicePath { get; }

        public AndroidDebugBridgeTransport(string DevicePath)
        {
            this.DevicePath = DevicePath;
            PhoneConnectionEnvironment = "";
            PhoneConnectionVariables = new Dictionary<string, string>();
            PhoneConnectionFeatures = Array.Empty<string>();
            PhoneSupportedProtocolVersion = 0;

            // adb.exe starts a daemon which would lock up the connection
            // Kill any instance so we can communicate with the device.
            KillAnyADBInstance();

            USBDevice = new USBDevice(DevicePath);

            foreach (USBPipe Pipe in USBDevice.Pipes)
            {
                if (Pipe.IsIn)
                {
                    InputPipe = Pipe;
                }

                if (Pipe.IsOut)
                {
                    OutputPipe = Pipe;
                }
            }

            if (InputPipe == null || OutputPipe == null)
            {
                USBDevice.Dispose();
                throw new Exception("Invalid USB device!");
            }

            IncomingMessageLoop();
        }

        private static void KillAnyADBInstance()
        {
            Process proc = new()
            {
                StartInfo = new("taskkill.exe", "/IM adb.exe /F")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            proc.Start();
            proc.WaitForExit();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AndroidDebugBridgeTransport()
        {
            Dispose(false);
        }

        public void Close()
        {
            USBDevice?.Dispose();
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