﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class AnyFriendlyUnitInObjectRangeCheck : ICheck<Unit>
{
    public AnyFriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
    {
        i_obj = obj;
        i_funit = funit;
        i_range = range;
        i_playerOnly = playerOnly;
        i_incOwnRadius = incOwnRadius;
        i_incTargetRadius = incTargetRadius;
    }

    public bool Invoke(Unit u)
    {
        if (!u.IsAlive())
            return false;

        float searchRadius = i_range;
        if (i_incOwnRadius)
            searchRadius += i_obj.GetCombatReach();
        if (i_incTargetRadius)
            searchRadius += u.GetCombatReach();

        if (!u.IsInMap(i_obj) || !u.InSamePhase(i_obj) || !u.IsWithinDoubleVerticalCylinder(i_obj, searchRadius, searchRadius))
            return false;

        if (!i_funit.IsFriendlyTo(u))
            return false;

        return !i_playerOnly || u.GetTypeId() == TypeId.Player;
    }

    readonly WorldObject i_obj;
    readonly Unit i_funit;
    readonly float i_range;
    readonly bool i_playerOnly;
    readonly bool i_incOwnRadius;
    readonly bool i_incTargetRadius;
}