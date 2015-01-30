/*--------------------------------------------------------------------------

Reactor

The MIT License (MIT)

Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

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

---------------------------------------------------------------------------*/

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Reactor
{
    internal static class IO
    {
        #region Stream

        public static void Read(Stream stream, byte [] readbuffer, Action<Exception, int> callback)
        {
            try
            {
                stream.BeginRead(readbuffer, 0, readbuffer.Length, (result) =>
                {

                    try
                    {
                        int read = stream.EndRead(result);

                        Loop.Post(() => {

                            callback(null, read);
                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() => {

                            callback(exception, 0);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception, 0);
            }
        }

        public static void Write(Stream stream, byte [] writebuffer, Action<Exception> callback)
        {
            try
            {
                stream.BeginWrite(writebuffer, 0, writebuffer.Length, (result) =>
                {
                    try
                    {
                        stream.EndWrite(result);

                        Loop.Post(() =>
                        {
                            callback(null);
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception);
            }
        }

        #endregion

        #region SslStream

        public static void AuthenticateAsClient(SslStream stream, string targetHost, Action<Exception> callback)
        {
            try
            {
                stream.BeginAuthenticateAsClient(targetHost, (result) => 
                {
                    try
                    {
                        stream.EndAuthenticateAsClient(result);

                        Loop.Post(() =>
                        {
                            callback(null);
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception);
                        });
                    }

                }, null);
            }
            catch (Exception exception)
            {
                callback(exception);
            }
        }

        public static void AuthenticateAsServer(SslStream stream, X509Certificate2 certificate, Action<Exception> callback)
        {
            try
            {
                stream.BeginAuthenticateAsServer(certificate, (result) =>
                {
                    try
                    {
                        stream.EndAuthenticateAsServer(result);

                        Loop.Post(() =>
                        {
                            callback(null);
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception);
                        });
                    }

                }, null);
            }
            catch (Exception exception)
            {
                callback(exception);
            }
        }

        #endregion

        #region NetworkStream

        public static void Read(NetworkStream stream, byte[] readbuffer, Action<Exception, int> callback)
        {
            try
            {
                stream.BeginRead(readbuffer, 0, readbuffer.Length, (result) =>
                {
                    try
                    {
                        int read = stream.EndRead(result);

                        Loop.Post(() =>
                        {
                            callback(null, read);
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, 0);
                        });
                    }

                }, null);
            }
            catch (Exception exception)
            {
                callback(exception, 0);
            }
        }

        public static void Write(NetworkStream stream, byte[] writebuffer, Action<Exception> callback)
        {
            try
            {
                stream.BeginWrite(writebuffer, 0, writebuffer.Length, (result) =>
                {
                    try
                    {
                        stream.EndWrite(result);

                        Loop.Post(() =>
                        {
                            callback(null);
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception);
                        });
                    }

                }, null);
            }
            catch (Exception exception)
            {
                callback(exception);
            }
        }

        #endregion

        #region Socket

        public static void Connect     (Socket socket, IPAddress address, int port, Action<Exception> callback)
        {
            try
            {
                socket.BeginConnect(address, port, (result) =>
                {
                    try
                    {
                        socket.EndConnect(result);

                        Loop.Post(() =>
                        {
                            callback(null);
                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception);
            }
        }

        public static void Resolve     (Socket socket, string hostname, Action<Exception, IPAddress[]> callback)
        {
            if(hostname == "localhost")
            {
                hostname = "127.0.0.1";
            }

            try
            {
                System.Net.Dns.BeginGetHostAddresses(hostname, (result) =>
                {
                    try
                    {
                        var addresses = System.Net.Dns.EndGetHostAddresses(result);

                        Loop.Post(() =>
                        {
                            callback(null, addresses);
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, null);
                        });
                    }

                }, null);     
            }
            catch(Exception exception)
            {
                callback(exception, null);
            }
        }

        public static void Accept      (Socket socket, Action<Exception> callback)
        {
            try
            {
                socket.BeginAccept((result) => {
                
                    try
                    {
                        socket.EndAccept(result);

                        Loop.Post(() =>
                        {
                            callback(null);
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception);
                        });
                    }

                }, null);
            }
            catch (Exception exception)
            {
                callback(exception);
            }
        }

        public static void Disconnect  (Socket socket, bool resuseSocket, Action<Exception> callback)
        {
            try
            {
                socket.BeginDisconnect(resuseSocket, (result) =>
                {
                    try
                    {
                        socket.EndDisconnect(result);

                        Loop.Post(() =>
                        {
                            callback(null);
                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception);
            }
        }

        public static void Receive     (Socket socket, byte [] receivebuffer, Action<Exception, int> callback)
        {
            try
            {
                SocketError socketError;

                socket.BeginReceive(receivebuffer, 0, receivebuffer.Length, SocketFlags.None, out socketError, result =>
                {
                    try
                    {
                        int read = socket.EndReceive(result, out socketError);

                        Loop.Post(() =>
                        {
                            callback(null, read);
                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, 0);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception, 0);
            }
        }
        
        public static void ReceiveFrom (Socket socket, byte [] receivebuffer, Action<Exception, EndPoint, int> callback)
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                socket.BeginReceiveFrom(receivebuffer, 0, receivebuffer.Length, SocketFlags.None, ref remoteEP, (result) =>
                {
                    try
                    {
                        int read = socket.EndReceiveFrom(result, ref remoteEP);

                        Loop.Post(() =>
                        {
                            callback(null, remoteEP, read);
                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, null, 0);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception, null, 0);
            }
        }

        public static void SendTo      (Socket socket, byte [] sendbuffer, int offset, int count, EndPoint endpoint, Action<Exception, int> callback)
        {
            try
            {
                socket.BeginSendTo(sendbuffer, offset, count, SocketFlags.None, endpoint, result =>
                {
                    try
                    {
                        int sent = socket.EndSendTo(result);

                        Loop.Post(() =>
                        {
                            callback(null, sent);
                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, 0);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception, 0);
            }
        }

        #endregion

        #region TcpListener

        public static void AcceptSocket(TcpListener listener, Action<Exception, Socket> callback)
        {
            try
            {
                listener.BeginAcceptSocket((result) =>
                {
                    try
                    {
                        var socket = listener.EndAcceptSocket(result);

                        Loop.Post(() =>
                        {
                            callback(null, socket);
                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, null);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception, null);
            }
        }

        public static void AcceptTcpClient(TcpListener listener, Action<Exception, TcpClient> callback)
        {
            try
            {
                listener.BeginAcceptTcpClient((result) =>
                {
                    try
                    {
                        var client = listener.EndAcceptTcpClient(result);

                        Loop.Post(() =>
                        {
                            callback(null, client);
                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, null);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception, null);
            }
        }

        #endregion

        #region HttpWebRequest

        public static void GetRequestStream(HttpWebRequest request, Action<Exception, Stream> callback)
        {
            try
            {
                request.BeginGetRequestStream((result) =>
                {
                    try
                    {
                        var stream = request.EndGetRequestStream(result);

                        Loop.Post(() =>
                        {
                            callback(null, stream);
                        });
                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, null);
                        });
                    }

                }, null);
            }
            catch (Exception exception)
            {
                
                callback(exception, null);
            }
        }

        public static void GetResponse(HttpWebRequest request, Action<Exception, HttpWebResponse> callback)
        {
            try
            {
                request.BeginGetResponse((result) =>
                {
                    try
                    {
                        var response = request.EndGetResponse(result);

                        Loop.Post(() =>
                        {
                            callback(null, response as HttpWebResponse);
                        });
                    }
                    catch (WebException webexception)
                    {
                        Loop.Post(() => {

                            callback(null, webexception.Response as HttpWebResponse);

                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, null);
                        });                        
                    }

                }, null);
            }
            catch (Exception exception)
            {
                callback(exception, null);
            }            
        }

        #endregion

        #region HttpListener

        public static void GetContext(System.Net.HttpListener listener, Action<Exception, System.Net.HttpListenerContext> callback)
        {
            try
            {
                listener.BeginGetContext((result) =>
                {
                    try
                    {
                        var context = listener.EndGetContext(result);

                        Loop.Post(() =>
                        {
                            callback(null, context);
                        });

                    }
                    catch (Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, null);
                        });
                    }

                }, null);
            }
            catch (Exception exception)
            {
                callback(exception, null);
            }
        }

        public static void GetContext(Reactor.Net.HttpListener listener, Action<Exception, Reactor.Net.HttpListenerContext> callback)
        {
            try
            {
                listener.BeginGetContext((result) =>
                {
                    try
                    {
                        var context = listener.EndGetContext(result);
                        
                        Loop.Post(() =>
                        {
                            callback(null, context);
                        });

                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, null);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception, null);
            }
        }

        #endregion

        #region HttpListenerRequest

        public static void GetClientCertificate(HttpListenerRequest request, Action<Exception, X509Certificate2> callback)
        {
            try
            {
                request.BeginGetClientCertificate((result) =>
                {
                    try
                    {
                        var certificate = request.EndGetClientCertificate(result);

                        Loop.Post(() =>
                        {
                            callback(null, certificate);
                        });
                    }
                    catch(Exception exception)
                    {
                        Loop.Post(() =>
                        {
                            callback(exception, null);
                        });
                    }

                }, null);
            }
            catch(Exception exception)
            {
                callback(exception, null);
            }
        }

        #endregion
    }
}