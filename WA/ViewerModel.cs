namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using Microsoft.Extensions.Logging;
    using ZLogger;

    // backends
    // wpf, windowhost, d3d12
    // interface IRenderer
    // {
    // }

    public class ViewerModel : NotifyPropertyChangedBase
    {
        private readonly ILogger _logger;
        private readonly CacheManager _cacheManager;
        private readonly DecoderManager _decoderManager;

        // todo register di
        private Dictionary<string, BuiltInImageEncoder> _imageEncoders = null;

        private BitmapSource _image;

        // packed fileと統合的に扱いたい
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

        public ObservableCollection<PackedFile> Files { get; internal set; } = new ObservableCollection<PackedFile>();

        // filesystem path
        // 廃止したい packed fileと統合的に扱いたい
        private string LogicalPath { get; set; }

        // relative path in archive
        // 廃止したい packed fileと統合的に扱いたい
        private string VirtualPath { get; set; }

        // 起動時に、全プラグインをロードすると致命的なので、対応フォーマットが判明したらその軽量なデータベースを作っておき、次回以降はそれをみて必要なやつのみロードするとかが必要か
        // x86 dllを読めるようにしないといけない 現実的にはx86アプリにする…x64がいいんだけれど
        // 一応、out-of-process com serverでいける https://qiita.com/mima_ita/items/57d7c1101543e214b1d6

        public ViewerModel(AppSettings settings, DecoderManager decoderManager, CacheManager cacheManager, ILogger logger)
        {
            _logger = logger;
            _cacheManager = cacheManager;
            _decoderManager = decoderManager;
        }

        public async Task ProcessAsync(string logicalPath, string virtualPath = null)
        {
            LogicalPath = logicalPath;
            VirtualPath = virtualPath;

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
                    if (_cacheManager.TryQuery<ImageOutputResult>(LogicalPath, VirtualPath, out var hit))
                    {
                        if (hit.Image != null)
                        {
                            Image = hit.Image.bmp;
                        }
                        else if (hit.Files != null)
                        {
                            // fixme rangeで追加したい…
                            // 変更イベントが毎回発生してしまう
                            Files.Clear();
                            foreach (var f in hit.Files.files)
                            {
                                Files.Add(f);
                            }
                        }

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
                            result = ProcessAsyncInArchive(loader, VirtualPath);
                        }
                        else
                        {
                            var decoder = _decoderManager.FindDecoder(loader);
                            if (decoder != null)
                            {
                                using (new StopwatchScope("Decode a file", _logger))
                                {
                                    result = decoder.DecodeImage(loader);
                                }
                            }
                        }

                        _cacheManager.Entry(LogicalPath, VirtualPath, result);
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
            if (_cacheManager.TryQuery<ImageOutputResult>(file.LogicalPath, file.Path, out var hit))
            {
                if (hit.Image != null)
                {
                    Image = hit.Image.bmp;
                }
                else if (hit.Files != null)
                {
                    // fixme rangeで追加したい…
                    // 変更イベントが毎回発生してしまう
                    Files.Clear();
                    foreach (var f in hit.Files.files)
                    {
                        Files.Add(f);
                    }
                }

                return;
            }

            // todo reuse instance
            using (var loader = new FileLoader(file.LogicalPath, Susie.API.Constant.MinFileSize))
            {
                await loader.ReadAsync();

                var decoder = _decoderManager.FindDecoder(loader);
                if (decoder != null)
                {
                    using (new StopwatchScope("Decode a file", _logger))
                    {
                        var result = ProcessAsyncInArchive(loader, decoder, file, false);

                        _cacheManager.Entry(file.LogicalPath, file.Path, result);
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

        private ImageOutputResult ProcessAsyncInArchive(FileLoader loader, string virtualPath)
        {
            var vpaths = SplitVirtualPath(virtualPath);

            // extract chaining archives
            using (new StopwatchScope("Decode a image into archive", _logger))
            {
                for (int i = 0; i < vpaths.Length; ++i)
                {
                    var decoder = _decoderManager.FindDecoder(loader);

                    if (decoder == null)
                    {
                        // cant decode
                        _logger.ZLogWarning("can't decode: {0}", loader.Path);
                        return null; // todo
                    }

                    loader = decoder.Decode(loader, vpaths[i]); // file handleを所有していないストリームなのでdisposeは不要
                }

                // decode final image
                var imageDecoder = _decoderManager.FindDecoder(loader);
                var result = imageDecoder.DecodeImage(loader);
                return result;
            }
        }

        private ImageOutputResult ProcessAsyncInArchive(FileLoader loader, ImageDecoder decoder, PackedFile packedFile, bool thumbnail)
        {
            // nest
            using (var extractedLoader = decoder.Decode(loader, packedFile))
            {
                var extractedDecoder = _decoderManager.FindDecoder(extractedLoader);
                if (extractedDecoder != null)
                {
                    return extractedDecoder.DecodeImage(extractedLoader, thumbnail);
                }
            }

            return null;
        }

        public async Task LoadThumbnail(PackedFile file)
        {
            if (_cacheManager.TryQuery<ImageOutputResult>(file.LogicalPath, file.Path, out var hit))
            {
                if (hit.Image != null)
                {
                    file.Thumbnail = hit.Image.bmp;
                }
                else if (hit.Files != null)
                {
                    // load file icon
                }

                return;
            }

            // fixme todo cache
            using (var loader = new FileLoader(file.LogicalPath, Susie.API.Constant.MinFileSize))
            {
                await loader.ReadAsync();

                var decoder = _decoderManager.FindDecoder(loader);
                if (decoder != null)
                {
                    using (new StopwatchScope("Decode a file", _logger))
                    {
                        var result = ProcessAsyncInArchive(loader, decoder, file, true);

                        _cacheManager.Entry(file.LogicalPath, file.Path, result);
                        if (result.Image != null)
                        {
                            file.Thumbnail = result.Image.bmp;
                        }
                        else if (result.Files != null)
                        {
                            // load file icon
                        }
                    }
                }
            }
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
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path must be not null.");
            }

            if (image == null)
            {
                throw new ArgumentNullException("image must be not null.");
            }

            // initial registry
            if (_imageEncoders == null)
            {
                _imageEncoders = new Dictionary<string, BuiltInImageEncoder>();
                RegisterBuiltinEncoders();
            }

            var file = new FileInfo(path);
            var ext = file.Extension;
            if (string.IsNullOrEmpty(ext))
            {
                // default or failed
                throw new ArgumentException();
            }

            if (!Directory.Exists(file.Directory.FullName))
            {
                Directory.CreateDirectory(file.Directory.FullName);
            }

            // find encoder
            // todo tweakable quality
            if (_imageEncoders.TryGetValue(ext, out var encoder))
            {
                encoder.EncodeToFile(path, image);
            }
            else
            {
                // todo error
            }
        }

        private string _exportFilter;

        public string ExportFilter
        {
            get
            {
                if (_exportFilter == null)
                {
                    if (_imageEncoders == null)
                    {
                        _imageEncoders = new Dictionary<string, BuiltInImageEncoder>();
                        RegisterBuiltinEncoders();
                    }

                    // | 区切りのフィルタ生成
                    // 複数拡張子は ; 区切り
                    // fixme 複数の拡張子を表現できない
                    var filter = _imageEncoders.Select(x => string.Concat(x.Value.FormatName, "|", "*", x.Key));
                    _exportFilter = string.Join('|', filter);
                }

                return _exportFilter;
            }
        }

        private void RegisterBuiltinEncoders()
        {
            _imageEncoders.Add(".bmp", new BuiltInImageEncoder("Bitmap", new BmpBitmapEncoder()));
            _imageEncoders.Add(".png", new BuiltInImageEncoder("PNG", new PngBitmapEncoder() { Interlace = PngInterlaceOption.Default }));
            _imageEncoders.Add(".jpg", new BuiltInImageEncoder("Jpeg", new JpegBitmapEncoder() { QualityLevel = 100 }));
            _imageEncoders.Add(".gif", new BuiltInImageEncoder("GIF", new GifBitmapEncoder()));
            _imageEncoders.Add(".tif", new BuiltInImageEncoder("TIFF", new TiffBitmapEncoder() { Compression = TiffCompressOption.Default }));
            _imageEncoders.Add(".hdp", new BuiltInImageEncoder("HDP", new WmpBitmapEncoder()));
        }
    }
}
