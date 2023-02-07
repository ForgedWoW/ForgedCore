﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(5143)]
public class spell_mage_arcane_missiles : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		//@TODO: Remove when proc system can handle arcane missiles.....
		caster.RemoveAura(MageSpells.SPELL_MAGE_CLEARCASTING_BUFF);
		caster.RemoveAura(MageSpells.SPELL_MAGE_CLEARCASTING_EFFECT);
		Aura pvpClearcast = caster.GetAura(MageSpells.SPELL_MAGE_CLEARCASTING_PVP_STACK_EFFECT);
		if (pvpClearcast != null)
		{
			pvpClearcast.ModStackAmount(-1);
		}
		caster.RemoveAura(MageSpells.SPELL_MAGE_RULE_OF_THREES_BUFF);
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectApplyHandler(OnApply, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
	}
}