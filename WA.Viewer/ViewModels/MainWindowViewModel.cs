using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WA.Viewer.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private const string _appTitle = "WHITE ALBUM";

        private ViewerModel _viewer;

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        // fixme readonly one way, getting fileinfo
        public ReactivePropertySlim<string> Title { get; } = new ReactivePropertySlim<string>(_appTitle);

        // https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging?view=netframeworkdesktop-4.8
        public ReadOnlyReactivePropertySlim<BitmapSource> Image { get; }

        public MainWindowViewModel(ViewerModel viewer)
        {
            _viewer = viewer;
            Image = _viewer.ObserveProperty(x => x.Image).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
            Task.Run(() => _viewer.ProcessAsync());
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
