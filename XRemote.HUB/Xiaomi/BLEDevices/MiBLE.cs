using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace XRemote.HUB.Xiaomi.BLEDevices
{
    public class MiBLE
    {
        public static byte[] Cipher(byte[] key, byte[] input)
        {
            var perm = new byte[256];
            var output = new byte[input.Length];

            //Init perm
            for (var i = 0; i < perm.Length; i++)
            {
                perm[i] = (byte)i;
            }

            var j = 0;
            for (var i = 0; i < perm.Length; i++)
            {
                var tmp = perm[i];

                j = (j + perm[i] + key[i % key.Length]) % 256;

                perm[i] = perm[j];
                perm[j] = tmp;
            }

            var index1 = 0;
            var index2 = 0;

            for (var i = 0; i < input.Length; i++)
            {
                var tmp = (byte)0;
                index1 = (index1 + 1) % 256;
                index2 = (index2 + perm[index1]) % 256;

                tmp = perm[index1];
                perm[index1] = perm[index2];
                perm[index2] = tmp;

                output[i] = (byte)(input[i] ^ perm[(perm[index1] + perm[index2]) % 256]);
            }

            return output;
        }

        public static byte[] MixB(byte[] mac, int product_id)
        {
            var bytes = new List<byte>
            {
                (byte)mac[0],
                (byte)mac[2],
                (byte)mac[5],
                (byte)((product_id >> 8) & 0xff),
                (byte)mac[4],
                (byte)mac[0],
                (byte)mac[5],
                (byte)(product_id & 0xff)
            };
            return bytes.ToArray();
        }

        public static byte[] MixA(byte[] mac, int product_id)
        {
            var bytes = new List<byte>
            {
                (byte)mac[0],
                (byte)mac[2],
                (byte)mac[5],
                (byte)(product_id & 0xff),
                (byte)(product_id & 0xff),
                (byte)mac[4],
                (byte)mac[5],
                (byte)mac[1]
            };
            return bytes.ToArray();
        }

        public static async Task<bool> WriteAsync(GattCharacteristic gattCharacteristic, IBuffer value, GattWriteOption option)
        {
            var resultWrite = await gattCharacteristic.WriteValueWithResultAsync(value, option);
            if (resultWrite.Status != GattCommunicationStatus.Success)
            {
                Console.WriteLine($"Error write, status: {resultWrite.Status:X}");
                return false;
            }
            else
            {
                Console.WriteLine($"Successful write");
                return true;
            }
        }

        public static async Task<IBuffer> ReadAsync(GattCharacteristic gattCharacteristic)
        {
            var resultWrite = await gattCharacteristic.ReadValueAsync();
            if (resultWrite.Status != GattCommunicationStatus.Success)
            {
                return resultWrite.Value;
            }
            else
            {
                return resultWrite.Value;
            }
        }

        public static IBuffer BytesToBuffer(byte[] arrayBytes)
        {
            var data = new DataWriter();
            data.WriteBytes(arrayBytes);
            return data.DetachBuffer();
        }

        public static async Task<GattCharacteristic> CharacteristicsByUUIDAsync(string guid, GattDeviceServicesResult result)
        {
            foreach (var service in result.Services)
            {
                var characs = await service.GetCharacteristicsAsync();
                foreach (var characteristic in characs.Characteristics)
                {
                    if (characteristic.Uuid.ToString().Contains(guid))
                    {
                        return characteristic;
                    }
                }
            }

            return null;
        }

        public static GattDeviceService ServicesByUUID(string guid, GattDeviceServicesResult result)
        {
            foreach (var service in result.Services)
            {
                if (service.Uuid.ToString().Contains(guid))
                {
                    return service;
                }
            }
            return null;
        }

        public static GattDescriptor[] GetDescriptors(GattDeviceService service)
        {
            var tempArray = new List<GattDescriptor>();
            foreach (var characteristic in service.GetCharacteristicsAsync().GetResults().Characteristics)
                tempArray.AddRange(characteristic.GetDescriptorsAsync().GetResults().Descriptors);

            return tempArray.ToArray();
        }
    }
}
