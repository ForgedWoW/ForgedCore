﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(198928)]
public class spell_mage_cinderstorm : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDamage(uint UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		if (target.HasAura(MageSpells.SPELL_MAGE_IGNITE_DOT))
		{
			//    int32 pct = Global.SpellMgr->GetSpellInfo(SPELL_MAGE_CINDERSTORM, Difficulty.None)->GetEffect(0).CalcValue(caster);
			var dmg = GetHitDamage();
			// MathFunctions.AddPct(ref dmg, pct);
			SetHitDamage(dmg);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}