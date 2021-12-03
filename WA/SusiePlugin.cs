using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace WA
{
    public class SusiePlugin : IDisposable
    {
        public readonly object _resolutionLock = new object();

        public SusiePlugin(string path)
        {
            var context = AssemblyLoadContext.Default;
            //lock (_resolutionLock)
            {
                var assembly = context.LoadFromAssemblyPath(path);
            }
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
