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
        internal PackedFile[] files;
    }

    // packed in archive
    public class PackedFile
    {
        // root relative
        public string Path { get; internal set; }
        public long FileOffset { get; internal set; }
        public long CompressedSize { get; internal set; }
        public long ExtractionSize { get; internal set; }

        // thumbnail
        // 恐らく遅延で生成するが、 ObservableCollection内は通知されないので、このクラスを INotifyPropertyChanged する必要がある
    }



}
