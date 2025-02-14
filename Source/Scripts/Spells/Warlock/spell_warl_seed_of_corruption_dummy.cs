﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

[SpellScript(27243)] // 27243 - Seed of Corruption
internal class spell_warl_seed_of_corruption_dummy : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateBuffer, 2, AuraType.Dummy));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 2, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void CalculateBuffer(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var caster = Caster;

		if (caster == null)
			return;

		amount.Value = caster.SpellBaseDamageBonusDone(SpellInfo.GetSchoolMask()) * GetEffectInfo(0).CalcValue(caster) / 100;
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var damageInfo = eventInfo.DamageInfo;

		if (damageInfo == null ||
			damageInfo.Damage == 0)
			return;

		var amount = (int)(aurEff.Amount - damageInfo.Damage);

		if (amount > 0)
		{
			aurEff.SetAmount(amount);

			if (!Target.HealthBelowPctDamaged(1, damageInfo.Damage))
				return;
		}

		Remove();

		var caster = Caster;

		if (!caster)
			return;

		caster.CastSpell(eventInfo.ActionTarget, WarlockSpells.SEED_OF_CORRUPTION_DAMAGE, true);
	}
}