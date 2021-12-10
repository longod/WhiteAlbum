using Prism.Mvvm;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WA.Viewer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "WHITE ALBUM";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private ViewerModel _viewer;
        private BitmapSource _image;

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

        // この伝搬方法は妥当なのか？
        private void Viewer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Image":
                    Image = _viewer.Image;
                    break;
                default:
                    break;
            }
        }
    }
}
