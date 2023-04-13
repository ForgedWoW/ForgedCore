﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

internal class NearestGameObjectFishingHole : ICheck<GameObject>
{
    private readonly WorldObject _obj;
    private float _range;

    public NearestGameObjectFishingHole(WorldObject obj, float range)
    {
        _obj = obj;
        _range = range;
    }

    public bool Invoke(GameObject go)
    {
        if (go.Template.type != GameObjectTypes.FishingHole || !go.IsSpawned || !_obj.Location.IsWithinDist(go, _range) || !_obj.Location.IsWithinDist(go, go.Template.FishingHole.radius))
            return false;

        _range = _obj.Location.GetDistance(go);

        return true;

    }
}