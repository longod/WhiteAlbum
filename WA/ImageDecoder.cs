// (c) longod, MIT License
namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;


    // extensionに対応したdecoderセット
    // renderer backendの多用化を考えると、この段階ではBitmapSource じゃない中間フォーマットを返して欲しい
    // しかし builtinや通常のwpfだと、BitmapSourceがもっとも扱いやすい
    // internal interface IImageDecoder<Resource> {}
    // これでバックエンドに対応するとマッピング時に型が必要になってしまう…
    // bitmap info, image desc, stream, span
    internal class ImageDecoder
    {
        private List<IDecoder> _decoders;

        internal virtual async Task<BitmapSource> TryDecodeAsync(FileLoader loader)
        {
            var image = await Task.Run(() =>
            {
                foreach (var d in _decoders)
                {
                    var desc = d.Decode(loader);
                    if (desc != null)
                    {
                        return desc;
                    }
                }

                return null;
            });

            if (image != null)
            {
                var source = await Convert(image);
                return source;
            }

            return null;
        }

        private async Task<BitmapSource> Convert(IntermediateImage image)
        {
            // temp
            return await Task.Run(() => BitmapSource.Create(
                (int)image.desc.Width,
                (int)image.desc.Height,
                WpfUtility.DefaultDpi,
                WpfUtility.DefaultDpi,
                System.Windows.Media.PixelFormats.Default,
                BitmapPalettes.BlackAndWhite,
                image.binary,
                0));
        }

        internal void RegisterDecoder(IDecoder decoder)
        {
            _decoders.Add(decoder);
        }

        internal void RegisterDecoder(IEnumerable<IDecoder> decoder)
        {
            _decoders.AddRange(decoder);
        }
    }
}
