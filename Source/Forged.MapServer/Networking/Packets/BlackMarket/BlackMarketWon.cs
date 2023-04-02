﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BlackMarket;

internal class BlackMarketWon : ServerPacket
{
    public ItemInstance Item;
    public uint MarketID;
    public int RandomPropertiesID;
    public BlackMarketWon() : base(ServerOpcodes.BlackMarketWon) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(MarketID);
        WorldPacket.WriteInt32(RandomPropertiesID);
        Item.Write(WorldPacket);
    }
}