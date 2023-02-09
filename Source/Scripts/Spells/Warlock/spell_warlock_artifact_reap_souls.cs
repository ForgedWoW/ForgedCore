﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// Reap Souls - 216698
	[SpellScript(216698)]
	public class spell_warlock_artifact_reap_souls : SpellScript, IHasSpellEffects, ISpellCheckCast
	{
		public List<ISpellEffect> SpellEffects => new();

		private void HandleHit(uint UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(caster, WarlockSpells.DEADWIND_HARVERST, true);
		}

		public SpellCastResult CheckCast()
		{
			var caster = GetCaster();

			if (caster == null)
				return SpellCastResult.DontReport;

			if (!caster.HasAura(WarlockSpells.TORMENTED_SOULS))
				return SpellCastResult.CantDoThatRightNow;

			return SpellCastResult.SpellCastOk;
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
	}
}