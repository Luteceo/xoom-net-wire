// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Wire.Fdx.Outbound;

public interface IManagedOutboundChannelProvider
{
    IReadOnlyDictionary<Id, IManagedOutboundChannel> AllOtherNodeChannels { get; }
        
    IManagedOutboundChannel ChannelFor(Node node);

    IReadOnlyDictionary<Id, IManagedOutboundChannel> ChannelsFor(IEnumerable<Node> nodes);

    void Close();

    void Close(Id id);
}