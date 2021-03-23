using Android.App;
using Android.Content;
using Android.OS;
using Xamarin.Forms;
using XRemote.ShareStructure;
using Android.Widget;

namespace XRemote.Mobile.Droid
{
    [Service]
    public class AndroidService : Service
    {
        private XServerClient xServerClient;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (xServerClient != null)
                return StartCommandResult.NotSticky;

            xServerClient = new XServerClient();
            System.Threading.Tasks.Task.Run(xServerClient.Connect);

            NotificationChannel channel = new NotificationChannel("XRemote", "XRemote Service", NotificationImportance.Default);
            NotificationManager notification = (NotificationManager)GetSystemService(Java.Lang.Class.FromType(typeof(NotificationManager)));
            notification.CreateNotificationChannel(channel);
            Intent notificationIntent = new Intent(this, typeof(MainActivity));
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, 0);
            var notificationBuilder = new Notification.Builder(this, channel.Id)
                .SetContentTitle("XRemote Serive")
                .SetContentText("Service remote connection to your device.")
                .SetContentIntent(pendingIntent)
                .Build();

            MessagingCenter.Unsubscribe<SharedClass>(this, "XServiceCommand");
            MessagingCenter.Subscribe<SharedClass>(this, "XServiceCommand", (value) =>
            {
                xServerClient.AddCommand(value);
            });

            MessagingCenter.Unsubscribe<SharedClass>(this, "Toast");
            MessagingCenter.Subscribe<string>(this, "Toast", (msg) =>
            {
                Toast.MakeText(this, msg, ToastLength.Long).Show();
            });

            StartForeground(1, notificationBuilder);

            return StartCommandResult.NotSticky;
        }
    }
}