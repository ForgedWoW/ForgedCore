﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Instance;

internal class InstanceEncounterGainCombatResurrectionCharge : ServerPacket
{
    public uint CombatResChargeRecovery;
    public int InCombatResCount;
    public InstanceEncounterGainCombatResurrectionCharge() : base(ServerOpcodes.InstanceEncounterGainCombatResurrectionCharge, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(InCombatResCount);
        WorldPacket.WriteUInt32(CombatResChargeRecovery);
    }
}