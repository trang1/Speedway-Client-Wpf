using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SpeedwayClientWpf
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region private members
        private static readonly MainWindowViewModel _mainWindowViewModel = new MainWindowViewModel();
        private readonly Listener listener;
      
        #endregion 

        #region Constructor

        public MainWindowViewModel()
        {
            listener = new Listener();
            Messages = new ObservableCollection<LogMessage>();

            ExitCommand = new DelegateCommand(() => Application.Current.Shutdown());
        }


        #endregion

        #region public members
        public static MainWindowViewModel Instance
        {
            get { return _mainWindowViewModel; }
        }

        public Listener Listener
        {
            get { return listener; }
        }
        public void PushMessage(LogMessage logMessage)
        {
            Messages.Add(logMessage);

            if(Messages.Count > 100)
                Messages.Clear();
        }
       
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

    /// <summary>
    /// Represents the object in log window
    /// </summary>
    public class LogMessage
    {
        public LogMessageType Type { get; set; }
        public string Text { get; set; }

        public LogMessage(LogMessageType type, string text)
        {
            Type = type;
            Text = string.Format("[{0}] {1}", DateTime.Now.ToLongTimeString(), text);
        }
    }

    public enum LogMessageType
    {
        Listener = 1,
        Error = 2
    }

    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LogMessageType type;
            if (Enum.TryParse(value.ToString(), out type))
            {
                switch(type)
                {
                    case LogMessageType.Listener:
                        return new SolidColorBrush(Colors.BlueViolet);
                    case LogMessageType.Error:
                        return new SolidColorBrush(Colors.Red);
                }
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
