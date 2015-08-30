using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor.Examples {

    // Turn Reactor.Future into a awaitable.
    //public static class Extensions {
    //    public static TaskAwaiter<object> GetAwaiter(this Reactor.Future future) {
    //        var tcs = new TaskCompletionSource<object>(); 
    //        future.Then  (() => tcs.SetResult(null));
    //        future.Error (error => tcs.SetException(error));
    //        return tcs.Task.GetAwaiter();
    //    }
    //    public static TaskAwaiter<T> GetAwaiter<T>(this Reactor.Future<T> future) {
    //        var tcs = new TaskCompletionSource<T>(); 
    //        future.Then  (result => tcs.SetResult(result));
    //        future.Error (error => tcs.SetException(error));
    //        return tcs.Task.GetAwaiter();
    //    }
    //}
  
}
