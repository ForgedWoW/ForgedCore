﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Query;

public class QueryCorpseLocationFromClient : ClientPacket
{
	public ObjectGuid Player;
	public QueryCorpseLocationFromClient(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Player = _worldPacket.ReadPackedGuid();
	}
}