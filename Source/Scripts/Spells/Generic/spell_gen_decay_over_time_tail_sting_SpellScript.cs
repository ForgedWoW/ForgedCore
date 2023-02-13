﻿using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 36659 - Tail Sting
internal class spell_gen_decay_over_time_tail_sting_SpellScript : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var aur = GetHitAura();

		aur?.SetStackAmount((byte)GetSpellInfo().StackAmount);
	}
}