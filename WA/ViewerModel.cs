// (c) longod, MIT License

namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows.Media.Imaging;

    // backends
    // wpf, windowhost, d3d12
    interface IRenderer
    {
    }

    // dotnet, C# dll, C++ dll, susie
    interface IDecoder
    {
    }

    // file loader
    interface IFileLoader
    {
    }

    // パス（ファイル、ディレクトリ、アーカイブ）を与えるとそれぞれに応じた処理をする
    class FileSystem
    {
        //void Open(string path);
        //bool IsDirectory { get; }
    }

    // loaded raw binary image
    public class RawBinary
    {
        string _path;
        byte[] _bin;// or memory stream
    }


    // presentable decoded image
    public class DecodedImage
    {
    }

    // management loader and raw binary
    public class FileManager
    {
    }

    // management decoer and decoded image
    public class ResourceManager
    {
    }

    public class FileSystemFactory
    {
        // directoryどうする extじゃなくてパス？
        IEnumerable<FileSystem> Find(string ext)
        {
            return null;
        }

    }


    public class ViewerModel
    {
        public ViewerModel()
        {
            // 起動時に、全プラグインをロードすると致命的なので、対応フォーマットが判明したらその軽量なデータベースを作っておき、次回以降はそれをみて必要なやつのみロードするとかが必要か
            // x86 dllを読めるようにしないといけない 現実的にはx86アプリにする…x64がいいんだけれど
            using (new SusiePlugin(@"..\..\..\..\Temp\spi\spi32008\ifgif.spi"))
            {
            }

            // アーカイブ内の特定ファイルを展開するには、実パスとアーカイブ内の仮想パスもサポートする必要がある

            byte[] binary = null;
            string path = @"..\..\..\..\Temp\image\big.bmp";
            using (var stream = File.OpenRead(path))
            {
                binary = new byte[stream.Length];
                var ret = stream.Read(binary); // or async
            }

            var ext = Path.GetExtension(path);

            // exrみたいなストリーミング表示可能なフォーマットはロードしながらできるんだろうか…

            //var decoder = new JpegBitmapDecoder(new MemoryStream(binary), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            //var decoder = new PngBitmapDecoder(new MemoryStream(binary), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            //decoder.Frames[0]

            Image = CreateBitmapFrame(binary);

#if false
            // https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging?view=netframeworkdesktop-4.8
            using (new StopwatchScope("Create BitmapImage"))
            {
                // BitmapFrame とどっちがはやい？->todo bench

                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                //bmp.CreateOptions = BitmapCreateOptions.DelayCreation; // 遅延にしたいが、多分binding見直さないとでない。通知が必要のはず。
                //bmp.DecodePixelWidth = 640;
                //bmp.DecodePixelHeight = 360;
                bmp.StreamSource = new MemoryStream(binary);
                bmp.EndInit();

                // これらはxaml Imageに対して設定するんじゃないの
                //RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.LowQuality);
                //RenderOptions.SetCachingHint(image, CachingHint.Cache);
                //RenderOptions.SetCacheInvalidationThresholdMinimum(image, 0.5);
                //RenderOptions.SetCacheInvalidationThresholdMaximum(image, 2.0);
                Image = bmp;
            }
#endif

        }

        public static BitmapSource CreateBitmapSource(byte[] binary)
        {
            throw new NotImplementedException();
        }


        public static BitmapSource CreateBitmapImage(byte[] binary)
        {
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            //bmp.CreateOptions = BitmapCreateOptions.DelayCreation; // 遅延にしたいが、多分binding見直さないとでない。通知が必要のはず。
            //bmp.DecodePixelWidth = 640;
            //bmp.DecodePixelHeight = 360;
            bmp.StreamSource = new MemoryStream(binary);
            bmp.EndInit();
            return bmp;
        }
        public static BitmapSource CreateBitmapFrame(byte[] binary)
        {
            return BitmapFrame.Create(new MemoryStream(binary), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        }

        public BitmapSource Image { get; set; }
    }
}
