﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class ProcResist : ServerPacket
{
    public ObjectGuid Caster;
    public float? Needed;
    public float? Rolled;
    public uint SpellID;
    public ObjectGuid Target;
    public ProcResist() : base(ServerOpcodes.ProcResist) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WritePackedGuid(Target);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteBit(Rolled.HasValue);
        WorldPacket.WriteBit(Needed.HasValue);
        WorldPacket.FlushBits();

        if (Rolled.HasValue)
            WorldPacket.WriteFloat(Rolled.Value);

        if (Needed.HasValue)
            WorldPacket.WriteFloat(Needed.Value);
    }
}