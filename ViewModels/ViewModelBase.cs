using System.ComponentModel;
using System.Windows;

namespace SpeedwayClientWpf.ViewModels
{
    /// <summary>
    /// Standard base viewModel for all viewModels using in the project
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Message to be shown to user
        /// </summary>
        /// <param name="text"></param>
        /// <param name="type"></param>
        /// <param name="isFiltered"></param>
        protected virtual void PushMessage(string text, LogMessageType type = LogMessageType.Common, bool isFiltered = false)
        {
            MainWindowViewModel.Instance.PushMessage(new LogMessage(type, text, isFiltered));
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