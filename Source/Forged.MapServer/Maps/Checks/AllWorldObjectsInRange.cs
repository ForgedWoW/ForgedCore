﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

public class AllWorldObjectsInRange : ICheck<WorldObject>
{
    private readonly WorldObject _pObject;
    private readonly float _fRange;

    public AllWorldObjectsInRange(WorldObject obj, float maxRange)
    {
        _pObject = obj;
        _fRange = maxRange;
    }

    public bool Invoke(WorldObject go)
    {
        return _pObject.Location.IsWithinDist(go, _fRange, false) && _pObject.Location.InSamePhase(go);
    }
}