﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 40306 - Stasis Field
internal class spell_stasis_field_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
	}

	private void OnPeriodic(AuraEffect aurEff)
	{
		var owner = Target;

		List<Creature> targets = new();
		StasisFieldSearcher creature_check = new(owner, 15.0f);
		CreatureListSearcher creature_searcher = new(owner, targets, creature_check, GridType.Grid);
		Cell.VisitGrid(owner, creature_searcher, 15.0f);

		if (!targets.Empty())
			return;

		PreventDefaultAction();
	}
}