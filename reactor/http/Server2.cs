///*--------------------------------------------------------------------------

//Reactor

//The MIT License (MIT)

//Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

//---------------------------------------------------------------------------*/

//using System;

//namespace Reactor.Http
//{
//    public class Server2 : Reactor.IReadable2<Reactor.Http.Context>
//    {
//        public  Reactor.ReadState               ReadState { get; set; }

//        private Reactor.Net.HttpListener            listener;

//        private Reactor.Subject<Reactor.Http.Context> topic;

//        public Server2()
//        {
//            this.topic = new Subject<Reactor.Http.Context>();

//            this.ReadState = ReadState.Paused;
//        }

//        #region IReadable2

//        public IReadable2<Reactor.Http.Context> Read(Reactor.Action<Reactor.Http.Context> callback)
//        {
//            this.topic.Next(callback);

//            return this;
//        }

//        public void Error(Reactor.Action<Exception> callback)
//        {
//            this.topic.Error(callback);
//        }

//        public IReadable2<Reactor.Http.Context> End(Action callback)
//        {
//            this.topic.End(callback);

//            return this;
//        }

//        public IReadable2<Reactor.Http.Context> Pause()
//        {
//            this.ReadState = ReadState.Paused;

//            return this;
//        }

//        public IReadable2<Reactor.Http.Context> Resume()
//        {
//            if (this.ReadState == ReadState.Paused)
//            {
//                this.ReadState = ReadState.Reading;

//                this.Process();
//            }

//            return this;
//        }

//        public IReadable2<Reactor.Http.Context> Pipe(IWriteable2<Reactor.Http.Context> writeable)
//        {
//            throw new InvalidOperationException();
//        }

//        #endregion

//        public Server2 Listen(int port)
//        {
//            try
//            {
//                this.listener = new Reactor.Net.HttpListener();

//                this.listener.Prefixes.Add(string.Format("http://*:{0}/", port));

//                this.listener.Start();

//                this.Resume();
//            }
//            catch (Exception exception)
//            {
//                this.topic.Error(exception);
//            }

//            return this;
//        }

//        #region Process

//        private void Process()
//        {
//            var future = Reactor.IO.GetContext(this.listener);

//            future.Then(context =>
//            {
//                this.topic.Next(new Reactor.Http.Context(context));

//                if (this.ReadState == ReadState.Reading)
//                {
//                    this.Process();
//                }
//            });

//            future.Error(error =>
//            {
//                this.topic.Error(error);

//                this.topic.End();
//            });
//        }

//        #endregion

//        #region Statics

//        public static Server2 Create()
//        {
//            return new Server2();
//        }

//        #endregion
//    }
//}
