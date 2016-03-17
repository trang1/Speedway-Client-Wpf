using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedwayClientWpf
{
    public interface INotifyPropertyChanged : System.ComponentModel.INotifyPropertyChanged
    {
        void OnPropertyChanged(string propertyName);
    }
}
