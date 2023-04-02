﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootUnit : ClientPacket
{
    public bool IsSoftInteract;
    public ObjectGuid Unit;
    public LootUnit(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Unit = WorldPacket.ReadPackedGuid();
        IsSoftInteract = WorldPacket.HasBit();
    }
}

//Structs