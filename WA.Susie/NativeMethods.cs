namespace WA.Susie
{
    using System.Runtime.InteropServices;

    internal static class NativeMethods
    {
        private const string Kernel32 = "kernel32.dll";

        // https://docs.microsoft.com/ja-jp/windows/win32/api/winbase/nf-winbase-locallock
        // [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        internal static extern unsafe void* LocalLock(void* hMem);

        // https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-localunlock
        // [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool LocalUnlock(void* hMem);

        // https://docs.microsoft.com/ja-jp/windows/win32/api/winbase/nf-winbase-localfree
        // [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport(Kernel32, CharSet = CharSet.Auto)]
        internal static extern unsafe void* LocalFree(void* hMem);

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
