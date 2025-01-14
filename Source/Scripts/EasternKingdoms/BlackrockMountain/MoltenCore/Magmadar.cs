// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Magmadar;

internal struct SpellIds
{
	public const uint Frenzy = 19451;
	public const uint MagmaSpit = 19449;
	public const uint Panic = 19408;
	public const uint LavaBomb = 19428;
}

internal struct TextIds
{
	public const uint EmoteFrenzy = 0;
}

[Script]
internal class boss_magmadar : BossAI
{
	public boss_magmadar(Creature creature) : base(creature, DataTypes.Magmadar) { }

	public override void Reset()
	{
		base.Reset();
		DoCast(Me, SpellIds.MagmaSpit, new CastSpellExtraArgs(true));
	}

	public override void JustEngagedWith(Unit victim)
	{
		base.JustEngagedWith(victim);

		Scheduler.Schedule(TimeSpan.FromSeconds(30),
							task =>
							{
								Talk(TextIds.EmoteFrenzy);
								DoCast(Me, SpellIds.Frenzy);
								task.Repeat(TimeSpan.FromSeconds(15));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(20),
							task =>
							{
								DoCastVictim(SpellIds.Panic);
								task.Repeat(TimeSpan.FromSeconds(35));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(12),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.LavaBomb);

								if (target)
									DoCast(target, SpellIds.LavaBomb);

								task.Repeat(TimeSpan.FromSeconds(12));
							});
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}
}