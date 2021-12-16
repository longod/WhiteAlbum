// (c) longod, MIT License

namespace WA
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using Microsoft.Extensions.Logging;

    // backends
    // wpf, windowhost, d3d12
    // interface IRenderer
    // {
    // }

    // パス（ファイル、ディレクトリ、アーカイブ）を与えるとそれぞれに応じた処理をする
    // class FileSystem
    // {
    //     void Open(string path);
    //     bool IsDirectory { get; }
    // }

    // loaded raw binary image
    // public class RawBinary
    // {
    //     string _path;
    //     byte[] _bin;// or memory stream
    // }

    // management loader and raw binary
    // public class FileManager
    // {
    // }

    // management decoer and decoded image
    // public class CacheManager
    // {
    // }

    public class ViewerModel : INotifyPropertyChanged
    {
        public class Args
        {
            // filesystem path
            internal string Path { get; private set; }

            // relative path in archive
            // virtual pathの spliterには reserved characterを使う。 | vertical bar が候補
            // アーカイブ内のパスが foo.zip/bar.jpg という表現の場合、foo.zipがディレクトリなのかアーカイブ内のアーカイブなのか分からないからだ
            // アーカイブファイル内のjpg: foo.zip|bar.jpg
            // ディレクトリ内のjpg: foo.zip/bar.jpg
            internal string VirtualPath { get; private set; }

            public Args(string[] args)
            {
                if (args != null)
                {
                    if (args.Length > 0)
                    {
                        Path = args[0];
                    }

                    if (args.Length > 1)
                    {
                        VirtualPath = args[1];
                    }
                }
            }
        }


        // filesystem path
        public string LogicalPath { get; set; }

        // relative path in archive
        public string VirtualPath { get; set; }

        // 起動時に、全プラグインをロードすると致命的なので、対応フォーマットが判明したらその軽量なデータベースを作っておき、次回以降はそれをみて必要なやつのみロードするとかが必要か
        // x86 dllを読めるようにしないといけない 現実的にはx86アプリにする…x64がいいんだけれど
        // 一応、out-of-process com serverでいける https://qiita.com/mima_ita/items/57d7c1101543e214b1d6

        public ViewerModel(Args args, AppSettings settings, PluginManager pluginManager, ILogger logger)
        {
            _logger = logger;
            _pluginManager = pluginManager;
            using (new StopwatchScope("ViewerModel", _logger))
            {
                if (args != null)
                {
                    LogicalPath = args.Path;
                    VirtualPath = args.VirtualPath;

                    if (settings.EnableBuiltInDecoders)
                    {
                        RegisterBuiltInDecoders();
                    }
                }
            }
        }

        private BitmapSource _image;

        public BitmapSource Image
        {
            get { return _image; }
            private set
            {
                if (value != _image)
                {
                    _image = value;
                    NotifyPropertyChanged(nameof(Image));
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
                using (new StopwatchScope("Process File Async", _logger))
                {
                    using (var loader = new FileLoader(LogicalPath, Susie.API.Constant.MinFileSize))
                    {
                        // FIXME support判定に必要な分だけ PeekAsyncで先にまず読んでおいて、
                        // プラグインの検索と並列に読み進めるのが理想的
                        await loader.ReadAsync();

                        var decoder = await FindDecoderAsync(loader);
                        if (decoder != null)
                        {
                            // TODO アーカイブの場合を考慮すると、bmp以外の IDecodedResult をかえす
                            // 実データが返ってくるまでvirtual pathを使って再帰的に処理を繰り返す必要がある
                            // builtinとの兼ね合いをどうするか…
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
            // todo async

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

        private PluginManager _pluginManager;
        private readonly ILogger _logger;

        private void RegisterBuiltInDecoders()
        {
            _imageDecoders.Add(".bmp", new BuiltInImageDecoder(typeof(BmpBitmapDecoder)));
            _imageDecoders.Add(".png", new BuiltInImageDecoder(typeof(PngBitmapDecoder)));
            _imageDecoders.Add(".jpg", new BuiltInImageDecoder(typeof(JpegBitmapDecoder)));
            _imageDecoders.Add(".git", new BuiltInImageDecoder(typeof(GifBitmapDecoder)));
            _imageDecoders.Add(".tif", new BuiltInImageDecoder(typeof(TiffBitmapDecoder)));
            _imageDecoders.Add(".wmp", new BuiltInImageDecoder(typeof(WmpBitmapDecoder)));
        }

        private void RegisterDecoders()
        {
            // todo 過去に動作したdecoderを登録する
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
