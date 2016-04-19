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
using System.Linq;
using System.Globalization;

namespace SpeedwayClientWpf.ViewModels
{
    /// <summary>
    /// Encapsulates presentation logic and state for each reader in the application
    /// </summary>
    public class ReaderViewModel : ViewModelBase
    {
        private TcpClient _client;
        bool _connected;
        bool _connecting; 
        private readonly object _locker = new Object();
        private DateTime? _currentTime;
        private DateTime _timeToSet;
        // SSH connection information
        private ConnectionInfo _connectionInfo;
        // a timer to update current time 
        private Timer _timer;
        private int _messagesCounter;
        // a counter which is added to the network message
        int _counter;
        readonly char[] _delimiterChars = { ',' };

        //the connecion process has been started but not finished yet
        bool Connecting
        {
            get { return _connecting; }
            set
            {
                _connecting = value;
                OnPropertyChanged("ConnectButtonContent");
                App.Current.Dispatcher.Invoke(CommandManager.InvalidateRequerySuggested);
            }
        }

        #region Public properties
        /// <summary>
        /// Name of the reader
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// IpAddress
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Port
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Current time captured from the reader
        /// </summary>
        public DateTime? CurrentTime
        {
            get { return _currentTime; }
            set
            {
                _currentTime = value;
                OnPropertyChanged("CurrentTime");
                OnPropertyChanged("CurrentTimeForUser");
            }
        }

        /// <summary>
        /// Current time representation for user
        /// </summary>
        public string CurrentTimeForUser
        {
            get { return _currentTime.HasValue ? _currentTime.Value.ToString("HH:mm:ss") : default (string); }
        }

        /// <summary>
        /// Time we want to set inside the reader
        /// </summary>
        public DateTime TimeToSet
        {
            get { return _timeToSet; }
            set
            {
                _timeToSet = value;
                OnPropertyChanged("TimeToSet");
            }
        }

        /// <summary>
        /// Connection status
        /// </summary>
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
                App.Current.Dispatcher.Invoke(CommandManager.InvalidateRequerySuggested);
            }
        }
        // a control to encapsulate the UI and UI logic 
        public ReaderControl ReaderControl { get; set; }
        /// <summary>
        /// Button content changes depending on the connection state
        /// </summary>
        public string ConnectButtonContent
        {
            get
            {
                if (Connecting) return "Connecting";
                return Connected ? "Disconnect" : "Connect";
            }
        }

        public ICommand ConnectCommand { get; set; }
        public ICommand UpdateTimeCommand { get; set; }
        public ICommand SetTimeCommand { get; set; }

        #endregion
               
        // this dictionary holds tag reads. It is used to save tag reads, 
        // so that the tag will not report if read again before x seconds have elapsed.
        Dictionary<int, int> _tags = new Dictionary<int, int>();

        public ReaderViewModel()
        {
            ReaderControl = new ReaderControl { DataContext = this };
            ConnectCommand = new DelegateCommand(Connect,
                () => Connected || (!Connected && IsEndPointValid() && !Connecting));
            UpdateTimeCommand = new DelegateCommand(UpdateTime, () => Connected);
            SetTimeCommand = new DelegateCommand(SetTime, () => Connected);
            //Task.Factory.StartNew(CheckConnection);

            _timer = new Timer(o =>
            {
                if(!MainWindowViewModel.Instance.Settings.UpdateReadersTimeManually)
                    TimeToSet = DateTime.Now;

                if (Connected)
                {
                    if (TimeToSet.Second == 0)
                        UpdateTime();
                    else if(CurrentTime.HasValue)
                        CurrentTime = CurrentTime.Value.AddSeconds(1);
                }
                if (_messagesCounter > 0)
                {
                    PushMessage(Name + ": "+ _messagesCounter+" messages received");
                    _messagesCounter = 0;
                }

            }, null, 10000, 1000);

            TimeToSet = DateTime.Now;
        }
        
        #region SSH methods
        private void SetTime()
        {
            try
            {
                using (var sshclient = new SshClient(_connectionInfo))
                {
                    sshclient.Connect();
                    using (var command = sshclient.CreateCommand(
                        "config system time " + TimeToSet.ToString("yyyy.MM.dd-HH:mm:ss")))
                    {
                        command.Execute();
                    }
                    sshclient.Disconnect();
                }
                UpdateTime();
                PushMessage(string.Format("Time for {0} successfully set.", Name));
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
                    using (var command = sshclient.CreateCommand("show system summary"))
                    {
                        /*    var result = "Status = '0,Success' \r\n" +
                                "SysDesc = 'Speedway R220'\r\n" +
                                "SysContact = 'unknown'\r\n" +
                                "SysName = 'SpeedwayR-11-32-30'\r\n" +
                                "SysLocation = 'unknown'\r\n" +
                                "SysTime = '" + DateTime.Now.ToString("ddd MMM dd HH:mm:ss UTC yyyy", new CultureInfo("en-US")) + "'\r\n";
                                //Wed Mar 23 07:35:13 UTC 2016'\r\n";
                           */
                        var result = command.Execute();

                        var line = new List<string>(result.Split('\n')).
                            FirstOrDefault(s => s.ToLower().Contains("systime"));
                        var date = line.Split('=')[1].Trim('\r',' ', '\'');
                        DateTime dt;
                        if(!DateTime.TryParse(date, out dt))
                        {
                            CurrentTime = DateTime.ParseExact(date, "ddd MMM dd HH:mm:ss UTC yyyy", 
                                CultureInfo.InvariantCulture);
                            PushMessage(Name + ": current time refreshed.");
                        }
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

        #endregion

        #region private
        private bool IsEndPointValid()
        {
            if (string.IsNullOrEmpty(IpAddress) || string.IsNullOrEmpty(Port))
                return false;

            int port;
            // check if ipAddress is valid
            Match match = Regex.Match(IpAddress, 
                @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            return match.Success && int.TryParse(Port, out port);
        }

        // writing the output message to a file
        private void WriteToFile(string text)
        {
            var folder = MainWindowViewModel.Instance.Settings.FolderPath;
            if (string.IsNullOrEmpty(folder)) return;

            try
            {
                var fileName = Path.Combine(folder, IpAddress + ".txt");

                lock (_locker)
                {
                    using (var file = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(file, Encoding.Unicode))
                    {
                        writer.WriteLine(text);
                    }
                }
            }
            catch (Exception exception)
            {
                var error = string.Format("{0}: error writing to file. {1}", Name, exception.Message);
                PushMessage(error, LogMessageType.Error);
                Trace.TraceError(error + exception.StackTrace);
            }
        }

        private void Connect()
        {
            if (!Connected)
            {
                _client = new TcpClient();
                _client.BeginConnect(IPAddress.Parse(IpAddress), int.Parse(Port), ConnectedCallback, null);
                Connecting = true;
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
                Connecting = false;
                _client.EndConnect(ar);
            }
            catch (Exception exception)
            {
                var error = string.Format("ERROR in {0}. {1}", Name, exception.Message);
                PushMessage(error, LogMessageType.Error);
                Trace.TraceError(error + exception.StackTrace);
                return;
            }

            PushMessage(string.Format("{0} connected successfully to {1}", Name, _client.Client.RemoteEndPoint));
            Connected = true;
            _connectionInfo = new ConnectionInfo(IpAddress, 22, "root", 
                new PasswordAuthenticationMethod("root", "impinj"));

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
                        {
                            // NOTE: The main problem is here
                            //PushMessage(string.Format("{0} message received: {1}", Name, message));
                            Task.Factory.StartNew(() => ProcessMessage(message));
                            
                            if(MainWindowViewModel.Instance.Settings.PlaySoundForRead)
                                SoundHelper.PlayReadSound();
                            //Debug.WriteLine("Message read");
                            _messagesCounter++;
                        }
                    }
                    catch (Exception exception)
                    {
                        PushMessage(Name + " disconnected.");
                        Trace.TraceError(Name + " disconnected. " + exception.Message + exception.StackTrace);

                        Connected = false;
                        break;
                    }
                }
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                var settings = MainWindowViewModel.Instance.Settings;

                string[] parts = message.Split(_delimiterChars);
                string bibHex = parts[1]; // sometimes the EPC hex is too long. Get the last 7 of hex
                if (bibHex.Length > 7)
                {
                    bibHex = bibHex.Substring(bibHex.Length - 7);
                }
                int bib = Convert.ToInt32(bibHex, 16);

                int epochTimeToArray = Convert.ToInt32(parts[2].Substring(0, 10)); //extract epoch time
                
                lock (_tags)
                {
                    if ((!string.IsNullOrEmpty(settings.TagFilter) &&
                         (string.IsNullOrEmpty(settings.TagFilter) || !bib.ToString().Contains(settings.TagFilter))) ||
                        (_tags.ContainsKey(bib) &&
                         (!_tags.ContainsKey(bib) || _tags[bib] + settings.RereadTime > epochTimeToArray))) return;

                    _tags[bib] = epochTimeToArray;
                }

                int epochTimeUltra = epochTimeToArray - 312768000; // convert the time ??
                int milliEpochTimeToArray = Convert.ToInt32(parts[2].Substring(10, 3));
                // peel of the milliseconds

                string ettime = parts[2].Substring(0, 13);
                long etime = Convert.ToInt64(ettime);
                var epoch =
                    new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(etime)
                        .ToString(settings.AddDateToOutput ? "yyyy-MM-dd HH:mm:ss.fff" : "HH:mm:ss.fff");
                var readerId = IpAddress.Split('.')[3];

                string outputToFile = string.Format("{0},{1},0,\"{2}\"",
                    parts[0], bib, epoch);

                if (settings.AddReaderInfoToOutput)
                    outputToFile = string.Format("{0},{1},{2}", outputToFile, readerId, parts[0]);

                string outputToNetwork = string.Format(
                    "0,{0},{1},{2},{3},{4},0,0,{5},0000000000000000,0,{6}{7}",
                    bib, epochTimeUltra, milliEpochTimeToArray, parts[0], parts[3], readerId, _counter++,
                    Environment.NewLine);

                PushMessage(string.Format("{0} filtered data: {1}", Name, outputToFile), LogMessageType.Reader,
                    true);

                WriteToFile(outputToFile);
                MainWindowViewModel.Instance.ListenerViewModel.SendMessage(outputToNetwork);

                if (settings.PlaySoundForFilteredRead)
                    SoundHelper.PlayFilteredReadSound();

            }
            catch (Exception exception)
            {
                var error = string.Format("ERROR: message parsing error from {0}. {1}", Name, exception.Message);
                PushMessage(error, LogMessageType.Error);
                Trace.TraceError(error + exception.StackTrace);
            }
        }
        #endregion
                
        void PushMessage(string message)
        {
            PushMessage(message, LogMessageType.Reader);
        }
    }
}
