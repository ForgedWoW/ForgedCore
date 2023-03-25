﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Taxi;

class ActivateTaxiReplyPkt : ServerPacket
{
	public ActivateTaxiReply Reply;
	public ActivateTaxiReplyPkt() : base(ServerOpcodes.ActivateTaxiReply) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Reply, 4);
		_worldPacket.FlushBits();
	}
}