﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[Script]
internal class spell_dru_wild_growth_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectUpdatePeriodicHandler(HandleTickUpdate, 0, AuraType.PeriodicHeal));
	}

	private void HandleTickUpdate(AuraEffect aurEff)
	{
		var caster = Caster;

		if (!caster)
			return;

		// calculate from base Damage, not from aurEff.GetAmount() (already modified)
		var damage = caster.CalculateSpellDamage(OwnerAsUnit, aurEff.GetSpellEffectInfo());

		// Wild Growth = first tick gains a 6% bonus, reduced by 2% each tick
		double reduction = 2.0f;
		var bonus = caster.GetAuraEffect(DruidSpellIds.RestorationT102PBonus, 0);

		if (bonus != null)
			reduction -= MathFunctions.CalculatePct(reduction, bonus.Amount);

		reduction *= (aurEff.GetTickNumber() - 1);

		MathFunctions.AddPct(ref damage, 6.0f - reduction);
		aurEff.SetAmount(damage);
	}
}