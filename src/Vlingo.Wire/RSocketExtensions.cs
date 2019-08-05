// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Threading.Tasks;
using RSocket;

namespace Vlingo.Wire
{
    public static class RSocketExtensions
    {
        public static IAsyncResult BeginConnect(this RSocketServer server, AsyncCallback callback, object state)
        {
            return server.ConnectAsync().AsApm(callback, state);
        }

        public static void EndConnect(this RSocketServer server, IAsyncResult asyncResult)
        {
            ((Task)asyncResult).Wait();
        }
    }
}