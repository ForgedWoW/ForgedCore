// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Karazhan.Midnight;

internal struct SpellIds
{
	// Attumen
	public const uint Shadowcleave = 29832;
	public const uint IntangiblePresence = 29833;
	public const uint SpawnSmoke = 10389;
	public const uint Charge = 29847;

	// Midnight
	public const uint Knockdown = 29711;
	public const uint SummonAttumen = 29714;
	public const uint Mount = 29770;
	public const uint SummonAttumenMounted = 29799;
}

internal struct TextIds
{
	public const uint SayKill = 0;
	public const uint SayRandom = 1;
	public const uint SayDisarmed = 2;
	public const uint SayMidnightKill = 3;
	public const uint SayAppear = 4;
	public const uint SayMount = 5;

	public const uint SayDeath = 3;

	// Midnight
	public const uint EmoteCallAttumen = 0;
	public const uint EmoteMountUp = 1;
}

internal enum Phases
{
	None,
	AttumenEngages,
	Mounted
}

[Script]
internal class boss_attumen : BossAI
{
	private ObjectGuid _midnightGUID;
	private Phases _phase;

	public boss_attumen(Creature creature) : base(creature, DataTypes.Attumen)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();
		base.Reset();
	}

	public override void EnterEvadeMode(EvadeReason why)
	{
		var midnight = ObjectAccessor.GetCreature(Me, _midnightGUID);

		if (midnight)
			_DespawnAtEvade(TimeSpan.FromSeconds(10), midnight);

		Me.DespawnOrUnsummon();
	}

	public override void ScheduleTasks()
	{
		Scheduler.Schedule(TimeSpan.FromSeconds(15),
							TimeSpan.FromSeconds(25),
							task =>
							{
								DoCastVictim(SpellIds.Shadowcleave);
								task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(25),
							TimeSpan.FromSeconds(45),
							task =>
							{
								var target = SelectTarget(SelectTargetMethod.Random, 0);

								if (target)
									DoCast(target, SpellIds.IntangiblePresence);

								task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(45));
							});

		Scheduler.Schedule(TimeSpan.FromSeconds(30),
							TimeSpan.FromSeconds(60),
							task =>
							{
								Talk(TextIds.SayRandom);
								task.Repeat(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
							});
	}

	public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
	{
		// Attumen does not die until he mounts Midnight, let health fall to 1 and prevent further Damage.
		if (damage >= Me.Health &&
			_phase != Phases.Mounted)
			damage = (uint)(Me.Health - 1);

		if (_phase == Phases.AttumenEngages &&
			Me.HealthBelowPctDamaged(25, damage))
		{
			_phase = Phases.None;

			var midnight = ObjectAccessor.GetCreature(Me, _midnightGUID);

			if (midnight)
				midnight.AI.DoCastAOE(SpellIds.Mount, new CastSpellExtraArgs(true));
		}
	}

	public override void KilledUnit(Unit victim)
	{
		Talk(TextIds.SayKill);
	}

	public override void JustSummoned(Creature summon)
	{
		if (summon.Entry == CreatureIds.AttumenMounted)
		{
			var midnight = ObjectAccessor.GetCreature(Me, _midnightGUID);

			if (midnight)
			{
				if (midnight.Health > Me.Health)
					summon.SetHealth(midnight.Health);
				else
					summon.SetHealth(Me.Health);

				summon.AI.DoZoneInCombat();
				summon.AI.SetGUID(_midnightGUID, (int)CreatureIds.Midnight);
			}
		}

		base.JustSummoned(summon);
	}

	public override void IsSummonedBy(WorldObject summoner)
	{
		if (summoner.Entry == CreatureIds.Midnight)
			_phase = Phases.AttumenEngages;

		if (summoner.Entry == CreatureIds.AttumenUnmounted)
		{
			_phase = Phases.Mounted;
			DoCastSelf(SpellIds.SpawnSmoke);

			Scheduler.Schedule(TimeSpan.FromSeconds(10),
								TimeSpan.FromSeconds(25),
								task =>
								{
									Unit target = null;
									List<Unit> targetList = new();

									foreach (var refe in Me.GetThreatManager().SortedThreatList)
									{
										target = refe.Victim;

										if (target &&
											!target.IsWithinDist(Me, 8.00f, false) &&
											target.IsWithinDist(Me, 25.0f, false))
											targetList.Add(target);

										target = null;
									}

									if (!targetList.Empty())
										target = targetList.SelectRandom();

									DoCast(target, SpellIds.Charge);
									task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
								});

			Scheduler.Schedule(TimeSpan.FromSeconds(25),
								TimeSpan.FromSeconds(35),
								task =>
								{
									DoCastVictim(SpellIds.Knockdown);
									task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
								});
		}
	}

	public override void JustDied(Unit killer)
	{
		Talk(TextIds.SayDeath);
		var midnight = Global.ObjAccessor.GetUnit(Me, _midnightGUID);

		if (midnight)
			midnight.KillSelf();

		_JustDied();
	}

	public override void SetGUID(ObjectGuid guid, int id)
	{
		if (id == CreatureIds.Midnight)
			_midnightGUID = guid;
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim() &&
			_phase != Phases.None)
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}

	public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
	{
		if (spellInfo.Mechanic == Mechanics.Disarm)
			Talk(TextIds.SayDisarmed);

		if (spellInfo.Id == SpellIds.Mount)
		{
			var midnight = ObjectAccessor.GetCreature(Me, _midnightGUID);

			if (midnight)
			{
				_phase = Phases.None;
				Scheduler.CancelAll();

				midnight.AttackStop();
				midnight.RemoveAllAttackers();
				midnight.ReactState = ReactStates.Passive;
				midnight.MotionMaster.MoveFollow(Me, 2.0f, 0.0f);
				midnight.AI.Talk(TextIds.EmoteMountUp);

				Me.AttackStop();
				Me.RemoveAllAttackers();
				Me.ReactState = ReactStates.Passive;
				Me.MotionMaster.MoveFollow(midnight, 2.0f, 0.0f);
				Talk(TextIds.SayMount);

				Scheduler.Schedule(TimeSpan.FromSeconds(1),
									task =>
									{
										var midnight = ObjectAccessor.GetCreature(Me, _midnightGUID);

										if (midnight)
										{
											if (Me.IsWithinDist2d(midnight.Location, 5.0f))
											{
												DoCastAOE(SpellIds.SummonAttumenMounted);
												Me.SetVisible(false);
												Me.MotionMaster.Clear();
												midnight.SetVisible(false);
											}
											else
											{
												midnight.MotionMaster.MoveFollow(Me, 2.0f, 0.0f);
												Me.MotionMaster.MoveFollow(midnight, 2.0f, 0.0f);
												task.Repeat();
											}
										}
									});
			}
		}
	}

	private void Initialize()
	{
		_midnightGUID.Clear();
		_phase = Phases.None;
	}
}

[Script]
internal class boss_midnight : BossAI
{
	private ObjectGuid _attumenGUID;
	private Phases _phase;

	public boss_midnight(Creature creature) : base(creature, DataTypes.Attumen)
	{
		Initialize();
	}

	public override void Reset()
	{
		Initialize();
		base.Reset();
		Me.SetVisible(true);
		Me.ReactState = ReactStates.Defensive;
	}

	public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
	{
		// Midnight never dies, let health fall to 1 and prevent further Damage.
		if (damage >= Me.Health)
			damage = (uint)(Me.Health - 1);

		if (_phase == Phases.None &&
			Me.HealthBelowPctDamaged(95, damage))
		{
			_phase = Phases.AttumenEngages;
			Talk(TextIds.EmoteCallAttumen);
			DoCastAOE(SpellIds.SummonAttumen);
		}
		else if (_phase == Phases.AttumenEngages &&
				Me.HealthBelowPctDamaged(25, damage))
		{
			_phase = Phases.Mounted;
			DoCastAOE(SpellIds.Mount, new CastSpellExtraArgs(true));
		}
	}

	public override void JustSummoned(Creature summon)
	{
		if (summon.Entry == CreatureIds.AttumenUnmounted)
		{
			_attumenGUID = summon.GUID;
			summon.AI.SetGUID(Me.GUID, (int)CreatureIds.Midnight);
			summon.AI.AttackStart(Me.Victim);
			summon.AI.Talk(TextIds.SayAppear);
		}

		base.JustSummoned(summon);
	}

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);

		Scheduler.Schedule(TimeSpan.FromSeconds(15),
							TimeSpan.FromSeconds(25),
							task =>
							{
								DoCastVictim(SpellIds.Knockdown);
								task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
							});
	}

	public override void EnterEvadeMode(EvadeReason why)
	{
		_DespawnAtEvade(TimeSpan.FromSeconds(10));
	}

	public override void KilledUnit(Unit victim)
	{
		if (_phase == Phases.AttumenEngages)
		{
			var unit = Global.ObjAccessor.GetUnit(Me, _attumenGUID);

			if (unit)
				Talk(TextIds.SayMidnightKill, unit);
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim() ||
			_phase == Phases.Mounted)
			return;

		Scheduler.Update(diff, () => DoMeleeAttackIfReady());
	}

	private void Initialize()
	{
		_phase = Phases.None;
	}
}