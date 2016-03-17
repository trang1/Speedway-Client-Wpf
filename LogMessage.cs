using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SpeedwayClientWpf.ViewModels
{
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
        Common = 0,
        Listener = 1,
        Reader = 2,
        Error = 3
    }

    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            LogMessageType type;
            if (Enum.TryParse(value.ToString(), out type))
            {
                switch (type)
                {
                    case LogMessageType.Common:
                        return new SolidColorBrush(Colors.Black);
                    case LogMessageType.Listener:
                        return new SolidColorBrush(Colors.BlueViolet);
                    case LogMessageType.Reader:
                        return new SolidColorBrush(Colors.DarkGreen);
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