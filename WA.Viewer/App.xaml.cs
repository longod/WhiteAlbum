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
        private ViewerModelArgs _args;


        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ViewerModel>(() => new ViewerModel(_args));
        }

        private void PrismApplication_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length >= 1)
            {
                _args = new ViewerModelArgs() { Path = e.Args[0] };
            }
        }

        private void PrismApplication_Exit(object sender, ExitEventArgs e)
        {

        }
    }
}
