using System;
using Rg.Plugins.Popup.Services;
using Sharpnado.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using XRemote.ShareStructure;
using XRemote.Mobile.Images;
using System.IO;

namespace XRemote.Mobile.PopupWindows
{
    public partial class ControlComputer
    {
        private List<View> panels;
        private StackLayout content;
        public bool isBusy;
        public Grid busyElement;

        public ControlComputer()
        {
            InitializeComponent();
            panels = new List<View>();
            panels.Add(SystemInfo);
            panels.Add(SyncPanel);
            panels.Add(AppsPanel);
            panels.Add(PowerPanel);

            foreach (View element in panels)
            {
                if (element.IsVisible)
                {
                    element.FadeTo(0);
                    element.IsVisible = false;
                }
            }

            SystemInfo.IsVisible = true;
            SystemInfo.FadeTo(1);
        }
        private void OpenPanel(View panel)
        {
            if (panel.IsVisible)
                return;

            foreach (View element in panels)
            {
                if (element.IsVisible)
                {
                    element.FadeTo(0);
                    element.IsVisible = false;
                }
            }

            panel.IsVisible = true;
            panel.FadeTo(1);
        }
        public void SetSystemInformation(string information)
        {
            string[] splitInformation = information.Split('|');

            nameOS.Text = splitInformation[0];
            sizeDisk.Text = splitInformation[1];
            nameWiFi.Text = splitInformation[2];
            sizeScreen.Text = splitInformation[3];
        }
        public void SetTemperatureInformation(string information)
        {
            string[] splitInformation = information.Split('|');

            CPUTemp.Text = splitInformation[0];
            GPUTemp.Text = splitInformation[1];
        }
        private void OnClose(object sender, EventArgs e)
        {
            Globals.mainPage.MenuPanel = true;
            PopupNavigation.Instance.PopAsync();
        }
        private void SelectApp(object sender, EventArgs e)
        {
            if (!Globals.IsConnected)
                return;

            if (isBusy)
            {
                Globals.Alert("Похоже Вы еще выбираете файл, закончите Ваш выбор и попробуйте ещё раз");
                return;
            }

            isBusy = true;
            Globals.SendCommand(new SharedClass() { Command = "SelectApp" });
            Globals.Alert("Выберите на Вашем устройстве приложение в открывшемся окне");
        }
        public void ClearApps()
        {
            content = null;
            AppsList.Children.Clear();
        }
        public void AddApp(ImageSource icon, string path)
        {
            if (content == null || content.Children.Count == 4)
            {
                content = new StackLayout();
                content.Orientation = StackOrientation.Horizontal;
                content.Spacing = 40;
                content.HorizontalOptions = LayoutOptions.Center;
                AppsList.Children.Add(content);
            }

            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += (sender, e) =>
            {
                if (!Globals.IsConnected)
                    return;

                if (isBusy)
                {
                    Globals.Alert("Дождитесь завершения текущей задачи");
                    return;
                }

                busyElement = (sender as Image).Parent as Grid;

                busyElement.Children[0].IsVisible = false;
                busyElement.Children[1].IsVisible = true;

                isBusy = true;

                Globals.SendCommand(new SharedClass() { Command = "OpenApp", Value = path });
            };

            Grid appModel = new Grid();
            appModel.RowDefinitions.Add(new RowDefinition() { Height = 50 });
            appModel.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Star });

            Image iconApp = new Image();
            iconApp.Source = icon;
            iconApp.GestureRecognizers.Add(tap);
            iconApp.IsVisible = true;

            ActivityIndicator loadApp = new ActivityIndicator();
            loadApp.IsRunning = true;
            loadApp.IsVisible = false;

            appModel.Children.Add(iconApp);
            appModel.Children.Add(loadApp);

            Grid.SetRow(iconApp, 0);
            Grid.SetColumn(iconApp, 0);

            Grid.SetRow(loadApp, 0);
            Grid.SetColumn(loadApp, 0);

            content.Children.Add(appModel);
        }
        private void Shutdown(object sender, EventArgs e) => Globals.SendCommand(new SharedClass() { Command = "ShutdownPC" });
        private void Reload(object sender, EventArgs e) => Globals.SendCommand(new SharedClass() { Command = "ReloadPC" });
        private void Sleep(object sender, EventArgs e) => Globals.SendCommand(new SharedClass() { Command = "SleepPC" });
        private void OpenSystemInfo(object sender, EventArgs e) => OpenPanel(SystemInfo);
        private void OpenSyncPanel(object sender, EventArgs e) => OpenPanel(SyncPanel);
        private void OpenPowerPanel(object sender, EventArgs e) => OpenPanel(PowerPanel);
        private void OpenAppsPanel(object sender, EventArgs e) => OpenPanel(AppsPanel);
    }
}