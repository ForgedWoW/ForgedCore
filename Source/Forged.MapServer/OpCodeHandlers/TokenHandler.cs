﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Token;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.OpCodeHandlers;

public class TokenHandler : IWorldSessionHandler
{
    [WorldPacketHandler(ClientOpcodes.CommerceTokenGetLog)]
    private void HandleCommerceTokenGetLog(CommerceTokenGetLog commerceTokenGetLog)
    {
        CommerceTokenGetLogResponse response = new()
        {
            // @todo: fix 6.x implementation
            UnkInt = commerceTokenGetLog.UnkInt,
            Result = TokenResult.Success
        };

        SendPacket(response);
    }

    [WorldPacketHandler(ClientOpcodes.CommerceTokenGetMarketPrice)]
    private void HandleCommerceTokenGetMarketPrice(CommerceTokenGetMarketPrice commerceTokenGetMarketPrice)
    {
        CommerceTokenGetMarketPriceResponse response = new()
        {
            // @todo: 6.x fix implementation
            CurrentMarketPrice = 300000000,
            UnkInt = commerceTokenGetMarketPrice.UnkInt,
            Result = TokenResult.Success
        };

        //packet.ReadUInt32("UnkInt32");

        SendPacket(response);
    }
}