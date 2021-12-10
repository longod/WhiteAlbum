// (c) longod, MIT License
namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Media;
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
                    if (d.Decode(loader, out var im))
                    {
                        return im;
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
            int rawStride = ((int)image.desc.Width * System.Windows.Media.PixelFormats.Bgr24.BitsPerPixel + 7) / 8;

            var bmp = await Task.Run(() =>
            {
                var b = BitmapSource.Create(
                  (int)image.desc.Width,
                  (int)image.desc.Height,
                  WpfUtility.DefaultDpi,
                  WpfUtility.DefaultDpi,
                  System.Windows.Media.PixelFormats.Bgr24,
                  null,
                  image.binary,
                  rawStride);
                var tb = new TransformedBitmap(b, new ScaleTransform(1.0, -1.0));
                tb.Freeze();
                return tb;
            }
            );
            // BitmapSourceは Must create DependencySource on same Thread as the DependencyObject
            // を発生させるので、freezeする
            // これを継承した生成器や、decoderはdispatcherでUIスレッドで作られるようにされていると思われるが、
            // BitmapSourceはそこをケアしていないのだろう
            // どこかの生成器でリークしているかも
            // https://pierre3.hatenablog.com/entry/2015/10/25/001207

            // BitmapSourceの基点は左上だが、本来のbmp formatのpositive heightは左下基点で反転してしまう
            // 事前にメモリを反転して詰め直すか、scale transformで行なう
            // exif も追々考慮する必要がある
            return bmp;
        }

        internal void RegisterDecoder(IDecoder decoder)
        {
            if (_decoders == null)
            {
                _decoders = new List<IDecoder>();
            }
            _decoders.Add(decoder);
        }

        internal void RegisterDecoder(IEnumerable<IDecoder> decoder)
        {
            if (_decoders == null)
            {
                _decoders = new List<IDecoder>();
            }
            _decoders.AddRange(decoder);
        }
    }
}
