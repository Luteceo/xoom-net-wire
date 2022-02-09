// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Wire.Channel;
using Vlingo.Xoom.Wire.Fdx.Bidirectional;
using Vlingo.Xoom.Wire.Nodes;
using Xunit.Abstractions;

namespace Vlingo.Xoom.Wire.Tests.Fdx.Bidirectional
{
    public sealed class SocketRequestResponseChannelTest : BaseServerChannelTest
    {
        public SocketRequestResponseChannelTest(ITestOutputHelper output) : base(output, 100)
        {
        }

        protected override IClientRequestResponseChannel GetClient(IResponseChannelConsumer consumer, int maxBufferPoolSize,
            int maxMessageSize, int testPort, ILogger logger) =>
            new BasicClientRequestResponseChannel(Address.From(Host.Of("localhost"), testPort, AddressType.None), consumer, maxBufferPoolSize, maxMessageSize, logger);

        protected override IServerRequestResponseChannel GetServer(Stage stage, IRequestChannelConsumerProvider provider, string name,
            int testPort, int processorPoolSize, int maxBufferPoolSize, int maxMessageSize, long probeInterval, long probeTimeout) =>
            ServerRequestResponseChannelFactory.Start(
                stage,
                provider,
                testPort,
                "test-server",
                processorPoolSize,
                maxBufferPoolSize,
                maxMessageSize,
                probeInterval,
                probeTimeout);
    }
}