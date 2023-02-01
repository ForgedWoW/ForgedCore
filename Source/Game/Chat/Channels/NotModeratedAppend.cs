﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking.Packets;

namespace Game.Chat
{
    internal struct NotModeratedAppend : IChannelAppender
    {
        public ChatNotify GetNotificationType()
        {
            return ChatNotify.NotModeratedNotice;
        }

        public void Append(ChannelNotify data)
        {
        }
    }
}