using Microsoft.Extensions.Logging;
using Microsoft.Win32;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WA.Album.ViewModels
{
    public class TestDialogViewModel : BindableBase, IDialogAware, IDisposable
    {
        public string Title => "TestDialog";

        public event Action<IDialogResult> RequestClose;


        private IRegionManager _regionManager;
        private IDialogService _dialogService;
        private ViewerModel _viewer;
        private CommandLineArgs _args;
        private ILogger _logger;

        private Point _movingOffset;
        private int _exportFilterIndex = 0;

        // scale = _mantissa ^ _exponent
        // この管理方法は明確だが、任意のスケーリング率の指定には向いていない
        private int _exponent = 0; // ^n
        private const double _mantissa = 2.0;
        private const int _maxExponent = 6; // todo image解像度に依存するようにする

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        // https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging?view=netframeworkdesktop-4.8
        public ReadOnlyReactivePropertySlim<BitmapSource> Image { get; }
        // todo 縮小中は、linearなどであってほしい
        public ReactivePropertySlim<BitmapScalingMode> ScalingMode { get; } = new ReactivePropertySlim<BitmapScalingMode>(BitmapScalingMode.NearestNeighbor);

        public ReactivePropertySlim<Transform> ImageTransform { get; private set; }


        // といっても、rootから乗算されたtransformが入ってきているわけではなさそう
        // ちゃんと水得するにはuielementが必要
        // https://stackoverflow.com/questions/5131118/find-the-applied-scaletransform-on-a-control-or-uielement
        public ReactivePropertySlim<Transform> ParentTransform { get; private set; }

        //private DelegateCommand<RoutedEventArgs> _loadedCommand;
        //public DelegateCommand<RoutedEventArgs> LoadedCommand => _loadedCommand ??= new DelegateCommand<RoutedEventArgs>(async (e) => await LoadedEvent(e));

        public TestDialogViewModel(IRegionManager regionManager, IDialogService dialogService, AppSettings settings, ViewerModel viewer, PluginManager pluginManager, ILogger logger)
        {
            _logger = logger;
            using (new StopwatchScope("Setup Window", _logger))
            {
                _viewer = viewer;

                _regionManager = regionManager;
                _dialogService = dialogService;

                Image = _viewer.ObserveProperty(x => x.Image).ToReadOnlyReactivePropertySlim().AddTo(_disposable); // FIXME これ遅すぎる debugger経由だと100ms以上かかる
                ImageTransform = new ReactivePropertySlim<Transform>(MatrixTransform.Identity);
                ParentTransform = new ReactivePropertySlim<Transform>();
            }

        }


        public bool CanCloseDialog()
        {
            return true;
        }

        public void Dispose()
        {
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue<PackedFile>("Item", out var item))
            {
                // todo await
                // ここではparameter処理だけの方がいいかも
                // logical path情報もpackに含める必要がある
                _viewer.ProcessAsync(item);
            }

        }
    }
}
