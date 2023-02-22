﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.DeathKnight;

[Script]
internal class spell_dk_vile_contagion : SpellScript, ISpellOnHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.FESTERING_WOUND);
	}

	public void OnHit()
	{
		var target = GetHitUnit();
		var exclude = target;
		if (target != null) {
			var pustules = target.GetAura(DeathKnightSpells.FESTERING_WOUND);
			if(pustules != null) {
				var stacks = pustules.GetStackAmount();
				var jumps = 7;
                for (int i = 0; i < jumps; i++){
					var bounce = target.SelectNearbyTarget(exclude, 8f);
                    GetCaster().CastSpell(bounce, DeathKnightSpells.FESTERING_WOUND, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, stacks));
                    exclude = target.SelectNearbyTarget(target, 8f);
                }
			}
		}
	}
}