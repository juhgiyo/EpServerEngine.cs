/*! 
@file ServerConf.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief ServerConf Interface
@version 2.0

@section LICENSE

The MIT License (MIT)

Copyright (c) 2014 Woong Gyu La <juhgiyo@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

@section DESCRIPTION

A ServerConf Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpServerEngine.cs
{
    /// <summary>
    /// Connect status
    /// </summary>
    public enum ConnectStatus
    {
        /// <summary>
        /// Success
        /// </summary>
        SUCCESS=0,
        /// <summary>
        /// Failed by time-out
        /// </summary>
        FAIL_TIME_OUT,
        /// <summary>
        /// Failed due to connection already exists
        /// </summary>
        FAIL_ALREADY_CONNECTED,
        /// <summary>
        /// Failed due to unknown error
        /// </summary>
        FAIL_SOCKET_ERROR
    }

    /// <summary>
    /// Server start status
    /// </summary>
    public enum StartStatus
    {
        /// <summary>
        /// Success
        /// </summary>
        SUCCESS = 0,
        /// <summary>
        /// Failed due to server already started
        /// </summary>
        FAIL_ALREADY_STARTED,
        /// <summary>
        /// Failed due to socket error
        /// </summary>
        FAIL_SOCKET_ERROR
    }

    /// <summary>
    /// Send status
    /// </summary>
    public enum SendStatus : uint
    {
        /// <summary>
        /// Success
        /// </summary>
        SUCCESS = 0,
        /// <summary>
        /// Failed due to socket error
        /// </summary>
        FAIL_SOCKET_ERROR,
        /// <summary>
        /// Failed due to no connection exists
        /// </summary>
        FAIL_NOT_CONNECTED,
        /// <summary>
        /// Failed due to invalid packet
        /// </summary>
        FAIL_INVALID_PACKET,
        /// <summary>
        /// Failed due to connection closing
        /// </summary>
        FAIL_CONNECTION_CLOSING,

    };
    /// <summary>
    /// Server configuration class
    /// </summary>
    public class ServerConf
    {
        /// <summary>
        /// Default hostname (localhost)
        /// </summary>
	    public const String DEFAULT_HOSTNAME="localhost";
        /// <summary>
        /// Default port (80808)
        /// </summary>
        public const String DEFAULT_PORT = "80808";
    }
}
