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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidDebugBridge
{
    public partial class AndroidDebugBridgeTransport
    {
        public void Shell()
        {
            using AndroidDebugBridgeStream stream = OpenStream("shell,v2,TERM=xterm-256color,pty:");

            stream.DataReceived += (object? sender, byte[] incomingMessage) =>
            {
                byte messageType = incomingMessage[0];
                if (messageType == 1)
                {
                    uint length = BitConverter.ToUInt32(incomingMessage[1..5]);
                    Console.Write(Encoding.UTF8.GetString(incomingMessage[5..((int)length + 5)]));

                    (sender as AndroidDebugBridgeStream)?.SendAcknowledgement();
                }
                else if (messageType == 3)
                {
                    // Close notification.
                }
                else
                {
                    // Unknown message type
                }
            };

            // Configure terminal size
            string terminalConfiguration = $"{Console.WindowWidth}x{Console.WindowHeight},0x0";

            byte[] ConfigurationRes = Encoding.UTF8.GetBytes($"{terminalConfiguration}\0");
            byte[] StringLength = BitConverter.GetBytes(ConfigurationRes.Length);

            List<byte> consoleConfigurationData = ConfigurationRes.ToList();
            consoleConfigurationData.Insert(0, 0x05);
            consoleConfigurationData.InsertRange(1, StringLength);

            stream.Write(consoleConfigurationData.ToArray());

            bool closed = false;

            Task consoleInputThread = Task.Run(() =>
            {
                while (!closed)
                {
                    ConsoleKeyInfo readKey = Console.ReadKey(true);

                    byte[] buffer = Encoding.UTF8.GetBytes(readKey.KeyChar.ToString());
                    byte[] StringLength = BitConverter.GetBytes(buffer.Length);

                    List<byte> consoleInputData = buffer.ToList();
                    consoleInputData.Insert(0, 0x00);
                    consoleInputData.InsertRange(1, StringLength);

                    stream.Write(consoleInputData.ToArray());
                }
            });

            stream.DataClosed += (object? sender, EventArgs args) =>
            {
                closed = true;
            };

            consoleInputThread.Wait();
        }
    }
}