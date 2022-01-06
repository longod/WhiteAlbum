﻿namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
    // 先読みとか
    // public class FileManager
    // {
    // }

    public class ViewerModel : NotifyPropertyChangedBase
    {
        private readonly ILogger _logger;
        private readonly PluginManager _pluginManager;
        private readonly CacheManager<BitmapSource> _cacheManager;

        private Dictionary<string, ImageDecoder> _imageDecoders = new Dictionary<string, ImageDecoder>();

        // filesystem path
        private string LogicalPath { get; set; }

        // relative path in archive
        private string VirtualPath { get; set; }

        // 起動時に、全プラグインをロードすると致命的なので、対応フォーマットが判明したらその軽量なデータベースを作っておき、次回以降はそれをみて必要なやつのみロードするとかが必要か
        // x86 dllを読めるようにしないといけない 現実的にはx86アプリにする…x64がいいんだけれど
        // 一応、out-of-process com serverでいける https://qiita.com/mima_ita/items/57d7c1101543e214b1d6

        public ViewerModel(AppSettings settings, PluginManager pluginManager, CacheManager<BitmapSource> cacheManager, ILogger logger)
        {
            _logger = logger;
            _pluginManager = pluginManager;
            _cacheManager = cacheManager;
            using (new StopwatchScope("ViewerModel", _logger))
            {
                if (settings.Data.EnableBuiltInDecoders)
                {
                    RegisterBuiltInDecoders();
                }
            }

            Files = new ObservableCollection<PackedFile>();
        }

        private BitmapSource _image;

        public BitmapSource Image
        {
            get
            {
                return _image;
            }

            private set
            {
                SetProperty(ref _image, value, nameof(Image));
            }
        }

        //private ObservableCollection<PackedFile> _files;

        public ObservableCollection<PackedFile> Files { get; internal set; }
        //{
        //    get
        //    {
        //        return _files;
        //    }
        //    private set
        //    {
        //        SetProperty(ref _image, value, nameof(Files));
        //    }
        //}


        public async Task ProcessAsync(string logicalPath, string virtualPath = null)
        {
            LogicalPath = logicalPath;
            VirtualPath = virtualPath;
            await ProcessAsync();
        }

        // todo obsolete no args
        private async Task ProcessAsync()
        {
            // load
            if (File.Exists(LogicalPath))
            {
                // アーカイブの場合、全部読む必要は無く、対応するかどうかファイルハンドルなど渡して調べてもらうのが良いが、まだ考慮しない
                // ファイルの場合、先頭から順次非同期ロードして、n kbのヘッダを読んだ段階でサポートしているかどうかさらに非同期で調べたい
                // キャッシュとか前後領域の先読みストリーミングとか色々あるけれど、最小構成から

                // ひとまずフルオンメモリー
                using (new StopwatchScope("Process File", _logger))
                {
                    // cache
                    if (_cacheManager.TryQuery(LogicalPath, VirtualPath, out var hit))
                    {
                        Image = hit;
                        return;
                    }

                    // todo archive操作用にどこかでキャッシュor保持しておいた方がよい
                    using (var loader = new FileLoader(LogicalPath, Susie.API.Constant.MinFileSize))
                    {
                        // FIXME support判定に必要な分だけ PeekAsyncで先にまず読んでおいて、
                        // プラグインの検索と並列に読み進めるのが理想的
                        await loader.ReadAsync();

                        // TODO アーカイブの場合を考慮すると、bmp以外の IDecodedResult をかえす
                        // 実データが返ってくるまでvirtual pathを使って再帰的に処理を繰り返す必要がある
                        // builtinとの兼ね合いをどうするか…
                        ImageOutputResult result = null;
                        if (!string.IsNullOrEmpty(VirtualPath))
                        {
                            result = await ProcessAsyncInArchive(loader, VirtualPath);
                        }
                        else
                        {
                            var decoder = await FindDecoderAsync(loader);
                            if (decoder != null)
                            {
                                using (new StopwatchScope("Decode a file", _logger))
                                {
                                    result = await decoder.DecodeImageAsync(loader);
                                }
                            }
                        }

                        //_cacheManager.Entry(LogicalPath, VirtualPath, bmp);
                        if (result.Image != null)
                        {
                            Image = result.Image.bmp;
                        }
                        else if (result.Files != null)
                        {
                            // fixme rangeで追加したい…
                            // 変更イベントが毎回発生してしまう
                            Files.Clear();
                            foreach (var f in result.Files.files)
                            {
                                Files.Add(f);
                            }
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

        public async Task ProcessAsync(PackedFile file)
        {
            // todo reuse instance
            using (var loader = new FileLoader(LogicalPath, Susie.API.Constant.MinFileSize))
            {
                await loader.ReadAsync();

                var decoder = await FindDecoderAsync(loader);
                if (decoder != null)
                {
                    using (new StopwatchScope("Decode a file", _logger))
                    {
                        var result = await ProcessAsyncInArchive(loader, decoder, file);
                        if (result.Image != null)
                        {
                            Image = result.Image.bmp;
                        }
                        else if (result.Files != null)
                        {
                            // fixme rangeで追加したい…
                            // 変更イベントが毎回発生してしまう
                            Files.Clear();
                            foreach (var f in result.Files.files)
                            {
                                Files.Add(f);
                            }
                        }
                    }
                }
            }
        }

        private async Task<ImageOutputResult> ProcessAsyncInArchive(FileLoader loader, string virtualPath)
        {
            var vpaths = SplitVirtualPath(virtualPath);

            // extract chaining archives
            using (new StopwatchScope("Decode a image into archive", _logger))
            {
                for (int i = 0; i < vpaths.Length; ++i)
                {
                    var decoder = await FindDecoderAsync(loader);
                    loader = await decoder.DecodeAsync(loader, vpaths[i]); // file handleを所有していないストリームなのでdisposeは不要
                }

                // decode final image
                var imageDecoder = await FindDecoderAsync(loader);
                var result = await imageDecoder.DecodeImageAsync(loader);
                return result;
            }
        }

        private async Task<ImageOutputResult> ProcessAsyncInArchive(FileLoader loader, ImageDecoder decoder, PackedFile packedFile)
        {
            // nest
            using (var extractedLoader = await decoder.DecodeAsync(loader, packedFile))
            {
                var extractedDecoder = await FindDecoderAsync(extractedLoader);
                if (extractedDecoder != null)
                {
                    return await extractedDecoder.DecodeImageAsync(extractedLoader);
                }
            }

            return null;
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
            var decoder = await _pluginManager.FindDecodablePluginAsync(loader);
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

        private void RegisterBuiltInDecoders()
        {
            _imageDecoders.Add(".bmp", new BuiltInImageDecoder(typeof(BmpBitmapDecoder)));
            _imageDecoders.Add(".png", new BuiltInImageDecoder(typeof(PngBitmapDecoder)));
            _imageDecoders.Add(".jpg", new BuiltInImageDecoder(typeof(JpegBitmapDecoder)));
            _imageDecoders.Add(".jpeg", new BuiltInImageDecoder(typeof(JpegBitmapDecoder)));
            _imageDecoders.Add(".gif", new BuiltInImageDecoder(typeof(GifBitmapDecoder)));
            _imageDecoders.Add(".tif", new BuiltInImageDecoder(typeof(TiffBitmapDecoder)));
            _imageDecoders.Add(".tiff", new BuiltInImageDecoder(typeof(TiffBitmapDecoder)));
            _imageDecoders.Add(".wmp", new BuiltInImageDecoder(typeof(WmpBitmapDecoder)));
        }

        private void RegisterDecoders()
        {
            // todo 過去に動作したdecoderを登録する
        }

        private static string[] SplitVirtualPath(string virtualPath)
        {
            // pathに使用できない文字をデリミタとする
            // 例えば、foo.zip/bar.jpg はファイルではなくディレクトリを示すが、
            // foo.zip|bar.jpg はアーカイブ内にあることを指している
            return virtualPath.Split('|');
        }

        // async version?
        public void Export(string path, BitmapSource image)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }
        }
    }
}
