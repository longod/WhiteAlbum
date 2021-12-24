namespace WA
{
    using System.Collections.Generic;
    using System.Windows.Media.Imaging;

    internal interface IOutputResult<TImageOutput, TFileOutput>
    {
        TImageOutput Image { get; }
        TFileOutput Files { get; }
    }

    // レンダラーが要求するフォーマットに変換されたイメージ
    // WPF Image element ro archives
    internal class ImageOutputResult : IOutputResult<ImageOutput, FileOutput>
    {
        // add more info
        public ImageOutput Image { get; internal set; }

        public FileOutput Files { get; internal set; }

    }

    internal class ImageOutput
    {
        internal BitmapSource bmp;
    }

    internal class FileOutput
    {
        internal struct File
        {
            internal string path;
            internal long fileOffset;
            internal long compressedSize;
            internal long extractionSize;
            internal long timestamp;
        }

        internal File[] files;
    }

}
