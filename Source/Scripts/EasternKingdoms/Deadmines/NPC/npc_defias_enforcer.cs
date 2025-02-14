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
	48502, 49850
})]
public class npc_defias_enforcer : ScriptedAI
{
	public uint BloodBathTimer;
	public uint RecklessnessTimer;

	public npc_defias_enforcer(Creature creature) : base(creature) { }

	public override void Reset()
	{
		BloodBathTimer = 8000;
		RecklessnessTimer = 13000;
	}

	public override void JustEnteredCombat(Unit who)
	{
		var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

		if (target != null)
			DoCast(target, boss_vanessa_vancleef.Spells.CHARGE);
	}

	public override void UpdateAI(uint diff)
	{
		if (BloodBathTimer <= diff)
		{
			DoCastVictim(boss_vanessa_vancleef.Spells.BLOODBATH);
			BloodBathTimer = RandomHelper.URand(8000, 11000);
		}
		else
		{
			BloodBathTimer -= diff;
		}

		if (RecklessnessTimer <= diff)
		{
			DoCast(Me, boss_vanessa_vancleef.Spells.BLOODBATH);
			RecklessnessTimer = 20000;
		}
		else
		{
			RecklessnessTimer -= diff;
		}

		DoMeleeAttackIfReady();
	}
}