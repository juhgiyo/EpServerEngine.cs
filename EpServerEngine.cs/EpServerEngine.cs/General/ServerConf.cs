using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpServerEngine.cs
{
    public enum ConnectStatus
    {
        SUCCESS=0,
        FAIL_TIME_OUT,
        FAIL_ALREADY_CONNECTED,
        FAIL_SOCKET_ERROR
    }

    public enum StartStatus
    {
        SUCCESS = 0,
        FAIL_ALREADY_STARTED,
        FAIL_SOCKET_ERROR
    }
    /*
    /// Receive Status
    public enum ReceiveStatus : uint
    {
        /// Success
        SUCCESS = 0,
        /// Time-out
        FAIL_TIME_OUT,
        /// Not connected
        FAIL_NOT_CONNECTED,
        /// Connection closing
        FAIL_CONNECTION_CLOSING,
        /// Socket error
        FAIL_SOCKET_ERROR,
        /// Not supported
        FAIL_NOT_SUPPORTED,

    };
    */

    /// Send Status
    public enum SendStatus : uint
    {
        /// Success
        SUCCESS = 0,
        /// Time-out
        //FAIL_TIME_OUT,
        /// Socket error
        FAIL_SOCKET_ERROR,
        /// Not connected
        FAIL_NOT_CONNECTED,
        FAIL_INVALID_PACKET,
        FAIL_CONNECTION_CLOSING,

    };
    public class ServerConf
    {
	    public const String DEFAULT_HOSTNAME="localhost";
        public const String DEFAULT_PORT = "80808";
    }
}
