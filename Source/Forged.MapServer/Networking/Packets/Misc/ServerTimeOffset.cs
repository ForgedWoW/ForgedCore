﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class ServerTimeOffset : ServerPacket
{
    public long Time;
    public ServerTimeOffset() : base(ServerOpcodes.ServerTimeOffset) { }

    public override void Write()
    {
        WorldPacket.WriteInt64(Time);
    }
}