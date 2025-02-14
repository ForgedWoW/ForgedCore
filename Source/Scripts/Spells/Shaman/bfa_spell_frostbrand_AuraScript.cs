﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// Frostbrand - 196834
[SpellScript(196834)]
public class bfa_spell_frostbrand : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var attacker = eventInfo.ActionTarget;
		var caster = Caster;

		if (caster == null || attacker == null)
			return;

		caster.CastSpell(attacker, ShamanSpells.FROSTBRAND_SLOW, true);

		if (caster.HasAura(ShamanSpells.HAILSTORM_TALENT))
			caster.CastSpell(attacker, ShamanSpells.HAILSTORM_TALENT_PROC, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 1, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}