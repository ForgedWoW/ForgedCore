﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class QuestUpdateComplete : ServerPacket
{
    public uint QuestID;
    public QuestUpdateComplete() : base(ServerOpcodes.QuestUpdateComplete) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(QuestID);
    }
}