using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SpeedwayClientWpf.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region private members
        private static readonly MainWindowViewModel _mainWindowViewModel = new MainWindowViewModel();
        private readonly ListenerViewModel _listenerViewModel;
      
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
        public void PushMessage(LogMessage logMessage)
        {
            Messages.Add(logMessage);

            if(Messages.Count > 100)
                Messages.Clear();
        }

        public ObservableCollection<ReaderViewModel> Readers { get; set; } 
        public ObservableCollection<LogMessage> Messages { get; set; }

        public ICommand ExitCommand { get; set; }

        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
