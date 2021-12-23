using Prism.Ioc;
using System.Windows;
using WA.Album.Views;

namespace WA.Album
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }
    }
}
