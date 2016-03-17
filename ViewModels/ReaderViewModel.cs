using System;
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
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string Port { get; set; }
        public bool Connected { get; set; }
        public ICommand ConnectCommand { get; set; }

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
                string request = string.Empty;
                // Буфер для хранения принятых от клиента данных
                byte[] buffer = new byte[1024];
                // Переменная для хранения количества байт, принятых от клиента
                int count;

                while ((count = _client.GetStream().Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Преобразуем эти данные в строку и добавим ее к переменной Request
                    request += Encoding.ASCII.GetString(buffer, 0, count);
                    // Запрос должен обрываться последовательностью \r\n
                    // Либо обрываем прием данных сами, если длина строки Request превышает 4 килобайта
                    // Нам не нужно получать данные из POST-запроса (и т. п.), а обычный запрос
                    // по идее не должен быть больше 4 килобайт
                    if (request.IndexOf("\r\n", StringComparison.Ordinal) >= 0 || request.Length > 4096)
                    {
                        PushMessage(string.Format("Data received from {0}: {1}", Name, request.TrimEnd('\r','\n')), 
                            LogMessageType.Reader);
                        request = string.Empty;
                    }
                }
            }
            else
            {
                PushMessage(string.Format("ERROR: Connection to {0}:{1} failed.", IpAddress, Port), LogMessageType.Error);
            }
        }
    }
}
