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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AndroidDebugBridge
{
    public partial class AndroidDebugBridgeTransport
    {
        public void Shell()
        {
            using AndroidDebugBridgeStream stream = OpenStream("shell,v2,TERM=xterm-256color,pty:");

            stream.DataReceived += (object? sender, byte[] incomingMessage) =>
            {
                // 0: STDIN
                // 1: STDOUT
                // 2: STDERR
                // 3: EXIT
                // 4: Close STDIN
                // 5: Window Size Change

                Regex escapeSequences = new(@"(\x9D|\x1B\]).+(\x07|\x9c)|\x1b [F-Nf-n]|\x1b#[3-8]|\x1b%[@Gg]|\x1b[()*+][A-Za-z0-9=`<>]|\x1b[()*+]\""[>4?]|\x1b[()*+]%[0-6=]|\x1b[()*+]&[4-5]|\x1b[-.\/][ABFHLM]|\x1b[6-9Fcl-o=>\|\}~]|(\x9f|\x1b_).+\x9c|(\x90|\x1bP).+\x9c|(\x9B|\x1B\[)[0-?]*[ -\/]*[@-~]|(\x9e|\x1b\^).+\x9c|\x1b[DEHMNOVWXYZ78]");

                byte messageType = incomingMessage[0];
                if (messageType == 0)
                {
                    // Can't do much here, STDIN is not writable ofc...
                }
                else if (messageType == 1)
                {
                    uint packetArgument = BitConverter.ToUInt32(incomingMessage[1..5]);
                    string output = Encoding.UTF8.GetString(incomingMessage[5..]);

                    MatchCollection matches = escapeSequences.Matches(output);

                    int[] escapeSequenceIndices = matches.SelectMany(x =>
                    {
                        List<int> indices = new();
                        for (int i = x.Index; i < i + x.Length; i++)
                        {
                            indices.Add(i);
                        }
                        return indices;
                    }).ToArray();

                    for (int i = 0; i < output.Length; i++)
                    {
                        if (escapeSequenceIndices.Contains(i))
                        {
                            // Ignore for now...
                            continue;
                        }

                        char ch = output[i];
                        if (ch == 0x01)
                        {
                            string escapeArgument = output[(i + 1)..(i + 5)];
                            i += 4;
                        }
                        else
                        {
                            Console.Out.Write(ch);
                        }
                    }

                    (sender as AndroidDebugBridgeStream)?.SendAcknowledgement();
                }
                else if (messageType == 2)
                {
                    uint packetArgument = BitConverter.ToUInt32(incomingMessage[1..5]);
                    string output = Encoding.UTF8.GetString(incomingMessage[5..]);

                    MatchCollection matches = escapeSequences.Matches(output);

                    int[] escapeSequenceIndices = matches.SelectMany(x =>
                    {
                        List<int> indices = new();
                        for (int i = x.Index; i < i + x.Length; i++)
                        {
                            indices.Add(i);
                        }
                        return indices;
                    }).ToArray();

                    for (int i = 0; i < output.Length; i++)
                    {
                        if (escapeSequenceIndices.Contains(i))
                        {
                            // Ignore for now...
                            continue;
                        }

                        char ch = output[i];
                        if (ch == 0x01)
                        {
                            string escapeArgument = output[(i + 1)..(i + 5)];
                            i += 4;
                        }
                        else
                        {
                            Console.Error.Write(ch);
                        }
                    }

                    (sender as AndroidDebugBridgeStream)?.SendAcknowledgement();
                }
                else if (messageType == 3)
                {
                    // Close notification.
                }
                else if (messageType == 4)
                {
                    // Close STDIN
                }
                else if (messageType == 5)
                {
                    // Window size change
                }
                else
                {
                    // Unknown message type
                }
            };

            // Configure terminal size
            string terminalConfiguration = $"{Console.WindowHeight}x{Console.WindowWidth},0x0";

            byte[] ConfigurationRes = Encoding.UTF8.GetBytes($"{terminalConfiguration}\0");
            byte[] StringLength = BitConverter.GetBytes(ConfigurationRes.Length);

            List<byte> consoleConfigurationData = ConfigurationRes.ToList();
            consoleConfigurationData.Insert(0, 0x05);
            consoleConfigurationData.InsertRange(1, StringLength);

            stream.Write(consoleConfigurationData.ToArray());

            BackgroundWorker consoleInputWorker = new()
            {
                WorkerSupportsCancellation = true
            };

            consoleInputWorker.DoWork += (object? sender, DoWorkEventArgs e) =>
            {
                ConsoleKeyInfo readKey = Console.ReadKey(true);

                if (!stream.IsClosed && readKey.KeyChar != '\0')
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(readKey.KeyChar.ToString());
                    byte[] StringLength = BitConverter.GetBytes(buffer.Length);

                    List<byte> consoleInputData = buffer.ToList();
                    consoleInputData.Insert(0, 0x00);
                    consoleInputData.InsertRange(1, StringLength);

                    stream.Write(consoleInputData.ToArray());
                }
            };

            consoleInputWorker.RunWorkerCompleted += (object? sender, RunWorkerCompletedEventArgs e) =>
            {
                if (!stream.IsClosed)
                {
                    consoleInputWorker.RunWorkerAsync();
                }
            };

            stream.DataClosed += (object? sender, EventArgs args) =>
            {
                consoleInputWorker.CancelAsync();
            };

            consoleInputWorker.RunWorkerAsync();

            Debug.WriteLine("Entering shell closed Loop!");

            // Wait til the stream is closed
            while (!stream.IsClosed)
            {
                Thread.Sleep(100);
            }

            Debug.WriteLine("Leaving shell closed Loop!");
        }

        private object ShellLock = new();

        public string Shell(string command)
        {
            lock (ShellLock)
            {
                string ConsoleOutputString = string.Empty;
                using AndroidDebugBridgeStream stream = OpenStream($"shell,v2,TERM=xterm-256color,pty:{command}");

                stream.DataReceived += (object? sender, byte[] incomingMessage) =>
                {
                    // 0: STDIN
                    // 1: STDOUT
                    // 2: STDERR
                    // 3: EXIT
                    // 4: Close STDIN
                    // 5: Window Size Change

                    Regex escapeSequences = new(@"(\x9D|\x1B\]).+(\x07|\x9c)|\x1b [F-Nf-n]|\x1b#[3-8]|\x1b%[@Gg]|\x1b[()*+][A-Za-z0-9=`<>]|\x1b[()*+]\""[>4?]|\x1b[()*+]%[0-6=]|\x1b[()*+]&[4-5]|\x1b[-.\/][ABFHLM]|\x1b[6-9Fcl-o=>\|\}~]|(\x9f|\x1b_).+\x9c|(\x90|\x1bP).+\x9c|(\x9B|\x1B\[)[0-?]*[ -\/]*[@-~]|(\x9e|\x1b\^).+\x9c|\x1b[DEHMNOVWXYZ78]");

                    byte messageType = incomingMessage[0];
                    if (messageType == 0)
                    {
                        // Can't do much here, STDIN is not writable ofc...
                    }
                    else if (messageType == 1)
                    {
                        uint packetArgument = BitConverter.ToUInt32(incomingMessage[1..5]);
                        string output = Encoding.UTF8.GetString(incomingMessage[5..]);

                        MatchCollection matches = escapeSequences.Matches(output);

                        int[] escapeSequenceIndices = matches.SelectMany(x =>
                        {
                            List<int> indices = new();
                            for (int i = x.Index; i < i + x.Length; i++)
                            {
                                indices.Add(i);
                            }
                            return indices;
                        }).ToArray();

                        for (int i = 0; i < output.Length; i++)
                        {
                            if (escapeSequenceIndices.Contains(i))
                            {
                                // Ignore for now...
                                continue;
                            }

                            char ch = output[i];
                            if (ch == 0x01)
                            {
                                string escapeArgument = output[(i + 1)..(i + 5)];
                                i += 4;
                            }
                            else
                            {
                                ConsoleOutputString += ch;
                            }
                        }

                        (sender as AndroidDebugBridgeStream)?.SendAcknowledgement();
                    }
                    else if (messageType == 2)
                    {
                        uint packetArgument = BitConverter.ToUInt32(incomingMessage[1..5]);
                        string output = Encoding.UTF8.GetString(incomingMessage[5..]);

                        MatchCollection matches = escapeSequences.Matches(output);

                        int[] escapeSequenceIndices = matches.SelectMany(x =>
                        {
                            List<int> indices = new();
                            for (int i = x.Index; i < i + x.Length; i++)
                            {
                                indices.Add(i);
                            }
                            return indices;
                        }).ToArray();

                        for (int i = 0; i < output.Length; i++)
                        {
                            if (escapeSequenceIndices.Contains(i))
                            {
                                // Ignore for now...
                                continue;
                            }

                            char ch = output[i];
                            if (ch == 0x01)
                            {
                                string escapeArgument = output[(i + 1)..(i + 5)];
                                i += 4;
                            }
                            else
                            {
                                ConsoleOutputString += ch;
                            }
                        }

                        (sender as AndroidDebugBridgeStream)?.SendAcknowledgement();
                    }
                    else if (messageType == 3)
                    {
                        // Close notification.
                    }
                    else if (messageType == 4)
                    {
                        // Close STDIN
                    }
                    else if (messageType == 5)
                    {
                        // Window size change
                    }
                    else
                    {
                        // Unknown message type
                    }
                };

                // Configure terminal size
                string terminalConfiguration = $"{500}x{500},0x0";

                byte[] ConfigurationRes = Encoding.UTF8.GetBytes($"{terminalConfiguration}\0");
                byte[] StringLength = BitConverter.GetBytes(ConfigurationRes.Length);

                List<byte> consoleConfigurationData = ConfigurationRes.ToList();
                consoleConfigurationData.Insert(0, 0x05);
                consoleConfigurationData.InsertRange(1, StringLength);

                stream.Write(consoleConfigurationData.ToArray());

                Debug.WriteLine("Entering shell/cmd closed Loop!");

                // Wait til the stream is closed
                while (!stream.IsClosed)
                {
                    Thread.Sleep(100);
                }

                if (stream.IsFaulted)
                {
                    throw stream.ReceivedException ?? new Exception("Stream caught an unknown exception!");
                }

                Debug.WriteLine("Leaving shell/cmd closed Loop!");

                return ConsoleOutputString;
            }
        }
    }
}