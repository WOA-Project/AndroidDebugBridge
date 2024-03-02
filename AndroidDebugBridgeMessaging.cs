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
using System.IO;

namespace AndroidDebugBridge
{
    public class AndroidDebugBridgeMessaging
    {
        private static uint ComputeAdbCrc(byte[] Payload)
        {
            if (Payload == null)
            {
                return 0;
            }

            uint crc = 0;

            foreach (byte PayloadByte in Payload)
            {
                crc = (crc + PayloadByte) & 0xFFFFFFFF;
            }

            return crc;
        }

        internal static byte[] GetCommandPacket(AndroidDebugBridgeCommands CommandIdentifier, uint FirstArgument, uint SecondArgument, byte[]? CommandPayload = null)
        {
            return GetCommandPacket((uint)CommandIdentifier, FirstArgument, SecondArgument, CommandPayload);
        }

        internal static byte[] GetCommandPacket(uint CommandIdentifier, uint FirstArgument, uint SecondArgument, byte[]? CommandPayload = null)
        {
            return CommandPacketToBuffer(CommandIdentifier, FirstArgument, SecondArgument, CommandPayload != null ? (uint)CommandPayload.Length : 0, CommandPayload != null ? ComputeAdbCrc(CommandPayload) : 0);
        }

        internal static byte[] CommandPacketToBuffer(uint CommandIdentifier, uint FirstArgument, uint SecondArgument, uint CommandPayloadLength, uint CommandPayloadCrc)
        {
            byte[] packet = new byte[24];

            using MemoryStream memoryStream = new(packet);
            using BinaryWriter binaryWriter = new(memoryStream);

            binaryWriter.Write(CommandIdentifier);
            binaryWriter.Write(FirstArgument);
            binaryWriter.Write(SecondArgument);
            binaryWriter.Write(CommandPayloadLength);
            binaryWriter.Write(CommandPayloadCrc);
            binaryWriter.Write(CommandIdentifier ^ 0xFFFFFFFF);

            return packet;
        }

        public static (uint CommandIdentifier, uint FirstArgument, uint SecondArgument, uint CommandPayloadLength, uint CommandPayloadCrc) BufferToCommandPacket(byte[] packet)
        {
            if (packet.Length != 24)
            {
                throw new InvalidDataException("Invalid Command Packet size!");
            }

            using MemoryStream memoryStream = new(packet);
            using BinaryReader binaryReader = new(memoryStream);

            uint CommandIdentifier = binaryReader.ReadUInt32();
            uint FirstArgument = binaryReader.ReadUInt32();
            uint SecondArgument = binaryReader.ReadUInt32();
            uint CommandPayloadLength = binaryReader.ReadUInt32();
            uint CommandPayloadCrc = binaryReader.ReadUInt32();

            uint magic = binaryReader.ReadUInt32();
            return (CommandIdentifier ^ 0xFFFFFFFF) != magic
                ? throw new InvalidDataException("Invalid Command Packet magic!")
                : (CommandIdentifier, FirstArgument, SecondArgument, CommandPayloadLength, CommandPayloadCrc);
        }

        internal static (AndroidDebugBridgeCommands CommandIdentifier, uint FirstArgument, uint SecondArgument, uint CommandPayloadLength, uint CommandPayloadCrc) ParseCommandPacket(byte[] packet)
        {
            (uint CommandIdentifier, uint FirstArgument, uint SecondArgument, uint CommandPayloadLength, uint CommandPayloadCrc) = BufferToCommandPacket(packet);
            return ((AndroidDebugBridgeCommands)CommandIdentifier, FirstArgument, SecondArgument, CommandPayloadLength, CommandPayloadCrc);
        }

        internal static void VerifyAdbCrc(byte[] Payload, uint ExpectedPayloadCrc)
        {
            uint PayloadCrc = ComputeAdbCrc(Payload);
            if (PayloadCrc != ExpectedPayloadCrc)
            {
                throw new InvalidDataException("Crc doesn't match!");
            }
        }
    }
}