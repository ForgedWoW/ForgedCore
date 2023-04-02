﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class UiMapQuestLinesResponse : ServerPacket
{
    public List<uint> QuestLineXQuestIDs = new();
    public int UiMapID;
    public UiMapQuestLinesResponse() : base(ServerOpcodes.UiMapQuestLinesResponse) { }

    public override void Write()
    {
        WorldPacket.Write(UiMapID);
        WorldPacket.WriteUInt32((uint)QuestLineXQuestIDs.Count);

        foreach (var item in QuestLineXQuestIDs)
            WorldPacket.Write(item);
    }
}