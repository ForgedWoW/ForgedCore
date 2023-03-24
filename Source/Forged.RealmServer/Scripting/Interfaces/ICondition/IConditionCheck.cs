﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Conditions;

namespace Forged.RealmServer.Scripting.Interfaces.ICondition;

public interface IConditionCheck : IScriptObject
{
	bool OnConditionCheck(Condition condition, ConditionSourceInfo sourceInfo);
}