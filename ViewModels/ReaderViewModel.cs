using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Renci.SshNet;

namespace SpeedwayClientWpf.ViewModels
{
    public class ReaderViewModel : ViewModelBase
    {
        private TcpClient _client;
        bool _connected;
        readonly char[] _delimiterChars = { ',' };
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string Port { get; set; }

        public string CurrentTime
        {
            get { return _currentTime; }
            set
            {
                _currentTime = value;
                OnPropertyChanged("CurrentTime");
            }
        }
        public TimeSpan TimeToSet
        {
            get { return _timeToSet; }
            set
            {
                _timeToSet = value;
                OnPropertyChanged("TimeToSet");
            }
        }
        public bool Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                _connected = value;
                OnPropertyChanged("Connected");
                OnPropertyChanged("ConnectButtonContent");
            }
        }
        public ICommand ConnectCommand { get; set; }
        public ICommand UpdateTimeCommand { get; set; }
        public ICommand SetTimeCommand { get; set; }
        private Timer _timer;

        Dictionary<int, int> _tags = new Dictionary<int, int>();  // this dictionary holds tag reads. It is used to save tag reads, so that the tag will not report if read again before x seconds have elapsed. The form will need a field to set this seconds value.
            
        public ReaderControl ReaderControl { get; set; }
        public string ConnectButtonContent
        {
            get
            {
                return Connected ? "Disconnect" : "Connect";
            }
        }

        public ReaderViewModel()
        {
            ReaderControl = new ReaderControl {DataContext = this};
            ConnectCommand = new DelegateCommand(Connect, () => Connected || (!Connected && IsEndPointValid()));
            UpdateTimeCommand= new DelegateCommand(UpdateTime, () => Connected);
            SetTimeCommand = new DelegateCommand(SetTime, ()=> Connected);
            //Task.Factory.StartNew(CheckConnection);
            _timer = new Timer(o =>
            {
                if (Connected)
                    UpdateTime();
                SetTimeToSet(DateTime.Now);
            }, null, 60000, 60000);

            SetTimeToSet(DateTime.Now);
        }

        void SetTimeToSet(DateTime dateTime)
        {
            TimeToSet = dateTime.AddTicks(-(dateTime.Ticks % TimeSpan.TicksPerSecond)).TimeOfDay;
        }

        private void SetTime()
        {
            try
            {
                using (var sshclient = new SshClient(_connectionInfo))
                {
                    sshclient.Connect();
                    using (var command = sshclient.CreateCommand("date -s " + TimeToSet))
                    {
                       command.Execute();
                    }
                    sshclient.Disconnect();
                }
                UpdateTime();
                PushMessage(string.Format("Time for {0} successfully set.", Name), LogMessageType.Reader);
            }
            catch (Exception exception)
            {
                var error = string.Format("ERROR: update time failed for {0}. {1}", Name, exception.Message);
                PushMessage(error, LogMessageType.Error);
                Trace.TraceError(error + exception.StackTrace);
            }
        }

        private void UpdateTime()
        {
            //var connection = new ConnectionInfo(IpAddress, 22, "root", new PasswordAuthenticationMethod("root", "impinj"));
            try
            {
                using (var sshclient = new SshClient(_connectionInfo))
                {
                    sshclient.Connect();
                    using (var command = sshclient.CreateCommand("date +%H:%M:%S"))
                    {
                        CurrentTime = command.Execute().Replace('\n',' ');
                    }

                    sshclient.Disconnect();
                }
            }
            catch (Exception exception)
            {
                var error = string.Format("ERROR: update time failed for {0}. {1}", Name, exception.Message);
                PushMessage(error, LogMessageType.Error);
                Trace.TraceError(error + exception.StackTrace);
            }
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
            if (string.IsNullOrEmpty(IpAddress) || string.IsNullOrEmpty(Port))
                return false;

            int port;
            Match match = Regex.Match(IpAddress, 
                @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            return match.Success && int.TryParse(Port, out port);
        }

        private void Connect()
        {
            if (!Connected)
            {
                _client = new TcpClient();
                _client.BeginConnect(IPAddress.Parse(IpAddress), int.Parse(Port), ConnectedCallback, null);
            }
            else
            {
                _client.Close();
            }
        }

        private void ConnectedCallback(IAsyncResult ar)
        {
            try
            {
                _client.EndConnect(ar);
            }
            catch (Exception exception)
            {
                var error = string.Format("ERROR in {0}. {1}", Name, exception.Message);
                PushMessage(error, LogMessageType.Error);
                Trace.TraceError(error + exception.StackTrace);
                return;
            }

            PushMessage(string.Format("{0} connected successfully to {1}", Name, _client.Client.RemoteEndPoint),
                LogMessageType.Reader);
            Connected = true;

            using (var sr = new StreamReader(_client.GetStream()))
            {
                while (true)
                {
                    if (!Connected)
                        break;

                    try
                    {
                        var message = sr.ReadLine();

                        if (message != null)
                            Task.Factory.StartNew(() => ProcessMessage(message));
                    }
                    catch (Exception exception)
                    {
                        PushMessage(Name + " disconnected.", LogMessageType.Reader);
                        Trace.TraceError(Name + " disconnected. " + exception.Message + exception.StackTrace);

                        Connected = false;
                        break;
                    }
                }
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

                int epochTimeToArray = Convert.ToInt32(parts[2].Substring(0, 10)); //extract epoch time

                if ((string.IsNullOrEmpty(tagFilter) ||
                     (!string.IsNullOrEmpty(tagFilter) && bib.ToString().Contains(tagFilter))) &&
                    // This filter EPC tags based on the filter given in the form.
                    (!_tags.ContainsKey(bib) || (_tags.ContainsKey(bib) && _tags[bib] + rereadTime <= epochTimeToArray)))
                {
                    int epochTimeUltra = epochTimeToArray - 312768000; // convert the time ??
                    int milliEpochTimeToArray = Convert.ToInt32(parts[2].Substring(10, 3)); // peel of the milliseconds

                    string ettime = parts[2].Substring(0, 13);
                    long etime = Convert.ToInt64(ettime);
                    var epoch =
                        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(etime)
                            .ToString("HH:mm:ss.fff");
                    var readerId = IpAddress.Split('.')[3];

                    string outputToFile = string.Format("{0},{1},{2},0,\"{3}\"{4}",
                        readerId, parts[0], bib, epoch, Environment.NewLine);
                    string outputToNetwork = string.Format("0,{0},{1},{2},1,23,0,0,0,0000000000000000,0,0{3}",
                        bib, epochTimeUltra, milliEpochTimeToArray, Environment.NewLine);

                    _tags[bib] = epochTimeToArray;
                    WriteToFile(outputToFile);
                    MainWindowViewModel.Instance.ListenerViewModel.SendMessage(outputToNetwork);
                }
            }
            catch (Exception exception)
            {
                var error = string.Format("ERROR: message parsing error from {0}. {1}", Name, exception.Message);
                PushMessage(error, LogMessageType.Error);
                Trace.TraceError(error + exception.StackTrace);
            }
        }

        private readonly object _locker = new Object();
        private string _currentTime;
        private TimeSpan _timeToSet;
        private readonly ConnectionInfo _connectionInfo = 
            new ConnectionInfo("192.168.0.113", 22, "root", new PasswordAuthenticationMethod("root", "1"));

        public void WriteToFile(string text)
        {
            var folder = MainWindowViewModel.Instance.FolderPath;
            if(string.IsNullOrEmpty(folder)) return;

            try
            {
                var fileName = Path.Combine(folder, IpAddress + ".txt");

                lock (_locker)
                {
                    using (var file = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(file, Encoding.Unicode))
                    {
                        writer.Write(text);
                    }
                }
            }
            catch (Exception exception)
            {
                var error = string.Format("{0}: error writing to file. {1}", Name, exception.Message);
                Trace.TraceError(error + exception.StackTrace);
            }
        }
    }
}
