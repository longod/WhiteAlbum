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
        internal override async Task<ImageOutputResult> DecodeImageAsync(FileLoader loader, bool thumbnail = false)
        {
            _args[0] = loader.Stream;
            return await Task.Run(() =>
            {
                BitmapDecoder decoder = (BitmapDecoder)_constructor.Invoke(_args);
                ImageOutputResult result = new ImageOutputResult() { Image = new ImageOutput() };
                if (thumbnail && decoder.Thumbnail != null)
                {
                    result.Image.bmp = decoder.Thumbnail; // todo how to generate
                }
                else
                {
                    result.Image.bmp = decoder.Frames[0]; // todo multiple images such as gif
                }
                return result;
            });

            // fallback
            // またはプラグインを優先して、ビルトインをfallbackとして使う
            // return await base.TryDecodeAsync(loader);
        }
    }
}
