﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.AI.CoreAI;

public class GuardAI : ScriptedAI.ScriptedAI
{
	public GuardAI(Creature creature) : base(creature) { }

	public override void UpdateAI(uint diff)
	{
		if (!UpdateVictim())
			return;

		DoMeleeAttackIfReady();
	}

	public override bool CanSeeAlways(WorldObject obj)
	{
		var unit = obj.AsUnit;

		if (unit != null)
			if (unit.IsControlledByPlayer && Me.IsEngagedBy(unit))
				return true;

		return false;
	}

	public override void EnterEvadeMode(EvadeReason why)
	{
		if (!Me.IsAlive)
		{
			Me.MotionMaster.MoveIdle();
			Me.CombatStop(true);
			EngagementOver();

			return;
		}

		Log.Logger.Verbose($"GuardAI::EnterEvadeMode: {Me.GUID} enters evade mode.");

		Me.RemoveAllAuras();
		Me.CombatStop(true);
		EngagementOver();

		Me.MotionMaster.MoveTargetedHome();
	}

	public override void JustDied(Unit killer)
	{
		if (killer != null)
		{
			var player = killer.CharmerOrOwnerPlayerOrPlayerItself;

			if (player != null)
				Me.SendZoneUnderAttackMessage(player);
		}
	}
}