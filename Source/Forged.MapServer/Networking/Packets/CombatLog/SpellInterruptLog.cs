﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellInterruptLog : ServerPacket
{
    public ObjectGuid Caster;
    public uint InterruptedSpellID;
    public uint SpellID;
    public ObjectGuid Victim;
    public SpellInterruptLog() : base(ServerOpcodes.SpellInterruptLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WritePackedGuid(Victim);
        WorldPacket.WriteUInt32(InterruptedSpellID);
        WorldPacket.WriteUInt32(SpellID);
    }
}