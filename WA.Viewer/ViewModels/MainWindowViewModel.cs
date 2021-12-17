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

        private Point _movingOffset;

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        // fixme readonly one way, getting fileinfo
        public ReactivePropertySlim<string> Title { get; } = new ReactivePropertySlim<string>(_appTitle);

        // https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging?view=netframeworkdesktop-4.8
        public ReadOnlyReactivePropertySlim<BitmapSource> Image { get; }

        public ReactivePropertySlim<Transform> ImageTransform { get; private set; }

        public DelegateCommand ShowSettingsWindowCommand { get; }
        public DelegateCommand ExitCommand { get; }

        public DelegateCommand<RoutedEventArgs> LoadedCommand { get; }
        public DelegateCommand<DragEventArgs> PreviewDragOverCommand { get; }
        public DelegateCommand<DragEventArgs> DropCommand { get; }

        public DelegateCommand<MouseButtonEventArgs> MouseDownCommand { get; }
        public DelegateCommand<MouseEventArgs> MouseMoveCommand { get; }
        public DelegateCommand<MouseButtonEventArgs> MouseUpCommand { get; }
        public DelegateCommand<MouseWheelEventArgs> MouseWheelCommand { get; }

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
            ImageTransform = new ReactivePropertySlim<Transform>(MatrixTransform.Identity);

            ExitCommand = new DelegateCommand(Exit);
            ShowSettingsWindowCommand = new DelegateCommand(ShowSettingsWindow);
            ShowConfigCommand = new DelegateCommand(ShowConfigTest);

            LoadedCommand = new DelegateCommand<RoutedEventArgs>(LoadedEvent);
            PreviewDragOverCommand = new DelegateCommand<DragEventArgs>(PreviewDragOverEvent);
            DropCommand = new DelegateCommand<DragEventArgs>(DropEvent);

            MouseDownCommand = new DelegateCommand<MouseButtonEventArgs>(MouseDownEvent);
            MouseMoveCommand = new DelegateCommand<MouseEventArgs>(MouseMoveEvent);
            MouseUpCommand = new DelegateCommand<MouseButtonEventArgs>(MouseUpEvent);
            MouseWheelCommand = new DelegateCommand<MouseWheelEventArgs>(MouseWheelEvent);
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
            System.Diagnostics.Trace.WriteLine("mouse wheel");
            // zoom ratioはintでもったほうがよさそう
        }

        private void LoadedEvent(RoutedEventArgs e)
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

        private void PreviewDragOverEvent(DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop); // handled
        }

        private async void DropEvent(DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            await _viewer.ProcessAsync(paths[0]);
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
            // fixme 必要悪
            // IValueProvider とかで取得して、view resourceにもたせてbindすればとれるか？
            var win = Application.Current.MainWindow;
            var handle = new System.Windows.Interop.WindowInteropHelper(win);
            _pluginManager.ShowConfigTest(handle.Handle);
        }

    }
}
