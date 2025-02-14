﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(2060)]
public class spell_pri_heal_flash_heal : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		var caster = Caster.AsPlayer;

		if (!caster.AsPlayer)
			return;

		if (caster.GetPrimarySpecialization() == TalentSpecialization.PriestHoly)
			if (caster.SpellHistory.HasCooldown(PriestSpells.HOLY_WORD_SERENITY))
				caster.SpellHistory.ModifyCooldown(PriestSpells.HOLY_WORD_SERENITY, TimeSpan.FromSeconds(-6 * Time.InMilliseconds));
	}
}