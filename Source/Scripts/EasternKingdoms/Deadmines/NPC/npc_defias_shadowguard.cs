﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(new uint[]
{
	48505, 49852
})]
public class npc_defias_shadowguard : ScriptedAI
{
	public uint SinisterTimer;
	public uint WhirlingBladesTimer;
	public uint ShadowstepTimer;

	public bool Below;

	public npc_defias_shadowguard(Creature creature) : base(creature) { }

	public override void Reset()
	{
		SinisterTimer = 2000;
		WhirlingBladesTimer = 6400;
		ShadowstepTimer = 6000;
		Below = false;
		Me.SetPower(PowerType.Energy, 100);
		Me.SetMaxPower(PowerType.Energy, 100);
		Me.SetPowerType(PowerType.Energy);
	}

	public override void UpdateAI(uint diff)
	{
		if (SinisterTimer <= diff)
		{
			DoCastVictim(boss_vanessa_vancleef.Spells.SINISTER);
			SinisterTimer = RandomHelper.URand(5000, 7000);
		}
		else
		{
			SinisterTimer -= diff;
		}

		if (WhirlingBladesTimer <= diff)
		{
			DoCast(Me, boss_vanessa_vancleef.Spells.BLADES);
			WhirlingBladesTimer = RandomHelper.URand(6400, 8200);
		}
		else
		{
			WhirlingBladesTimer -= diff;
		}

		if (HealthBelowPct(35) && !Below)
		{
			DoCast(Me, boss_vanessa_vancleef.Spells.EVASION);
			Below = true;
		}

		if (ShadowstepTimer <= diff)
		{
			var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

			if (target != null)
				DoCast(target, boss_vanessa_vancleef.Spells.SHADOWSTEP);

			ShadowstepTimer = RandomHelper.URand(6400, 8200);
		}
		else
		{
			ShadowstepTimer -= diff;
		}

		DoMeleeAttackIfReady();
	}
}