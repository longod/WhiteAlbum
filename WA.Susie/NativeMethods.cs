namespace WA.Susie
{
    using System.Runtime.InteropServices;

    // todo
    // https://qiita.com/mitsu_at3/items/94807ee0b3bf34ffb6b2
    // https://qiita.com/kob58im/items/e40081491a75204ccb6e
    internal static class NativeMethods
    {
        private const string Kernel32 = "kernel32.dll";

        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        internal static extern unsafe void* LocalLock(void* hMem);

        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        internal static extern unsafe bool LocalUnlock(void* hMem);

        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        internal static extern unsafe bool LocalFree(void* hMem);

        internal unsafe struct LocalLockScope<T> : System.IDisposable
                where T : unmanaged
        {
            private void* _ptr;
            private T* _local;

            internal T* Pointer => _local;

            internal LocalLockScope(void* ptr)
            {
                _ptr = ptr;
                _local = (T*)LocalLock(_ptr);
            }

            public void Dispose()
            {
                _ = LocalUnlock(_ptr);
            }
        }
    }
}
