// (c) longod, MIT License

namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;

    // backends
    // wpf, windowhost, d3d12
    interface IRenderer
    {
    }

    // dotnet, C# dll, C++ dll, susie
    interface IDecoder : IDisposable
    {
        // renderer backendの多用化を考えると、この段階ではBitmapSource じゃない中間フォーマットを返して欲しい
        // bitmap info, image desc, stream, span
        bool Decode(FileLoader loader, out IntermediateImage image);
        bool IsSupported(FileLoader loader);
    }

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
    public class CacheManager
    {
    }

    // 特定の画像フォーマットによらない画像情報
    // 最終的な表示イメージ変換に必要な情報を含む
    public struct ImageDesc
    {
        public uint Width;
        public uint Height;
        public ushort DepthOrArray;
        public ushort MipLevels;
        // dimension
        // format
        // origin
    }

    public class IntermediateImage
    {
        public ImageDesc desc;
        public byte[] binary;
    }

    public static class WpfUtility
    {
        public const int DefaultDpi = 96; // どこかに定義ないのか
    }

    public class Config
    {
        public List<string> PluginDirectories = new List<string>() { @"..\..\..\..\Temp\spi\" };
    }

    public class ViewerModelArgs
    {
        public string Path; // filesystem path
        public string VirtualPath; // relative path in archive
    }

    // modelにINotifyPropertyChanged つかうのはふつうなのか？
    public class ViewerModel : INotifyPropertyChanged
    {
        // filesystem path
        public string LogicalPath { get; set; }

        // relative path in archive
        public string VirtualPath { get; set; }


        public ViewerModel(ViewerModelArgs args)
        {
            using (new StopwatchScope("ViewerModel"))
            {
                if (args != null)
                {
                    LogicalPath = args.Path;
                    VirtualPath = args.VirtualPath;
                }
                // register builtin types
                bool enabledBuiltInDecoders = false;
                if (enabledBuiltInDecoders)
                {
                    RegisterBuiltInDecoders();
                }
            }
        }

        public ViewerModel()
        {

            //PluginManager pm = new PluginManager();
            //pm.FindPlugins();
            //pm.LoadAllPlugins();

            // 起動時に、全プラグインをロードすると致命的なので、対応フォーマットが判明したらその軽量なデータベースを作っておき、次回以降はそれをみて必要なやつのみロードするとかが必要か
            // x86 dllを読めるようにしないといけない 現実的にはx86アプリにする…x64がいいんだけれど
            // 一応、out-of-process com serverでいける https://qiita.com/mima_ita/items/57d7c1101543e214b1d6
            //using (new SusiePlugin(@"..\..\..\..\Debug\ifnull.spi"))
            //{
            //}

            //using (new SusiePlugin(@"..\..\..\..\Temp\spi\spi32008\ifgif.spi"))
            //{
            //}

            // アーカイブ内の特定ファイルを展開するには、実パスとアーカイブ内の仮想パスもサポートする必要がある

            byte[] binary = null;
            string path = @"..\..\..\..\Temp\image\big.bmp";
            using (var stream = File.OpenRead(path))
            {
                binary = new byte[stream.Length];
                var ret = stream.Read(binary); // or async
            }

            // exrみたいなストリーミング表示可能なフォーマットはロードしながらできるんだろうか…


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

        public static BitmapSource CreateBitmapFrame(byte[] binary)
        {
            return BitmapFrame.Create(new MemoryStream(binary), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        }

        private BitmapSource _image;
        public BitmapSource Image
        {
            get { return _image; }
            set
            {
                if (value != _image)
                {
                    _image = value;
                    NotifyPropertyChanged("Image");
                }

            }
        }

        public async Task ProcessAsync()
        {
            // load
            if (File.Exists(LogicalPath))
            {
                // アーカイブの場合、全部読む必要は無く、対応するかどうかファイルハンドルなど渡して調べてもらうのが良いが、まだ考慮しない
                // ファイルの場合、先頭から順次非同期ロードして、n kbのヘッダを読んだ段階でサポートしているかどうかさらに非同期で調べたい
                // キャッシュとか前後領域の先読みストリーミングとか色々あるけれど、最小構成から

                // ひとまずフルオンメモリー
                using (new StopwatchScope("Process File Async"))
                {
                    using (var loader = new FileLoader(LogicalPath))
                    {
                        await loader.LoadAsync();
                        var decoder = await FindDecoderAsync(loader);
                        if (decoder != null)
                        {
                            var bmp = await decoder.TryDecodeAsync(loader);
                            Image = bmp;
                        }
                    }
                }

            }
            else if (Directory.Exists(LogicalPath))
            {
                // special case
                // directory loader
            }

        }

        private async Task<ImageDecoder> FindDecoderAsync(FileLoader loader)
        {
            // find decoder extension and header
            // tood async

            // 拡張子にマッピングされたデコーダーがヒットするかどうか
            var ext = loader.Extension;
            ImageDecoder instance = null;
            if (_imageDecoders.TryGetValue(ext, out instance))
            {
                bool continueFinding = false; // みつかっても残りのプラグインを調べるかどうか
                if (!continueFinding)
                {
                    return instance;
                }
            }

            // ヒットしない、プラグインで該当するかどうか解決を試みる
            // 解決できる場合は、対応する拡張子にマッピングする
            // この拡張子でマッピング済みということがplugin maneger側にも分からないと、continueFinding時に同じものがでてくる
            var decoder = await _pluginManager.ResolveAsync(loader);
            if (decoder != null)
            {
                if (instance == null)
                {
                    instance = new ImageDecoder();
                }

                instance.RegisterDecoder(decoder);
                _imageDecoders.Add(ext, instance);
            }

            return instance;
        }

        private Dictionary<string, ImageDecoder> _imageDecoders = new Dictionary<string, ImageDecoder>();

        // todo call dispose
        private PluginManager _pluginManager = new PluginManager();
        private void RegisterBuiltInDecoders()
        {
            _imageDecoders.Add(".bmp", new BuiltInImageDecoder(typeof(BmpBitmapDecoder)));
            _imageDecoders.Add(".png", new BuiltInImageDecoder(typeof(PngBitmapDecoder)));
            _imageDecoders.Add(".jpg", new BuiltInImageDecoder(typeof(JpegBitmapDecoder)));
            _imageDecoders.Add(".git", new BuiltInImageDecoder(typeof(GifBitmapDecoder)));
            _imageDecoders.Add(".tif", new BuiltInImageDecoder(typeof(TiffBitmapDecoder)));
            _imageDecoders.Add(".wmp", new BuiltInImageDecoder(typeof(WmpBitmapDecoder)));
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
