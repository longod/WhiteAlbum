namespace WA
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

    internal enum ImageFormat
    {
        RGB,
        RGBA,
        BGR,
        BGRA,
        Index, // palette
    }

    internal struct ImageInfo
    {
        internal uint Width;
        internal uint Height;
        internal ushort DepthOrArray;
        internal ushort MipLevels;
        internal ushort BitsPerPixel;
        internal ImageFormat Format;
        internal ImageDimension Dimension;
        internal ImageOrientation Orientation;
        internal ImageRotation Rotation;
    }
}
