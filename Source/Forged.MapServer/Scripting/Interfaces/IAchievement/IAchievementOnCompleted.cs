﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Scripting.Interfaces.IAchievement;

public interface IAchievementOnCompleted : IScriptObject
{
	void OnCompleted(Player player, AchievementRecord achievement);
}