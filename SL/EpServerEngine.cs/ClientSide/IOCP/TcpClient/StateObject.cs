using System.Threading;

namespace System.Net.Sockets
{
    internal sealed class StateObject : IAsyncResult
    {
        AutoResetEvent _autoResetEvent;
        public int Size { get; set; }
        public AsyncCallback Callback { get; set; }
        public SocketAsyncEventArgs SocketAsyncEventArgs { get; set; }

        public object AsyncState
        {
            get;
            internal set;
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_autoResetEvent == null)
                {
                    _autoResetEvent = new AutoResetEvent(false);
                }

                return _autoResetEvent;
            }
        }

        public bool CompletedSynchronously { get { return false; } }
        public bool IsCompleted { get; internal set; }
    }
}
