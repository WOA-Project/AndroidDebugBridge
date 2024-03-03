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
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AndroidDebugBridge
{
    public partial class AndroidDebugBridgeTransport
    {
        private static bool IsConnected = false;

        private readonly RSACryptoServiceProvider RSACryptoServiceProvider = new(2048);

        public uint PhoneSupportedProtocolVersion
        {
            get; private set;
        }

        public string PhoneConnectionEnvironment
        {
            get; private set;
        }

        public IReadOnlyDictionary<string, string> PhoneConnectionVariables
        {
            get; private set;
        }

        public IReadOnlyList<string> PhoneConnectionFeatures
        {
            get; private set;
        }

        private byte[] GetAdbPublicKeyPayload()
        {
            RSAParameters publicKey = RSACryptoServiceProvider.ExportParameters(false);
            (uint n0inv, uint[] n, uint[] rr, int exponent) = AndroidDebugBridgeCryptography.ConvertRSAToADBRSA(publicKey);
            byte[] ConvertedKey = AndroidDebugBridgeCryptography.ADBRSAToBuffer(n0inv, n, rr, exponent);
            return Encoding.UTF8.GetBytes($"{Convert.ToBase64String(ConvertedKey)} {Environment.UserName}@{Environment.MachineName}\0");
        }

        public void Connect()
        {
            // -> CNXN + System Information
            AndroidDebugBridgeMessage ConnectMessage = AndroidDebugBridgeMessage.GetConnectMessage();
            SendMessage(ConnectMessage);
        }

        public void WaitTilConnected()
        {
            while (!IsConnected)
            {
                Thread.Sleep(100);
            }
        }
    }
}