# Android Debug Bridge over USB for .NET

This repository holds an implementation of the Android Debug Bridge (or ADB for short) protocol over USB in C#.
It makes use of WinUSB (via WinUSBNet) for communicating over USB to a target device.

Currently the following features are not supported:

- Push
- Pull
- List
- Single command execution
- Token signing for authentication
- Legacy Shell interface (only v2 is supported at the moment)

These will be addressed at a later time.

## Sample code

Below's code may give an idea of how to use the library currently:

```csharp
using AndroidDebugBridge;

namespace Playground
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string usbid = @"\\?\USB#VID_045E&PID_0C26#0F0012E214600A#{dee824ef-729b-4a0e-9c14-b7117d33a817}"; // Android (Duo 2)
            //string usbid = @"\\?\USB#VID_18D1&PID_D001#0F0012E214600A#{dee824ef-729b-4a0e-9c14-b7117d33a817}"; // TWRP (Duo 2)

            Console.WriteLine($"Opening {usbid}...");
            using AndroidDebugBridgeTransport transport = new(usbid);

            Console.WriteLine("Connecting...");
            transport.Connect();

            transport.WaitTilConnected();

            Console.WriteLine($"Connected to: {transport.PhoneConnectionString}");
            Console.WriteLine($"Protocol version: {transport.PhoneSupportedProtocolVersion}");

            Console.WriteLine("Opening shell...");
            transport.Shell();

            Console.WriteLine("Shell closed!");

            Console.WriteLine("Rebooting...");
            transport.Reboot();
        }
    }
}
```