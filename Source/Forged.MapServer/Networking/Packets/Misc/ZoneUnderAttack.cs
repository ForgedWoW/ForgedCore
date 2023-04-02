﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class ZoneUnderAttack : ServerPacket
{
    public int AreaID;
    public ZoneUnderAttack() : base(ServerOpcodes.ZoneUnderAttack, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(AreaID);
    }
}