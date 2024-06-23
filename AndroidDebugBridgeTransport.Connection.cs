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
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AndroidDebugBridge
{
    public partial class AndroidDebugBridgeTransport
    {
        public bool IsConnected { get; private set; } = false;

        private readonly RSACryptoServiceProvider RSACryptoServiceProvider = new(2048);

        private uint PhoneSupportedProtocolVersion;
        private string PhoneConnectionEnvironment;
        private IReadOnlyDictionary<string, string> PhoneConnectionVariables;
        private IReadOnlyList<string> PhoneConnectionFeatures;

        public uint GetPhoneSupportedProtocolVersion()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot get the phone supported protocol version with no accepted connection!");
            }

            return PhoneSupportedProtocolVersion;
        }

        public string GetPhoneConnectionEnvironment()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot get the phone connection environment with no accepted connection!");
            }

            return PhoneConnectionEnvironment; 
        }

        public IReadOnlyDictionary<string, string> GetPhoneConnectionVariables()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot get the phone connection variables with no accepted connection!");
            }

            return PhoneConnectionVariables;
        }

        public IReadOnlyList<string> GetPhoneConnectionFeatures()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot get the phone connection features with no accepted connection!");
            }

            return PhoneConnectionFeatures;
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
            if (IsConnected)
            {
                throw new Exception("Cannot connect to an already connected device!");
            }

            // -> CNXN + System Information
            AndroidDebugBridgeMessage ConnectMessage = AndroidDebugBridgeMessage.GetConnectMessage();
            SendMessage(ConnectMessage);
        }

        public void WaitTilConnected()
        {
            Debug.WriteLine("Entering WaitTilConnected Loop!");

            while (!IsConnected)
            {
                Thread.Sleep(100);
            }

            Debug.WriteLine("Leaving WaitTilConnected Loop!");
        }
    }
}