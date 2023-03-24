﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Maps;

namespace Forged.RealmServer.Scripting.Interfaces.IMap;

public interface IMapOnUpdate<T> : IScriptObject where T : Map
{
	void OnUpdate(T obj, uint diff);
}