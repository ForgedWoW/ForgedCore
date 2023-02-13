﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(205411)]
public class spell_dh_desperate_instincts : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = GetCaster();

		if (caster == null || eventInfo.GetDamageInfo() != null)
			return;

		if (caster.GetSpellHistory().HasCooldown(DemonHunterSpells.SPELL_DH_BLUR_BUFF))
			return;

		var triggerOnHealth = caster.CountPctFromMaxHealth(aurEff.GetAmount());
		var currentHealth   = caster.GetHealth();

		// Just falling below threshold
		if (currentHealth > triggerOnHealth && (currentHealth - eventInfo.GetDamageInfo().GetDamage()) <= triggerOnHealth)
			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_BLUR_BUFF, false);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.TriggerSpellOnHealthPct, AuraScriptHookType.EffectProc));
	}
}