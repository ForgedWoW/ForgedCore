﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

class CoinRemoved : ServerPacket
{
	public ObjectGuid LootObj;
	public CoinRemoved() : base(ServerOpcodes.CoinRemoved) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(LootObj);
	}
}