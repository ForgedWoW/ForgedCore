﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.GameObject;

class GameObjectDespawn : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public GameObjectDespawn() : base(ServerOpcodes.GameObjectDespawn) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
	}
}