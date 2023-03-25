﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Channel;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

struct PlayerNotFoundAppend : IChannelAppender
{
	public PlayerNotFoundAppend(string playerName)
	{
		_playerName = playerName;
	}

	public ChatNotify GetNotificationType() => ChatNotify.PlayerNotFoundNotice;

	public void Append(ChannelNotify data)
	{
		data.Sender = _playerName;
	}

	readonly string _playerName;
}