using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpeedwayClientWpf.ViewModels
{
    /// <summary>
    /// Represents our server to broadcast tags
    /// </summary>
    public class ListenerViewModel : ViewModelBase
    {
        public static ManualResetEvent MyEvent = new ManualResetEvent(false);

        // List of connected clients
        private readonly List<Socket> _sockets = new List<Socket>();

        private string _ipAddress;
        private TcpListener _listener;
        private string _port;

        public ListenerViewModel()
        {
            StartListeningCommand = new DelegateCommand(StartListening, () => !IsListening);
            StopListeningCommand = new DelegateCommand(StopListening, () => IsListening);
        }

        public bool IsListening { get; set; }
        public ICommand StartListeningCommand { get; set; }
        public ICommand StopListeningCommand { get; set; }

        /// <summary>
        /// IP Address which user selects from the combobox
        /// </summary>
        public string IpAddress
        {
            get
            {
                IPAddress a;
                if (IPAddress.TryParse(_ipAddress, out a))
                    return _ipAddress;

                return "127.0.0.1";
            }
            set
            {
                _ipAddress = value;
                OnPropertyChanged("IpAddress");
            }
        }

        /// <summary>
        /// Port to listen
        /// </summary>
        public string Port
        {
            get
            {
                int i;
                if (int.TryParse(_port, out i))
                    return _port;
                // default value = 23
                return "23";
            }
            set { _port = value; }
        }

        /// <summary>
        /// List of IP Addresses of current computer
        /// </summary>
        public IList<string> Ips
        {
            get
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                return ipHostInfo.AddressList.Where(i => !i.IsIPv6LinkLocal).Select(i => i.ToString()).ToList();
            }
        }

        public void StartListening()
        {
            var localEp = new IPEndPoint(IPAddress.Parse(IpAddress), int.Parse(Port));
            
            _listener = new TcpListener(localEp);

            // start new thread listening for connections
            Task.Factory.StartNew(() =>
            {
                try
                {
                    PushMessage("Local address and port : " + localEp);
                    _listener.Start();
                    IsListening = true;

                    while (IsListening)
                    {
                        MyEvent.Reset();

                        PushMessage(string.Format("Connected {0} clients.", _sockets.Count));
                        PushMessage("Waiting for a connection...");
                        
                        _listener.BeginAcceptSocket(AcceptCallback, _listener);

                        // Waiting for Set()
                        MyEvent.WaitOne();
                    }

                    // disconnecting all clients
                    foreach (Socket handler in _sockets)
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }

                    _listener.Stop();
                    _sockets.Clear();
                    PushMessage("Listener stopped.");
                }
                catch (Exception e)
                {
                    PushMessage("ERROR: " + e.Message, LogMessageType.Error);
                    Trace.TraceError("Error listening clients. " + e.Message + e.StackTrace);
                    IsListening = false;
                }
            }
                );
        }

        public void StopListening()
        {
            IsListening = false;
            // proceed to disconnect
            MyEvent.Set();
        }

        /// <summary>
        /// Sends tag messages to connected clients
        /// </summary>
        /// <param name="outputToNetwork"></param>
        public void SendMessage(string outputToNetwork)
        {
            CheckSockets();
            _sockets.ForEach(s => Send(s, outputToNetwork));
        }

        #region private
        private void AcceptCallback(IAsyncResult ar)
        {
            if (! IsListening) return;
            try
            {
                // Retrieve the socket from the state object
                var listener = (TcpListener) ar.AsyncState;

                // Complete accepting the incoming connection
                Socket handler = listener.EndAcceptSocket(ar);

                PushMessage("Client connected : " + handler.RemoteEndPoint);

                // Notify all clients about new connection accepted
                _sockets.ForEach(s => Send(s, string.Format("Client connected: {0} \r\n", handler.RemoteEndPoint)));
                CheckSockets();

                _sockets.Add(handler);
                
                // Proceed to listening
                MyEvent.Set();
            }
            catch (Exception exception)
            {
                PushMessage("ERROR accepting connection. " + exception.Message, LogMessageType.Error);
                Trace.TraceError("ERROR accepting connection. " + exception.Message + exception.StackTrace);
            }
        }

        /// <summary>
        /// Check if connection is available
        /// </summary>
        private void CheckSockets()
        {
            for (int i = _sockets.Count - 1; i >= 0; i--)
            {
                if (!_sockets[i].Connected)
                    _sockets.RemoveAt(i);
            }
        }
        
        private void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            try
            {
                // Begin sending the data to the remote device
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    SendCallback, handler);
            }
            catch (Exception e)
            {
                PushMessage("ERROR sending data. " + e.Message, LogMessageType.Error);
                Trace.TraceError("ERROR sending data. " + e.Message + e.StackTrace);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object
                var handler = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device
                int bytesSent = handler.EndSend(ar);
                PushMessage(string.Format("Sent {0} bytes to client ({1}).", bytesSent, handler.RemoteEndPoint));
            }
            catch (Exception e)
            {
                PushMessage("ERROR sending data. " + e.Message, LogMessageType.Error);
                Trace.TraceError("ERROR sending data. " + e.Message + e.StackTrace);
            }
        }

        private void PushMessage(string text)
        {
            base.PushMessage(text, LogMessageType.Listener);
        }
        #endregion
    }
}