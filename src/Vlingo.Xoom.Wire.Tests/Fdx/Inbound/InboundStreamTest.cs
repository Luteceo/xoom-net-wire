// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using Vlingo.Xoom.Actors.TestKit;
using Vlingo.Xoom.Wire.Fdx.Inbound;
using Vlingo.Xoom.Wire.Nodes;
using Vlingo.Xoom.Wire.Tests.Channel;
using Xunit;
using Xunit.Abstractions;

namespace Vlingo.Xoom.Wire.Tests.Fdx.Inbound;

public class InboundStreamTest : IDisposable
{
    private readonly TestActor<IInboundStream> _inboundStream;
    private readonly MockInboundStreamInterest _interest;
    private readonly MockChannelReader _reader;
    private readonly TestWorld _world;

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void TestInbound(int happenings)
    {
        var counter = 0;
        var accessSafely = AccessSafely.AfterCompleting(happenings)
            .WritingWith<int>("count", value => counter += value)
            .ReadingWith("count", () => counter);
        _interest.TestResult.UntilStops = accessSafely;
        _interest.TestResult.Happenings = happenings;

        ProbeUntilConsumed(() => accessSafely.ReadFrom<int>("count") < 1, _reader);

        _inboundStream.Actor.Stop();

        _interest.TestResult.UntilStops.ReadFromExpecting("count", happenings);
            
        var count = 0;
        var tempArray = _interest.TestResult.Messages.ToArray();

        for (var i = 1; i < _interest.TestResult.Messages.Count + 1; i++)
        {
            if (tempArray.Contains($"{MockChannelReader.MessagePrefix}{i}"))
            {
                count++;
            }
        }
            
        Assert.True(_interest.TestResult.MessageCount.Get() > 0);
        Assert.Equal(count, _reader.ProbeChannelCount.Get());
    }

    public InboundStreamTest(ITestOutputHelper output)
    {
        var converter = new Converter(output);
        Console.SetOut(converter);

        _world = TestWorld.Start("test-inbound-stream");

        _interest = new MockInboundStreamInterest(output);

        _reader = new MockChannelReader();
            
        _inboundStream = _world.ActorFor<IInboundStream>(
            () => new InboundStreamActor(_interest, AddressType.Op, _reader, 10), "test-inbound");
    }

    public void Dispose()
    {
        _world.Terminate();
    }

    private void ProbeUntilConsumed(Func<bool> reading, MockChannelReader reader)
    {
        do
        {
            reader.ProbeChannel();
        } while (reading());
    }
}