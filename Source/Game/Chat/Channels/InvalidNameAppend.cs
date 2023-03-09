﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking.Packets;

namespace Game.Chat;

struct InvalidNameAppend : IChannelAppender
{
	public ChatNotify GetNotificationType() => ChatNotify.InvalidNameNotice;

	public void Append(ChannelNotify data) { }
}