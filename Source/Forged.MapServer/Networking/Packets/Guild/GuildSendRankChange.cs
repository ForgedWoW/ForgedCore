﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildSendRankChange : ServerPacket
{
	public ObjectGuid Other;
	public ObjectGuid Officer;
	public bool Promote;
	public uint RankID;
	public GuildSendRankChange() : base(ServerOpcodes.GuildSendRankChange) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Officer);
		_worldPacket.WritePackedGuid(Other);
		_worldPacket.WriteUInt32(RankID);

		_worldPacket.WriteBit(Promote);
		_worldPacket.FlushBits();
	}
}