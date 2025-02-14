﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 33076 - Prayer of Mending
internal class spell_pri_prayer_of_mending : SpellScript, IHasSpellEffects
{
	private SpellEffectInfo _healEffectDummy;
	private SpellInfo _spellInfoHeal;
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override bool Load()
	{
		_spellInfoHeal = Global.SpellMgr.GetSpellInfo(PriestSpells.PRAYER_OF_MENDING_HEAL, Difficulty.None);
		_healEffectDummy = _spellInfoHeal.GetEffect(0);

		return true;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleEffectDummy(int effIndex)
	{
		var basePoints = Caster.SpellHealingBonusDone(HitUnit, _spellInfoHeal, (uint)_healEffectDummy.CalcValue(Caster), DamageEffectType.Heal, _healEffectDummy, 1, Spell);
		CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
		args.AddSpellMod(SpellValueMod.AuraStack, EffectValue);
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)basePoints);
		Caster.CastSpell(HitUnit, PriestSpells.PRAYER_OF_MENDING_AURA, args);
	}
}