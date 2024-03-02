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
namespace AndroidDebugBridge
{
    public enum AndroidDebugBridgeCommands
    {
        AUTH = 0x48545541,
        CLSE = 0x45534C43,
        CNXN = 0x4E584E43,
        OKAY = 0x59414B4F,
        OPEN = 0x4E45504F,
        SYNC = 0x434E5953,
        WRTE = 0x45545257,

        // SYNC sub commands
        /*DATA = 0x44415441,
        QUIT = 0x51554954,
        RECV = 0x52454356,
        SEND = 0x53454E44,
        STAT = 0x53544154*/
    }
}