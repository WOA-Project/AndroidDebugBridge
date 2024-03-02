﻿/*
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
using System.Text;

namespace AndroidDebugBridge
{
    internal class AndroidDebugBridgeMessage
    {
        public AndroidDebugBridgeCommands CommandIdentifier
        {
            get; set;
        }

        public uint FirstArgument
        {
            get; set;
        }

        public uint SecondArgument
        {
            get; set;
        }

        public byte[]? Payload
        {
            get; set;
        }

        public AndroidDebugBridgeMessage(AndroidDebugBridgeCommands commandIdentifier, uint firstArgument, uint secondArgument, byte[]? payload = null)
        {
            CommandIdentifier = commandIdentifier;
            FirstArgument = firstArgument;
            SecondArgument = secondArgument;
            Payload = payload;
        }

        internal static AndroidDebugBridgeMessage GetConnectMessage()
        {
            byte[] SystemInformation = Encoding.UTF8.GetBytes("host::features=shell_v2,cmd,stat_v2,ls_v2,fixed_push_mkdir,apex,abb,fixed_push_symlink_timestamp,abb_exec,remount_shell,track_app,sendrecv_v2,sendrecv_v2_brotli,sendrecv_v2_lz4,sendrecv_v2_zstd,sendrecv_v2_dry_run_send,openscreen_mdns");
            return new AndroidDebugBridgeMessage(AndroidDebugBridgeCommands.CNXN, 0x01000001, 0x00100000, SystemInformation);
        }

        internal static AndroidDebugBridgeMessage GetAuthMessage(uint type, byte[] data)
        {
            // type:
            //
            // Token: 1
            // Signature: 2
            // RSA Public: 3
            return new AndroidDebugBridgeMessage(AndroidDebugBridgeCommands.AUTH, type, 0, data);
        }

        internal static AndroidDebugBridgeMessage GetOpenMessage(uint localId, string dest)
        {
            byte[] buffer = Encoding.UTF8.GetBytes($"{dest}\0");
            return new AndroidDebugBridgeMessage(AndroidDebugBridgeCommands.OPEN, localId, 0, buffer);
        }

        internal static AndroidDebugBridgeMessage GetWriteMessage(uint localId, uint remoteId, byte[] data)
        {
            return new AndroidDebugBridgeMessage(AndroidDebugBridgeCommands.WRTE, localId, remoteId, data);
        }

        internal static AndroidDebugBridgeMessage GetCloseMessage(uint localId, uint remoteId)
        {
            return new AndroidDebugBridgeMessage(AndroidDebugBridgeCommands.CLSE, localId, remoteId, null);
        }

        internal static AndroidDebugBridgeMessage GetReadyMessage(uint localId, uint remoteId)
        {
            return new AndroidDebugBridgeMessage(AndroidDebugBridgeCommands.OKAY, localId, remoteId, null);
        }
    }
}