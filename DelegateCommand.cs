using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SpeedwayClientWpf
{
    public class DelegateCommand : ICommand, INotifyPropertyChanged
    {
        // Fields
        private Func<object, bool> _canExecute;
        private Action<object> _execute;
        private Func<string> _getUiName;
        private Func<Visibility> _getVisibility;

        public bool CanProvideUiName
        {
            get { return _getUiName != null; }
        }

        public bool CanProvideVisibility
        {
            get { return _getVisibility != null; }
        }

        // Events
        public virtual event EventHandler CanExecuteChanged
        {
            add
            {
                if (this._canExecute != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }
            remove
            {
                if (this._canExecute != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        private DelegateCommand()
        {
        }


        public DelegateCommand(Action execute, Func<bool> canExecute = null, Func<string> getUiName = null, Func<Visibility> getVisibility = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            this._execute = parameter => execute();
            this._canExecute = parameter => canExecute == null || canExecute();
            _getUiName = getUiName;
            _getVisibility = getVisibility;
        }



        public static DelegateCommand Create<ParameterType>(Action<ParameterType> execute, Predicate<ParameterType> canExecute = null, Func<string> getUiName = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            DelegateCommand delegateCommand = new DelegateCommand();

            delegateCommand._execute = (paramater) => execute((ParameterType)paramater);
            delegateCommand._canExecute = (paramater) => canExecute == null || canExecute(paramater != null ? (ParameterType)paramater : default(ParameterType));
            delegateCommand._getUiName = getUiName;

            return delegateCommand;
        }

        public bool CanExecute(object parameter)
        {
            this.OnPropertyChanged(() => UiName);
            this.OnPropertyChanged(() => Visibility);

            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return _canExecute == null || _canExecute(parameter);

            return true;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public virtual string UiName
        {
            get { return _getUiName(); }
        }

        public virtual Visibility Visibility
        {
            get { return _getVisibility(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null)
                return;
            PropertyChangedEventArgs propertyChangedEventArgs = new PropertyChangedEventArgs(propertyName);
            PropertyChanged(this, propertyChangedEventArgs);
        }
    }
}
