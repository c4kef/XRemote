using Xamarin.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using XRemote.ShareStructure;
using Network;

namespace XRemote.Mobile.Droid
{
    class XServerClient
    {
        private TcpConnection client;
        private List<SharedClass> shareds;

        public async void Connect()
        {
            shareds = new List<SharedClass>();
            while (true)
            {
                try
                {
                    Globals.IsConnected = client != null && client.IsAlive;

                    if (client == null || !client.IsAlive)
                    {
                        client = ConnectionFactory.CreateTcpConnectionAsync("192.168.0.15", 1708).GetAwaiter().GetResult().Item1;
                        client.RegisterStaticPacketHandler<SharedClass>((packet, connection) =>
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            MessagingCenter.Send(packet, "XClientCommand");
                        }));
                    }

                    await Task.Delay(50);
                    if (shareds == null || shareds.Count <= 0)
                        continue;

                    SharedResponse response = await client.SendAsync<SharedResponse>(shareds[0]);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        MessagingCenter.Send(response.Result, "XClientCommand");
                    });
                    shareds.RemoveAt(0);
                }
                catch (System.Exception ex) { }
            }
        }

        public void AddCommand(SharedClass shared)
        {
            if (!Globals.IsConnected)
            {
                shareds?.Clear();
                return;
            }

            shareds?.Add(shared);
        }
    }
}