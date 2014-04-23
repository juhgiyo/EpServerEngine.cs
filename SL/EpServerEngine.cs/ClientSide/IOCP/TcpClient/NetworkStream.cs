using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    public class NetworkStream : Stream
    {
        Timer _timer;
        bool _isDisposed;
        bool _hasTimedOut;
        bool _dataTransferPending = true;
        readonly Socket _socket;
        readonly bool _ownsSocket;
        readonly FileAccess _access;
        readonly AutoResetEvent _autoEvent;

        public NetworkStream(Socket socket)
            : this(socket, FileAccess.ReadWrite, false) { }

        public NetworkStream(Socket socket, bool ownsSocket)
            : this(socket, FileAccess.ReadWrite, ownsSocket) { }

        public NetworkStream(Socket socket, FileAccess access)
            : this(socket, access, false) { }

        public NetworkStream(Socket socket, FileAccess access, bool ownsSocket)
        {
            _socket = socket;
            _access = access;
            _ownsSocket = ownsSocket;
            _autoEvent = new AutoResetEvent(false);
            _timer = new Timer(OnTimerElapsed, null, TimerIntervals.Never, TimerIntervals.Never);

            this.ReadTimeout = 10000;
            this.WriteTimeout = 5000;
        }

        public event EventHandler TimedOut;
        void NotifyTimedOut()
        {
            if (this.TimedOut != null)
            {
                this.TimedOut(this, EventArgs.Empty);
            }
        }

        void OnTimerElapsed(object stateToken)
        {
            _hasTimedOut = true;
            this.StopTimer();
            this.NotifyTimedOut();
        }

        void StartTimer(int timeoutInMilliseconds)
        {
            _timer.Change(timeoutInMilliseconds, Timeout.Infinite);
        }

        void StopTimer()
        {
            _timer.Change(TimerIntervals.Never, TimerIntervals.Never);
        }

        public override bool CanRead { get { return (_access & FileAccess.Read) == FileAccess.Read; } }
        public override bool CanWrite { get { return (_access & FileAccess.Write) == FileAccess.Write; } }
        public override int WriteTimeout { get; set; }
        public override int ReadTimeout { get; set; }
        public bool HasTimedOut { get { return _hasTimedOut; } }

        public Socket Socket { get { return _socket; } }

        void ValidateReadArguments(byte[] buffer, int offset, int size)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", "Buffer must not be null.");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Offset must not be smaller than 0.");
            }

            if (offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset", "Offset must not be larger than buffer size.");
            }

            if (size < 0)
            {
                throw new ArgumentOutOfRangeException("size", "Size must not be smaller than 0.");
            }

            var overflow = (buffer.Length - offset) < size;
            if (overflow)
            {
                throw new ArgumentOutOfRangeException("Size must not be greater than the difference between offset and length.");
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            this.Reset();
            this.ValidateReadArguments(buffer, offset, size);

            var stateObject = new StateObject()
            {
                AsyncState = state,
                IsCompleted = false,
                Callback = callback,
            };


            this.StartReceiving(buffer, offset, size, stateObject);
            return stateObject;
        }

        void StartReceiving(byte[] buffer, int offset, int size, StateObject stateObject)
        {
            var e = new SocketAsyncEventArgs();
            e.SetBuffer(buffer, offset, size);
            e.UserToken = stateObject;
            e.Completed += OnDataReceivedAsync;

            try
            {
                // start timeout timer
                this.StartTimer(this.ReadTimeout);
                this.Socket.ReceiveAsync(e);
            }
            catch (SocketException ex)
            {
                throw new IOException("Socket error.", ex);
            }
        }

        void Continue()
        {
            _dataTransferPending = false;
            _autoEvent.Set();
        }

        void WaitOne()
        {
            _autoEvent.WaitOne();
        }

        void OnDataReceivedAsync(object sender, SocketAsyncEventArgs e)
        {
            this.StopTimer();

            if (_hasTimedOut)
            {
                return;
            }

            this.Continue();

            var stateObject = e.UserToken as StateObject;
            stateObject.SocketAsyncEventArgs = e;
            stateObject.Callback(stateObject);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (_dataTransferPending)
            {
                this.WaitOne();
            }

            var stateObject = asyncResult as StateObject;
            var e = stateObject.SocketAsyncEventArgs;
            return e.BytesTransferred;
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            this.Reset();
            this.ValidateWriteArguments(buffer, offset, size);

            var stateObject = new StateObject()
            {
                AsyncState = state,
                IsCompleted = false,
                Callback = callback,
            };

            this.StartSending(buffer, offset, size, stateObject);
            return stateObject;
        }

        void StartSending(byte[] buffer, int offset, int size, StateObject stateObject)
        {
            var e = new SocketAsyncEventArgs();
            e.SetBuffer(buffer, offset, size);
            e.UserToken = stateObject;
            e.Completed += OnDataSendingAsync;

            try
            {
                this.StartTimer(this.WriteTimeout);
                this.Socket.SendAsync(e);
            }
            catch (SocketException ex)
            {
                throw new IOException("Socket error.", ex);
            }
        }

        void Reset()
        {
            _hasTimedOut = false;
            _dataTransferPending = true;
        }

        void OnDataSendingAsync(object sender, SocketAsyncEventArgs e)
        {
            this.StopTimer();

            if (_hasTimedOut)
            {
                return;
            }

            this.Continue();

            var stateObject = e.UserToken as StateObject;
            stateObject.SocketAsyncEventArgs = e;
            stateObject.Callback(stateObject);
        }

        void ValidateWriteArguments(byte[] buffer, int offset, int size)
        {
            // conditions are equal to read method
            this.ValidateReadArguments(buffer, offset, size);
        }

        new public int EndWrite(IAsyncResult asyncResult)
        {
            if (_dataTransferPending)
            {
                this.WaitOne();
            }
            var stateObject = asyncResult as StateObject;
            var e = stateObject.SocketAsyncEventArgs;
            return e.BytesTransferred;
        }

        public override int Read([InAttribute] [OutAttribute] byte[] buffer, int offset, int size)
        {
            this.ValidateReadArguments(buffer, offset, size);

            var e = new SocketAsyncEventArgs();
            e.SetBuffer(buffer, offset, size);
            e.Completed += OnDataReceivedSync;

            try
            {
                this.StartTimer(this.ReadTimeout);
                this.Socket.ReceiveAsync(e);
                this.WaitOne();
                this.StopTimer();
                return e.BytesTransferred;
            }
            catch (SocketException ex)
            {
                this.StopTimer();
                this.Continue();
                throw new IOException("Socket error.", ex);
            }
        }

        void OnDataReceivedSync(object sender, SocketAsyncEventArgs e)
        {
            this.Continue();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        void OnDataSendingSync(object sender, SocketAsyncEventArgs e)
        {
            this.Continue();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.ValidateWriteArguments(buffer, offset, count);

            var e = new SocketAsyncEventArgs();
            e.SetBuffer(buffer, offset, count);
            e.Completed += OnDataSendingSync;

            try
            {
                this.StartTimer(this.WriteTimeout);
                this.Socket.SendAsync(e);
                this.WaitOne();
                this.StopTimer();
            }
            catch (SocketException ex)
            {
                this.StopTimer();
                this.Continue();
                throw new IOException("Socket error.", ex);
            }
        }

        public new static bool Equals(object objA, object objB)
        {
            if (objA == null && objB == null)
            {
                return true;
            }

            return objA.Equals(objB);
        }

        public override void Flush()
        {
            // does nothing
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public void Close(int timeout)
        {
            if (timeout < -1)
            {
                throw new ArgumentOutOfRangeException("timeout", "Timeout must not be smaller than -1.");
            }

            throw new NotImplementedException();
        }

        public override void Close()
        {
            base.Close();
            if (_ownsSocket && _socket != null)
            {
                _socket.Close();
            }
        }

        public new void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            base.Dispose();
            this.Close();

            _isDisposed = true;
        }
    }
}