namespace WA
{
    using System;
    using System.IO;
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

        // animationを考慮すると、 ReadOnlyCollection<BitmapFrame> のようなほうがよい
        // boxingしまくりで遅そう…
        internal override async Task<ImageOutputResult> DecodeAsync(FileLoader loader)
        {
            _args[0] = loader.Stream;
            return await Task.Run(() =>
            {
                BitmapDecoder decoder = (BitmapDecoder)_constructor.Invoke(_args);
                ImageOutputResult result = new ImageOutputResult() { Bmp = decoder.Frames[0] };
                return result;
            });

            // fallback
            // またはプラグインを優先して、ビルトインをfallbackとして使う
            // return await base.TryDecodeAsync(loader);
        }
    }
}
