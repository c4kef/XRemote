using Xamarin.Forms;
using Sharpnado.MaterialFrame;

namespace XRemote.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Initializer.Initialize(false, false);
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
            Globals.SaveSettings();
        }

        protected override void OnResume()
        {
        }
    }
}

