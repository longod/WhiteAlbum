using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using System;

namespace WA.Viewer.ViewModels
{
    class SettingsControlViewModel : BindableBase, IDialogAware
    {
        public string Title => "Settings";

        public event Action<IDialogResult> RequestClose;

        private AppSettings _settings;

        public ReactivePropertySlim<bool> EnableLogging { get; }
        public ReactivePropertySlim<bool> EnableBuiltInDecoders { get; }


        private DelegateCommand<string> _closeDialogCommand;
        public DelegateCommand<string> CloseDialogCommand => _closeDialogCommand ??= new DelegateCommand<string>(CloseDialog);

        public SettingsControlViewModel(AppSettings settings)
        {
            _settings = settings;

            EnableLogging = new ReactivePropertySlim<bool>(_settings.Data.EnableLogging);
            EnableLogging.Subscribe(x => _settings.Data.EnableLogging = x);

            EnableBuiltInDecoders = new ReactivePropertySlim<bool>(_settings.Data.EnableBuiltInDecoders);
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
    }
}
