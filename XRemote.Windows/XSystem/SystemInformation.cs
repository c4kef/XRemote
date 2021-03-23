using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using OpenHardwareMonitor.Hardware;
using System.Runtime.InteropServices;

namespace XRemote.Windows.XSystem
{
    public enum EWX_SHUTDOWN_INFO
    {
        EWX_LOGOFF = 0x00000000,
        EWX_SHUTDOWN = 0x00000001,
        EWX_REBOOT = 0x00000002,
        EWX_FORCE = 0x00000004,
        EWX_POWEROFF = 0x00000008,
        EWX_FORCEIFHUNG = 0x00000010,
    };
    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    class SystemInformation
    {
        public SystemInformation()
        {
            updateVisitor = new UpdateVisitor();
            computer = new Computer();
            computer.Open();
            computer.CPUEnabled = true;
            computer.GPUEnabled = true;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr
        phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name,
        ref long pluid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall,
        ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool ExitWindowsEx(int flg, int rea);

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0; // 'Large icon
        public const uint SHGFI_SMALLICON = 0x1; // 'Small icon
        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const int TOKEN_QUERY = 0x00000008;
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        internal UpdateVisitor updateVisitor;
        internal Computer computer;
        public Bitmap GetFileIcon(string pathToFile)
        {
            IntPtr hImgSmall;
            SHFILEINFO shinfo = new SHFILEINFO();
            hImgSmall = SHGetFileInfo(pathToFile, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);
            return Icon.FromHandle(shinfo.hIcon).ToBitmap();
        }

        internal void ShutdownWindows(object p)
        {
            throw new NotImplementedException();
        }

        public void ShutdownWindows(EWX_SHUTDOWN_INFO flg)
        {
            TokPriv1Luid tp;
            IntPtr hproc = GetCurrentProcess();
            IntPtr htok = IntPtr.Zero;
            OpenProcessToken(hproc, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok);
            tp.Count = 1;
            tp.Luid = 0;
            tp.Attr = SE_PRIVILEGE_ENABLED;
            LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tp.Luid);
            AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            ExitWindowsEx((int)flg, 0);
        }

        public float GetTemperatureProcessor()
        {
            computer.Accept(updateVisitor);
            for (int i = 0; i < computer.Hardware.Length; i++)
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                            return (float)computer.Hardware[i].Sensors[j].Value;
            return 0;
        }

        public float GetTemperatureGPU()
        {
            computer.Accept(updateVisitor);
            for (int i = 0; i < computer.Hardware.Length; i++)
                if (computer.Hardware[i].HardwareType == HardwareType.GpuNvidia || computer.Hardware[i].HardwareType == HardwareType.GpuAti)
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                            return (float)computer.Hardware[i].Sensors[j].Value;
            return 0;
        }
    }
}
