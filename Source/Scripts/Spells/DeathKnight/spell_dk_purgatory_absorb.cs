﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(114556)]
public class spell_dk_purgatory_absorb : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void CalculateAmount(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
	{
		amount = -1;
	}

	private double Absorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, double absorbAmount)
	{
		var target = GetTarget();

		if (dmgInfo.GetDamage() < target.GetHealth())
			return absorbAmount;

		// No damage received under Shroud of Purgatory
		if (target.ToPlayer().HasAura(DeathKnightSpells.SHROUD_OF_PURGATORY))
		{
			return dmgInfo.GetDamage();
		}

		if (target.ToPlayer().HasAura(DeathKnightSpells.PERDITION))
			return absorbAmount;

		double bp   = dmgInfo.GetDamage();
		var   args = new CastSpellExtraArgs();
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)bp);
		args.SetTriggerFlags(TriggerCastFlags.FullMask);
		target.CastSpell(target, DeathKnightSpells.SHROUD_OF_PURGATORY, args);
		target.CastSpell(target, DeathKnightSpells.PERDITION, TriggerCastFlags.FullMask);
		target.SetHealth(1);
		return dmgInfo.GetDamage();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
	}
}