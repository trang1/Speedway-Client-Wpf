using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
        private Timer _timer;
        private List<LogMessage> _messagesQueue;
        #endregion

        #region Constructor

        public MainWindowViewModel()
        {
            _listenerViewModel = new ListenerViewModel();
            Messages = new ObservableCollection<LogMessage>();
            FilteredMessages = new ObservableCollection<LogMessage>();
            _messagesQueue = new List<LogMessage>();
            _settings = new Settings();
            _timer = new Timer(LogCallback, null, 1, 1);
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

        private void LogCallback(object state)
        {
            lock (_messagesQueue)
            {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    foreach (var message in _messagesQueue)
                    {
                        Messages.Insert(0, message);
                    }
                    _messagesQueue.Clear();
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
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
        public ObservableCollection<LogMessage> FilteredMessages { get; set; }
        
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
            lock(_messagesQueue)
            {
                _messagesQueue.Add(logMessage);
            }

            if (logMessage.IsFiltered)
                Application.Current.Dispatcher.Invoke(new System.Action(() =>
                    FilteredMessages.Insert(0, logMessage)));

           // if (Messages.Count > 10000)
           //     Messages.Clear();

           // OnPropertyChanged("FilteredMessages");
        }

        #endregion
    }
}