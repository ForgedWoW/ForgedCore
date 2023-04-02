﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Pet;

internal class PetLearnedSpells : ServerPacket
{
    public List<uint> Spells = new();
    public PetLearnedSpells() : base(ServerOpcodes.PetLearnedSpells, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Spells.Count);

        foreach (var spell in Spells)
            WorldPacket.WriteUInt32(spell);
    }
}