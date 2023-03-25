﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Achievements;

public class CompletedAchievementData
{
	public long Date;
	public List<ObjectGuid> CompletingPlayers = new();
	public bool Changed;
}