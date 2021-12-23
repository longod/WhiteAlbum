using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;

namespace WA.Viewer.ViewModels
{
    class SettingsControlViewModel : BindableBase, IDialogAware, IDisposable
    {
        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        public string Title => "Settings";

        public event Action<IDialogResult> RequestClose;

        private AppSettings _settings;
        private PluginManager _pluginManager;

        public ReactivePropertySlim<bool> EnableLogging { get; }
        public ReactivePropertySlim<bool> EnableBuiltInDecoders { get; }

        private DelegateCommand<string> _closeDialogCommand;
        public DelegateCommand<string> CloseDialogCommand => _closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

        public SettingsControlViewModel(AppSettings settings, PluginManager pluginManager)
        {
            _settings = settings;
            _pluginManager = pluginManager;

            EnableLogging = new ReactivePropertySlim<bool>(_settings.Data.EnableLogging).AddTo(_disposable);
            EnableLogging.Subscribe(x => _settings.Data.EnableLogging = x);

            EnableBuiltInDecoders = new ReactivePropertySlim<bool>(_settings.Data.EnableBuiltInDecoders).AddTo(_disposable);
            EnableBuiltInDecoders.Subscribe(x => _settings.Data.EnableBuiltInDecoders = x);
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
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
