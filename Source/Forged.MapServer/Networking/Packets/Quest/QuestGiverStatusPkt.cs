﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestGiverStatusPkt : ServerPacket
{
    public QuestGiverInfo QuestGiver;

    public QuestGiverStatusPkt() : base(ServerOpcodes.QuestGiverStatus, ConnectionType.Instance)
    {
        QuestGiver = new QuestGiverInfo();
    }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(QuestGiver.Guid);
        WorldPacket.WriteUInt32((uint)QuestGiver.Status);
    }
}