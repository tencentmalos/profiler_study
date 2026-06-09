using SCLCoreCLR;
using System;
using System.Runtime.InteropServices;



namespace ProfilerStudy
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

        static bool mIsReadWineVersion = false;
        static IntPtr mWineVersion = IntPtr.Zero;
        static bool mIsRunOnWine = false;

        public static bool IsRunOnWine()
        {
            if(!mIsReadWineVersion)
            {
                try
                {
                    // 在Wine环境中，这个调用会成功，并返回Wine版本字符串的指针。
                    // 在真正的Windows环境中，这个调用会抛出异常。
                    mWineVersion = wine_get_version();
                    Log.WriteLine($"Now run on wine, wine version is:{mWineVersion.ToString()}");
                    mIsRunOnWine = true;
                }
                catch
                {
                    mIsRunOnWine = false;
                }

                mIsReadWineVersion = true;
            }

            return mIsRunOnWine;
            ////string winePrefix = Environment.GetEnvironmentVariable("WINEPREFIX");
            ////return !string.IsNullOrEmpty(winePrefix);
        }
    }

}