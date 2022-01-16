using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;

namespace WA.Viewer.ViewModels
{
    class SettingsControlViewModel : BindableBase, IDialogAware, IDisposable
    {
        private bool _executedClosingCommand = false;

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        public string Title => "Settings";

        public event Action<IDialogResult> RequestClose;

        private AppSettings _settings;
        private PluginManager _pluginManager;

        public ReactivePropertySlim<bool> EnableLogging { get; }
        public ReactivePropertySlim<bool> EnableBuiltInDecoders { get; }

        public ReadOnlyReactiveCollection<string> PluginDirectories { get; }
        public ReadOnlyReactiveCollection<string> PluginList { get; }

        private DelegateCommand<object> _pluginDirectoryUpCommand;
        public DelegateCommand<object> PluginDirectoryUpCommand => _pluginDirectoryUpCommand ??= new DelegateCommand<object>(PluginDirectoryUpEvent);
        private DelegateCommand<object> _pluginDirectoryDownCommand;
        public DelegateCommand<object> PluginDirectoryDownCommand => _pluginDirectoryDownCommand ??= new DelegateCommand<object>(PluginDirectoryDownEvent);
        private DelegateCommand<object> _pluginDirectoryAddCommand;
        public DelegateCommand<object> PluginDirectoryAddCommand => _pluginDirectoryAddCommand ??= new DelegateCommand<object>(PluginDirectoryAddEvent);
        private DelegateCommand<object> _pluginDirectoryRemoveCommand;
        public DelegateCommand<object> PluginDirectoryRemoveCommand => _pluginDirectoryRemoveCommand ??= new DelegateCommand<object>(PluginDirectoryRemoveEvent);

        private DelegateCommand<object> _pluginListRescanCommand;
        public DelegateCommand<object> PluginListRescanCommand => _pluginListRescanCommand ??= new DelegateCommand<object>(PluginListRescanEvent);
        private DelegateCommand<object> _pluginListConfigCommand;
        public DelegateCommand<object> PluginListConfigCommand => _pluginListConfigCommand ??= new DelegateCommand<object>(PluginListConfigEvent);

        private DelegateCommand<string> _closeDialogCommand;
        public DelegateCommand<string> CloseDialogCommand => _closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

        public SettingsControlViewModel(AppSettings settings, PluginManager pluginManager)
        {
            _settings = settings;
            _pluginManager = pluginManager;
            _pluginManager.ScanPluginDirectory();

            EnableLogging = new ReactivePropertySlim<bool>(_settings.Data.EnableLogging).AddTo(_disposable);
            EnableLogging.Subscribe(x => _settings.Data.EnableLogging = x);

            EnableBuiltInDecoders = new ReactivePropertySlim<bool>(_settings.Data.EnableBuiltInDecoders).AddTo(_disposable);
            EnableBuiltInDecoders.Subscribe(x => _settings.Data.EnableBuiltInDecoders = x);


            PluginDirectories = _settings.Data.PluginDirectories.ToReadOnlyReactiveCollection().AddTo(_disposable);
            PluginList = _pluginManager.PluginList.ToReadOnlyReactiveCollection().AddTo(_disposable);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            if (!_executedClosingCommand)
            {
                // cancel
                AppSettings.Revert(_settings);
            }
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        private void SwapDirectory(int from, int to)
        {
            if (from < 0 || from >= _settings.Data.PluginDirectories.Count)
            {
                return;
            }
            if (to < 0 || to >= _settings.Data.PluginDirectories.Count)
            {
                return;
            }
            var list = _settings.Data.PluginDirectories;
            var temp = list[from];
            list[from] = list[to];
            list[to] = temp;
        }

        private void RemoveDirectory(int index)
        {
            if (index >= 0 && index < _settings.Data.PluginDirectories.Count)
            {
                _settings.Data.PluginDirectories.RemoveAt(index);
            }
        }

        private void AddDirectory(string path)
        {
            _settings.Data.PluginDirectories.Add(path);
        }

        private void PluginDirectoryUpEvent(object e)
        {
            var list = (ListBox)e;
            var index = list?.SelectedIndex;
            if (index.HasValue)
            {
                SwapDirectory(index.Value, index.Value - 1);
            }
        }

        private void PluginDirectoryDownEvent(object e)
        {
            var list = (ListBox)e;
            var index = list?.SelectedIndex;
            if (index.HasValue)
            {
                SwapDirectory(index.Value, index.Value + 1);
            }
        }

        private void PluginDirectoryAddEvent(object e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog() == true)
            {
                AddDirectory(dialog.SelectedPath);
            }
        }
        private void PluginDirectoryRemoveEvent(object e)
        {
            // confirm dialog?
            var list = (ListBox)e;
            var index = list?.SelectedIndex;
            if (index.HasValue)
            {
                RemoveDirectory(index.Value);
            }
        }

        private void PluginListRescanEvent(object e)
        {
            _pluginManager.ScanPluginDirectory(true);
        }

        private void PluginListConfigEvent(object e)
        {
            // test
            // todo 無かった場合のケアとかをしたいが…戻り値はかえすべきではないらしいフォールバックをどうにか
            var list = (ListView)e;
            var item = (string)list?.SelectedItem;
            if (item != null)
            {
                var win = Window.GetWindow((DependencyObject)e);
                var handle = new System.Windows.Interop.WindowInteropHelper(win);
                _pluginManager.ShowConfig(item, handle);
            }

        }

        protected virtual void CloseDialog(string parameter)
        {
            ButtonResult result = ButtonResult.None;
            if (parameter?.ToLower() == "true")
            {
                // sync
                _settings.Save();
                // todo reflect settings if available

                result = ButtonResult.OK;
            }
            else if (parameter?.ToLower() == "false")
            {
                AppSettings.Revert(_settings);
                result = ButtonResult.Cancel;
            }
            _executedClosingCommand = true;
            RaiseRequestClose(new DialogResult(result));
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
