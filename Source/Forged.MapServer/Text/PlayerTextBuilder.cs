﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Chat;
using Framework.Constants;

namespace Forged.MapServer.Text;

public class PlayerTextBuilder : MessageBuilder
{
    private readonly Gender _gender;
    private readonly Language _language;
    private readonly ChatMsg _msgType;
    private readonly WorldObject _source;
    private readonly WorldObject _talker;
    private readonly WorldObject _target;
    private readonly byte _textGroup;
    private readonly uint _textId;
    private readonly CreatureTextManager _textManager;

    public PlayerTextBuilder(WorldObject obj, WorldObject speaker, Gender gender, ChatMsg msgtype, byte textGroup, uint id, Language language, WorldObject target)
    {
        _source = obj;
        _gender = gender;
        _talker = speaker;
        _msgType = msgtype;
        _textGroup = textGroup;
        _textId = id;
        _language = language;
        _target = target;
        _textManager = obj.ClassFactory.Resolve<CreatureTextManager>();
    }

    public override PacketSenderOwning<ChatPkt> Invoke(Locale locIdx = Locale.enUS)
    {
        var text = _textManager.GetLocalizedChatString(_source.Entry, _gender, _textGroup, _textId, locIdx);
        PacketSenderOwning<ChatPkt> chat = new();
        chat.Data.Initialize(_msgType, _language, _talker, _target, text, 0, "", locIdx);

        return chat;
    }
}