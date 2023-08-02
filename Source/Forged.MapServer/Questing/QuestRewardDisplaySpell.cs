﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Questing;

public struct QuestRewardDisplaySpell
{
    public uint PlayerConditionId;
    public uint SpellId;

    public QuestRewardDisplaySpell(uint spellId, uint playerConditionId)
    {
        SpellId = spellId;
        PlayerConditionId = playerConditionId;
    }
}