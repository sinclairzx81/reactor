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
using System.Net;

namespace Reactor.Net
{
    public static class Dns
    {
        public static void GetHostAddresses(string hostname, Action<Exception, IPAddress[]> OnGetHostAddresses)
        {
            System.Net.Dns.BeginGetHostAddresses(hostname, (result) => {

                try
                {
                    var addresses = System.Net.Dns.EndGetHostAddresses(result);

                    Loop.Post(() =>
                    {
                        OnGetHostAddresses(null, addresses);
                    });
                }
                catch (Exception exception)
                {
                    Loop.Post(() =>
                    {
                        OnGetHostAddresses(exception, null);
                    });
                }

            }, null);
        }
        
        public static void GetHostEntry(string host, Action<Exception, IPHostEntry> OnIPHostEntry)
        {
            System.Net.Dns.BeginGetHostEntry(host, (result) =>
            {
                try
                {
                    var iphostentry = System.Net.Dns.EndGetHostEntry(result);

                    Loop.Post(() =>
                    {
                        OnIPHostEntry(null, iphostentry);
                    });
                }
                catch (Exception exception)
                {
                    Loop.Post(() =>
                    {
                        OnIPHostEntry(exception, null);
                    });
                }

            }, null);
  
        }
    }
}
