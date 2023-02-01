// Copyright © 2012-2023 VLINGO LABS. All rights reserved.
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
using Vlingo.Xoom.Common;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Wire.Channel;

public class SocketChannelWriter
{
    private const int DefaultRetries = 10;
    private readonly int _id;
    private Socket? _channel;
    private readonly Address _address;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _connectAtOnce;
    private int _retries;
    private readonly AtomicBoolean _isConnected;

    public SocketChannelWriter(Address address, ILogger logger)
    {
        _id = new Random().Next(1, 1000);
        _isConnected = new AtomicBoolean(false);
        _address = address;
        _logger = logger;
        _channel = null;
        _connectAtOnce = new SemaphoreSlim(1);
        _retries = 0;
        _logger.Debug($"Creating socket ID={_id}");
    }

    public void Close()
    {
        if (IsClosed)
        {
            return;
        }
            
        if (_channel != null)
        {
            try
            {
                _logger.Info($"{this}: Closing socket...");
                _channel.Close();
            }
            catch (Exception e)
            {
                _logger.Error($"{this}: Channel close failed because: {e.Message}", e);
            }
            finally
            {
                _isConnected.Set(false);
                _connectAtOnce.Release();
            }
        }

        _channel = null;
    }

    public int Write(RawMessage message, MemoryStream buffer)
    {
        buffer.Clear();
        message.CopyBytesTo(buffer);
        buffer.Flip();
        return Write(buffer);
    }

    public int Write(MemoryStream buffer)
    {
        while (_channel == null && _retries < DefaultRetries)
        {
            PreparedChannel();
        }

        var totalBytesWritten = 0;
        if (_channel == null || !_isConnected.Get())
        {
            return totalBytesWritten;
        }

        try
        {
            while (buffer.HasRemaining())
            {
                var bytes = new byte[buffer.Length];
                buffer.Read(bytes, 0, bytes.Length);
                totalBytesWritten += bytes.Length;
                _logger.Debug($"{this}: Sending bytes [{bytes.Length}]");
                _channel?.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, _channel);
            }
        }
        catch (Exception e)
        {
            _logger.Error($"{this}: Write to channel failed because: {e.Message}", e);
            Close();
        }

        return totalBytesWritten;
    }

    public bool IsClosed => !_isConnected.Get();

    public bool IsBroken => _channel == null && _retries >= DefaultRetries;
        
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            var channel = ar.AsyncState as Socket;
            channel?.EndSend(ar);
            _logger.Debug($"{this}: Sent successfully");
        }
        catch (Exception e)
        {
            _logger.Error($"{this}: Failed to send to channel because: {e.Message}", e);
            Close();
        }
    }

    public override string ToString() => $"SocketChannelWriter[Id={_id}, address={_address}, channel={_channel}, IsClosed={IsClosed}, Retrying={_retries}, IsBroken={IsBroken}]";

    private void PreparedChannel()
    {
        try
        {
            if (!IsClosed && _channel != null)
            {
                if (_channel.Poll(10000, SelectMode.SelectWrite))
                {
                    _retries = 0;
                }
                    
                Close();
            }

            _connectAtOnce.Wait();
            _channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _channel.BeginConnect(_address.HostName, _address.Port, ConnectCallback, _channel);
            _retries = 0;
        }
        catch (Exception e)
        {
            _logger.Error($"{this}: Failed to prepare channel because: {e.Message}. Retrying: {_retries}", e);
            _connectAtOnce.Release();
            Close();
        }

        ++_retries;
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            var channel = ar.AsyncState as Socket;
            channel?.EndConnect(ar);
            _logger.Debug($"{this}: Socket successfully connected to remote endpoint {channel?.RemoteEndPoint}");
            _isConnected.Set(true);
            _connectAtOnce.Release();
        }
        catch (Exception e)
        {
            ++_retries;
            _logger.Error($"{this}: Failed to connect to channel because: {e.Message}", e);
            Close();
        }
    }
}