﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Trade;

public class SetTradeGold : ClientPacket
{
	public ulong Coinage;
	public SetTradeGold(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Coinage = _worldPacket.ReadUInt64();
	}
}