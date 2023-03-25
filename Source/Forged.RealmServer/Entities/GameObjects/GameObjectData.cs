﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;
using Forged.RealmServer.Maps;

namespace Forged.RealmServer.Entities;
// From `gameobject_template_addon`, `gameobject_overrides`

public class GameObjectData : SpawnData
{
	public Quaternion Rotation;
	public uint Animprogress;
	public GameObjectState GoState;
	public uint ArtKit;

	public GameObjectData() : base(SpawnObjectType.GameObject) { }
}