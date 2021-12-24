﻿using Microsoft.Extensions.Logging;
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
        private Point _scalingPivot;

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        // fixme readonly one way, getting fileinfo
        public ReactivePropertySlim<string> Title { get; } = new ReactivePropertySlim<string>(_appTitle);

        // https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging?view=netframeworkdesktop-4.8
        public ReadOnlyReactivePropertySlim<BitmapSource> Image { get; }

        public ReactivePropertySlim<Transform> ImageTransform { get; private set; }

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
            matrix.OffsetX = delta.X;
            matrix.OffsetY = delta.Y;
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
            // TODO ZOOM IN/OUT
            // https://stackoverflow.com/questions/741956/pan-zoom-image

            //System.Diagnostics.Trace.WriteLine("mouse wheel");
            // zoom ratioはintでもったほうがよさそう
            var win = Window.GetWindow((DependencyObject)e.Source);
            _scalingPivot = e.GetPosition(win);
            var matrix = ImageTransform.Value.Value;
            // scalingしてspaceをあわせないといけない
            _scalingPivot.X -= matrix.OffsetX;
            _scalingPivot.Y -= matrix.OffsetY;
            //_scalingPivot = matrix.Transform(_scalingPivot);
            System.Diagnostics.Trace.WriteLine("mouse wheel " + _scalingPivot);
            const double scale = 1.2;
            // 逆方向に回転させても、1, 2tick前回の挙動になるのは…
            if (e.Delta > 0)
            {
                _scale *= 2;
                // mag
                Matrix s = Matrix.Identity;
                s.ScaleAt(_scale, _scale, 0, 0);
                //matrix.ScaleAtPrepend(scale, scale, _scalingPivot.X, _scalingPivot.Y);
                matrix *= s;
                //matrix = s * matrix;
                System.Diagnostics.Trace.WriteLine("mouse wheel delta up" + e.Delta);
            }
            else
            {
                _scale *= 0.5;
                Matrix s = Matrix.Identity;
                s.ScaleAt(_scale, _scale, 0, 0);
                matrix *= s;
                // min
                //matrix.ScaleAtPrepend(1.0 / scale, 1.0 / scale, _scalingPivot.X, _scalingPivot.Y);
                //matrix = s * matrix;
                System.Diagnostics.Trace.WriteLine("mouse wheel delta down" + e.Delta);
            }
            ImageTransform.Value = new MatrixTransform(matrix);
        }
        double _scale = 1;

        private void MouseDoubleClickEvent(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // reset
                ImageTransform.Value = new MatrixTransform(Matrix.Identity);
            }
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
