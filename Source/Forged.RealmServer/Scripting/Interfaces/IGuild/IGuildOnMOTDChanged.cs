﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Guilds;

namespace Forged.RealmServer.Scripting.Interfaces.IGuild;

public interface IGuildOnMOTDChanged : IScriptObject
{
	void OnMOTDChanged(Guild guild, string newMotd);
}