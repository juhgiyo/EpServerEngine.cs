[![Gratipay][gratipay-image]][gratipay-url]
EpServerEngine.cs
============
#### Visual C# IOCP TCP Server/Client Engine ####


DESCRIPTION
-----------

EpServerEngine.cs is a Visual C#+ software framework that supports the development of server/client application under a [MIT License](http://opensource.org/licenses/mit-license.php).
It handles all the initialize/usage Socket library, which is time consuming task. 
EpServerEngine.cs supports Visual C# 2012.
Source can be downloaded at [EpServerEngine.cs](http://github.com/juhgiyo/epserverengine.cs)


FEATURES
--------

* Easy to build server/client application.
  - Just implement Packet Structure and callback class for your server/client, 
       and EpServerEngine.cs will do rest for you.
  - No need to spend the time for handling thread synchronization.


What is in the EpServerEngine?
------------------------------

* General
  1. Packet
  2. Packet Serializer

* Client Side
  1. IOCP TCP
     * IOCP TCP Client

* Server Side
  1. IOCP TCP
     * IOCP TCP Server
     * IOCP TCP Socket

USAGE
-----

To find the usage examples, please see the [wiki page](https://github.com/juhgiyo/EpServerEngine.cs/wiki)


REFERENCE
---------
* [EpLibrary.cs](https://github.com/juhgiyo/EpLibrary.cs)

DONATION
---------
[![Gratipay][gratipay-image]][gratipay-url]

or donation by Pledgie  
<a href='https://pledgie.com/campaigns/27765'><img alt='Click here to lend your support to: EpServerEngine.cs and make a donation at pledgie.com !' src='https://pledgie.com/campaigns/27765.png?skin_name=chrome' border='0' ></a>

LICENSE
-------

[The MIT License](http://opensource.org/licenses/mit-license.php)

Copyright (c) 2014 Woong Gyu La <[juhgiyo@gmail.com](mailto:juhgiyo@gmail.com)>

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
[gratipay-image]: https://img.shields.io/gratipay/juhgiyo.svg?style=flat
[gratipay-url]: https://gratipay.com/juhgiyo/
