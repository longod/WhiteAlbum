using Cysharp.Text;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ZLogger;


namespace WA.Viewer
{
    /// <summary>
    /// partial of App.xaml
    /// </summary>
    public partial class App
    {
        private string[] _args;
        private ILoggerFactory _loggerFactory;

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<CommandLineArgs>(() => new CommandLineArgs(_args));
            containerRegistry.RegisterSingleton<AppSettings>(() => AppSettings.Load()); // どこも非同期に読むタイミングが無い
            containerRegistry.RegisterSingleton<ILogger>(_ =>
            {
                _loggerFactory = LoggerFactory.Create(builder =>
                {
                    var settings = Container.Resolve<AppSettings>();

                    builder.ClearProviders();
                    if (settings.Data.EnableLogging)
                    {
#if DEBUG
                        builder.SetMinimumLevel(LogLevel.Debug);
#else
                        builder.SetMinimumLevel(LogLevel.Debug); // todo enable debug logging
                                                                 //builder.SetMinimumLevel(LogLevel.Information);
#endif
                        builder.AddZLoggerFile(Path.Combine(DirectoryUtility.GetBaseDirectory(), "WA.Viewer.log"), options =>
                        {
                            SetupLoggerOptions(options);
                        });
                    }
                    else
                    {
                        builder.SetMinimumLevel(LogLevel.None);
                    }
#if true // debugger output for wpf application (no-console window)
                    builder.AddZLoggerLogProcessor(new TraceLogProcessor(SetupLoggerOptions(new ZLoggerOptions())));
#else // コンソールアプリケーションしか考慮されていないのか、WPFではVisualStudio outputに出力されない
                    builder.AddZLoggerConsole(options =>
                    {
                        SetupLoggerOptions(options);
                    }, false); // consoleOutputEncodingToUtf8=true だとIOExceptionが出る
#endif
                });

                return _loggerFactory.CreateLogger("WA.Viewer");
            });
            containerRegistry.RegisterSingleton<Susie.StringConverter>(() => Susie.StringConverter.SJIS);
            containerRegistry.RegisterSingleton<PluginManager>();
            containerRegistry.RegisterSingleton<CacheManager<BitmapSource>>();
            containerRegistry.RegisterSingleton<ViewerModel>();

            // https://prismlibrary.com/docs/wpf/dialog-service.html
            containerRegistry.RegisterDialog<Views.SettingsControl>("SettingsWindow");
        }

        private void PrismApplication_Startup(object sender, StartupEventArgs e)
        {
            _args = e.Args; // store commandline args
        }

        private void PrismApplication_Exit(object sender, ExitEventArgs e)
        {
            Container.Resolve<AppSettings>().Save();
            Container.Resolve<PluginManager>().Dispose();
            _loggerFactory?.Dispose();
        }

        private static ZLoggerOptions SetupLoggerOptions(ZLoggerOptions options)
        {
            var prefixFormat = ZString.PrepareUtf8<DateTime, LogLevel>("[{0}] [{1}] ");
            options.PrefixFormatter = (writer, info) => prefixFormat.FormatTo(ref writer, info.Timestamp.DateTime.ToLocalTime(), info.LogLevel);
            return options;
        }
    }
}
