// (c) longod, MIT License

namespace WA
{
    using System.Runtime.InteropServices;

    // https://qiita.com/mitsu_at3/items/94807ee0b3bf34ffb6b2
    // https://qiita.com/kob58im/items/e40081491a75204ccb6e
    internal static class NativeMethods
    {
        const string Kernel32 = "kernel32.dll";

        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        internal static extern unsafe void* LocalLock(void* hMem);

        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        internal static extern unsafe bool LocalUnlock(void* hMem);

        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        internal static extern unsafe bool LocalFree(void* hMem);

    }
}
