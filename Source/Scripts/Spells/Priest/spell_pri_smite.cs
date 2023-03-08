﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(585)]
public class spell_pri_smite : SpellScript, IHasSpellEffects, ISpellAfterCast
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SMITE_ABSORB);
	}

	public void AfterCast()
	{
		var caster = Caster.AsPlayer;

		if (caster == null)
			return;

		if (caster.GetPrimarySpecialization() == TalentSpecialization.PriestHoly)
			if (caster.GetSpellHistory().HasCooldown(PriestSpells.HOLY_WORD_CHASTISE))
				caster.GetSpellHistory().ModifyCooldown(PriestSpells.HOLY_WORD_CHASTISE, TimeSpan.FromSeconds(-6 * Time.InMilliseconds));
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHit(int effIndex)
	{
		var caster = Caster.AsPlayer;
		var target = HitUnit;

		if (caster == null || target == null)
			return;

		if (!caster.AsPlayer)
			return;

		var dmg = HitDamage;

		if (caster.HasAura(PriestSpells.HOLY_WORDS) || caster.GetPrimarySpecialization() == TalentSpecialization.PriestHoly)
			if (caster.GetSpellHistory().HasCooldown(PriestSpells.HOLY_WORD_CHASTISE))
				caster.GetSpellHistory().ModifyCooldown(PriestSpells.HOLY_WORD_CHASTISE, TimeSpan.FromSeconds(-4 * Time.InMilliseconds));
	}
}