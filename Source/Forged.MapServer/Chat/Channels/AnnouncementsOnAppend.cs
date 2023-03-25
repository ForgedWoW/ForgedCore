﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

struct AnnouncementsOnAppend : IChannelAppender
{
	public AnnouncementsOnAppend(ObjectGuid guid)
	{
		_guid = guid;
	}

	public ChatNotify GetNotificationType() => ChatNotify.AnnouncementsOnNotice;

	public void Append(ChannelNotify data)
	{
		data.SenderGuid = _guid;
	}

	readonly ObjectGuid _guid;
}