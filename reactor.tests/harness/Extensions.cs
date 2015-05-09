using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Reactor.Tests
{
    public static class Extensions {
        public static TaskAwaiter<object> GetAwaiter(this Reactor.Async.Future future) {
            var tcs = new TaskCompletionSource<object>(); 
            future.Then  (() => tcs.SetResult(null));
            future.Error (error => tcs.SetException(error));
            return tcs.Task.GetAwaiter();
        }
    }
}
