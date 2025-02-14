﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(155228)]
public class spell_hun_lone_wolf : AuraScript, IHasAuraEffects, IAuraOnUpdate
{
	internal static uint[] g_BuffSpells =
	{
		(uint)LoneWolfes.LoneWolfMastery, (uint)LoneWolfes.LoneWolfStamina, (uint)LoneWolfes.LoneWolfCritical, (uint)LoneWolfes.LoneWolfHaste, (uint)LoneWolfes.LoneWolfSpellPower, (uint)LoneWolfes.LoneWolfPrimarStats, (uint)LoneWolfes.LoneWolfVersatility, (uint)LoneWolfes.LoneWolfMultistrike
	};

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public void AuraOnUpdate(uint diff)
	{
		if (!OwnerAsUnit)
			return;

		var player = OwnerAsUnit.AsPlayer;

		if (player == null)
			return;

		var pet = player.CurrentPet;
		var aurEff = GetEffect(0);

		if (pet != null)
		{
			player.RemoveAura(LoneWolfes.LoneWolfAura);

			aurEff.ChangeAmount(0, true, true);

			var auraEffect = GetEffect(1);

			if (auraEffect != null)
				auraEffect.ChangeAmount(0, true, true);

			for (byte I = 0; I < 8; ++I)
				player.RemoveAura(g_BuffSpells[I]);
		}
		else
		{
			if (!player.HasAura(LoneWolfes.LoneWolfAura))
			{
				player.CastSpell(player, LoneWolfes.LoneWolfAura, true);

				aurEff.ChangeAmount(SpellInfo.GetEffect(0).BasePoints, true, true);

				var auraEffect = aurEff.Base.GetEffect(1);

				if (auraEffect != null)
					auraEffect.ChangeAmount(SpellInfo.GetEffect(0).BasePoints, true, true);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (!Caster)
			return;

		var player = Caster.AsPlayer;

		if (player != null)
		{
			player.LearnSpell(LoneWolfes.LoneWolfMastery, false);
			player.LearnSpell(LoneWolfes.LoneWolfStamina, false);
			player.LearnSpell(LoneWolfes.LoneWolfCritical, false);
			player.LearnSpell(LoneWolfes.LoneWolfHaste, false);
			player.LearnSpell(LoneWolfes.LoneWolfSpellPower, false);
			player.LearnSpell(LoneWolfes.LoneWolfPrimarStats, false);
			player.LearnSpell(LoneWolfes.LoneWolfVersatility, false);
			player.LearnSpell(LoneWolfes.LoneWolfMultistrike, false);
		}
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		if (!Caster)
			return;

		var player = Caster.AsPlayer;

		if (player != null)
		{
			player.RemoveSpell(LoneWolfes.LoneWolfMastery);
			player.RemoveSpell(LoneWolfes.LoneWolfStamina);
			player.RemoveSpell(LoneWolfes.LoneWolfCritical);
			player.RemoveSpell(LoneWolfes.LoneWolfHaste);
			player.RemoveSpell(LoneWolfes.LoneWolfSpellPower);
			player.RemoveSpell(LoneWolfes.LoneWolfPrimarStats);
			player.RemoveSpell(LoneWolfes.LoneWolfVersatility);
			player.RemoveSpell(LoneWolfes.LoneWolfMultistrike);
			player.RemoveAura(LoneWolfes.LoneWolfAura);
		}
	}
}