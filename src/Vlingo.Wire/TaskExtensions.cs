// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;

namespace Vlingo.Wire
{
    public static class TaskExtensions
    {
        public static IAsyncResult AsApm(this Task task, AsyncCallback callback, object state)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");   
            }

            var tcs = new TaskCompletionSource<object>(state);
            task.ContinueWith(t => 
            {
                if (t.IsFaulted)
                {
                    if (t.Exception != null) tcs.TrySetException(t.Exception.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();   
                }
                else
                {
                    task.ContinueWith(precedent => tcs.TrySetResult(null)).Wait();   
                }

                callback?.Invoke(tcs.Task);
            }, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}