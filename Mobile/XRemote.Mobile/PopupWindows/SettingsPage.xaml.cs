using System;
using Rg.Plugins.Popup.Services;
using Sharpnado.Tasks;
using System.Collections.Generic;
using System.Threading;
using Xamarin.Forms;
using OpenWeatherAPI;

namespace XRemote.Mobile.PopupWindows
{
    public partial class SettingsPage
    {
        private List<View> panels;
        private bool canFind = true;
        
        public SettingsPage()
        {
            InitializeComponent();
            panels = new List<View>();
        }

        private void OnClose(object sender, EventArgs e)
        {
            Globals.mainPage.MenuPanel = true;
            PopupNavigation.Instance.PopAsync();
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

        private void FindCity(object sender, EventArgs e)
        {
            string tempCity = string.Empty;
            if (string.IsNullOrEmpty(tempCity = NameCity.Text) || !canFind)
                return;

            ProcessFindCity.IsVisible = true;
            BtnFindCity.IsVisible = canFind = false;
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    Query tempWeather = API.Query(tempCity);
                    Globals.weatherInfo = tempWeather;
                    Globals.settings.CityTemp = tempCity;
                    Device.BeginInvokeOnMainThread(() => StatusMessage.Text = "город успешно обновлен");
                }
                catch
                {
                    Device.BeginInvokeOnMainThread(() => StatusMessage.Text = "город не был найден");
                }

                Device.BeginInvokeOnMainThread(() => ProcessFindCity.IsVisible = false);
                Device.BeginInvokeOnMainThread(() => BtnFindCity.IsVisible = canFind = true);
            })).Start();

        }
    }
}