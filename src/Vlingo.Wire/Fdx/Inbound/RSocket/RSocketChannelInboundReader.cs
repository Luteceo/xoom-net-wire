// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using RSocket;
using RSocket.Transports;
using Vlingo.Actors;
using Vlingo.Wire.Channel;

namespace Vlingo.Wire.Fdx.Inbound.RSocket
{
    public class RSocketChannelInboundReader : ChannelMessageDispatcher, IChannelReader, IDisposable
    {
        private readonly RSocketServer _channel;
        private readonly List<RSocketClient> _clientChannels;
        private readonly ILogger _logger;
        private IChannelReaderConsumer _consumer;
        private readonly int _maxMessageSize;
        private readonly string _name;
        private readonly int _port;
        private bool _closed;
        private bool _disposed;
        private readonly ManualResetEvent _acceptDone;
        
        public RSocketChannelInboundReader(int port, string name, int maxMessageSize, ILogger logger)
        {
            _port = port;
            _name = name;
            _maxMessageSize = maxMessageSize;
            _logger = logger;
            _channel = new RSocketServer(new SocketTransport($"tcp://+:{port}/"));
            _clientChannels = new List<RSocketClient>();
            _acceptDone = new ManualResetEvent(false);
        }
        
        public void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;

            try
            {
                _channel.Close();
                foreach (var clientChannel in _clientChannels.ToArray())
                {
                    clientChannel.Close();
                }
                Dispose(true);
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to close channel for: '{_name}'", e);
            }
        }

        public override string Name { get; }
        public void OpenFor(IChannelReaderConsumer consumer)
        {
            var server = new RSocketServer(new LoopbackTransport());
            
            throw new System.NotImplementedException();
        }

        public void ProbeChannel()
        {
            throw new System.NotImplementedException();
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
      
            if (disposing) 
            {
                Close();
            }
      
            _disposed = true;
        }
        
        public override IChannelReaderConsumer Consumer { get; }
        
        public override ILogger Logger { get; }
    }
}