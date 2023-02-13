﻿using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(197923)]
public class spell_dh_fel_rush_dash_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster != null)
			caster.m_Events
			      .AddEventAtOffset(() =>
			                        {
				                        if (!caster.HasAura(DemonHunterSpells.SPELL_DH_FEL_RUSH_AIR))
					                        caster.SetDisableGravity(false);
			                        },
			                        TimeSpan.FromMilliseconds(100));
	}

	private void CalcSpeed(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		amount = 1250;
		RefreshDuration();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 1, AuraType.ModSpeedNoControl));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 3, AuraType.ModMinimumSpeed));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 9, AuraType.ModMinimumSpeedRate, AuraEffectHandleModes.SendForClientMask, AuraScriptHookType.EffectAfterRemove));
	}
}