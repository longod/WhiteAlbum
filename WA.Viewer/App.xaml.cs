using Prism.Ioc;
using System.Windows;
using WA.Viewer.Views;

namespace WA.Viewer
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
