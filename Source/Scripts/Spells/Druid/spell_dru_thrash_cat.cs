﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(106830)]
public class spell_dru_thrash_cat : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void EffectHitTarget(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (caster == null || target == null)
		{
			return;
		}

		// This prevent awarding multiple Combo Points when multiple targets hit with Thrash AoE
		if (m_awardComboPoint)
		{
			// Awards the caster 1 Combo Point
			caster.ModifyPower(PowerType.ComboPoints, 1);
		}
		m_awardComboPoint = false;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(EffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private bool m_awardComboPoint = true;
}