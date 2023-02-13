﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201463)]
public class spell_dh_artifact_deceivers_fury : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void OnProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_DECEIVERS_FURY_ENERGIZE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)aurEff.GetAmount()));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}