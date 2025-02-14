﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(108968)]
public class spell_pri_void_shift : SpellScript, IHasSpellEffects, ISpellCheckCast
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public SpellCastResult CheckCast()
	{
		if (ExplTargetUnit)
			if (ExplTargetUnit.TypeId != TypeId.Player)
				return SpellCastResult.BadTargets;

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var _player = Caster.AsPlayer;

		if (_player != null)
		{
			var target = HitUnit;

			if (target != null)
			{
				var playerPct = _player.HealthPct;
				var targetPct = target.HealthPct;

				if (playerPct < 25)
					playerPct = 25;

				if (targetPct < 25)
					targetPct = 25;

				playerPct /= 100;
				targetPct /= 100;

				_player.SetHealth(_player.MaxHealth * targetPct);
				target.SetHealth(target.MaxHealth * playerPct);
			}
		}
	}
}