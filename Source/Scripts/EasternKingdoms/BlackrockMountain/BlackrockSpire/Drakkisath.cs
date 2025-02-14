// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.Drakkisath;

internal struct SpellIds
{
	public const uint Firenova = 23462;
	public const uint Cleave = 20691;
	public const uint Confliguration = 16805;
	public const uint Thunderclap = 15548; //Not sure if right Id. 23931 would be a harder possibility.
}

[Script]
internal class boss_drakkisath : BossAI
{
	public boss_drakkisath(Creature creature) : base(creature, DataTypes.GeneralDrakkisath) { }

	public override void Reset()
	{
		_Reset();
	}

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);

		Scheduler.Schedule(TimeSpan.FromSeconds(6),
							task =>
							{
								DoCastVictim(SpellIds.Firenova);
								task.Repeat(TimeSpan.FromSeconds(10));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								DoCastVictim(SpellIds.Cleave);
								task.Repeat(TimeSpan.FromSeconds(8));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(15),
							task =>
							{
								DoCastVictim(SpellIds.Confliguration);
								task.Repeat(TimeSpan.FromSeconds(18));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(17),
							task =>
							{
								DoCastVictim(SpellIds.Thunderclap);
								task.Repeat(TimeSpan.FromSeconds(20));
							});
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}