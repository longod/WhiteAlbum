// (c) longod, MIT License
namespace WA
{

    // 特定の画像フォーマットによらない中間画像情報
    // 最終的な表示イメージ変換に必要な情報を含む
    // intermidiate decoded image
    internal class DecodedImage
    {
        internal enum ImageDimension
        {
            Texture1D,
            Texture2D,
            Texture2DArray,
            Texture3D,
            TextureCube,
        }

        internal enum ImageOrientation
        {
            TopLeft,
            BottomLeft,
        }

        internal enum ImageRotation
        {
            None,
            Degree90,
            Degree180,
            Degree270,
        }

        internal enum PixelFormat
        {
            RGB,
            RGBA,
            BGR,
            BGRA,
            Index, // palette
        }

        internal uint Width { get; set; }

        internal uint Height { get; set; }

        internal ushort DepthOrArray { get; set; } = 1;

        internal ushort MipLevels { get; set; } = 1;

        internal ushort BitsPerPixel { get; set; }

        internal PixelFormat Format { get; set; } = PixelFormat.BGR;

        internal ImageDimension Dimension { get; set; } = ImageDimension.Texture2D;

        internal ImageOrientation Orientation { get; set; } = ImageOrientation.BottomLeft;

        internal ImageRotation Rotation { get; set; } = ImageRotation.None;

        internal byte[] Binary { get; set; }
    }
}
