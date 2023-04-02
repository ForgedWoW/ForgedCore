﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GetGarrisonInfoResult : ServerPacket
{
    public uint FactionIndex;
    public List<FollowerSoftCapInfo> FollowerSoftCaps = new();
    public List<GarrisonInfo> Garrisons = new();
    public GetGarrisonInfoResult() : base(ServerOpcodes.GetGarrisonInfoResult, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(FactionIndex);
        WorldPacket.WriteInt32(Garrisons.Count);
        WorldPacket.WriteInt32(FollowerSoftCaps.Count);

        foreach (var followerSoftCapInfo in FollowerSoftCaps)
            followerSoftCapInfo.Write(WorldPacket);

        foreach (var garrison in Garrisons)
            garrison.Write(WorldPacket);
    }
}