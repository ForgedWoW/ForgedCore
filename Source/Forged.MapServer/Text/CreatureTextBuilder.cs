﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Text;

public class CreatureTextBuilder : MessageBuilder
{
    private readonly Gender _gender;
    private readonly Language _language;
    private readonly ChatMsg _msgType;
    private readonly WorldObject _source;
    private readonly WorldObject _target;
    private readonly byte _textGroup;
    private readonly uint _textId;
    private readonly CreatureTextManager _textMgr;

    public CreatureTextBuilder(WorldObject obj, Gender gender, ChatMsg msgtype, byte textGroup, uint id, Language language, WorldObject target)
    {
        _source = obj;
        _gender = gender;
        _msgType = msgtype;
        _textGroup = textGroup;
        _textId = id;
        _language = language;
        _target = target;
        _textMgr = obj.ClassFactory.Resolve<CreatureTextManager>();
    }

    public override ChatPacketSender Invoke(Locale locale = Locale.enUS)
    {
        var text = _textMgr.GetLocalizedChatString(_source.Entry, _gender, _textGroup, _textId, locale);

        return new ChatPacketSender(_msgType, _language, _source, _target, text, 0, locale);
    }
}