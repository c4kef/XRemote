using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using Network;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using XRemote.ShareStructure;

namespace XRemote.Windows.Server
{
    class ServerObject
    {
        private ServerConnectionContainer server;

        public delegate void SubscribeExceptions(Exception ex);
        public event SubscribeExceptions subExceptions;
        private Connection connection;
        private XSystem.SystemInformation xSystem;
        private List<string> additionalApps;

        public void Listen()
        {
            additionalApps = new List<string>();
            server = ConnectionFactory.CreateServerConnectionContainer(1708, false);
            server.AllowUDPConnections = false;
            server.ConnectionEstablished += (connection, type) =>
            {
                if (this.connection != null && this.connection.IsAlive)
                {
                    connection.Close(Network.Enums.CloseReason.ServerClosed);
                    return;
                }

                this.connection = connection;
                connection.RegisterStaticPacketHandler<SharedClass>(HandlerCommand);
            };

            server.Start();
            xSystem = new XSystem.SystemInformation();
        }

        private void HandlerCommand(SharedClass packet, Connection connection)
        {
            SharedClass shared = new SharedClass();
            switch (packet.Command)
            {
                case "GetSystemInformation":
                    {
                        var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                        string wifi = SSIDWiFi();
                        wifi = string.IsNullOrEmpty(wifi) ? "Not found" : wifi;
                        shared.Command = "SystemInfo";
                        shared.Value = $"{(string)reg.GetValue("ProductName")}|{DriveInfo.GetDrives().First(a => a.Name == @"C:\").TotalSize / 1000000000}gb|{wifi}|{SystemInformation.VirtualScreen.Width}x{SystemInformation.VirtualScreen.Height}";
                        break;
                    }
                case "GetSystemTemperatures":
                    {
                        string tempCPU = (xSystem.GetTemperatureProcessor() != 0) ? $"{xSystem.GetTemperatureProcessor()}˚" : "Not found";
                        string tempGPU = (xSystem.GetTemperatureGPU() != 0) ? $"{xSystem.GetTemperatureGPU()}˚" : "Not found";
                        shared.Command = "SystemTemps";
                        shared.Value = $"{tempCPU}|{tempGPU}";
                        break;
                    }
                case "CopyToClipboard":
                    {
                        break;
                    }
                case "ShutdownPC":
                    {
                        xSystem.ShutdownWindows(XSystem.EWX_SHUTDOWN_INFO.EWX_SHUTDOWN | XSystem.EWX_SHUTDOWN_INFO.EWX_FORCE);
                        break;
                    }
                case "ReloadPC":
                    {
                        xSystem.ShutdownWindows(XSystem.EWX_SHUTDOWN_INFO.EWX_REBOOT | XSystem.EWX_SHUTDOWN_INFO.EWX_FORCE);
                        break;
                    }
                case "SleepPC":
                    {
                        Application.SetSuspendState(PowerState.Hibernate, true, true);
                        break;
                    }
                case "GetFileInfo":
                    {
                        if (!File.Exists(packet.Value))
                        {
                            MessageBox.Show("File not found!");
                            break;
                        }
                        FileInfo file = new FileInfo(packet.Value);
                        shared.Command = "FileInfo";
                        shared.Value = file.Name;
                        using (var ms = new MemoryStream())
                        {
                            xSystem.GetFileIcon(packet.Value).Save(ms, ImageFormat.Png);
                            FilesInfo files = new FilesInfo();
                            files.Add(ms.ToArray());
                            shared.Files = files.ToArray();
                        }
                        break;
                    }
                case "GetInstalledApps":
                    {
                        SendInstalledApps();
                        break;
                    }
                case "OpenApp":
                    {
                        if (!File.Exists(packet.Value))
                            break;


                        try { Process.Start(packet.Value); }
                        catch { }

                        shared.Command = "OpenApp";
                        shared.Value = "OK";
                        break;
                    }
                case "SelectApp":
                    {
                        Thread handler = new Thread(new ThreadStart(() =>
                        {
                            OpenFileDialog selectFile = new OpenFileDialog();
                            selectFile.Filter = "Приложение (.exe)|*.exe";
                            SharedClass answer = new SharedClass();

                            if (selectFile.ShowDialog() != DialogResult.OK)
                                goto exit;

                            additionalApps.Add(selectFile.FileName);

                            exit:
                            SendInstalledApps();
                        }));
                        handler.SetApartmentState(ApartmentState.STA);
                        handler.Start();
                        break;
                    }
                default:
                    MessageBox.Show($"-> Command not found! ({packet.Command})");
                    break;
            }
            connection.Send(new SharedResponse(shared, packet));
        }

        private void SendInstalledApps()
        {
            List<string> tempApps = new List<string>();
            tempApps.AddRange(additionalApps);
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        if (subkey.GetValue("DisplayIcon") == null)
                            continue;

                        string path = subkey.GetValue("DisplayIcon").ToString().Replace(",", "").Replace("\"", "");
                        int indexExe = path.IndexOf(".exe") + 4;
                        path = path.Remove(indexExe, path.Length - indexExe);
                        if (!File.Exists(path))
                            continue;

                        tempApps.Add(path);
                    }
                }
            }
            FilesInfo files = new FilesInfo();
            foreach (string path in tempApps.Distinct().ToList())
                if (File.Exists(path))
                {
                    MemoryStream ms = new MemoryStream();
                    Bitmap icon = new Bitmap(xSystem.GetFileIcon(path), new Size(100, 100));
                    icon.Save(ms, ImageFormat.Png);
                    files.Add(ms.ToArray(), null, path);
                }

            server.TCP_BroadCast(new SharedClass() { Command = "InstalledApps", Files = files.ToArray() });
        }

        private string SSIDWiFi()
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "netsh.exe",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var line = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("SSID") && !l.Contains("BSSID"));
            if (line == null)
            {
                return string.Empty;
            }
            var ssid = line.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1].TrimStart();
            return ssid;
        }
        protected internal void SendInfoException(Exception ex) => subExceptions(ex);
        protected internal void Disconnect() => server.Stop();
    }
}