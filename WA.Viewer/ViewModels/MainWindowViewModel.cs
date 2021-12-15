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

        // test
        public DelegateCommand ShowConfigCommand { get; }
        PluginManager _pluginManager;

        public MainWindowViewModel(IRegionManager regionManager, IDialogService dialogService, ViewerModel viewer, PluginManager pluginManager)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
            _viewer = viewer;
            Image = _viewer.ObserveProperty(x => x.Image).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
            Task.Run(() => _viewer.ProcessAsync());

            ExitCommand = new DelegateCommand(Exit);
            ShowSettingsWindowCommand = new DelegateCommand(ShowSettingsWindow);
            ShowConfigCommand = new DelegateCommand(ShowConfigTest);

            _pluginManager = pluginManager;
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
