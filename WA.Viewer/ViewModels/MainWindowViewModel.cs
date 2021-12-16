using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WA.Viewer.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private const string _appTitle = "WHITE ALBUM";

        private IRegionManager _regionManager;
        private IDialogService _dialogService;
        private ViewerModel _viewer;

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        // fixme readonly one way, getting fileinfo
        public ReactivePropertySlim<string> Title { get; } = new ReactivePropertySlim<string>(_appTitle);

        // https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging?view=netframeworkdesktop-4.8
        public ReadOnlyReactivePropertySlim<BitmapSource> Image { get; }

        public DelegateCommand ShowSettingsWindowCommand { get; }
        public DelegateCommand ExitCommand { get; }

        public DelegateCommand<RoutedEventArgs> LoadedCommand { get; }
        public DelegateCommand<DragEventArgs> PreviewDragOverCommand { get; }
        public DelegateCommand<DragEventArgs> DropCommand { get; }

        // test
        public DelegateCommand ShowConfigCommand { get; }
        PluginManager _pluginManager;

        public MainWindowViewModel(IRegionManager regionManager, IDialogService dialogService, AppSettings settings, ViewerModel viewer, PluginManager pluginManager)
        {
            _viewer = viewer;
            Task.Run(() => _viewer.ProcessAsync());

            _regionManager = regionManager;
            _dialogService = dialogService;
            _pluginManager = pluginManager; // test

            Image = _viewer.ObserveProperty(x => x.Image).ToReadOnlyReactivePropertySlim().AddTo(_disposable);

            ExitCommand = new DelegateCommand(Exit);
            ShowSettingsWindowCommand = new DelegateCommand(ShowSettingsWindow);
            ShowConfigCommand = new DelegateCommand(ShowConfigTest);

            LoadedCommand = new DelegateCommand<RoutedEventArgs>(Loaded);
            PreviewDragOverCommand = new DelegateCommand<DragEventArgs>(ImagePreviewDragOver);
            DropCommand = new DelegateCommand<DragEventArgs>(ImageDrop);
        }


        public void Dispose()
        {
            _disposable.Dispose();
            if (_regionManager != null)
            {
                foreach (var region in _regionManager.Regions)
                {
                    region.RemoveAll();
                }
            }
        }

        private void Loaded(RoutedEventArgs e)
        {
#if true // todo only development
            var args = Environment.GetCommandLineArgs();
            // include self dll name
            if (args.Length >= 2)
            {
                if (args[1] == "-k")
                {
                    System.Diagnostics.Process.GetCurrentProcess().Kill(); // force kill
                }
                else if (args[1] == "-e")
                {
                    Application.Current.Shutdown();
                }
            }
#endif
        }

        private void ImagePreviewDragOver(DragEventArgs e)
        {
            // マウスカーソルをコピーにする。
            e.Effects = DragDropEffects.Copy;
            // ドラッグされてきたものがFileDrop形式の場合だけ、このイベントを処理済みにする。
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop);
        }

        // ImageのDropイベントに対する処理
        private void ImageDrop(DragEventArgs e)
        {
            // ドロップされたものがFileDrop形式の場合は、各ファイルのパス文字列を文字列配列に格納する。
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            // 複数ドロップの可能性もあるので、今回は最初のファイルを選択して表示
            //ViewImage.Value = files[0];
        }

        private void Exit()
        {
            Application.Current.Shutdown();
        }
        private void ShowSettingsWindow()
        {
            _dialogService.ShowDialog("SettingsWindow");
        }


        private void ShowConfigTest()
        {
            var win = Application.Current.MainWindow; // 必要悪
            var handle = new System.Windows.Interop.WindowInteropHelper(win);
            _pluginManager.ShowConfigTest(handle.Handle);
        }

    }
}
