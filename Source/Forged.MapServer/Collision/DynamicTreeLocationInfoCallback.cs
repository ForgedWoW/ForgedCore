﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Collision.Maps;
using Forged.MapServer.Collision.Models;
using Forged.MapServer.Phasing;

namespace Forged.MapServer.Collision;

public class DynamicTreeLocationInfoCallback : WorkerCallback
{
	readonly PhaseShift _phaseShift;
	readonly LocationInfo _locationInfo = new();
	GameObjectModel _hitModel = new();

	public DynamicTreeLocationInfoCallback(PhaseShift phaseShift)
	{
		_phaseShift = phaseShift;
	}

	public override void Invoke(Vector3 p, GameObjectModel obj)
	{
		if (obj.GetLocationInfo(p, _locationInfo, _phaseShift))
			_hitModel = obj;
	}

	public LocationInfo GetLocationInfo()
	{
		return _locationInfo;
	}

	public GameObjectModel GetHitModel()
	{
		return _hitModel;
	}
}