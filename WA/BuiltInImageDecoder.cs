// (c) longod, MIT License
namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;

    internal class BuiltInImageDecoder : ImageDecoder
    {
        private System.Reflection.ConstructorInfo _constructor;
        private object[] _args = new object[] { null, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnDemand };

        internal BuiltInImageDecoder(Type bitmapDecoder)
        {
            // cacheしておきたい
            _constructor = bitmapDecoder.GetConstructor(new Type[] { typeof(Stream), typeof(BitmapCreateOptions), typeof(BitmapCacheOption) });
        }

        // boxingしまくりで遅そう…
        internal override async Task<BitmapSource> TryDecodeAsync(FileLoader loader)
        {
            _args[0] = loader.Stream;
            BitmapDecoder decoder = null;
            using (new StopwatchScope("TryDecodeAsync"))
            {
                decoder = (BitmapDecoder)_constructor.Invoke(_args);
            }
            return decoder.Frames[0];

            // fallback
            // またはプラグインを有線して、ビルトインをfallbackとして使う
            //return await base.TryDecodeAsync(loader);
        }
    }

}
