﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(203783)]
public class spell_dh_shear_proc : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = GetCaster();

		if (caster == null || eventInfo.GetSpellInfo() != null)
			return;

		var procChance = 100;

		if (eventInfo.GetSpellInfo().Id == DemonHunterSpells.SPELL_DH_SHEAR)
		{
			procChance =  15;
			procChance += caster.GetAuraEffectAmount(ShatteredSoulsSpells.SPELL_DH_SHATTER_THE_SOULS, 0);
		}

		/*
			if (RandomHelper.randChance(procChance))
			    caster->CastSpell(caster, SPELL_DH_SHATTERED_SOULS_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)SPELL_DH_LESSER_SOUL_SHARD));
			*/

		if (caster.GetSpellHistory().HasCooldown(DemonHunterSpells.SPELL_DH_FELBLADE))
			if (RandomHelper.randChance(caster.GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_SHEAR_PROC, 3)))
				caster.GetSpellHistory().ResetCooldown(DemonHunterSpells.SPELL_DH_FELBLADE);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}