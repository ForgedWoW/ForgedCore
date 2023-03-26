﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Interfaces;
using Forged.MapServer.Phasing;
using Framework.Constants;

namespace Forged.MapServer.Maps.GridNotifiers;

public class PlayerListSearcher : IGridNotifierPlayer
{
    private readonly PhaseShift _phaseShift;
    private readonly List<Unit> _objects;
    private readonly ICheck<Player> _check;

	public GridType GridType { get; set; }

	public PlayerListSearcher(WorldObject searcher, List<Unit> objects, ICheck<Player> check, GridType gridType = GridType.World)
	{
		_phaseShift = searcher.PhaseShift;
		_objects = objects;
		_check = check;
		GridType = gridType;
	}

	public PlayerListSearcher(PhaseShift phaseShift, List<Unit> objects, ICheck<Player> check, GridType gridType = GridType.World)
	{
		_phaseShift = phaseShift;
		_objects = objects;
		_check = check;
		GridType = gridType;
	}

	public void Visit(IList<Player> objs)
	{
		for (var i = 0; i < objs.Count; ++i)
		{
			var player = objs[i];

			if (player != null && player.InSamePhase(_phaseShift))
				if (_check.Invoke(player))
					_objects.Add(player);
		}
	}
}