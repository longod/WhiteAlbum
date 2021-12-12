﻿// (c) longod, MIT License
namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        private List<IPluginProxy> _decoders;

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

        private async Task<BitmapSource> Convert(DecodedImage image)
        {
            // fixme temp
            //int rawStride = ((int)image.Width * image.BitsPerPixel + 7) / 8;

            var bmp = await Task.Run(() =>
            {
                PixelFormat format = GetPixelFormat(image);
                BitmapPalette plaette = GetBitmapPalette(image);
                Transform transform = GetTransform(image);

                var stride = (((image.Width * image.BitsPerPixel) + 31u) & ~31u) >> 3;
                var b = BitmapSource.Create(
                  (int)image.Width,
                  (int)image.Height,
                  WpfUtility.DefaultDpiX,
                  WpfUtility.DefaultDpiX,
                  format,
                  plaette,
                  image.Binary,
                  (int)stride);
                if (transform == null)
                {
                    b.Freeze();
                    return b;
                }

                var tb = new TransformedBitmap(b, transform);
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

        private Transform GetTransform(DecodedImage image)
        {
            RotateTransform rotate = null;
            switch (image.Rotation)
            {
                case DecodedImage.ImageRotation.None:
                    break;
                case DecodedImage.ImageRotation.Degree90:
                    rotate = new RotateTransform(90.0f);
                    break;
                case DecodedImage.ImageRotation.Degree180:
                    rotate = new RotateTransform(180.0f);
                    break;
                case DecodedImage.ImageRotation.Degree270:
                    rotate = new RotateTransform(270.0f);
                    break;
                default:
                    break;
            }

            ScaleTransform scale = null;
            switch (image.Orientation)
            {
                case DecodedImage.ImageOrientation.TopLeft:
                    break;
                case DecodedImage.ImageOrientation.BottomLeft:
                    scale = new ScaleTransform(1.0, -1.0);
                    break;
                default:
                    break;
            }

            if (rotate == null && scale == null)
            {
                return null;
            }
            else if (scale != null)
            {
                return scale;
            }
            else if (rotate != null)
            {
                return rotate;
            }
            else
            {
                // TODO 乗算順の仕様は？
                var trs = scale.Value * rotate.Value;
                return new MatrixTransform(trs);
            }
        }

        private static BitmapPalette GetBitmapPalette(DecodedImage image)
        {
            switch (image.Format)
            {
                case DecodedImage.PixelFormat.Index:
                    // FIXME 元データのパレットを参照して生成する
                    switch (image.BitsPerPixel)
                    {
                        case 1:
                            return BitmapPalettes.BlackAndWhite; // 2 colors
                        case 2:
                            return BitmapPalettes.Gray4; // 4 colors
                        case 4:
                            return BitmapPalettes.Halftone8; // 16 colors
                        case 8:
                            return BitmapPalettes.Halftone256; // 256 colors
                        default:
                            throw new NotSupportedException($"image.BitsPerPixel : {image.BitsPerPixel}");
                    }

                default:
                    return null;
            }
        }

        private static PixelFormat GetPixelFormat(DecodedImage image)
        {
            switch (image.Format)
            {
                case DecodedImage.PixelFormat.RGB:
                    return PixelFormats.Rgb24;
                case DecodedImage.PixelFormat.RGBA:
                    // argb, rgba
                    break;
                case DecodedImage.PixelFormat.BGR:
                    return PixelFormats.Bgr24;
                case DecodedImage.PixelFormat.BGRA:
                    return PixelFormats.Bgra32;
                case DecodedImage.PixelFormat.Index:
                    switch (image.BitsPerPixel)
                    {
                        case 1:
                            return PixelFormats.Indexed1;
                        case 2:
                            return PixelFormats.Indexed2;
                        case 4:
                            return PixelFormats.Indexed4;
                        case 8:
                            return PixelFormats.Indexed8;
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }

            throw new NotSupportedException($"image.Format: {image.Format}, image.BitsPerPixel: {image.BitsPerPixel}");
        }

        internal void RegisterDecoder(IPluginProxy decoder)
        {
            if (_decoders == null)
            {
                _decoders = new List<IPluginProxy>();
            }
            _decoders.Add(decoder);
        }

        internal void RegisterDecoder(IEnumerable<IPluginProxy> decoder)
        {
            if (_decoders == null)
            {
                _decoders = new List<IPluginProxy>();
            }
            _decoders.AddRange(decoder);
        }
    }
}