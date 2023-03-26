﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

public class AllCreaturesWithinRange : ICheck<Creature>
{
    private readonly WorldObject _pObject;
    private readonly float _fRange;

	public AllCreaturesWithinRange(WorldObject obj, float maxRange = 0f)
	{
		_pObject = obj;
		_fRange = maxRange;
	}

	public bool Invoke(Creature creature)
	{
		if (_fRange != 0f)
		{
			if (_fRange > 0.0f && !_pObject.IsWithinDist(creature, _fRange, false))
				return false;

			if (_fRange < 0.0f && _pObject.IsWithinDist(creature, _fRange, false))
				return false;
		}

		return true;
	}
}