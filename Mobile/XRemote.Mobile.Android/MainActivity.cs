using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.OS;
using Sharpnado.MaterialFrame.Droid;
using Android.Provider;
using Android.Content;
using Rg.Plugins.Popup;

namespace XRemote.Mobile.Droid
{
    [Activity(Label = "XRemote.Mobile", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            /*if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
                Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                Window.DecorView.SystemUiVisibility = 0;
                Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            }*/
            AndroidMaterialFrameRenderer.BlurProcessingDelayMilliseconds = 10;

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            Rg.Plugins.Popup.Popup.Init(this);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            LoadApplication(new App());

            Intent serviceIntent = new Intent(this, typeof(AndroidService));
            StartForegroundService(serviceIntent);
            /*if (Intent.Action == Intent.ActionSend)
            {
                // Пример данных хранящийхся в Extras
                var uriFromExtras = Intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri;
                var subject = Intent.GetStringExtra(Intent.ExtraSubject);

                ClipData clipData = Intent.ClipData;
                ClipData.Item item = clipData.GetItemAt(0);
                string text = item.ToString();
                // Получаем данные из ClipData 
                var pdf = Intent.ClipData.GetItemAt(0);

                // Открываем поток из URI 
                var pdfStream = ContentResolver.OpenInputStream(pdf.Uri);

                // Сохраняем 
                var memOfPdf = new System.IO.MemoryStream();
                pdfStream.CopyTo(memOfPdf);
                var docsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                var filePath = System.IO.Path.Combine(docsPath, "temp.pdf");

                System.IO.File.WriteAllBytes(filePath, memOfPdf.ToArray());
            }*/
        }

        private string GetFilePath(Android.Net.Uri uri)
        {
            string[] proj = { MediaStore.Images.ImageColumns.Data };
            var cursor = ManagedQuery(uri, proj, null, null, null);
            var colIndex = cursor.GetColumnIndex(MediaStore.Images.ImageColumns.Data);
            cursor.MoveToFirst();
            return cursor.GetString(colIndex);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}