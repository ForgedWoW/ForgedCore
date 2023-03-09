﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;

namespace Game.Spells;

public class DynObjAura : Aura
{
	public DynObjAura(AuraCreateInfo createInfo) : base(createInfo)
	{
		LoadScripts();
		Cypher.Assert(DynobjOwner != null);
		Cypher.Assert(DynobjOwner.IsInWorld);
		Cypher.Assert(DynobjOwner.Map == createInfo.Caster.Map);
		_InitEffects(createInfo.AuraEffectMask, createInfo.Caster, createInfo.BaseAmount);
		DynobjOwner.SetAura(this);
	}

	public override void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
	{
		if (IsRemoved)
			return;

		_Remove(removeMode);
		base.Remove(removeMode);
	}

	public override void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster)
	{
		var dynObjOwnerCaster = DynobjOwner.GetCaster();
		var radius = DynobjOwner.GetRadius();

		foreach (var spellEffectInfo in SpellInfo.Effects)
		{
			if (!HasEffect(spellEffectInfo.EffectIndex))
				continue;

			// we can't use effect type like area auras to determine check type, check targets
			var selectionType = spellEffectInfo.TargetA.CheckType;

			if (spellEffectInfo.TargetB.ReferenceType == SpellTargetReferenceTypes.Dest)
				selectionType = spellEffectInfo.TargetB.CheckType;

			List<Unit> targetList = new();
			var condList = spellEffectInfo.ImplicitTargetConditions;

			WorldObjectSpellAreaTargetCheck check = new(radius, DynobjOwner.Location, dynObjOwnerCaster, dynObjOwnerCaster, SpellInfo, selectionType, condList, SpellTargetObjectTypes.Unit);
			UnitListSearcher searcher = new(DynobjOwner, targetList, check, GridType.All);
			Cell.VisitGrid(DynobjOwner, searcher, radius);

			// by design WorldObjectSpellAreaTargetCheck allows not-in-world units (for spells) but for auras it is not acceptable
			targetList.RemoveAll(unit => !unit.IsSelfOrInSameMap(DynobjOwner));

			foreach (var unit in targetList)
			{
				if (!targets.ContainsKey(unit))
					targets[unit] = 0;

				targets[unit] |= 1u << spellEffectInfo.EffectIndex;
			}
		}
	}
}