﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

class CalendarInviteStatusPacket : ServerPacket
{
	public CalendarFlags Flags;
	public ulong EventID;
	public CalendarInviteStatus Status;
	public bool ClearPending;
	public long ResponseTime;
	public long Date;
	public ObjectGuid InviteGuid;
	public CalendarInviteStatusPacket() : base(ServerOpcodes.CalendarInviteStatus) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(InviteGuid);
		_worldPacket.WriteUInt64(EventID);
		_worldPacket.WritePackedTime(Date);
		_worldPacket.WriteUInt32((uint)Flags);
		_worldPacket.WriteUInt8((byte)Status);
		_worldPacket.WritePackedTime(ResponseTime);

		_worldPacket.WriteBit(ClearPending);
		_worldPacket.FlushBits();
	}
}