namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    // extensionに対応したdecoderセット
    // renderer backendの多用化を考えると、この段階ではBitmapSource じゃない中間フォーマットを返して欲しい
    // しかし builtinや通常のwpfだと、BitmapSourceがもっとも扱いやすい
    // internal interface IImageDecoder<Resource> {}
    // これでバックエンドに対応するとマッピング時に型が必要になってしまう…
    // bitmap info, image desc, stream, span
    // todo archiveも扱っているので、リネームなりinterfaceつくって分離なり
    internal class ImageDecoder
    {
        private List<IPluginProxy> _decoders;

        internal virtual ImageOutputResult DecodeImage(FileLoader loader, bool thumbnail = false)
        {
            IIntermediateResult result = null;
            foreach (var d in _decoders)
            {
                if (d.Decode(loader, out result, thumbnail))
                {
                    break;
                }
            }

            if (result != null)
            {
                // fixme もうちょっとスマートにpolymorph
                if (result is ImageIntermediateResult)
                {
                    return Convert((ImageIntermediateResult)result);
                }
                else if (result is ArchiveIntermediateResult)
                {
                    return Convert((ArchiveIntermediateResult)result);
                }
            }

            return null;
        }

        // extract file in archive
        // using virtualPath or something
        internal virtual FileLoader Decode(FileLoader loader, PackedFile packed)
        {
            IIntermediateResult result = null;
            foreach (var d in _decoders)
            {
                if (d.Decode(loader, packed, out result))
                {
                    break;
                }
            }

            if (result != null)
            {
                // fixme もうちょっとスマートにpolymorph
                if (result is BinaryIntermediateResult)
                {
                    return Convert(packed.Path, (BinaryIntermediateResult)result);
                }
            }

            return null;
        }

        internal virtual FileLoader Decode(FileLoader loader, string path)
        {
            IIntermediateResult result = null;
            foreach (var d in _decoders)
            {
                if (d.Decode(loader, path, out result))
                {
                    break;
                }
            }

            if (result != null)
            {
                // fixme もうちょっとスマートにpolymorph
                if (result is BinaryIntermediateResult)
                {
                    return Convert(path, (BinaryIntermediateResult)result);
                }
            }

            return null;
        }

        private ImageOutputResult Convert(ImageIntermediateResult image)
        {
            BitmapSource bmp = null;
            PixelFormat format = GetPixelFormat(image);
            BitmapPalette plaette = GetBitmapPalette(image);
            Transform transform = GetTransform(image);

            var stride = (((image.Info.Width * image.Info.BitsPerPixel) + 31u) & ~31u) >> 3;
            bmp = BitmapSource.Create(
              (int)image.Info.Width,
              (int)image.Info.Height,
              WpfUtility.DefaultDpiX,
              WpfUtility.DefaultDpiX,
              format,
              plaette,
              image.Binary,
              (int)stride);
            if (transform != null)
            {
                bmp = new TransformedBitmap(bmp, transform);
            }

            // BitmapSourceは Must create DependencySource on same Thread as the DependencyObject
            // を発生させるので、freezeする
            // これを継承した生成器や、decoderはdispatcherでUIスレッドで作られるようにされていると思われるが、
            // BitmapSourceはそこをケアしていないのだろう
            // どこかの生成器でリークしているかも
            // https://pierre3.hatenablog.com/entry/2015/10/25/001207
            bmp.Freeze();

            // BitmapSourceの基点は左上だが、本来のbmp formatのpositive heightは左下基点で反転してしまう
            // 事前にメモリを反転して詰め直すか、scale transformで行なう
            // exif も追々考慮する必要がある
            ImageOutputResult result = new ImageOutputResult() { Image = new ImageOutput() { bmp = bmp } };
            return result;
        }

        private ImageOutputResult Convert(ArchiveIntermediateResult image)
        {
            // copyしたほうがよいが、最適化が必要
            ImageOutputResult result = new ImageOutputResult() { Files = new FileOutput() { files = image.files } };
            return result;
        }

        private FileLoader Convert(string path, BinaryIntermediateResult image)
        {
            FileLoader loader = new FileLoader(path, image.Binary, Susie.API.Constant.MinFileSize); // 委譲
            return loader;
        }

        private Transform GetTransform(ImageIntermediateResult image)
        {
            RotateTransform rotate = null;
            switch (image.Info.Rotation)
            {
                case ImageRotation.None:
                    break;
                case ImageRotation.Degree90:
                    rotate = new RotateTransform(90.0f);
                    break;
                case ImageRotation.Degree180:
                    rotate = new RotateTransform(180.0f);
                    break;
                case ImageRotation.Degree270:
                    rotate = new RotateTransform(270.0f);
                    break;
                default:
                    break;
            }

            ScaleTransform scale = null;
            switch (image.Info.Orientation)
            {
                case ImageOrientation.TopLeft:
                    break;
                case ImageOrientation.BottomLeft:
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

        private static BitmapPalette GetBitmapPalette(ImageIntermediateResult image)
        {
            if (image.Palette != null)
            {
                return new BitmapPalette(image.Palette);
            }

            // fallback
            switch (image.Info.Format)
            {
                case ImageFormat.Index:
                    // FIXME 元データのパレットを参照して生成する
                    switch (image.Info.BitsPerPixel)
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
                            throw new NotSupportedException($"image.BitsPerPixel : {image.Info.BitsPerPixel}");
                    }

                default:
                    return null;
            }
        }

        private static PixelFormat GetPixelFormat(ImageIntermediateResult image)
        {
            switch (image.Info.Format)
            {
                case ImageFormat.RGB:
                    return PixelFormats.Rgb24;
                case ImageFormat.RGBA:
                    // argb, rgba
                    break;
                case ImageFormat.BGR:
                    return PixelFormats.Bgr24;
                case ImageFormat.BGRA:
                    return PixelFormats.Bgra32;
                case ImageFormat.Index:
                    switch (image.Info.BitsPerPixel)
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

            throw new NotSupportedException($"image.Format: {image.Info.Format}, image.BitsPerPixel: {image.Info.BitsPerPixel}");
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
