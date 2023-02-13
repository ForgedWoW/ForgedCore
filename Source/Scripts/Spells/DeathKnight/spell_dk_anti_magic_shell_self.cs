﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

public class spell_dk_anti_magic_shell_self : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private int absorbPct;

	public override bool Load()
	{
		absorbPct = GetSpellInfo().GetEffect(0).CalcValue(GetCaster());

		return true;
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		amount = (int)GetUnitOwner().CountPctFromMaxHealth(40);
	}

	private void Absorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref uint absorbAmount)
	{
		absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), absorbPct);
	}

	private void Trigger(AuraEffect aurEff, DamageInfo UnnamedParameter, ref uint absorbAmount)
	{
		var target = GetTarget();
		// Patch 6.0.2 (October 14, 2014): Anti-Magic Shell now restores 2 Runic Power per 1% of max health absorbed.
		var damagePerRp    = target.CountPctFromMaxHealth(1) / 2.0f;
		var energizeAmount = (absorbAmount / damagePerRp) * 10.0f;
		var args           = new CastSpellExtraArgs();
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)energizeAmount);
		args.SetTriggerFlags(TriggerCastFlags.FullMask);
		args.SetTriggeringAura(aurEff);
		target.CastSpell(target, DeathKnightSpells.SPELL_DK_RUNIC_POWER_ENERGIZE, args);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Trigger, 0, false, AuraScriptHookType.EffectAfterAbsorb));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
	}
}