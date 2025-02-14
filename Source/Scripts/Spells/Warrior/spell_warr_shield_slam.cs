﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

[SpellScript(new uint[]
{
	23922, 316523
})]
public class spell_warr_shield_slam : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDispel, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandlePassive, 2, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDispel(int effIndex)
	{
		// 6.0.3 HOTFIX: Shield Slam modified by Glyph of Shield Slam now only dispels 1 magical effect while the Warrior is in Defensive Stance.
		if (Caster.ShapeshiftForm != ShapeShiftForm.DefensiveStance)
			PreventHitDefaultEffect(effIndex);
	}

	private void HandlePassive(int effIndex)
	{
		//Handles the passive bonuses
		var caster = Caster;

		if (caster != null)
			if (caster.HasAura(WarriorSpells.HEAVY_REPERCUSSIONS) && caster.HasAura(WarriorSpells.SHIELD_BLOCKC_TRIGGERED))
				caster.GetAura(WarriorSpells.SHIELD_BLOCKC_TRIGGERED).SetDuration(caster.GetAura(WarriorSpells.SHIELD_BLOCKC_TRIGGERED).Duration + 1500);
	}
}