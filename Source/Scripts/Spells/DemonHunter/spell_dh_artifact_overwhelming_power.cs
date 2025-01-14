﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201464)]
public class spell_dh_artifact_overwhelming_power : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
	}

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (RandomHelper.randChance(caster.GetAuraEffectAmount(DemonHunterSpells.OVERWHELMING_POWER, 0)))
			caster.CastSpell(caster, ShatteredSoulsSpells.SHATTERED_SOULS_MISSILE, SpellValueMod.BasePoint0, (int)ShatteredSoulsSpells.LESSER_SOUL_SHARD, true);
	}
}