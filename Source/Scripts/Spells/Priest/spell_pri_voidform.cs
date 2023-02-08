﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(194249)]
public class spell_pri_voidform : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			caster.RemoveAurasDueToSpell(PriestSpells.SPELL_PRIEST_LINGERING_INSANITY);
		}
	}

	private void HandlePeriodic(AuraEffect aurEff)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		// This spell must end when insanity hit 0
		if (caster.GetPower(PowerType.Insanity) == 0)
		{
			caster.RemoveAura(aurEff.GetBase());
			return;
		}

		int tick = GetAura().GetStackAmount() - 1;
		switch (tick)
		{
			case 0:
				caster.CastSpell(caster, PriestSpells.SPELL_PRIEST_VOIDFORM_TENTACLES, true);
				break;
			case 3:
				caster.CastSpell(caster, PriestSpells.SPELL_PRIEST_VOIDFORM_TENTACLES + 1, true);
				break;
			case 6:
				caster.CastSpell(caster, PriestSpells.SPELL_PRIEST_VOIDFORM_TENTACLES + 2, true);
				break;
			case 9:
				caster.CastSpell(caster, PriestSpells.SPELL_PRIEST_VOIDFORM_TENTACLES + 3, true);
				break;
			default:
				break;
		}

		caster.CastSpell(caster, PriestSpells.SPELL_PRIEST_VOIDFORM_BUFFS, true);
	}

	private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		for (uint i = 0; i < 4; ++i)
		{
			caster.RemoveAurasDueToSpell(PriestSpells.SPELL_PRIEST_VOIDFORM_TENTACLES + i);
		}

		int                haste = aurEff.GetAmount();
		CastSpellExtraArgs mod   = new CastSpellExtraArgs();
		mod.AddSpellMod(SpellValueMod.BasePoint0, haste);

		AuraEffect aEff = caster.GetAuraEffectOfRankedSpell(PriestSpells.SPELL_PRIEST_VOIDFORM_BUFFS, 3, caster.GetGUID());
		if (aEff != null)
		{
			mod.AddSpellMod(SpellValueMod.BasePoint1, aEff.GetAmount());
		}

		mod.TriggerFlags = TriggerCastFlags.FullMask;
		caster.CastSpell(caster, PriestSpells.SPELL_PRIEST_LINGERING_INSANITY, mod);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.AddPctModifier));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
	}
}