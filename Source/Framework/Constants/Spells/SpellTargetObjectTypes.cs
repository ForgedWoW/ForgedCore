﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellTargetObjectTypes
{
	None = 0,
	Src,
	Dest,
	Unit,
	UnitAndDest,
	Gobj,
	GobjItem,
	Item,
	Corpse,

	// Only For Effect Target Type
	CorpseEnemy,
	CorpseAlly
}