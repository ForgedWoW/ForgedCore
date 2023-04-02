﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Combat;

public class ThreatUpdate : ServerPacket
{
    public List<ThreatInfo> ThreatList = new();
    public ObjectGuid UnitGUID;
    public ThreatUpdate() : base(ServerOpcodes.ThreatUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(UnitGUID);
        WorldPacket.WriteInt32(ThreatList.Count);

        foreach (var threatInfo in ThreatList)
        {
            WorldPacket.WritePackedGuid(threatInfo.UnitGUID);
            WorldPacket.WriteInt64(threatInfo.Threat);
        }
    }
}