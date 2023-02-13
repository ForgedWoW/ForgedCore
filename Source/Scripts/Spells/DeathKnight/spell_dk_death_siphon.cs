﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(108196)]
public class spell_dk_death_siphon : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void HandleScriptEffect(uint UnnamedParameter)
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
			if (GetHitUnit())
			{
				float bp   = GetHitDamage();
				var   args = new CastSpellExtraArgs();
				args.AddSpellMod(SpellValueMod.BasePoint0, (int)bp);
				args.SetTriggerFlags(TriggerCastFlags.FullMask);
				_player.CastSpell(_player, DeathKnightSpells.SPELL_DK_DEATH_SIPHON_HEAL, args);
			}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}