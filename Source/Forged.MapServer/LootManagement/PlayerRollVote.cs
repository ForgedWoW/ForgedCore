﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.LootManagement;

public class PlayerRollVote
{
    public RollVote Vote;
    public byte RollNumber;

    public PlayerRollVote()
    {
        Vote = RollVote.NotValid;
        RollNumber = 0;
    }
}