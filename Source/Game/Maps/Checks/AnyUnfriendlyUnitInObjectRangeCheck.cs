﻿using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AnyUnfriendlyUnitInObjectRangeCheck : ICheck<Unit>
{
    public AnyUnfriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, Func<Unit, bool> additionalCheck = null)
    {
        i_obj = obj;
        i_funit = funit;
        i_range = range;
        _additionalCheck = additionalCheck;
    }

    public bool Invoke(Unit u)
    {
        if (u.IsAlive() && i_obj.IsWithinDist(u, i_range) && !i_funit.IsFriendlyTo(u) && (_additionalCheck == null || _additionalCheck.Invoke(u)))
            return true;
        else
            return false;
    }

    readonly WorldObject i_obj;
    readonly Unit i_funit;
    readonly float i_range;
    readonly Func<Unit, bool> _additionalCheck;
}