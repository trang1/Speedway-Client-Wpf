using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SpeedwayClientWpf.ViewModels
{
    /// <summary>
    ///     Represents the viewModel for MainWindow
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region private members

        private static readonly MainWindowViewModel _mainWindowViewModel = new MainWindowViewModel();
        private readonly ListenerViewModel _listenerViewModel;
        private readonly Settings _settings;

        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            _listenerViewModel = new ListenerViewModel();
            Messages = new ObservableCollection<LogMessage>();
            _settings = new Settings();

            /* Readers = new ObservableCollection<ReaderViewModel>
            {
                new ReaderViewModel {Name = "Reader 1", Port = "14150"},
                new ReaderViewModel {Name = "Reader 2", Port = "14150"},
                new ReaderViewModel {Name = "Reader 3", Port = "14150"},
                new ReaderViewModel {Name = "Reader 4", Port = "14150"}
            };*/

            SaveStateCommand = new DelegateCommand(SaveState);
            ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());

            LoadState();
        }

        #endregion

        #region private methods

        private void LoadState()
        {
            _settings.LoadSettings();
            _listenerViewModel.IpAddress = ConfigHelper.Get("LocalIpAddress");
            _listenerViewModel.Port = ConfigHelper.Get("ListenerPort");

            Readers = new ObservableCollection<ReaderViewModel>(ConfigHelper.GetReaders());
        }

        private void SaveState()
        {
            _settings.SaveSettings();
            ConfigHelper.Set("LocalIpAddress", _listenerViewModel.IpAddress);
            ConfigHelper.Set("ListenerPort", _listenerViewModel.Port);
            ConfigHelper.SaveReaders(Readers);
        }

        #endregion

        #region public members

        public static MainWindowViewModel Instance
        {
            get { return _mainWindowViewModel; }
        }

        /// <summary>
        ///     Our broadcast server
        /// </summary>
        public ListenerViewModel ListenerViewModel
        {
            get { return _listenerViewModel; }
        }

        /// <summary>
        ///     Show only filtered messages
        /// </summary>
        public bool IsLogFiltered { get; set; }

        /// <summary>
        ///     Collection of reader infos
        /// </summary>
        public ObservableCollection<ReaderViewModel> Readers { get; set; }

        /// <summary>
        ///     Collection of the messages in the log window
        /// </summary>
        public ObservableCollection<LogMessage> Messages { get; set; }

        public ObservableCollection<LogMessage> FilteredMessages
        {
            get { return new ObservableCollection<LogMessage>(Messages.Where(m => m.IsFiltered)); }
        }

        public Settings Settings
        {
            get { return _settings; }
        }

        public ICommand ExitCommand { get; set; }
        public ICommand SaveStateCommand { get; set; }

        /// <summary>
        ///     Adds a message to the log window. If the count of the messages is more than 100000, we should clear the list.
        /// </summary>
        /// <param name="logMessage"></param>
        public void PushMessage(LogMessage logMessage)
        {
            Messages.Insert(0, logMessage);

            if (Messages.Count > 100000)
                Messages.Clear();

            OnPropertyChanged("FilteredMessages");
        }

        #endregion
    }
}