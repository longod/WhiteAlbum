namespace WA
{
    using System.Windows.Media.Imaging;

    internal interface IOutputResult
    {
    }

    // レンダラーが要求するフォーマットに変換されたイメージ
    // WPF Image element
    internal class ImageOutputResult : IOutputResult
    {
        internal BitmapSource Bmp { get; set; }

        internal ImageInfo Info { get; set; }
    }

    // temp
    internal class ArchiveOutputResult : IOutputResult
    {
    }
}
