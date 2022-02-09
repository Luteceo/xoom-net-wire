// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Message;

namespace Vlingo.Xoom.Wire.Channel
{
    public class SocketChannelSelectionReader: SelectionReader
    {
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _syncRead;

        public SocketChannelSelectionReader(ChannelMessageDispatcher dispatcher, ILogger logger) : base(dispatcher)
        {
            _logger = logger;
            _syncRead = new SemaphoreSlim(1);
        }

        public override void Read(Socket channel, RawMessageBuilder builder)
        {
            _syncRead.Wait();
            var buffer = builder.WorkBuffer();
            var bytes = new byte[buffer.Length];
            var state = new StateObject(channel, buffer, bytes, builder);
            _logger.Debug($"SocketChannelSelectionReader receiving [{buffer.Length}]");
            channel.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, ReceiveCallback, state);
            Dispatcher.DispatchMessageFor(builder);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                var state = ar.AsyncState as StateObject;
                var client = state?.WorkSocket;
                var buffer = state?.Buffer;
                var bytes = state?.Bytes;
                var builder = state?.Builder;

                var bytesRead = client?.EndReceive(ar);

                if (bytesRead.HasValue && bytesRead.Value > 0 && state != null && bytes != null)
                {
                    _logger.Debug($"SocketChannelSelectionReader Writing to buffer: bytes length [{bytes.Length}], offset [{state.TotalRead}], bytes read [{bytesRead.Value}]");
                    buffer?.Write(bytes, state.TotalRead, bytesRead.Value);
                    state.TotalRead += bytesRead.Value;
                }

                var bytesRemain = client?.Available;
                if (bytesRemain > 0 && bytes != null)
                {
                    _logger.Debug($"SocketChannelSelectionReader receiving more [{bytes.Length}]");
                    client?.BeginReceive(bytes, 0, bytes.Length, SocketFlags.None, ReceiveCallback, state);
                }
                else
                {
                    if (bytesRead > 0)
                    {
                        _logger.Debug("SocketChannelSelectionReader received and dispatching");
                        Dispatcher.DispatchMessageFor(builder);
                    }

                    _syncRead.Release();
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error while receiving data", e);
                _syncRead.Release();
            }
        }
        
        private class StateObject
        {
            public StateObject(Socket workSocket, Stream buffer, byte[] bytes, RawMessageBuilder builder)
            {
                WorkSocket = workSocket;
                Buffer = buffer;
                Bytes = bytes;
                Builder = builder;
            }
            
            public Socket WorkSocket { get; }
            
            public Stream Buffer { get; }
            
            public byte[] Bytes { get; }
            
            public RawMessageBuilder Builder { get; }
            
            public int TotalRead { get; set; }
        }
    }
}