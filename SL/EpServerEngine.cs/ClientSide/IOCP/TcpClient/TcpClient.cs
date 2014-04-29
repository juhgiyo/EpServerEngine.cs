using System.Diagnostics;
using System.Threading;

namespace System.Net.Sockets
{
    public sealed class TcpClient : IDisposable
    {
        EndPoint _endpoint;
        bool _responsePending;
        AutoResetEvent _autoResetEvent;
        readonly NetworkStream _networkStream;

        public TcpClient()
            : this(AddressFamily.InterNetwork) { }


        public TcpClient(IPEndPoint endpoint)
            : this(AddressFamily.InterNetwork)
        {
            this.Connect(endpoint);
        }

        public TcpClient(string host, int port)
            : this(AddressFamily.InterNetwork)
        {
            var endpoint = new DnsEndPoint(host, port);
            this.Connect(endpoint);
        }

        public TcpClient(AddressFamily addressFamily)
        {
            _autoResetEvent = new AutoResetEvent(false);

            this.Client = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            _networkStream = new NetworkStream(this.Client);
        }

        public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback requestCallback, object userToken)
        {
            _endpoint = new IPEndPoint(address, port);
            return this.BeginConnect(requestCallback, userToken);
        }

        public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback requestCallback, object userToken)
        {
            throw new NotImplementedException();
        }

        public IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback, object userToken)
        {
            _endpoint = new DnsEndPoint(host, port);
            return this.BeginConnect(requestCallback, userToken);
        }

        IAsyncResult BeginConnect(AsyncCallback requestCallback, object userToken)
        {
            var stateObject = new StateObject()
            {
                AsyncState = userToken,
                Callback = requestCallback,
                IsCompleted = false
            };

            this.ConnectAsync(stateObject);
            return stateObject;
        }

        void OnConnectedAsync(object sender, SocketAsyncEventArgs e)
        {
            this.Continue();

            var stateObject = e.UserToken as StateObject;
            stateObject.IsCompleted = true;
            stateObject.Callback(stateObject);
        }

        void OnConnected(object sender, SocketAsyncEventArgs e)
        {
            this.Continue();
        }

        void ConnectAsync(StateObject stateObject)
        {
            var e = new SocketAsyncEventArgs();
            e.UserToken = stateObject;
            e.RemoteEndPoint = _endpoint;
            e.Completed += OnConnectedAsync;
            try
            {
                this.Client.ConnectAsync(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
            }
        }

        public void Connect(IPEndPoint endpoint)
        {
            this.Connect(endpoint);
        }

        public void Connect(IPAddress address, int port)
        {
            var endpoint = new IPEndPoint(address, port);
            this.Connect(endpoint);
        }

        public void Connect(IPAddress[] address, int port)
        {
            throw new NotImplementedException();
        }

        public void Connect(string host, int port)
        {
            var endpoint = new DnsEndPoint(host, port);
            this.Connect(endpoint);
        }

        void Connect(EndPoint endpoint)
        {
            _endpoint = endpoint;

            var e = new SocketAsyncEventArgs();
            e.RemoteEndPoint = _endpoint;
            e.Completed += OnConnected;

            try
            {
                this.Client.ConnectAsync(e);
                this.WaitOne();
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                this.Continue();
            }
        }

        public void EndConnect(IAsyncResult asyncResult)
        {
            if (!_responsePending)
            {
                this.WaitOne();
            }
        }

        void Continue()
        {
            _responsePending = false;
            _autoResetEvent.Set();
        }

        void WaitOne()
        {
            _autoResetEvent.WaitOne();
        }

        public NetworkStream GetStream()
        {
            return _networkStream;
        }

        public int Available { get { throw new NotSupportedException(); } }
        public Socket Client { get; set; }
        public bool Connected { get { return this.Client != null && this.Client.Connected; } }
        public bool Active { get { return this.Connected; } }
        public bool ExclusiveAddressUse { get { return false; } }
        public bool NoDelay
        {
            get { return Client.NoDelay; }
            set { Client.NoDelay=value; }
        }

        public void Dispose()
        {
            var stream = this.GetStream();
            stream.Dispose();

            try
            {
                this.Client.Shutdown(SocketShutdown.Both);
                this.Client.Close();
            }
            catch (ObjectDisposedException ex)
            {
                // no one cares at this point
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
            }
            catch (SocketException ex)
            {
                // no one cares at this point
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
            }
        }
    }
}