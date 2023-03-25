﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Text;
using Framework.Constants;

namespace Forged.MapServer.Chat.Channels;

class ChannelUserlistUpdateBuilder : MessageBuilder
{
	readonly Channel _source;
	readonly ObjectGuid _guid;

	public ChannelUserlistUpdateBuilder(Channel source, ObjectGuid guid)
	{
		_source = source;
		_guid = guid;
	}

	public override PacketSenderOwning<UserlistUpdate> Invoke(Locale locale = Locale.enUS)
	{
		var localeIdx = Global.WorldMgr.GetAvailableDbcLocale(locale);

		PacketSenderOwning<UserlistUpdate> userlistUpdate = new()
        {
            Data =
            {
                UpdatedUserGUID = _guid,
                ChannelFlags = _source.GetFlags(),
                UserFlags = _source.GetPlayerFlags(_guid),
                ChannelID = _source.GetChannelId(),
                ChannelName = _source.GetName(localeIdx)
            }
        };

        return userlistUpdate;
	}
}