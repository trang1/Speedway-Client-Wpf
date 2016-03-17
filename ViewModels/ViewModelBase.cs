using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpeedwayClientWpf.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        protected void PushMessage(string text, LogMessageType type = LogMessageType.Common)
        {
            Application.Current.Dispatcher.Invoke(
                () => MainWindowViewModel.Instance.PushMessage(new LogMessage(type, text)));
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
