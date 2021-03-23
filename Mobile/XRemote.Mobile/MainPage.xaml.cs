using System;
using System.Threading;
using Rg.Plugins.Popup.Services;
using Sharpnado.Tasks;
using Xamarin.Forms;
using XRemote.ShareStructure;
using System.Threading.Tasks;
using OpenWeatherAPI;
using Sharpnado.MaterialFrame;

namespace XRemote.Mobile
{
    public partial class MainPage : ContentPage
    {
        public bool MenuPanel
        {
            set
            {
                MainMenu.IsVisible = value;
            }
            get
            {
                return MainMenu.IsVisible;
            }
        }

        public MainPage()
        {
            SetValue(NavigationPage.HasNavigationBarProperty, false);

            MessagingCenter.Send("start_service", "XRemoteService");

            InitializeComponent();

            MessagingCenter.Unsubscribe<SharedClass>(this, "XClientCommand");
            MessagingCenter.Subscribe<SharedClass>(this, "XClientCommand", (value) =>
            {
                Globals.CommandHandler(value);
            });

            bool isNight = (GetHello() == "Добрый вечер" || GetHello() == "Доброй ночи");
            CanvasMainPage.BackgroundImageSource = isNight ? ImageSource.FromFile("catalina_dark.jpg") : ImageSource.FromFile("catalina_light.jpg");
            HelloTime.Text = $"{GetHello()}, Артемий";

            ResourcesHelper.SetBlurStyle(MaterialFrame.BlurStyle.Light);
            ResourcesHelper.SetAcrylic(true);

            if (Globals.threadHandler == null || !Globals.threadHandler.IsAlive)
            {
                Globals.threadHandler = new Thread(new ThreadStart(HandlerThread));
                Globals.threadHandler.Start();
            }

            Globals.Init(this);
        }

        protected override bool OnBackButtonPressed() => true;

        protected override void OnAppearing()
        {
            base.OnAppearing();

            TaskMonitor.Create(DelayExecute);
        }

        private async Task DelayExecute()
        {
            await Task.Delay(3000);
            Globals.SendCommand(new SharedClass() { Command = "GetInstalledApps" });
            Globals.SendCommand(new SharedClass() { Command = "GetSystemInformation" });
        }

        public void HandlerThread()
        {
            while (true)
            {
                Thread.Sleep(2_000);
                Globals.SendCommand(new SharedClass() { Command = "GetSystemTemperatures" });

                Device.BeginInvokeOnMainThread(() =>
                {
                    PC_Status.Text = (Globals.IsConnected) ? "В сети" : "Неактивен";
                    WeatherInfo.Text = $"{Globals.FirstUpperString(Globals.weatherInfo.Weathers[0].Description)} {Math.Round(Globals.weatherInfo.Main.Temperature.CelsiusCurrent)}°C  {Globals.FirstUpperString(Globals.settings.CityTemp)}";
                });
            }
        }

        private string GetHello()
        {
            int Hour = int.Parse(DateTime.Now.ToString("HH"));
            if (Hour < 4 || Hour >= 22) return "Доброй ночи";
            if (Hour < 12) return "Доброе утро";
            if (Hour < 16) return "Добрый день";
            return "Добрый вечер";
        }

        private void OpenControlPC(object sender, EventArgs e)
        {
            MenuPanel = false;
            PopupNavigation.Instance.PushAsync(Globals.controlComputer);
        }

        private void OpenSettings(object sender, EventArgs e)
        {
            MenuPanel = false;
            PopupNavigation.Instance.PushAsync(Globals.settingsPage);
        }
    }
}
