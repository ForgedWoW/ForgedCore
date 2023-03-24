﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Scripting.Interfaces.IWorld;

public interface IWorldOnMotdChange : IScriptObject
{
	void OnMotdChange(string newMotd);
}