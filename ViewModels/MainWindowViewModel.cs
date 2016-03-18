using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace SpeedwayClientWpf.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region private members
        private static readonly MainWindowViewModel _mainWindowViewModel = new MainWindowViewModel();
        private readonly ListenerViewModel _listenerViewModel;
        private string _folderPath;
        private string _tagFilter;
        private int _rereadTime;

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
        #endregion 

        #region Constructor

        public MainWindowViewModel()
        {
            _listenerViewModel = new ListenerViewModel();
            Messages = new ObservableCollection<LogMessage>();

            Readers = new ObservableCollection<ReaderViewModel>
            {
                new ReaderViewModel {Name = "Reader 1", Port = "14150"},
                new ReaderViewModel {Name = "Reader 2", Port = "14150"},
                new ReaderViewModel {Name = "Reader 3", Port = "14150"},
                new ReaderViewModel {Name = "Reader 4", Port = "14150"}
            };

            SelectFolderPathCommand = new DelegateCommand(SelectFolderPath);
            ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());
        }
        
        #endregion

        #region public members
        public static MainWindowViewModel Instance
        {
            get { return _mainWindowViewModel; }
        }

        public ListenerViewModel ListenerViewModel
        {
            get { return _listenerViewModel; }
        }

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

        public string TagFilter
        {
            get { return _tagFilter; }
            set
            {
                _tagFilter = value;
                OnPropertyChanged("TagFilter");
            }
        }
        public int RereadTime
        {
            get { return _rereadTime; }
            set
            {
                _rereadTime = value;
                OnPropertyChanged("RereadTime");
            }
        }
        public void PushMessage(LogMessage logMessage)
        {
            Messages.Add(logMessage);

            if(Messages.Count > 100)
                Messages.Clear();
        }

        public ObservableCollection<ReaderViewModel> Readers { get; set; } 
        public ObservableCollection<LogMessage> Messages { get; set; }

        public ICommand ExitCommand { get; set; }
        public ICommand SelectFolderPathCommand { get; set; }

        #endregion
    }
}
