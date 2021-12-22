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

        // add more info
    }

    // temp
    internal class ArchiveOutputResult : IOutputResult
    {
    }
}
