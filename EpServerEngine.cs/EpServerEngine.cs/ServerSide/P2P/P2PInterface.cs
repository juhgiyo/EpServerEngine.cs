/*! 
@file P2PInterface.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief P2P Interface
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

A P2P Interface.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpServerEngine.cs
{
    /// <summary>
    /// P2P interface
    /// </summary>
    public interface IP2P
    {
        /// <summary>
        /// flag whether P2P is paired
        /// </summary>
        bool Paired
        {
            get;
        }
        /// <summary>
        /// callback object
        /// </summary>
        IP2PCallback CallBackObj
        {
            get;
            set;
        }
        /// <summary>
        /// Connect given two socket as p2p
        /// </summary>
        /// <param name="socket1">first socket</param>
        /// <param name="socket2">second socket</param>
        /// <param name="callback">callback object</param>
        /// <returns>true if paired otherwise false</returns>
        bool ConnectPair(INetworkSocket socket1, INetworkSocket socket2, IP2PCallback callback);
        /// <summary>
        /// Detach pair
        /// </summary>
        void DetachPair();
    }
    /// <summary>
    /// P2P callback interface
    /// </summary>
    public interface IP2PCallback
    {
        /// <summary>
        /// Called when p2p is detached
        /// </summary>
        /// <param name="p2p">p2p instance</param>
        /// <param name="socket1">first socket</param>
        /// <param name="socket2">second socket</param>
        void OnDetached(IP2P p2p, INetworkSocket socket1, INetworkSocket socket2);
    }
}
