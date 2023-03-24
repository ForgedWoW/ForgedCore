﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.AreaTriggers;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.AI;

public class SmartAreaTriggerAI : AreaTriggerAI
{
	readonly SmartScript _script = new();

	public SmartAreaTriggerAI(AreaTrigger areaTrigger) : base(areaTrigger) { }

	public override void OnInitialize()
	{
		GetScript().OnInitialize(At);
	}

	public override void OnUpdate(uint diff)
	{
		GetScript().OnUpdate(diff);
	}

	public override void OnUnitEnter(Unit unit)
	{
		GetScript().ProcessEventsFor(SmartEvents.AreatriggerOntrigger, unit);
	}

	public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker)
	{
		GetScript().SetTimedActionList(e, entry, invoker);
	}

	public SmartScript GetScript()
	{
		return _script;
	}
}