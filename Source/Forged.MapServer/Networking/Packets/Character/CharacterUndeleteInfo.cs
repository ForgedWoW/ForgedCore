﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharacterUndeleteInfo
{
	// User specified variables
	public ObjectGuid CharacterGuid; // Guid of the character to restore
	public int ClientToken = 0;      // @todo: research

	// Server side data
	public string Name;
}