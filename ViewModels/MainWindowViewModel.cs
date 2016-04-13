using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace SpeedwayClientWpf.ViewModels
{
    /// <summary>
    /// Represents the viewModel for MainWindow
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        #region private members
        private static readonly MainWindowViewModel _mainWindowViewModel = new MainWindowViewModel();
        private readonly ListenerViewModel _listenerViewModel;
        private string _folderPath;
        private string _tagFilter;
        private int _rereadTime;
        private bool _addDateToOutput;
        private bool _addReaderInfoToOutput;

        /// <summary>
        /// Shows a dialog to choose a folder to save the files
        /// </summary>
        private void SelectFolderPath()
        {
            var dialog = new FolderBrowserDialog
            {
                
                Description = "Please select directory to save the files.",
                SelectedPath = FolderPath,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                FolderPath = dialog.SelectedPath;
        }

        /// <summary>
        /// Saves settings to the app.config file
        /// </summary>
        private void SaveSettings()
        {
            ConfigHelper.Set("TagFilter", TagFilter);
            ConfigHelper.Set("RereadTime", RereadTime.ToString());
            ConfigHelper.Set("FolderPath", FolderPath);
            ConfigHelper.Set("LocalIpAddress", _listenerViewModel.IpAddress);
            ConfigHelper.Set("ListenerPort", _listenerViewModel.Port);
            ConfigHelper.Set("AddDateToOutput", AddDateToOutput.ToString());
            ConfigHelper.Set("AddReaderInfoToOutput", AddReaderInfoToOutput.ToString());
            ConfigHelper.SaveReaders(Readers);
        }

        /// <summary>
        /// Loads settings from the app.config file
        /// </summary>
        private void LoadSettings()
        {
            TagFilter = ConfigHelper.Get("TagFilter");
            FolderPath = ConfigHelper.Get("FolderPath");
            _listenerViewModel.IpAddress = ConfigHelper.Get("LocalIpAddress");
            _listenerViewModel.Port = ConfigHelper.Get("ListenerPort");
            AddDateToOutput = bool.Parse(ConfigHelper.Get("AddDateToOutput"));
            AddReaderInfoToOutput = bool.Parse(ConfigHelper.Get("AddReaderInfoToOutput"));

            int rereadtime;
            if (int.TryParse(ConfigHelper.Get("RereadTime"), out rereadtime))
                RereadTime = rereadtime;

            Readers = new ObservableCollection<ReaderViewModel>(ConfigHelper.GetReaders());
        }

        #endregion 

        #region Constructor

        public MainWindowViewModel()
        {
            _listenerViewModel = new ListenerViewModel();
            Messages = new ObservableCollection<LogMessage>();

           /* Readers = new ObservableCollection<ReaderViewModel>
            {
                new ReaderViewModel {Name = "Reader 1", Port = "14150"},
                new ReaderViewModel {Name = "Reader 2", Port = "14150"},
                new ReaderViewModel {Name = "Reader 3", Port = "14150"},
                new ReaderViewModel {Name = "Reader 4", Port = "14150"}
            };*/

            SelectFolderPathCommand = new DelegateCommand(SelectFolderPath);
            SaveSettingsCommand = new DelegateCommand(SaveSettings);
            ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());

            LoadSettings();
        }

        #endregion

        #region public members
        public static MainWindowViewModel Instance
        {
            get { return _mainWindowViewModel; }
        }

        /// <summary>
        /// Our broadcast server
        /// </summary>
        public ListenerViewModel ListenerViewModel
        {
            get { return _listenerViewModel; }
        }

        /// <summary>
        /// Folder to save the output files
        /// </summary>
        public string FolderPath
        {
            get { return _folderPath; }
            set
            {
                if (Directory.Exists(value))
                {
                    _folderPath = value;
                    OnPropertyChanged("FolderPath");
                }
            }
        }

        /// <summary>
        /// Some string to filter tags
        /// </summary>
        public string TagFilter
        {
            get { return _tagFilter; }
            set
            {
                _tagFilter = value;
                OnPropertyChanged("TagFilter");
            }
        }

        /// <summary>
        /// Time to check if tag was seen before, seconds
        /// </summary>
        public int RereadTime
        {
            get { return _rereadTime; }
            set
            {
                _rereadTime = value;
                OnPropertyChanged("RereadTime");
            }
        }

        /// <summary>
        /// Add date to the file output
        /// </summary>
        public bool AddDateToOutput
        {
            get
            {
                return _addDateToOutput;
            }
            set
            {
                _addDateToOutput = value;
                OnPropertyChanged("AddDateToOutput");
            }
        }

        /// <summary>
        /// Add reader id & antenna id to the file output
        /// </summary>
        public bool AddReaderInfoToOutput
        {
            get { return _addReaderInfoToOutput; }
            set
            {
                _addReaderInfoToOutput = value;
                OnPropertyChanged("AddReaderInfoToOutput");
            }
        }

        public bool IsLogFiltered { get; set; }
        /// <summary>
        /// Adds a message to the log window. If the count of the messages is more than 1000, we should clear the list.
        /// </summary>
        /// <param name="logMessage"></param>
        public void PushMessage(LogMessage logMessage)
        {
            Messages.Insert(0, logMessage);

            if(Messages.Count > 100000)
                Messages.Clear();

            OnPropertyChanged("FilteredMessages");
        }

        /// <summary>
        /// Collection of reader infos
        /// </summary>
        public ObservableCollection<ReaderViewModel> Readers { get; set; } 
        
        /// <summary>
        /// Collection of the messages in the log window
        /// </summary>
        public ObservableCollection<LogMessage> Messages { get; set; }

        public ObservableCollection<LogMessage> FilteredMessages
        {
            get { return new ObservableCollection<LogMessage>(Messages.Where(m => m.IsFiltered)); }
        }
        public ICommand ExitCommand { get; set; }
        public ICommand SelectFolderPathCommand { get; set; }
        public ICommand SaveSettingsCommand { get; set; }

        #endregion
    }
}
