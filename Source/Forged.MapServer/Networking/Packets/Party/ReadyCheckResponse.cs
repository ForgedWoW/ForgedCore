﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class ReadyCheckResponse : ServerPacket
{
    public bool IsReady;
    public ObjectGuid PartyGUID;
    public ObjectGuid Player;
    public ReadyCheckResponse() : base(ServerOpcodes.ReadyCheckResponse) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(PartyGUID);
        WorldPacket.WritePackedGuid(Player);

        WorldPacket.WriteBit(IsReady);
        WorldPacket.FlushBits();
    }
}