﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

internal class ReadyCheckResponseClient : ClientPacket
{
    public bool IsReady;
    public byte PartyIndex;
    public ReadyCheckResponseClient(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = WorldPacket.ReadUInt8();
        IsReady = WorldPacket.HasBit();
    }
}