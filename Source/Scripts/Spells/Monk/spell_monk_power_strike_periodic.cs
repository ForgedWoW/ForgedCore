﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(121817)]
public class spell_monk_power_strike_periodic : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		Target.CastSpell(Target, MonkSpells.POWER_STRIKES_AURA, true);
	}
}