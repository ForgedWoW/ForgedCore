﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Shaman;

//NPC ID : 106321
[CreatureScript(106321)]
public class npc_tailwind_totem : ScriptedAI
{
	public npc_tailwind_totem(Creature creature) : base(creature) { }

	public override void Reset()
	{
		var time = TimeSpan.FromSeconds(1);

		Me.Events.AddRepeatEventAtOffset(() =>
										{
											Me.CastSpell(Me, TotemSpells.TOTEM_TAIL_WIND_EFFECT, true);

											return time;
										},
										time);
	}
}