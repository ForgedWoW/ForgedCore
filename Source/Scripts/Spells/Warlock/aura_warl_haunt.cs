﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 48181 - Haunt
[SpellScript(48181)]
public class aura_warl_haunt : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.ModSchoolMaskDamageFromCaster, AuraEffectHandleModes.RealOrReapplyMask));
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = Caster;

		if (caster == null || TargetApplication.RemoveMode != AuraRemoveMode.Death)
			return;

		caster.SpellHistory.ResetCooldown(WarlockSpells.HAUNT, true);
	}
}