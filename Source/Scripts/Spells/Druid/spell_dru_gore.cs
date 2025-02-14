﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[Script] // 210706 - Gore
internal class spell_dru_gore : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckEffectProc, 0, AuraType.Dummy));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private bool CheckEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		return RandomHelper.randChance(aurEff.Amount);
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
	{
		var owner = Target;
		owner.CastSpell(owner, DruidSpellIds.GoreProc);
		owner.SpellHistory.ResetCooldown(DruidSpellIds.Mangle, true);
	}
}