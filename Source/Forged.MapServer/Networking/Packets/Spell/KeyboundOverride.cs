﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Spell;

internal class KeyboundOverride : ClientPacket
{
    public ushort OverrideID;
    public KeyboundOverride(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        OverrideID = WorldPacket.ReadUInt16();
    }
}