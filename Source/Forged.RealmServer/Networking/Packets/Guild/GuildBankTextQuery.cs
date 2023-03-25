﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public class GuildBankTextQuery : ClientPacket
{
	public int Tab;
	public GuildBankTextQuery(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Tab = _worldPacket.ReadInt32();
	}
}