﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.AI;
using Game.Entities;

namespace Game.Scripting.Interfaces.IGameObject;

public interface IGameObjectGetAI : IScriptObject
{
	GameObjectAI GetAI(GameObject go);
}