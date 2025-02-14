﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 39446 - Aura of Madness
internal class spell_item_aura_of_madness : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		uint[][] triggeredSpells =
		{
			//CLASS_NONE
			Array.Empty<uint>(),
			//CLASS_WARRIOR
			new uint[]
			{
				ItemSpellIds.Sociopath, ItemSpellIds.Delusional, ItemSpellIds.Kleptomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.MartyrComplex
			},
			//CLASS_PALADIN
			new uint[]
			{
				ItemSpellIds.Sociopath, ItemSpellIds.Delusional, ItemSpellIds.Kleptomania, ItemSpellIds.Megalomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.Narcissism, ItemSpellIds.MartyrComplex, ItemSpellIds.Dementia
			},
			//CLASS_HUNTER
			new uint[]
			{
				ItemSpellIds.Delusional, ItemSpellIds.Megalomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.Narcissism, ItemSpellIds.MartyrComplex, ItemSpellIds.Dementia
			},
			//CLASS_ROGUE
			new uint[]
			{
				ItemSpellIds.Sociopath, ItemSpellIds.Delusional, ItemSpellIds.Kleptomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.MartyrComplex
			},
			//CLASS_PRIEST
			new uint[]
			{
				ItemSpellIds.Megalomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.Narcissism, ItemSpellIds.MartyrComplex, ItemSpellIds.Dementia
			},
			//CLASS_DEATH_KNIGHT
			new uint[]
			{
				ItemSpellIds.Sociopath, ItemSpellIds.Delusional, ItemSpellIds.Kleptomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.MartyrComplex
			},
			//CLASS_SHAMAN
			new uint[]
			{
				ItemSpellIds.Megalomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.Narcissism, ItemSpellIds.MartyrComplex, ItemSpellIds.Dementia
			},
			//CLASS_MAGE
			new uint[]
			{
				ItemSpellIds.Megalomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.Narcissism, ItemSpellIds.MartyrComplex, ItemSpellIds.Dementia
			},
			//CLASS_WARLOCK
			new uint[]
			{
				ItemSpellIds.Megalomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.Narcissism, ItemSpellIds.MartyrComplex, ItemSpellIds.Dementia
			},
			//CLASS_UNK
			Array.Empty<uint>(),
			//CLASS_DRUID
			new uint[]
			{
				ItemSpellIds.Sociopath, ItemSpellIds.Delusional, ItemSpellIds.Kleptomania, ItemSpellIds.Megalomania, ItemSpellIds.Paranoia, ItemSpellIds.Manic, ItemSpellIds.Narcissism, ItemSpellIds.MartyrComplex, ItemSpellIds.Dementia
			}
		};

		PreventDefaultAction();
		var caster = eventInfo.Actor;
		var spellId = triggeredSpells[(int)caster.Class].SelectRandom();
		caster.CastSpell(caster, spellId, new CastSpellExtraArgs(aurEff));

		if (RandomHelper.randChance(10))
			caster.Say(TextIds.SayMadness);
	}
}