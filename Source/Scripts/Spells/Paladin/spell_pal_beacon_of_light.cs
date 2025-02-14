﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// 53563 - Beacon of Light
[SpellScript(53563)]
public class spell_pal_beacon_of_light : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		var target = ExplTargetUnit;

		if (target == null)
			return SpellCastResult.DontReport;

		if (target.HasAura(PaladinSpells.BEACON_OF_FAITH))
			return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}
}