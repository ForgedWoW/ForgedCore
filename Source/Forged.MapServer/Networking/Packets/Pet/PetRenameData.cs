﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Networking.Packets.Pet;

struct PetRenameData
{
	public ObjectGuid PetGUID;
	public int PetNumber;
	public string NewName;
	public bool HasDeclinedNames;
	public DeclinedName DeclinedNames;
}