﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionListOwnedItems : ClientPacket
{
    public ObjectGuid Auctioneer;
    public uint Offset;
    public Array<AuctionSortDef> Sorts = new(2);
    public AddOnInfo? TaintedBy;
    public AuctionListOwnedItems(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = WorldPacket.ReadPackedGuid();
        Offset = WorldPacket.ReadUInt32();

        if (WorldPacket.HasBit())
            TaintedBy = new AddOnInfo();

        var sortCount = WorldPacket.ReadBits<uint>(2);

        for (var i = 0; i < sortCount; ++i)
            Sorts[i] = new AuctionSortDef(WorldPacket);

        TaintedBy?.Read(WorldPacket);
    }
}