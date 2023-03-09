﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

class UseToy : ClientPacket
{
	public SpellCastRequest Cast = new();
	public UseToy(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Cast.Read(_worldPacket);
	}
}