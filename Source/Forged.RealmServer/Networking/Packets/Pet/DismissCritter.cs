﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class DismissCritter : ClientPacket
{
	public ObjectGuid CritterGUID;
	public DismissCritter(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CritterGUID = _worldPacket.ReadPackedGuid();
	}
}

//Structs