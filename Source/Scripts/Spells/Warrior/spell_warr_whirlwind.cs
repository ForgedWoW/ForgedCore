﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

// Whirlwind - 190411
[SpellScript(190411)]
public class spell_warr_whirlwind : SpellScript, ISpellAfterCast
{
	public void AfterCast()
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (caster.HasAura(WarriorSpells.WRECKING_BALL_EFFECT))
			caster.RemoveAura(WarriorSpells.WRECKING_BALL_EFFECT);

		if (caster.HasAura(WarriorSpells.MEAT_CLEAVER))
			if (RandomHelper.randChance(10))
				caster.CastSpell(null, WarriorSpells.ENRAGE_AURA, true);

		if (caster.HasAura(WarriorSpells.THIRST_FOR_BATTLE))
		{
			caster.AddAura(WarriorSpells.THIRST_FOR_BATTLE_BUFF, caster);
			var thirst = caster.GetAura(WarriorSpells.THIRST_FOR_BATTLE_BUFF).GetEffect(0);

			//if (thirst != null)
			//	thirst.Amount;
		}

		caster.AddAura(85739, caster);
	}
}