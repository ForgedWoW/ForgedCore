﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionOutbidNotification : ServerPacket
{
    public ulong BidAmount;
    public AuctionBidderNotification Info;
    public ulong MinIncrement;

    public AuctionOutbidNotification() : base(ServerOpcodes.AuctionOutbidNotification) { }

    public override void Write()
    {
        Info.Write(WorldPacket);
        WorldPacket.WriteUInt64(BidAmount);
        WorldPacket.WriteUInt64(MinIncrement);
    }
}