﻿using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_trigger_exclude_target_aura_spell : SpellScript, ISpellAfterHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(spellInfo.ExcludeTargetAuraSpell);
	}

	public void AfterHit()
	{
		var target = GetHitUnit();

		if (target)
			// Blizz seems to just apply aura without bothering to cast
			GetCaster().AddAura(GetSpellInfo().ExcludeTargetAuraSpell, target);
	}
}