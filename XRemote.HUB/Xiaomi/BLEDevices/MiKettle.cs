using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace XRemote.HUB.Xiaomi.BLEDevices
{
    class MiKettle
    {
        public MiKettle(GattDeviceServicesResult device, string mac) => InitDefinations(device, mac);
        public MiKettle(GattDeviceServicesResult device, string mac, byte[] token) => InitDefinations(device, mac, token);
        public struct MiCharacteristics
        {
            public const string AuthInit = "00000010-0000-1000-8000-00805f9b34fb";
            public const string Auth = "00000001-0000-1000-8000-00805f9b34fb";
            public const string Verify = "00000004-0000-1000-8000-00805f9b34fb";

            public const string Setup = "0000aa01-0000-1000-8000-00805f9b34fb";
            public const string Status = "0000aa02-0000-1000-8000-00805f9b34fb";
            public const string Time = "0000aa04-0000-1000-8000-00805f9b34fb";
            public const string KeepWarmRefillMode = "0000aa05-0000-1000-8000-00805f9b34fb";
        }

        public byte[] mAuthKey1 = new byte[] { 0x90, 0xCA, 0x85, 0xDE };
        public byte[] mAuthKey2 = new byte[] { 0x92, 0xAB, 0x54, 0xFA };

        public enum MiAction
        {
            idle,
            heating,
            cooling,
            keeping_warm
        }
        public enum MiMode
        {
            none = 255,
            boil = 1,
            keep_warm = 2
        }
        public enum MiWarmType
        {
            boil_and_cool_down,
            heat_to_temperature
        }
        public enum MiWarmRefilMode
        {
            keep_warm,
            turn_off
        }
        public enum MiExtendHeating
        {
            turn_off,
            turn_on
        }
        public enum MiInitialization
        {
            OK,
            Err,
            ErrVerification
        }
        public struct LocalCacheMiKettle
        {
            public MiAction mAction;
            public MiMode mMode;
            public int mWarmTemp;
            public int mCurrentTemp;
            public MiWarmType mWarmType;
            public int mLastChangedKeepWarmTime;
            public MiWarmRefilMode mRefilMode;
            public int mCurrentKeepWarmTime;
            public MiExtendHeating mExtendedHeating;
        }

        public delegate void StatusInitialization(MiInitialization status, string message);
        public event StatusInitialization SubStatusInitialization;
        public LocalCacheMiKettle mCache;
        public GattDeviceServicesResult Device { get; private set; }
        private int ProductId { get; set; }
        private byte[] ReversedMac { get; set; }
        private byte[] Token { get; set; }
        private bool IsWaitNotification { get; set; } = true;

        private void InitDefinations(GattDeviceServicesResult device, string mac, [System.Runtime.InteropServices.Optional] byte[] token)
        {
            var tempToken = new byte[12];
            new Random().NextBytes(tempToken);

            Token = (token is null) ? tempToken : token;
            ReversedMac = mac.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray().Reverse().ToArray();
            ProductId = 275;
            Device = device;
            mCache = new LocalCacheMiKettle();
        }
        private void WaitNotification()
        {
            while (IsWaitNotification) { }
        }
        private void NotifyHandler(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            if (sender.Uuid.ToString().Contains(MiCharacteristics.Auth))
            {
                var dataReader = DataReader.FromBuffer(eventArgs.CharacteristicValue);
                var data = new byte[eventArgs.CharacteristicValue.Length];
                dataReader.ReadBytes(data);

                var response = MiBLE.Cipher(MiBLE.MixB(ReversedMac, ProductId), MiBLE.Cipher(MiBLE.MixA(ReversedMac, ProductId), data));
                if (Enumerable.SequenceEqual(response, Token))
                {
                    IsWaitNotification = false;
                }
                else
                {
                    SubStatusInitialization?.Invoke(MiInitialization.ErrVerification, "Response and token not compare...");
                }
                return;
            }
            else if (sender.Uuid.ToString().Contains(MiCharacteristics.Status))
            {
                var dataReader = DataReader.FromBuffer(eventArgs.CharacteristicValue);
                var data = new byte[eventArgs.CharacteristicValue.Length];
                dataReader.ReadBytes(data);
                mCache.mAction = (MiAction)data[0];
                mCache.mMode = (MiMode)data[1];
                mCache.mWarmTemp = data[4];
                mCache.mCurrentTemp = data[5];
                mCache.mWarmType = (MiWarmType)data[6];
                mCache.mLastChangedKeepWarmTime = data[7] + data[8];
                mCache.mRefilMode = (MiWarmRefilMode)data[9];
                mCache.mCurrentKeepWarmTime = data[10];
                mCache.mExtendedHeating = (MiExtendHeating)data[11];
                return;
            }
        }
        public async Task ConnectAsync()
        {
            if (Device.Status == GattCommunicationStatus.Success)
            {
                await MiBLE.WriteAsync(await MiBLE.CharacteristicsByUUIDAsync("00000010-0000-1000-8000-00805f9b34fb", Device), MiBLE.BytesToBuffer(new byte[] { 0x90, 0xCA, 0x85, 0xDE }), GattWriteOption.WriteWithoutResponse);
                var auth = await MiBLE.CharacteristicsByUUIDAsync("00000001-0000-1000-8000-00805f9b34fb", Device);
                var statusSubAuth = await auth.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (statusSubAuth == GattCommunicationStatus.Success)
                {
                    var registeredCharacteristic = auth;
                    registeredCharacteristic.ValueChanged += NotifyHandler;
                    await MiBLE.WriteAsync(await MiBLE.CharacteristicsByUUIDAsync("00000001-0000-1000-8000-00805f9b34fb", Device), MiBLE.BytesToBuffer(MiBLE.Cipher(MiBLE.MixA(ReversedMac, ProductId), Token)), GattWriteOption.WriteWithResponse);
                    IsWaitNotification = true;
                    WaitNotification();
                    var authDesc = await MiBLE.CharacteristicsByUUIDAsync("00000001-0000-1000-8000-00805f9b34fb", Device);
                    var value = MiBLE.BytesToBuffer(MiBLE.Cipher(Token, new byte[] { 0x92, 0xAB, 0x54, 0xFA }));
                    await MiBLE.WriteAsync(authDesc, value, GattWriteOption.WriteWithoutResponse);
                    await MiBLE.ReadAsync(await MiBLE.CharacteristicsByUUIDAsync("00000004-0000-1000-8000-00805f9b34fb", Device));

                    Thread.Sleep(1_000);//Wait...

                    var status = await MiBLE.CharacteristicsByUUIDAsync("0000aa02-0000-1000-8000-00805f9b34fb", Device);
                    var communicationStatus = await status.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (communicationStatus == GattCommunicationStatus.Success)
                    {
                        status.ValueChanged += NotifyHandler;
                        Thread.Sleep(1_000);//Wait...
                        SubStatusInitialization?.Invoke(MiInitialization.OK, "Successful connected to MiKettle");
                    }
                    else
                    {
                        SubStatusInitialization?.Invoke(MiInitialization.Err, $"Error communication, status code: {communicationStatus:X}");
                    }
                }
                else
                {
                    SubStatusInitialization?.Invoke(MiInitialization.Err, $"Error subscribe to auth, status code: {statusSubAuth:X}");
                }
            }
            else
            {
                SubStatusInitialization?.Invoke(MiInitialization.Err, $"Error initializing device, status code: {Device.Status:X}");
            }
        }
        public async Task SetKeepWarmParametersAsync(MiWarmType type, int temperature)
        {
            var data = new DataWriter();
            data.WriteByte((byte)type);
            data.WriteByte((byte)temperature);
            await MiBLE.WriteAsync(await MiBLE.CharacteristicsByUUIDAsync(MiCharacteristics.Setup, Device), data.DetachBuffer(), GattWriteOption.WriteWithResponse);
        }
        public async Task SetKeepWarmTimeLimitAsync(int time)
        {
            var newTime = Math.Round((decimal)time * 2);

            if (newTime >= 0 && newTime <= 24)
            {
                var data = new DataWriter();
                data.WriteByte((byte)newTime);
                await MiBLE.WriteAsync(await MiBLE.CharacteristicsByUUIDAsync(MiCharacteristics.Time, Device), data.DetachBuffer(), GattWriteOption.WriteWithResponse);
            }
        }
        public async Task SetKeepWarmRefillModeAsync(MiWarmRefilMode mode)
        {
            var data = new DataWriter();
            data.WriteByte((byte)mode);
            await MiBLE.WriteAsync(await MiBLE.CharacteristicsByUUIDAsync(MiCharacteristics.KeepWarmRefillMode, Device), data.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
        }
    }
}
