﻿using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script]
internal class spell_hun_steady_shot : SpellScript, ISpellOnHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.SteadyShotFocus);
	}

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public void OnHit()
	{
		GetCaster().CastSpell(GetCaster(), HunterSpells.SteadyShotFocus, true);
	}
}