﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class SocketGems : ClientPacket
{
    public ObjectGuid[] GemItem = new ObjectGuid[ItemConst.MaxGemSockets];
    public ObjectGuid ItemGuid;
    public SocketGems(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ItemGuid = WorldPacket.ReadPackedGuid();

        for (var i = 0; i < ItemConst.MaxGemSockets; ++i)
            GemItem[i] = WorldPacket.ReadPackedGuid();
    }
}