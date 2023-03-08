// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.EasternKingdoms.BaradinHold.Occuthar;

internal struct SpellIds
{
	public const uint SearingShadows = 96913;
	public const uint FocusedFireFirstDamage = 97212;
	public const uint FocusedFireTrigger = 96872;
	public const uint FocusedFireVisual = 96886;
	public const uint FocusedFire = 96884;
	public const uint EyesOfOccuthar = 96920;
	public const uint GazeOfOccuthar = 96942;
	public const uint OccutharsDestuction = 96968;
	public const uint Berserk = 47008;
}

internal struct EventIds
{
	public const uint SearingShadows = 1;
	public const uint FocusedFire = 2;
	public const uint EyesOfOccuthar = 3;
	public const uint Berserk = 4;

	public const uint FocusedFireFirstDamage = 1;
}

internal struct MiscConst
{
	public const uint MaxOccutharVehicleSeats = 7;
}

[Script]
internal class boss_occuthar : BossAI
{
	private readonly Vehicle _vehicle;

	public boss_occuthar(Creature creature) : base(creature, DataTypes.Occuthar)
	{
		_vehicle = me.GetVehicleKit();
		Cypher.Assert(_vehicle != null);
	}

	public override void JustEngagedWith(Unit who)
	{
		base.JustEngagedWith(who);
		instance.SendEncounterUnit(EncounterFrameType.Engage, me);
		_events.ScheduleEvent(EventIds.SearingShadows, TimeSpan.FromSeconds(8));
		_events.ScheduleEvent(EventIds.FocusedFire, TimeSpan.FromSeconds(15));
		_events.ScheduleEvent(EventIds.EyesOfOccuthar, TimeSpan.FromSeconds(30));
		_events.ScheduleEvent(EventIds.Berserk, TimeSpan.FromMinutes(5));
	}

	public override void EnterEvadeMode(EvadeReason why)
	{
		base.EnterEvadeMode(why);
		instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
		_DespawnAtEvade();
	}

	public override void JustDied(Unit killer)
	{
		_JustDied();
		instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
	}

	public override void JustSummoned(Creature summon)
	{
		summons.Summon(summon);

		if (summon.Entry == CreatureIds.FocusFireDummy)
		{
			DoCast(summon, SpellIds.FocusedFire);

			for (sbyte i = 0; i < MiscConst.MaxOccutharVehicleSeats; ++i)
			{
				var vehicle = _vehicle.GetPassenger(i);

				if (vehicle)
					vehicle.CastSpell(summon, SpellIds.FocusedFireVisual);
			}
		}
	}

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		_events.Update(diff);

		if (me.HasUnitState(UnitState.Casting))
			return;

		_events.ExecuteEvents(eventId =>
		{
			switch (eventId)
			{
				case EventIds.SearingShadows:
					DoCastAOE(SpellIds.SearingShadows);
					_events.ScheduleEvent(EventIds.SearingShadows, TimeSpan.FromSeconds(25));

					break;
				case EventIds.FocusedFire:
					DoCastAOE(SpellIds.FocusedFireTrigger, new CastSpellExtraArgs(true));
					_events.ScheduleEvent(EventIds.FocusedFire, TimeSpan.FromSeconds(15));

					break;
				case EventIds.EyesOfOccuthar:
					DoCastAOE(SpellIds.EyesOfOccuthar);
					_events.RescheduleEvent(EventIds.FocusedFire, TimeSpan.FromSeconds(15));
					_events.ScheduleEvent(EventIds.EyesOfOccuthar, TimeSpan.FromSeconds(60));

					break;
				case EventIds.Berserk:
					DoCast(me, SpellIds.Berserk, new CastSpellExtraArgs(true));

					break;
				default:
					break;
			}
		});

		DoMeleeAttackIfReady();
	}
}

[Script]
internal class npc_eyestalk : ScriptedAI
{
	private readonly InstanceScript _instance;
	private byte _damageCount;

	public npc_eyestalk(Creature creature) : base(creature)
	{
		_instance = creature.GetInstanceScript();
	}

	public override void IsSummonedBy(WorldObject summoner)
	{
		// player is the spellcaster so register summon manually
		var occuthar = ObjectAccessor.GetCreature(me, _instance.GetGuidData(DataTypes.Occuthar));

		occuthar?.GetAI().JustSummoned(me);
	}

	public override void Reset()
	{
		_events.Reset();
		_events.ScheduleEvent(EventIds.FocusedFireFirstDamage, TimeSpan.FromSeconds(0));
	}

	public override void UpdateAI(uint diff)
	{
		_events.Update(diff);

		if (_events.ExecuteEvent() == EventIds.FocusedFireFirstDamage)
		{
			DoCastAOE(SpellIds.FocusedFireFirstDamage);

			if (++_damageCount < 2)
				_events.ScheduleEvent(EventIds.FocusedFireFirstDamage, TimeSpan.FromSeconds(1));
		}
	}

	public override void EnterEvadeMode(EvadeReason why) { } // Never evade
}

[Script] // 96872 - Focused Fire
internal class spell_occuthar_focused_fire_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		if (targets.Count < 2)
			return;

		targets.RemoveAll(target => Caster.Victim == target);

		if (targets.Count >= 2)
			targets.RandomResize(1);
	}
}

[Script] // Id - 96931 Eyes of Occu'thar
internal class spell_occuthar_eyes_of_occuthar_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return !spellInfo.Effects.Empty() && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
	}

	public override bool Load()
	{
		return Caster.IsPlayer;
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		if (targets.Empty())
			return;

		targets.RandomResize(1);
	}

	private void HandleScript(int effIndex)
	{
		HitUnit.CastSpell(Caster, (uint)EffectValue, true);
	}
}

[Script] // Id - 96932 Eyes of Occu'thar
internal class spell_occuthar_eyes_of_occuthar_vehicle_SpellScript : SpellScript, ISpellAfterHit
{
	public override bool Load()
	{
		var instance = Caster.GetMap().ToInstanceMap();

		if (instance != null)
			return instance.GetScriptName() == nameof(instance_baradin_hold);

		return false;
	}

	public void AfterHit()
	{
		Position pos = HitUnit.Location;

		var occuthar = ObjectAccessor.GetCreature(Caster, Caster.GetInstanceScript().GetGuidData(DataTypes.Occuthar));

		if (occuthar != null)
		{
			Creature creature = occuthar.SummonCreature(CreatureIds.EyeOfOccuthar, pos);

			creature?.CastSpell(HitUnit, SpellIds.GazeOfOccuthar, false);
		}
	}
}

[Script] // 96942 / 101009 - Gaze of Occu'thar
internal class spell_occuthar_occuthars_destruction_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Load()
	{
		return Caster && Caster.TypeId == TypeId.Unit;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 2, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = Caster;

		if (caster)
		{
			if (IsExpired)
				caster.CastSpell((WorldObject)null, SpellIds.OccutharsDestuction, new CastSpellExtraArgs(aurEff));

			caster.
			AsCreature.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
		}
	}
}