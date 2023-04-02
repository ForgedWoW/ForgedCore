﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellDelayed : ServerPacket
{
    public int ActualDelay;
    public ObjectGuid Caster;
    public SpellDelayed() : base(ServerOpcodes.SpellDelayed, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WriteInt32(ActualDelay);
    }
}