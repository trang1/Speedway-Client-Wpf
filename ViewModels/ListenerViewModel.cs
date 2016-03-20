using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpeedwayClientWpf.ViewModels
{
    public class ListenerViewModel : ViewModelBase
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

                return "23";
            }
            set { _port = value; }
        }
        public ListenerViewModel()
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
            PushMessage("Local address and port : " + localEp, LogMessageType.Listener);

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

                        PushMessage(string.Format("Connected {0} clients.", sockets.Count), LogMessageType.Listener);
                        PushMessage("Waiting for a connection...", LogMessageType.Listener);
                        _listener.BeginAcceptSocket(new AsyncCallback(AcceptCallback), _listener);
                        
                        MyEvent.WaitOne();
                    }
                    
                    foreach (var handler in sockets)
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }

                    _listener.Stop();
                    sockets.Clear();
                    PushMessage("Listener stopped.", LogMessageType.Listener);
                }
                catch (Exception e)
                {
                    PushMessage("ERROR: " + e.Message, LogMessageType.Error);
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
        public void AcceptCallback(IAsyncResult ar)
        {
            if(! IsListening) return;
            var listener = (TcpListener)ar.AsyncState;
            Socket handler = listener.EndAcceptSocket(ar);

            PushMessage("Client connected : " + handler.RemoteEndPoint, LogMessageType.Listener);
            
            sockets.ForEach(s => Send(s, string.Format("Client connected: {0} \r\n", handler.RemoteEndPoint)));
            CheckSockets();

            sockets.Add(handler);
            MyEvent.Set();
        }

        private void CheckSockets()
        {
            for (int i = sockets.Count - 1; i >= 0; i--)
            {
                if (!sockets[i].Connected)
                    sockets.RemoveAt(i);
            }
        }


        private void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            try
            {
                // Begin sending the data to the remote device.
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }
            catch (Exception e)
            {
                PushMessage("ERROR: " + e.Message, LogMessageType.Error);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                PushMessage(string.Format("Sent {0} bytes to client ({1}).", bytesSent, handler.RemoteEndPoint),
                    LogMessageType.Listener);
            }
            catch (Exception e)
            {
                PushMessage("ERROR: " + e.Message, LogMessageType.Error);
            }
        }

        public void SendMessage(string outputToNetwork)
        {
            CheckSockets();
            sockets.ForEach(s => Send(s, outputToNetwork));
        }
    }
}
