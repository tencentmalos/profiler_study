using System;
using System.Runtime.InteropServices;



namespace FramePro
{
    public enum Platform
    {
        Unknown = -1,
        Windows,
        Windows_UWP,
        XBoxOne,
        Unused,
        Linux,
        PS4,
        Android,
        Mac,
        iOS,
        Switch
    }


    public static class PlatformTool
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        static extern IntPtr wine_get_version();

        public static bool IsRunOnWine()
        {
            try
            {
                // 在Wine环境中，这个调用会成功，并返回Wine版本字符串的指针。
                // 在真正的Windows环境中，这个调用会抛出异常。
                IntPtr versionPtr = wine_get_version();

                return true;
            }
            catch
            {
                // 如果抛出异常，我们可以假定它不是Wine环境。
                return false;
            }


            ////string winePrefix = Environment.GetEnvironmentVariable("WINEPREFIX");
            ////return !string.IsNullOrEmpty(winePrefix);
        }
    }

}