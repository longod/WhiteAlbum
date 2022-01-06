using Microsoft.Extensions.Logging;
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

namespace WA.Viewer.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private const string _appTitle = "WHITE ALBUM";

        private IRegionManager _regionManager;
        private IDialogService _dialogService;
        private ViewerModel _viewer;
        private CommandLineArgs _args;
        private ILogger _logger;

        private Point _movingOffset;

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        // fixme readonly one way, getting fileinfo
        public ReactivePropertySlim<string> Title { get; } = new ReactivePropertySlim<string>(_appTitle);

        // https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging?view=netframeworkdesktop-4.8
        public ReadOnlyReactivePropertySlim<BitmapSource> Image { get; }
        // todo 縮小中は、linearなどであってほしい
        public ReactivePropertySlim<BitmapScalingMode> ScalingMode { get; } = new ReactivePropertySlim<BitmapScalingMode>(BitmapScalingMode.NearestNeighbor);

        public ReactivePropertySlim<Transform> ImageTransform { get; private set; }

        private DelegateCommand _exportCommand;
        public DelegateCommand ExportCommand => _exportCommand ??= new DelegateCommand(ExportEvent);
        public DelegateCommand _showSettingsWindowCommand;
        public DelegateCommand ShowSettingsWindowCommand => _showSettingsWindowCommand ??= new DelegateCommand(ShowSettingsWindowEvent);
        private DelegateCommand _exitCommand;
        public DelegateCommand ExitCommand => _exitCommand ??= new DelegateCommand(ExitEvent);

        private DelegateCommand<RoutedEventArgs> _loadedCommand;
        public DelegateCommand<RoutedEventArgs> LoadedCommand => _loadedCommand ??= new DelegateCommand<RoutedEventArgs>(async (e) => await LoadedEvent(e));
        private DelegateCommand<DragEventArgs> _previewDragOverCommand;
        public DelegateCommand<DragEventArgs> PreviewDragOverCommand => _previewDragOverCommand ??= new DelegateCommand<DragEventArgs>(PreviewDragOverEvent);
        private DelegateCommand<DragEventArgs> _dropCommand;
        public DelegateCommand<DragEventArgs> DropCommand => _dropCommand ??= new DelegateCommand<DragEventArgs>(async (e) => await DropEvent(e));

        private DelegateCommand<MouseButtonEventArgs> _mouseDownCommand;
        public DelegateCommand<MouseButtonEventArgs> MouseDownCommand => _mouseDownCommand ??= new DelegateCommand<MouseButtonEventArgs>(MouseDownEvent);
        private DelegateCommand<MouseEventArgs> _mouseMoveCommand;
        public DelegateCommand<MouseEventArgs> MouseMoveCommand => _mouseMoveCommand ??= new DelegateCommand<MouseEventArgs>(MouseMoveEvent);
        private DelegateCommand<MouseButtonEventArgs> _mouseUpCommand;
        public DelegateCommand<MouseButtonEventArgs> MouseUpCommand => _mouseUpCommand ??= new DelegateCommand<MouseButtonEventArgs>(MouseUpEvent);
        private DelegateCommand<MouseWheelEventArgs> _mouseWheelCommand;
        public DelegateCommand<MouseWheelEventArgs> MouseWheelCommand => _mouseWheelCommand ??= new DelegateCommand<MouseWheelEventArgs>(MouseWheelEvent);
        private DelegateCommand<MouseButtonEventArgs> _mouseDoubleClickCommand;
        public DelegateCommand<MouseButtonEventArgs> MouseDoubleClickCommand => _mouseDoubleClickCommand ??= new DelegateCommand<MouseButtonEventArgs>(MouseDoubleClickEvent);

        private DelegateCommand<object> _zoomInCommand;
        public DelegateCommand<object> ZoomInCommand => _zoomInCommand ??= new DelegateCommand<object>(ZoomInEvent);
        private DelegateCommand<object> _zoomOutCommand;
        public DelegateCommand<object> ZoomOutCommand => _zoomOutCommand ??= new DelegateCommand<object>(ZoomOutEvent);

        // test
        private DelegateCommand _showConfigCommand;
        public DelegateCommand ShowConfigCommand => _showConfigCommand ??= new DelegateCommand(ShowConfigTestEvent);
        PluginManager _pluginManager;

        public MainWindowViewModel(IRegionManager regionManager, IDialogService dialogService, CommandLineArgs args, AppSettings settings, ViewerModel viewer, PluginManager pluginManager, ILogger logger)
        {
            _logger = logger;
            using (new StopwatchScope("Setup Window", _logger))
            {
                _args = args;
                _viewer = viewer;

                _regionManager = regionManager;
                _dialogService = dialogService;
                _pluginManager = pluginManager; // test

                Image = _viewer.ObserveProperty(x => x.Image).ToReadOnlyReactivePropertySlim().AddTo(_disposable); // FIXME これ遅すぎる debugger経由だと100ms以上かかる
                ImageTransform = new ReactivePropertySlim<Transform>(MatrixTransform.Identity);
            }
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

        private Matrix MoveOffsetImage(Point point)
        {
            Vector delta = point - _movingOffset;
            var matrix = ImageTransform.Value.Value;
            // fit pixel
            matrix.OffsetX = Math.Round(delta.X);
            matrix.OffsetY = Math.Round(delta.Y);
            return matrix;
        }

        private void MouseDownEvent(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var win = Window.GetWindow((DependencyObject)e.Source);
                // set relative offset
                var matrix = ImageTransform.Value.Value;
                _movingOffset = e.GetPosition(win);
                _movingOffset.X -= matrix.OffsetX;
                _movingOffset.Y -= matrix.OffsetY;

                // エレメント外にでてもマウスイベントを受け取る
                // 呼び出した瞬間に、この関数が終わる前にmoveイベントが動き出すので、やることを済ませておく
                win.CaptureMouse();
            }
        }

        private void MouseMoveEvent(MouseEventArgs e)
        {
            var win = Window.GetWindow((DependencyObject)e.Source);
            if (win.IsMouseCaptured)
            {
                var point = e.GetPosition(win);
                Matrix matrix = MoveOffsetImage(point);
                ImageTransform.Value = new MatrixTransform(matrix);
            }

        }

        private void MouseUpEvent(MouseButtonEventArgs e)
        {
            var win = Window.GetWindow((DependencyObject)e.Source);
            if (win.IsMouseCaptured)
            {
                win.ReleaseMouseCapture();
                // set final location
                var point = e.GetPosition(win);
                Matrix matrix = MoveOffsetImage(point);
                ImageTransform.Value = new MatrixTransform(matrix);
            }
        }

        private void MouseWheelEvent(MouseWheelEventArgs e)
        {
            // todo 領域外の場合にclipするなど
            // todo easing
            // todo apply to scale delta value
            var win = Window.GetWindow((DependencyObject)e.Source);
            var position = e.GetPosition(win);
            var matrix = ImageTransform.Value.Value;
            if (e.Delta > 0)
            {
                matrix.ScaleAt(2.0, 2.0, position.X, position.Y);
            }
            else
            {
                matrix.ScaleAt(0.5, 0.5, position.X, position.Y);
            }

            // fit pixel
            matrix.OffsetX = Math.Round(matrix.OffsetX);
            matrix.OffsetY = Math.Round(matrix.OffsetY);

            ImageTransform.Value = new MatrixTransform(matrix);
        }

        private void MouseDoubleClickEvent(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // reset
                ImageTransform.Value = new MatrixTransform(Matrix.Identity);
            }
        }

        private void ZoomInEvent(object e)
        {
            // todo 領域外の場合にclipするなど
            // todo easing
            var position = Mouse.GetPosition((IInputElement)e);
            var matrix = ImageTransform.Value.Value;
            matrix.ScaleAt(2.0, 2.0, position.X, position.Y);

            // fit pixel
            matrix.OffsetX = Math.Round(matrix.OffsetX);
            matrix.OffsetY = Math.Round(matrix.OffsetY);

            ImageTransform.Value = new MatrixTransform(matrix);
        }

        private void ZoomOutEvent(object e)
        {
            // todo 領域外の場合にclipするなど
            // todo easing
            var position = Mouse.GetPosition((IInputElement)e);
            var matrix = ImageTransform.Value.Value;
            matrix.ScaleAt(0.5, 0.5, position.X, position.Y);

            // fit pixel
            matrix.OffsetX = Math.Round(matrix.OffsetX);
            matrix.OffsetY = Math.Round(matrix.OffsetY);

            ImageTransform.Value = new MatrixTransform(matrix);
        }

        private async Task LoadedEvent(RoutedEventArgs e)
        {
#if true // todo only development
            if (_args.Args.Length >= 1)
            {
                if (_args.Args[0] == "-k")
                {
                    System.Diagnostics.Process.GetCurrentProcess().Kill(); // force kill
                }
                else if (_args.Args[0] == "-e")
                {
                    Application.Current.Shutdown();
                }
            }
#endif

            // parse initial path
            string LogicalPath = null;
            string VirtualPath = null;
            if (_args.Args != null)
            {

                if (_args.Args.Length > 0)
                {
                    LogicalPath = _args.Args[0];
                }

                if (_args.Args.Length > 1)
                {
                    VirtualPath = _args.Args[1];
                }
            }

            await _viewer.ProcessAsync(LogicalPath, VirtualPath);
        }

        private void PreviewDragOverEvent(DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop); // handled
        }

        private async Task DropEvent(DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            await _viewer.ProcessAsync(paths[0]);
        }

        private void ExportEvent()
        {
            // todo open dialog and choise format
            string path = "export.bmp";
            _viewer.Export(path, Image.Value);
        }

        private void ExitEvent()
        {
            Application.Current.Shutdown();
        }

        private void ShowSettingsWindowEvent()
        {
            _dialogService.ShowDialog("SettingsWindow");
        }

        private void ShowConfigTestEvent()
        {
            // fixme 必要悪, 引数から取得する方法を考える
            var win = Application.Current.MainWindow;
            var handle = new System.Windows.Interop.WindowInteropHelper(win);
            _pluginManager.ShowConfigTest(handle.Handle);
        }

    }
}
