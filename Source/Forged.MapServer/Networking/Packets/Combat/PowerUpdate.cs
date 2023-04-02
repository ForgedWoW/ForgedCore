﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Combat;

public class PowerUpdate : ServerPacket
{
    public ObjectGuid Guid;
    public List<PowerUpdatePower> Powers;

    public PowerUpdate() : base(ServerOpcodes.PowerUpdate)
    {
        Powers = new List<PowerUpdatePower>();
    }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Guid);
        WorldPacket.WriteInt32(Powers.Count);

        foreach (var power in Powers)
        {
            WorldPacket.WriteInt32(power.Power);
            WorldPacket.WriteUInt8(power.PowerType);
        }
    }
}