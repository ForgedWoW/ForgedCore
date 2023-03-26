﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

internal class NearestAssistCreatureInCreatureRangeCheck : ICheck<Creature>
{
    private readonly Creature _obj;
    private readonly Unit _enemy;
    private float _range;

	public NearestAssistCreatureInCreatureRangeCheck(Creature obj, Unit enemy, float range)
	{
		_obj = obj;
		_enemy = enemy;
		_range = range;
	}

	public bool Invoke(Creature u)
	{
		if (u == _obj)
			return false;

		if (!u.CanAssistTo(_obj, _enemy))
			return false;

		// Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
		if (!_obj.IsWithinDist(u, _range, true, false, false))
			return false;

		if (!_obj.IsWithinLOSInMap(u))
			return false;

		_range = _obj.GetDistance(u); // use found unit range as new range limit for next check

		return true;
	}
}