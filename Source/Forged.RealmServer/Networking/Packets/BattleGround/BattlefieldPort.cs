﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

class BattlefieldPort : ClientPacket
{
	public RideTicket Ticket = new();
	public bool AcceptedInvite;
	public BattlefieldPort(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Ticket.Read(_worldPacket);
		AcceptedInvite = _worldPacket.HasBit();
	}
}