﻿using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 194509 - Power Word: Radiance
internal class spell_pri_power_word_radiance : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.Atonement, PriestSpells.AtonementTriggered, PriestSpells.Trinity) && spellInfo.GetEffects().Count > 3;
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(OnTargetSelect, 1, Targets.UnitDestAreaAlly));
		SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 1, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}

	private void OnTargetSelect(List<WorldObject> targets)
	{
		uint maxTargets = (uint)(GetEffectInfo(2).CalcValue(GetCaster()) + 1); // adding 1 for explicit Target unit

		if (targets.Count > maxTargets)
		{
			Unit explTarget = GetExplTargetUnit();

			// Sort targets so units with no atonement are first, then units who are injured, then oher units
			// Make sure explicit Target unit is first
			targets.Sort((lhs, rhs) =>
			             {
				             if (lhs == explTarget) // explTarget > anything: always true
					             return 1;

				             if (rhs == explTarget) // anything > explTarget: always false
					             return -1;

				             return MakeSortTuple(lhs).Equals(MakeSortTuple(rhs)) ? 1 : -1;
			             });

			targets.Resize(maxTargets);
		}
	}

	private void HandleEffectHitTarget(uint effIndex)
	{
		Unit caster = GetCaster();

		if (caster.HasAura(PriestSpells.Trinity))
			return;

		int durationPct = GetEffectInfo(3).CalcValue(caster);

		if (caster.HasAura(PriestSpells.Atonement))
			caster.CastSpell(GetHitUnit(), PriestSpells.AtonementTriggered, new CastSpellExtraArgs(SpellValueMod.DurationPct, durationPct).SetTriggerFlags(TriggerCastFlags.FullMask));
	}

	private Tuple<bool, bool> MakeSortTuple(WorldObject obj)
	{
		return Tuple.Create(IsUnitWithNoAtonement(obj), IsUnitInjured(obj));
	}

	// Returns true if obj is a unit but has no atonement
	private bool IsUnitWithNoAtonement(WorldObject obj)
	{
		Unit unit = obj.ToUnit();

		return unit != null && !unit.HasAura(PriestSpells.AtonementTriggered, GetCaster().GetGUID());
	}

	// Returns true if obj is a unit and is injured
	private static bool IsUnitInjured(WorldObject obj)
	{
		Unit unit = obj.ToUnit();

		return unit != null && unit.IsFullHealth();
	}
}