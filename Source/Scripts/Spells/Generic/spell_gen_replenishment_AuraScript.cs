﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_replenishment_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		return OwnerAsUnit.GetPower(PowerType.Mana) != 0;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicEnergize));
	}

	private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		switch (SpellInfo.Id)
		{
			case GenericSpellIds.Replenishment:
				amount.Value = (OwnerAsUnit.GetMaxPower(PowerType.Mana) * 0.002f);

				break;
			case GenericSpellIds.InfiniteReplenishment:
				amount.Value = (OwnerAsUnit.GetMaxPower(PowerType.Mana) * 0.0025f);

				break;
			default:
				break;
		}
	}
}