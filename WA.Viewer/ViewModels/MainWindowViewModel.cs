using Prism.Mvvm;
using System.Windows.Media.Imaging;

namespace WA.Viewer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private ViewerModel _viewer;

        public BitmapSource Image { get { return _viewer.Image; } }

        public MainWindowViewModel(ViewerModel viewer)
        {
            _viewer = viewer;
        }
    }
}
