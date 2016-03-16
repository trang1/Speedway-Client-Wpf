using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpeedwayClientWpf
{
    public class Listener : INotifyPropertyChanged
    {
        private TcpListener _listener;
        public bool IsListening { get; set; }
        public ICommand StartListeningCommand { get; set; }
        public ICommand StopListeningCommand { get; set; }
        public string IpAddress
        {
            get
            {
                IPAddress a;
                if (IPAddress.TryParse(_ipAddress, out a))
                    return _ipAddress;

                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                return ipHostInfo.AddressList[2].ToString();
            }
            set
            {
                _ipAddress = value;
                OnPropertyChanged("IpAddress");
            }
        }

        public string Port
        {
            get
            {
                int i;
                if (int.TryParse(_port, out i))
                    return _port;

                return "11000";
            }
            set { _port = value; }
        }

        public Listener()
        {
            StartListeningCommand = new DelegateCommand(StartListening, () => !IsListening);
            StopListeningCommand = new DelegateCommand(StopListening, () => IsListening);
        }

        public static ManualResetEvent MyEvent = new ManualResetEvent(false);

        List<Socket> sockets = new List<Socket>();
        private string _ipAddress;
        private string _port;

        public void StartListening()
        {
            var localEp = new IPEndPoint(IPAddress.Parse(IpAddress), int.Parse(Port));
            PushMessage("Local address and port : " + localEp);

            _listener = new TcpListener(localEp);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    _listener.Start();
                    IsListening = true;

                    while (IsListening)
                    {
                        MyEvent.Reset();

                        PushMessage(string.Format("Connected {0} clients.", sockets.Count));
                        PushMessage("Waiting for a connection...");
                        _listener.BeginAcceptSocket(new AsyncCallback(acceptCallback), _listener);
                        
                        MyEvent.WaitOne();
                    }
                    
                    foreach (var handler in sockets)
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }

                    _listener.Stop();
                    sockets.Clear();
                    PushMessage("Listener stopped.");
                }
                catch (Exception e)
                {
                    PushMessage("ERROR: " + e.Message, 2);
                    IsListening = false;
                }
            }
                );
        }

        public void StopListening()
        {
            IsListening = false;
            MyEvent.Set();
        }
        public void acceptCallback(IAsyncResult ar)
        {
            if(! IsListening) return;
            var listener = (TcpListener)ar.AsyncState;
            Socket handler = listener.EndAcceptSocket(ar);

            PushMessage("Client connected : " + handler.RemoteEndPoint);
            sockets.ForEach(s => Send(s, "Client connected : " + handler.RemoteEndPoint));

            sockets.Add(handler);
            MyEvent.Set();
            // Additional code to read data goes here. 
        }


        private void Send(Socket handler, String data)
        {
            if (!handler.Connected)
            {
                sockets.Remove(handler);
                return;
            }

            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                PushMessage(string.Format("Sent {0} bytes to client ({1}).", bytesSent, handler.RemoteEndPoint));
            }
            catch (Exception e)
            {
                PushMessage("ERROR: " + e.Message, 2);
            }
        }
        private static void PushMessage(string text, int type = 1)
        {
            App.Current.Dispatcher.Invoke(() =>
                MainWindowViewModel.Instance.PushMessage(new LogMessage(type, text)));
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
