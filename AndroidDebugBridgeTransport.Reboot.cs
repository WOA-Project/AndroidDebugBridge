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

namespace AndroidDebugBridge
{
    public partial class AndroidDebugBridgeTransport
    {
        public void Reboot(string mode = "")
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot reboot with no accepted connection!");
            }

            using AndroidDebugBridgeStream stream = OpenStream($"reboot:{mode}");
        }

        public void RebootBootloader()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot reboot to bootloader with no accepted connection!");
            }

            Reboot("bootloader");
        }

        public void RebootRecovery()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot reboot to recovery with no accepted connection!");
            }

            Reboot("recovery");
        }

        public void RebootFastBootD()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot reboot to fastbootd with no accepted connection!");
            }

            Reboot("fastboot");
        }

        public void RebootSideload()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot reboot to sideload with no accepted connection!");
            }

            Reboot("sideload");
        }

        public void RebootEDL()
        {
            if (!IsConnected)
            {
                throw new Exception("Cannot reboot to edl with no accepted connection!");
            }

            Reboot("edl");
        }
    }
}