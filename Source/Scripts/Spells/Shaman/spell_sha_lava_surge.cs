﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 77756 - Lava Surge
[SpellScript(77756)]
internal class spell_sha_lava_surge : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProcChance, 0, AuraType.Dummy));
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private bool CheckProcChance(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var procChance = aurEff.Amount;
		var igneousPotential = Target.GetAuraEffect(ShamanSpells.IgneousPotential, 0);

		if (igneousPotential != null)
			procChance += igneousPotential.Amount;

		return RandomHelper.randChance(procChance);
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		Target.CastSpell(Target, ShamanSpells.LavaSurge, true);
	}
}