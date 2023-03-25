﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

struct InvalidNameAppend : IChannelAppender
{
	public ChatNotify GetNotificationType() => ChatNotify.InvalidNameNotice;

	public void Append(ChannelNotify data) { }
}