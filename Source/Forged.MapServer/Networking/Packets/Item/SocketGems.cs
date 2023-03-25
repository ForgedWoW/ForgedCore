﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

class SocketGems : ClientPacket
{
	public ObjectGuid ItemGuid;
	public ObjectGuid[] GemItem = new ObjectGuid[ItemConst.MaxGemSockets];
	public SocketGems(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ItemGuid = _worldPacket.ReadPackedGuid();

		for (var i = 0; i < ItemConst.MaxGemSockets; ++i)
			GemItem[i] = _worldPacket.ReadPackedGuid();
	}
}