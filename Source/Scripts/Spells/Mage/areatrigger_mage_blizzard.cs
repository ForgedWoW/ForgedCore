﻿using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 4658 - AreaTrigger Create Properties
internal class areatrigger_mage_blizzard : AreaTriggerAI
{
	private TimeSpan _tickTimer;

	public areatrigger_mage_blizzard(AreaTrigger areatrigger) : base(areatrigger)
	{
		_tickTimer = TimeSpan.FromMilliseconds(1000);
	}

	public override void OnUpdate(uint diff)
	{
		_tickTimer -= TimeSpan.FromMilliseconds(diff);

		while (_tickTimer <= TimeSpan.Zero)
		{
			Unit caster = at.GetCaster();

			caster?.CastSpell(at.GetPosition(), MageSpells.BlizzardDamage, new CastSpellExtraArgs());

			_tickTimer += TimeSpan.FromMilliseconds(1000);
		}
	}
}