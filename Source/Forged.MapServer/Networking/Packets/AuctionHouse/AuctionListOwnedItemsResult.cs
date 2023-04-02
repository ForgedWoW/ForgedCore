﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public class AuctionListOwnedItemsResult : ServerPacket
{
    public uint DesiredDelay;
    public bool HasMoreResults;
    public List<AuctionItem> Items = new();
    public List<AuctionItem> SoldItems = new();
    public AuctionListOwnedItemsResult() : base(ServerOpcodes.AuctionListOwnedItemsResult) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Items.Count);
        WorldPacket.WriteInt32(SoldItems.Count);
        WorldPacket.WriteUInt32(DesiredDelay);
        WorldPacket.WriteBit(HasMoreResults);
        WorldPacket.FlushBits();

        foreach (var item in Items)
            item.Write(WorldPacket);

        foreach (var item in SoldItems)
            item.Write(WorldPacket);
    }
}