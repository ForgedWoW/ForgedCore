﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DungeonFinding;

public class LfgReward
{
    public uint FirstQuest;
    public uint MaxLevel;
    public uint OtherQuest;

    public LfgReward(uint maxLevel = 0, uint firstQuest = 0, uint otherQuest = 0)
    {
        MaxLevel = maxLevel;
        FirstQuest = firstQuest;
        OtherQuest = otherQuest;
    }
}