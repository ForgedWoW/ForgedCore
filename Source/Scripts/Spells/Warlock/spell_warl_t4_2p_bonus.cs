﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

[SpellScript(37377, "spell_warl_t4_2p_bonus_shadow", false, WarlockSpells.FLAMESHADOW)] // 37377 - Shadowflame
[SpellScript(39437, "spell_warl_t4_2p_bonus_fire", false, WarlockSpells.SHADOWFLAME)]   // 39437 - Shadowflame Hellfire and RoF
internal class spell_warl_t4_2p_bonus : AuraScript, IHasAuraEffects
{
	private readonly uint _triggerSpell;

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public spell_warl_t4_2p_bonus(uint triggerSpell)
	{
		_triggerSpell = triggerSpell;
	}


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = eventInfo.Actor;
		caster.CastSpell(caster, _triggerSpell, new CastSpellExtraArgs(aurEff));
	}
}