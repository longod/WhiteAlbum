namespace WA
{
    using System;

    // ImageDecoder と Plugin を吸収するinterface
    internal interface IPluginProxy : IDisposable
    {
        // renderer backendの多用化を考えると、この段階ではBitmapSource じゃない中間フォーマットを返して欲しい
        // bitmap info, image desc, stream, span
        bool Decode(FileLoader loader, out IIntermediateResult result);

        // extrac archive
        // todo PackedFileは適切でないかもしれないが、抽象的に解凍可能な情報を含んだ型が必要
        // todo 上と同じ単一のインターフェイスにしたい
        bool Decode(FileLoader loader, PackedFile packed, out IIntermediateResult result);

        bool IsSupported(FileLoader loader);
    }
}
