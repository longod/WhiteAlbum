namespace WA
{
    using System;

    // ImageDecoder と Plugin を吸収するinterface
    internal interface IPluginProxy : IDisposable
    {
        // renderer backendの多用化を考えると、この段階ではBitmapSource じゃない中間フォーマットを返して欲しい
        // bitmap info, image desc, stream, span
        bool Decode(FileLoader loader, out ImageIntermediateResult image);

        bool IsSupported(FileLoader loader);
    }
}
