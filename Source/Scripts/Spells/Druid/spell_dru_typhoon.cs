﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(61391)]
public class spell_dru_typhoon : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleKnockBack, 0, SpellEffectName.KnockBack, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleKnockBack(int effIndex)
	{
		// Glyph of Typhoon
		if (Caster.HasAura(DruidSpells.GLYPH_OF_TYPHOON))
			PreventHitDefaultEffect(effIndex);
	}
}