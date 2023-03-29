﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Entities;

internal class ConversationActorFillVisitor
{
    private readonly Conversation _conversation;
    private readonly Unit _creator;
    private readonly Map _map;
    private readonly ConversationActorTemplate _actor;

    public ConversationActorFillVisitor(Conversation conversation, Unit creator, Map map, ConversationActorTemplate actor)
    {
        _conversation = conversation;
        _creator = creator;
        _map = map;
        _actor = actor;
    }

    public void Invoke(ConversationActorTemplate template)
    {
        if (template.WorldObjectTemplate == null)
            Invoke(template.WorldObjectTemplate);

        if (template.NoObjectTemplate == null)
            Invoke(template.NoObjectTemplate);

        if (template.ActivePlayerTemplate == null)
            Invoke(template.ActivePlayerTemplate);

        if (template.TalkingHeadTemplate == null)
            Invoke(template.TalkingHeadTemplate);
    }

    public void Invoke(ConversationActorWorldObjectTemplate worldObject)
    {
        Creature bestFit = null;

        foreach (var creature in _map.CreatureBySpawnIdStore.LookupByKey(worldObject.SpawnId))
        {
            bestFit = creature;

            // If creature is in a personal phase then we pick that one specifically
            if (creature.Location.PhaseShift.PersonalGuid == _creator.GUID)
                break;
        }

        if (bestFit)
            _conversation.AddActor(_actor.Id, _actor.Index, bestFit.GUID);
    }

    public void Invoke(ConversationActorNoObjectTemplate noObject)
    {
        _conversation.AddActor(_actor.Id, _actor.Index, ConversationActorType.WorldObject, noObject.CreatureId, noObject.CreatureDisplayInfoId);
    }

    public void Invoke(ConversationActorActivePlayerTemplate activePlayer)
    {
        _conversation.AddActor(_actor.Id, _actor.Index, ObjectGuid.Create(HighGuid.Player, 0xFFFFFFFFFFFFFFFF));
    }

    public void Invoke(ConversationActorTalkingHeadTemplate talkingHead)
    {
        _conversation.AddActor(_actor.Id, _actor.Index, ConversationActorType.TalkingHead, talkingHead.CreatureId, talkingHead.CreatureDisplayInfoId);
    }
}