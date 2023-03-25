﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Pet;

class PetAbandon : ClientPacket
{
	public ObjectGuid Pet;
	public PetAbandon(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Pet = _worldPacket.ReadPackedGuid();
	}
}