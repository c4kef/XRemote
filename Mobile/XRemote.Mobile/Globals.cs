using System;
using System.Threading;
using System.IO;
using XRemote.Mobile.PopupWindows;
using Xamarin.Forms;
using XRemote.ShareStructure;
using Newtonsoft.Json;
using OpenWeatherAPI;

namespace XRemote.Mobile
{
    public static class Globals
    {
        public static ControlComputer controlComputer { get; private set; }
        public static MainPage mainPage { get; private set; }
        public static SettingsPage settingsPage { get; private set; }
        public static Settings settings { get; private set; }

        public static bool IsConnected;
        public static Thread threadHandler;
        public static Query weatherInfo;

        public static void Init(MainPage main)
        {
            int countUnsuccessfull = 0;

            controlComputer = new ControlComputer();
            settingsPage = new SettingsPage();
            mainPage = main;

            tryAgain:
            try
            {
                if (LoadSettings())
                {
                     System.Console.WriteLine($"City is: {settings.CityTemp}");
                }
                else
                {
                    settings = new Settings();
                }
                weatherInfo = API.Query(settings.CityTemp);
            }
            catch { if (countUnsuccessfull++ > 3) Environment.Exit(1); else goto tryAgain; }
        }

        public static void SendCommand(SharedClass command) => Device.BeginInvokeOnMainThread(() => MessagingCenter.Send(command, "XServiceCommand"));
        public static void Alert(string message) => Device.BeginInvokeOnMainThread(() => MessagingCenter.Send(message, "Toast"));

        public static void CommandHandler(SharedClass command)
        {
            switch (command.Command)
            {
                case "SystemInfo":
                    {
                        controlComputer.SetSystemInformation(command.Value);
                        break;
                    }
                case "SystemTemps":
                    {
                        controlComputer.SetTemperatureInformation(command.Value);
                        break;
                    }
                case "FileInfo":
                    {
                        FilesInfo files = FilesInfo.FromBin(command.Files);
                        ImageSource img = ImageSource.FromStream(() =>
                        {
                            MemoryStream msIcon = new MemoryStream(files.data[0].Icon);
                            return msIcon;
                        });
                        controlComputer.AddApp(img, command.Value);
                        break;
                    }
                case "InstalledApps":
                    {
                        controlComputer.ClearApps();
                        FilesInfo files = FilesInfo.FromBin(command.Files);
                        int i = 0;
                        foreach (var file in files.data)
                        {
                            ImageSource img = ImageSource.FromStream(() =>
                            {
                                MemoryStream msIcon = new MemoryStream(file.Icon);
                                return msIcon;
                            });
                            controlComputer.AddApp(img, file.Path);
                             System.Console.WriteLine($"C4ke: {++i}");
                        }
                        controlComputer.isBusy = false;
                        break;
                    }
                case "OpenApp":
                    {
                        controlComputer.isBusy = false;
                        controlComputer.busyElement.Children[0].IsVisible = true;
                        controlComputer.busyElement.Children[1].IsVisible = false;
                        break;
                    }
                default:
                     System.Console.WriteLine($"-> Command not found! ({command.Command})");
                    break;
            }
        }

        public static string FirstUpperString(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static void SaveSettings()
        {
            if (settings == null)
                return;

            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Setup.json"), JsonConvert.SerializeObject(settings));
        }

        public static bool LoadSettings()
        {
            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Setup.json")))
                return false;

            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Setup.json")));
            return true;
        }
    }
}
