﻿using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class AllFriendlyUnitsInRange : ICheck<Unit>
{
    public AllFriendlyUnitsInRange(Unit obj, float range)
    {
        unit = obj;
        i_range = range;
    }

    public bool Invoke(Unit u)
    {
        if (!u.IsAlive())
            return false;
        if (!u.IsVisible())
            return false;
        if (!u.IsFriendlyTo(unit))
            return false;

        if (i_range != 0f)
        {
            if (i_range > 0.0f && !unit.IsWithinDist(u, i_range, false))
                return false;
            if (i_range < 0.0f && unit.IsWithinDist(u, i_range, false))
                return false;
        }

        return true;
    }

    readonly Unit unit;
    readonly float i_range;
}