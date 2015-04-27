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

namespace Reactor.Domains
{
    public interface IDomain
    {
        void Stop();
    }

    public class Domain : MarshalByRefObject, IDomain
    {
        private AppDomain domain;

        private void Start (AppDomain domain, Reactor.Action callback)
        {
            this.domain = domain;

            Reactor.Loop.Start();

            Reactor.Loop.Post(callback);
        }

        public  void Stop  ()
        {
            Reactor.Loop.Stop();

            AppDomain.Unload(this.domain);
        }

        #region Statics

        public static IDomain Create (string name, Reactor.Action callback)
        {
            var setup = new AppDomainSetup()
            {
                ApplicationBase    = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,

                ConfigurationFile  = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,

                ApplicationName    = AppDomain.CurrentDomain.SetupInformation.ApplicationName,

                LoaderOptimization = LoaderOptimization.MultiDomainHost
            };

            var appdomain = AppDomain.CreateDomain(name, null, setup);

            var domain    = (Domain)appdomain.CreateInstanceAndUnwrap(

                typeof(Domain).Assembly.FullName, 

                typeof(Domain).FullName);
  
            domain.Start(appdomain, callback);

            return domain;
        }

        public static IDomain Create (Reactor.Action callback)
        {
            return Domain.Create(Guid.NewGuid().ToString(), callback);
        }

        #endregion
    }
}
