using System;
using System.Threading.Tasks;
using System.Threading;
using XRemote.HUB.BLE;
using XRemote.HUB.Xiaomi.BLEDevices;

namespace XRemote.HUB
{
    class Program
    {
        private static MiKettle kettle;
        private static bool isInit;
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var watcher = new BLEFinder(new GattServiceIds());

            // Hook into events
            watcher.StartedListening += async () =>
            {
                Console.WriteLine("Started listening");
            };

            watcher.StoppedListening += async () =>
            {
                Console.WriteLine("Stopped listening");
            };

            watcher.NewDeviceDiscovered += async (device) =>
            {
                if (isInit)
                    return;
                isInit = true;
                Console.WriteLine($"New device: {device}");
                var responseDevice = await watcher.PairToDeviceAsync(device.DeviceId);
                kettle = new MiKettle(responseDevice, "B8:7C:6F:84:13:F7", new byte[] { 0xb3, 0x96, 0x26, 0xdf, 0x6a, 0x84, 0x48, 0x38, 0x93, 0x33, 0xa1, 0xc5 });
                kettle.SubStatusInitialization += MKettle_SubStatusInitialization;
                await kettle.ConnectAsync();

            };

            /*watcher.DeviceNameChanged += (device) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine( $"Device name changed: {device}");
            };*/

            watcher.DeviceTimeout += (device) =>
            {
                Console.WriteLine( $"Device timeout: {device}");
            };

            // Start listening
            watcher.StartListening();
            while (true) { Thread.Sleep(500); }
        }
        private static void MKettle_SubStatusInitialization(MiKettle.MiInitialization status, string message)
        {
            Console.WriteLine($"Status: {status.ToString()} Msg: {message}");
            if (status == MiKettle.MiInitialization.OK)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        var cmd = Console.ReadLine().Split(" ");
                        switch (cmd[0])
                        {
                            case "SetWarmParametrs":
                                {
                                    await kettle.SetKeepWarmParametersAsync((MiKettle.MiWarmType)int.Parse(cmd[1]), int.Parse(cmd[2]));
                                    break;
                                }
                            case "SetWarmRefil":
                                {
                                    await kettle.SetKeepWarmRefillModeAsync((MiKettle.MiWarmRefilMode)int.Parse(cmd[1]));
                                    break;
                                }
                            case "SetTimeLimit":
                                {
                                    await kettle.SetKeepWarmTimeLimitAsync(int.Parse(cmd[1]));
                                    break;
                                }
                            case "GetInfo":
                                {
                                    PrintStatus();
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine($"-> Command \"{cmd[0]}\" not found");
                                    break;
                                }
                        }
                    }
                });
            }
        }

        private static void PrintStatus()
        {
            Console.WriteLine("---Kettle Info---\n");
            Console.WriteLine($"Action: {kettle.mCache.mAction.ToString()}");
            Console.WriteLine($"Mode: {kettle.mCache.mMode.ToString()}");
            Console.WriteLine($"Warm temperature: {kettle.mCache.mWarmTemp.ToString()}");
            Console.WriteLine($"Current temperature: {kettle.mCache.mCurrentTemp.ToString()}");
            Console.WriteLine($"Warm type: {kettle.mCache.mWarmType.ToString()}");
            Console.WriteLine($"Last changed warm time: {kettle.mCache.mLastChangedKeepWarmTime.ToString()}");
            Console.WriteLine($"Refil mode: {kettle.mCache.mRefilMode.ToString()}");
            Console.WriteLine($"Current warm time: {kettle.mCache.mCurrentKeepWarmTime.ToString()}");
            Console.WriteLine($"Extend heating: {kettle.mCache.mExtendedHeating.ToString()}");
        }
    }
}
