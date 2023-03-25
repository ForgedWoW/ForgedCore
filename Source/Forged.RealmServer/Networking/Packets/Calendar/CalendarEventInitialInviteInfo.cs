﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class CalendarEventInitialInviteInfo
{
	public ObjectGuid InviteGuid;
	public byte Level = 100;

	public CalendarEventInitialInviteInfo(ObjectGuid inviteGuid, byte level)
	{
		InviteGuid = inviteGuid;
		Level = level;
	}
}