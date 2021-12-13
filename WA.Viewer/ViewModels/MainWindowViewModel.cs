using Prism.Mvvm;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WA.Viewer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private const string _appTitle = "WHITE ALBUM";
        private string _title = _appTitle;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private ViewerModel _viewer; // todo dispose
        private BitmapSource _image;

        // https://docs.microsoft.com/ja-jp/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging?view=netframeworkdesktop-4.8
        public BitmapSource Image
        {
            get { return _image; }
            set { SetProperty(ref _image, value); }
        }

        public MainWindowViewModel(ViewerModel viewer)
        {
            _viewer = viewer;
            _viewer.PropertyChanged += Viewer_PropertyChanged;
            Task.Run(() => _viewer.ProcessAsync());
        }

        // FIXME この伝搬方法は妥当なのか？ messenger?
        private void Viewer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewer.Image):
                    Image = _viewer.Image;
                    Title = _appTitle + ' ' + _viewer.LogicalPath + ' ' + _viewer.VirtualPath; // todo display more file info 
                    break;
                default:
                    break;
            }
        }
    }
}
