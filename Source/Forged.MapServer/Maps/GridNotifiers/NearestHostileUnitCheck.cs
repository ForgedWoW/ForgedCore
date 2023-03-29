﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class NearestHostileUnitCheck : ICheck<Unit>
{
    private readonly Creature _me;
    private readonly bool _playerOnly;
    private float _range;

    public NearestHostileUnitCheck(Creature creature, float dist = 0, bool playerOnly = false)
    {
        _me = creature;
        _playerOnly = playerOnly;

        _range = (dist == 0 ? 9999 : dist);
    }

    public bool Invoke(Unit u)
    {
        if (!_me.Location.IsWithinDist(u, _range))
            return false;

        if (!_me.WorldObjectCombat.IsValidAttackTarget(u))
            return false;

        if (_playerOnly && !u.IsTypeId(TypeId.Player))
            return false;

        _range = _me.Location.GetDistance(u); // use found unit range as new range limit for next check

        return true;
    }
}