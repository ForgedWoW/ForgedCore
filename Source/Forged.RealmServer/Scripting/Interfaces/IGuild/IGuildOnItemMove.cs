﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Forged.RealmServer.Guilds;
using Game.Common.Entities.Items;
using Game.Common.Entities.Players;

namespace Forged.RealmServer.Scripting.Interfaces.IGuild;

public interface IGuildOnItemMove : IScriptObject
{
	void OnItemMove(Guild guild, Player player, Item pItem, bool isSrcBank, byte srcContainer, byte srcSlotId, bool isDestBank, byte destContainer, byte destSlotId);
}