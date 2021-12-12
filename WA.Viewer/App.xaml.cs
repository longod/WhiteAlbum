using Prism.Ioc;
using System.Threading.Tasks;
using System.Windows;
using WA.Viewer.Views;

namespace WA.Viewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private string[] _args;

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<AppSettings>(() => AppSettings.Load()); // どこも非同期に読むタイミングが無い
            containerRegistry.RegisterSingleton<PluginManager>();
            containerRegistry.RegisterSingleton<ViewerModel.Args>(() => new ViewerModel.Args(_args));
            containerRegistry.RegisterSingleton<ViewerModel>();
        }

        private void PrismApplication_Startup(object sender, StartupEventArgs e)
        {
            _args = e.Args;
        }

        private void PrismApplication_Exit(object sender, ExitEventArgs e)
        {
            Container.Resolve<AppSettings>().Save();
            Container.Resolve<PluginManager>().Dispose();
        }
    }
}
