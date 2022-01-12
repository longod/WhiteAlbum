namespace WA
{
    using System.Diagnostics;
    using System.IO;

    public static class DirectoryUtility
    {
        public static string GetBaseDirectory()
        {
            // AppContext.BaseDirectory doesn't point to apphost directory that PublishSingleFile enabled.
            // https://github.com/dotnet/runtime/issues/3704
            return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }
    }
}
