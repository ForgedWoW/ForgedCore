﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

//197211 - Fury of Air
[SpellScript(197211)]
public class spell_sha_fury_of_air : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicTriggerSpell));
	}

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		var caster = Caster;

		if (caster == null)
			return;

		if (caster.GetPower(PowerType.Maelstrom) >= 5)
			caster.SetPower(PowerType.Maelstrom, caster.GetPower(PowerType.Maelstrom) - 5);
		else
			caster.RemoveAura(ShamanSpells.FURY_OF_AIR);
	}
}