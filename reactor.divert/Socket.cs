/*--------------------------------------------------------------------------

Reactor.Divert

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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace Reactor.Divert
{
    public class Socket : Reactor.Divert.ISocket
    {
        private const uint                       buffersize = 65536;

        private string                           filter;

        private IntPtr                           handle;

        private Thread                           thread;

        private IntPtr                           readbuffer;

        private IntPtr                           writebuffer;

        private WINDIVERT_ADDRESS                addr;

        private bool                             started;

        private Reactor.Action<byte[]>           onread;

        private Reactor.Action<System.Exception> onerror;

        public Socket         (string filter)
        {
            this.filter       = filter;

            this.handle       = IntPtr.Zero;

            this.started      = false;

            this.readbuffer   = Marshal.AllocHGlobal((int)buffersize); // clean these up

            this.writebuffer  = Marshal.AllocHGlobal((int)buffersize);

            this.addr         = default(WINDIVERT_ADDRESS);

            this.onread       = data  => this.Write(data);

            this.onerror      = error => { };

            this.handle       = WinDivert.WinDivertOpen(this.filter, WINDIVERT_LAYER.WINDIVERT_LAYER_NETWORK, 0, 0);

            if (handle      == new IntPtr(-1))
            {
                var exception = new Win32Exception(Marshal.GetLastWin32Error());

                throw exception;
            }

            this.started = true;

            this.thread  = new Thread(this.Runtime);

            this.thread.Start();
        }

        public void     Read  (Reactor.Action<byte[]> callback)
        {
            this.onread = callback;
        }

        public void     Error (Reactor.Action<System.Exception> callback)
        {
            this.onerror = callback;
        }

        public void     Write (byte [] data)
        {
            Marshal.Copy(data, 0, this.writebuffer, data.Length);

            uint send = 0;

            if (!WinDivert.WinDivertSend(this.handle, this.writebuffer, (uint)data.Length, ref addr, out send))
            {
                var exception = new Win32Exception(Marshal.GetLastWin32Error());

                this.onerror(exception);

                this.started = false;

                return;
            }
        }

        public void     End   ()
        {
            this.started = false;

            WinDivert.WinDivertClose(this.handle);
        }

        #region Runtime

        private byte[] ReadInternal()
        {
            uint read = 0;

            if (!WinDivert.WinDivertRecv(this.handle, this.readbuffer, buffersize, out addr, out read))
            {
                var error = new Win32Exception(Marshal.GetLastWin32Error());

                this.onerror(error);

                return null;
            }

            var data = new byte[read];

            Marshal.Copy(this.readbuffer, data, 0, (int)read);

            return data;
        }

        private void Runtime ()
        {
            this.started = true;

            while (this.started)
            {
                var data = this.ReadInternal();
                
                if(data != null)
                {
                    Reactor.Loop.Post(() => this.onread(data));
                }
            }

            WinDivert.WinDivertClose(this.handle);
        }

        #endregion

        #region Statics

        public static Reactor.Divert.Socket Create(string filter)
        {
            return new Reactor.Divert.Socket(filter);
        }

        public static Reactor.Divert.Socket Create()
        {
            var filter = "(inbound or outbound)";

            return new Reactor.Divert.Socket(filter);
        }

        #endregion
    }
}