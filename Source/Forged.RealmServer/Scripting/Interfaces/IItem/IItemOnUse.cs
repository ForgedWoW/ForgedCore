﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Forged.RealmServer.Spells;
using Game.Common.Entities.Items;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;

namespace Forged.RealmServer.Scripting.Interfaces.IItem;

public interface IItemOnUse : IScriptObject
{
	bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId);
}