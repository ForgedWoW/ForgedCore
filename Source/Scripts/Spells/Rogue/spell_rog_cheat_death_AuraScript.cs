﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(31230)]
public class spell_rog_cheat_death_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(RogueSpells.SPELL_ROGUE_CHEAT_DEATH_COOLDOWN);
	}

	public override bool Load()
	{
		return GetUnitOwner().GetTypeId() == TypeId.Player;
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		// Set absorbtion amount to unlimited
		amount = -1;
	}

	private void Absorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref uint absorbAmount)
	{
		var target = GetTarget().ToPlayer();

		if (target.HasAura(CheatDeath.SPELL_ROGUE_CHEAT_DEATH_DMG_REDUC))
		{
			absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), 85);

			return;
		}
		else
		{
			if (dmgInfo.GetDamage() < target.GetHealth() || target.HasAura(RogueSpells.SPELL_ROGUE_CHEAT_DEATH_COOLDOWN))
				return;

			var health7 = target.CountPctFromMaxHealth(7);
			target.SetHealth(1);
			var healInfo = new HealInfo(target, target, (uint)health7, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
			target.HealBySpell(healInfo);
			target.CastSpell(target, CheatDeath.SPELL_ROGUE_CHEAT_DEATH_ANIM, true);
			target.CastSpell(target, CheatDeath.SPELL_ROGUE_CHEAT_DEATH_DMG_REDUC, true);
			target.CastSpell(target, RogueSpells.SPELL_ROGUE_CHEAT_DEATH_COOLDOWN, true);
			absorbAmount = dmgInfo.GetDamage();
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 1));
	}
}