﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(203974)]
public class spell_druid_earthwarden : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		if (!Caster.AsPlayer.SpellHistory.HasCooldown(Spells.EARTHWARDEN))
			Caster.AddAura(Spells.EARTHWARDEN_TRIGGERED, Caster);

		Caster.AsPlayer.SpellHistory.AddCooldown(Spells.EARTHWARDEN, 0, TimeSpan.FromMicroseconds(500));
	}

	private struct Spells
	{
		public static readonly uint EARTHWARDEN = 203974;
		public static readonly uint EARTHWARDEN_TRIGGERED = 203975;
		public static readonly uint TRASH = 77758;
	}
}