﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class GameObjectSetStateLocal : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public byte State;
	public GameObjectSetStateLocal() : base(ServerOpcodes.GameObjectSetStateLocal, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
		_worldPacket.WriteUInt8(State);
	}
}