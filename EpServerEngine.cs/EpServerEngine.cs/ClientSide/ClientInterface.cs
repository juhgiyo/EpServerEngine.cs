using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EpServerEngine.cs
{
    public sealed class ClientOps{

		public ClientCallbackInterface callBackObj;

        public String hostName;

		public String port;

        public bool noDelay;

        public int waitTimeInMilliSec;

        public ClientOps()
		{
			callBackObj=null;
			hostName=ServerConf.DEFAULT_HOSTNAME;
			port=ServerConf.DEFAULT_PORT;
            noDelay = true;
            waitTimeInMilliSec = Timeout.Infinite;
		}

        public ClientOps(ClientCallbackInterface callBackObj, String hostName, String port, bool noDelay = true, int waitTimeInMilliSec = Timeout.Infinite)
        {
            this.callBackObj = callBackObj;
            this.hostName = hostName;
            this.port = port;
            this.noDelay = noDelay;
            this.waitTimeInMilliSec = waitTimeInMilliSec;
        }

		public static ClientOps defaultClientOps=new ClientOps();
	};

    public interface ClientInterface
    {
        String GetHostName();

        String GetPort();

        void Connect(ClientOps ops);

        void Disconnect();

        bool IsConnectionAlive();

        void Send(Packet packet);

        
    }

	public interface ClientCallbackInterface{
        void OnConnected(ClientInterface client, ConnectStatus status);

	    void OnReceived(ClientInterface client, Packet receivedPacket);

        void OnSent(ClientInterface client, SendStatus status);

        void OnDisconnect(ClientInterface client);
	};
}
