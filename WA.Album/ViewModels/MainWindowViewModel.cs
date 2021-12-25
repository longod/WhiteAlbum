using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WA.Album.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private string _title = "WHITE ALBUM";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private IRegionManager _regionManager;
        private IDialogService _dialogService;
        private ViewerModel _viewer;
        private CommandLineArgs _args;
        private ILogger _logger;

        private CompositeDisposable _disposable { get; } = new CompositeDisposable();

        private DelegateCommand<RoutedEventArgs> _loadedCommand;
        public DelegateCommand<RoutedEventArgs> LoadedCommand => _loadedCommand ??= new DelegateCommand<RoutedEventArgs>(async (e) => await LoadedEvent(e));
        private DelegateCommand<DragEventArgs> _previewDragOverCommand;
        public DelegateCommand<DragEventArgs> PreviewDragOverCommand => _previewDragOverCommand ??= new DelegateCommand<DragEventArgs>(PreviewDragOverEvent);
        private DelegateCommand<DragEventArgs> _dropCommand;
        public DelegateCommand<DragEventArgs> DropCommand => _dropCommand ??= new DelegateCommand<DragEventArgs>(async (e) => await DropEvent(e));

        public ReadOnlyReactiveCollection<PackedFile> Files { get; }


        public MainWindowViewModel(IRegionManager regionManager, IDialogService dialogService, CommandLineArgs args, AppSettings settings, ViewerModel viewer, PluginManager pluginManager, ILogger logger)
        {
            _logger = logger;
            _args = args;
            _viewer = viewer;

            _regionManager = regionManager;
            _dialogService = dialogService;

            Files = _viewer.Files.ToReadOnlyReactiveCollection().AddTo(_disposable);
        }

        public void Dispose()
        {
            _disposable.Dispose();
            if (_regionManager != null)
            {
                foreach (var region in _regionManager.Regions)
                {
                    region.RemoveAll();
                }
            }
        }

        private async Task LoadedEvent(RoutedEventArgs e)
        {
            // parse initial path
            string LogicalPath = null;
            string VirtualPath = null;
            if (_args.Args != null)
            {

                if (_args.Args.Length > 0)
                {
                    LogicalPath = _args.Args[0];
                }

                if (_args.Args.Length > 1)
                {
                    VirtualPath = _args.Args[1];
                }
            }

            await _viewer.ProcessAsync(LogicalPath, VirtualPath);
        }

        private void PreviewDragOverEvent(DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = e.Data.GetDataPresent(DataFormats.FileDrop); // handled
        }

        private async Task DropEvent(DragEventArgs e)
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            await _viewer.ProcessAsync(paths[0]);
        }
    }
}
