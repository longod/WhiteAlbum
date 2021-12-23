using Prism.Mvvm;

namespace WA.Album.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "WHITE ALBUM";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {

        }
    }
}
