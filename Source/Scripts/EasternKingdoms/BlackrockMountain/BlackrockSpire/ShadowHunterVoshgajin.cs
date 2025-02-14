// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.ShadowHunterVoshgajin;

internal struct SpellIds
{
	public const uint Curseofblood = 24673;
	public const uint Hex = 16708;
	public const uint Cleave = 20691;
}

[Script]
internal class boss_shadow_hunter_voshgajin : BossAI
{
	public boss_shadow_hunter_voshgajin(Creature creature) : base(creature, DataTypes.ShadowHunterVoshgajin) { }

	public override void Reset()
	{
		_Reset();
		//DoCast(me, SpellIcearmor, true);
	}

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);

		Scheduler.Schedule(TimeSpan.FromSeconds(2),
							task =>
							{
								DoCastVictim(SpellIds.Curseofblood);
								task.Repeat(TimeSpan.FromSeconds(45));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(8),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

								if (target)
									DoCast(target, SpellIds.Hex);

								task.Repeat(TimeSpan.FromSeconds(15));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(14),
							task =>
							{
								DoCastVictim(SpellIds.Cleave);
								task.Repeat(TimeSpan.FromSeconds(7));
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