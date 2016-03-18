using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpeedwayClientWpf.ViewModels
{
    public class ReaderViewModel : ViewModelBase
    {
        private TcpClient _client;
        readonly char[] _delimiterChars = { ',' };
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string Port { get; set; }
        public bool Connected { get; set; }
        public ICommand ConnectCommand { get; set; }

        Dictionary<int, int> _tags = new Dictionary<int, int>();  // this dictionary holds tag reads. It is used to save tag reads, so that the tag will not report if read again before x seconds have elapsed. The form will need a field to set this seconds value.
            
        public ReaderControl ReaderControl { get; set; }

        public ReaderViewModel()
        {
            ReaderControl = new ReaderControl {DataContext = this};
            ConnectCommand = new DelegateCommand(Connect, () => IsEndPointValid() && !Connected);

            Task.Factory.StartNew(CheckConnection);
        }

        private void CheckConnection()
        {
            while (true)
            {
                Thread.Sleep(10000);
                if (Connected)
                {
                    try
                    {
                        Connected = !(_client.Client.Poll(1, SelectMode.SelectRead) && _client.Client.Available == 0);
                    }
                    catch (SocketException)
                    {
                        Connected = false;
                    }

                    if (!Connected)
                        PushMessage(string.Format("ERROR: {0} connection lost.", Name), LogMessageType.Error);
                }
            }
        }


        private bool IsEndPointValid()
        {
            IPAddress ip;
            int port;

            return IPAddress.TryParse(IpAddress, out ip) && int.TryParse(Port, out port);
        }

        private void Connect()
        {
            _client = new TcpClient();
            _client.BeginConnect(IPAddress.Parse(IpAddress), int.Parse(Port), ConnectedCallback, null);
        }

        private void ConnectedCallback(IAsyncResult ar)
        {
            if (_client.Connected)
            {
                PushMessage(string.Format("{0} connected successfully to {1}", Name, _client.Client.RemoteEndPoint),
                    LogMessageType.Reader);
                Connected = true;
                _client.EndConnect(ar);

                // Объявим строку, в которой будет хранится запрос клиента
                //string message = string.Empty;
                // Буфер для хранения принятых от клиента данных
                //byte[] buffer = new byte[1024];
                // Переменная для хранения количества байт, принятых от клиента
                //int count;

                while (true)
                    //(count = _client.GetStream().Read(buffer, 0, buffer.Length)) > 0)
                {
                    using (var sr = new StreamReader(_client.GetStream()))
                    {
                        var message = sr.ReadLine();
                        //message += Encoding.ASCII.GetString(buffer, 0, count);
                        // Запрос должен обрываться последовательностью \r\n
                        // Либо обрываем прием данных сами, если длина строки Request превышает 4 килобайта
                        // Нам не нужно получать данные из POST-запроса (и т. п.), а обычный запрос
                        // по идее не должен быть больше 4 килобайт
                        if (message.IndexOf("\n", StringComparison.Ordinal) >= 0 || message.Length > 4096)
                        {
                            ProcessMessage(message);
                            //message = string.Empty;
                        }
                    }
                }
            }
            else
            {
                PushMessage(string.Format("ERROR: Connection to {0}:{1} failed.", IpAddress, Port), LogMessageType.Error);
            }
        }

        private void ProcessMessage(string message)
        {
            PushMessage(string.Format("Data received from {0}: {1}", Name, message.TrimEnd('\r', '\n')),
                            LogMessageType.Reader);

            try
            {
                string[] parts = message.Split(_delimiterChars);
                string bibHex = parts[1]; // sometimes the EPC hex is too long. Get the last 7 of hex
                if (bibHex.Length > 7)
                {
                    bibHex = bibHex.Substring(bibHex.Length - 7);
                }
                int bib = Convert.ToInt32(bibHex, 16);
                var tagFilter = MainWindowViewModel.Instance.TagFilter;
                var rereadTime = MainWindowViewModel.Instance.RereadTime;

                int epochTimeToArray = Convert.ToInt32(parts[2].Substring(0, 10));  //extract epoch time

                if ((string.IsNullOrEmpty(tagFilter) || (!string.IsNullOrEmpty(tagFilter) && bib.ToString().Contains(tagFilter))) && // This filter EPC tags based on the filter given in the form.
                    (!_tags.ContainsKey(bib) || (_tags.ContainsKey(bib) && _tags[bib] + rereadTime <= epochTimeToArray))) 
                {
                    int epochTimeUltra = epochTimeToArray - 312768000; // convert the time ??
                    int milliEpochTimeToArray = Convert.ToInt32(parts[2].Substring(10, 3));  // peel of the milliseconds



                    _tags[bib] = epochTimeToArray;
                    WriteToFile(message);
                }
            }
            catch(Exception e)
            {
                PushMessage(string.Format("ERROR: message parsing error from {0}. {1}", Name, e.Message), LogMessageType.Error);
            }
        }

        private readonly object _locker = new Object();
        public void WriteToFile(string text)
        {
            var folder = MainWindowViewModel.Instance.FolderPath;
            if(string.IsNullOrEmpty(folder)) return;

            var fileName = Path.Combine(folder, IpAddress+ ".txt");
            
            lock (_locker)
            {
                using (var file = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(file, Encoding.Unicode))
                {
                    writer.Write(text);
                }
            }
        }
    }
}
