﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Guilds;
using Forged.MapServer.Mails;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Calendar;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Calendar;

public class CalendarManager
{
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly List<CalendarEvent> _events;
    private readonly List<ulong> _freeEventIds = new();
    private readonly List<ulong> _freeInviteIds = new();
    private readonly GuildManager _guildManager;
    private readonly ClassFactory _classFactory;
    private readonly MultiMap<ulong, CalendarInvite> _invites;
    private readonly ObjectAccessor _objectAccessor;
    private ulong _maxEventId;
    private ulong _maxInviteId;

    public CalendarManager(CharacterDatabase characterDatabase, CharacterCache characterCache, ObjectAccessor objectAccessor, GuildManager guildManager, ClassFactory classFactory)
    {
        _characterDatabase = characterDatabase;
        _characterCache = characterCache;
        _objectAccessor = objectAccessor;
        _guildManager = guildManager;
        _classFactory = classFactory;
        _events = new List<CalendarEvent>();
        _invites = new MultiMap<ulong, CalendarInvite>();
    }

    public void AddEvent(CalendarEvent calendarEvent, CalendarSendEventType sendType)
    {
        _events.Add(calendarEvent);
        UpdateEvent(calendarEvent);
        SendCalendarEvent(calendarEvent.OwnerGuid, calendarEvent, sendType);
    }

    public void AddInvite(CalendarEvent calendarEvent, CalendarInvite invite, SQLTransaction trans = null)
    {
        if (!calendarEvent.IsGuildAnnouncement && calendarEvent.OwnerGuid != invite.InviteeGuid)
            SendCalendarEventInvite(invite);

        if (!calendarEvent.IsGuildEvent || invite.InviteeGuid == calendarEvent.OwnerGuid)
            SendCalendarEventInviteAlert(calendarEvent, invite);

        if (calendarEvent.IsGuildAnnouncement)
            return;

        _invites.Add(invite.EventId, invite);
        UpdateInvite(invite, trans);
    }

    public void DeleteOldEvents()
    {
        var oldEventsTime = GameTime.CurrentTime - SharedConst.CalendarOldEventsDeletionTime;

        foreach (var calendarEvent in _events.Where(calendarEvent => calendarEvent.Date < oldEventsTime))
            RemoveEvent(calendarEvent, ObjectGuid.Empty);
    }

    public void FreeInviteId(ulong id)
    {
        if (id == _maxInviteId)
            --_maxInviteId;
        else
            _freeInviteIds.Add(id);
    }

    public CalendarEvent GetEvent(ulong eventId)
    {
        foreach (var calendarEvent in _events.Where(calendarEvent => calendarEvent.EventId == eventId))
            return calendarEvent;

        Log.Logger.Debug("CalendarMgr:GetEvent: {0} not found!", eventId);

        return null;
    }

    public List<CalendarInvite> GetEventInvites(ulong eventId)
    {
        return _invites[eventId];
    }

    public List<CalendarEvent> GetEventsCreatedBy(ObjectGuid guid, bool includeGuildEvents = false)
    {
        return _events.Where(calendarEvent => calendarEvent.OwnerGuid == guid &&
                                              (includeGuildEvents ||
                                               (!calendarEvent.IsGuildEvent && !calendarEvent.IsGuildAnnouncement)))
                      .ToList();
    }

    public ulong GetFreeEventId()
    {
        if (_freeEventIds.Empty())
            return ++_maxEventId;

        var eventId = _freeEventIds.FirstOrDefault();
        _freeEventIds.RemoveAt(0);

        return eventId;
    }

    public ulong GetFreeInviteId()
    {
        if (_freeInviteIds.Empty())
            return ++_maxInviteId;

        var inviteId = _freeInviteIds.FirstOrDefault();
        _freeInviteIds.RemoveAt(0);

        return inviteId;
    }

    public List<CalendarEvent> GetGuildEvents(ulong guildId)
    {
        List<CalendarEvent> result = new();

        if (guildId == 0)
            return result;

        result.AddRange(_events.Where(calendarEvent => calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement)
                               .Where(calendarEvent => calendarEvent.GuildId == guildId));

        return result;
    }

    public CalendarInvite GetInvite(ulong inviteId)
    {
        foreach (var calendarEvent in _invites.Values)
            if (calendarEvent.InviteId == inviteId)
                return calendarEvent;

        Log.Logger.Debug("CalendarMgr:GetInvite: {0} not found!", inviteId);

        return null;
    }

    public List<CalendarEvent> GetPlayerEvents(ObjectGuid guid)
    {
        List<CalendarEvent> events = new();

        foreach (var pair in _invites.KeyValueList)
            if (pair.Value.InviteeGuid == guid)
            {
                var evnt = GetEvent(pair.Key);

                if (evnt != null) // null check added as attempt to fix #11512
                    events.Add(evnt);
            }

        var player = _objectAccessor.FindPlayer(guid);

        if (player?.GuildId == 0)
            return events;

        events.AddRange(_events.Where(calendarEvent => player != null && calendarEvent.GuildId == player.GuildId));

        return events;
    }

    public List<CalendarInvite> GetPlayerInvites(ObjectGuid guid)
    {
        return _invites.Values.Where(calendarEvent => calendarEvent.InviteeGuid == guid).ToList();
    }

    public uint GetPlayerNumPending(ObjectGuid guid)
    {
        return (uint)GetPlayerInvites(guid).Count(calendarEvent => calendarEvent.Status is CalendarInviteStatus.Invited or CalendarInviteStatus.Tentative or CalendarInviteStatus.NotSignedUp);
    }

    public void LoadFromDB()
    {
        var oldMSTime = Time.MSTime;

        uint count = 0;
        _maxEventId = 0;
        _maxInviteId = 0;

        //                                              0        1      2      3            4          5          6     7      8
        var result = _characterDatabase.Query("SELECT EventID, Owner, Title, Description, EventType, TextureID, Date, Flags, LockDate FROM calendar_events");

        if (!result.IsEmpty())
            do
            {
                var eventID = result.Read<ulong>(0);
                var ownerGUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1));
                var title = result.Read<string>(2);
                var description = result.Read<string>(3);
                var type = (CalendarEventType)result.Read<byte>(4);
                var textureID = result.Read<int>(5);
                var date = result.Read<long>(6);
                var flags = (CalendarFlags)result.Read<uint>(7);
                var lockDate = result.Read<long>(8);
                ulong guildID = 0;

                if (flags.HasAnyFlag(CalendarFlags.GuildEvent) || flags.HasAnyFlag(CalendarFlags.WithoutInvites))
                    guildID = _characterCache.GetCharacterGuildIdByGuid(ownerGUID);

                CalendarEvent calendarEvent = new(eventID, ownerGUID, guildID, type, textureID, date, flags, title, description, lockDate);
                _events.Add(calendarEvent);

                _maxEventId = Math.Max(_maxEventId, eventID);

                ++count;
            } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} calendar events in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        count = 0;
        oldMSTime = Time.MSTime;

        //                                    0         1        2        3       4       5             6               7
        result = _characterDatabase.Query("SELECT InviteID, EventID, Invitee, Sender, Status, ResponseTime, ModerationRank, Note FROM calendar_invites");

        if (!result.IsEmpty())
            do
            {
                var inviteId = result.Read<ulong>(0);
                var eventId = result.Read<ulong>(1);
                var invitee = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2));
                var senderGUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3));
                var status = (CalendarInviteStatus)result.Read<byte>(4);
                var responseTime = result.Read<long>(5);
                var rank = (CalendarModerationRank)result.Read<byte>(6);
                var note = result.Read<string>(7);

                CalendarInvite invite = new(inviteId, eventId, invitee, senderGUID, responseTime, status, rank, note);
                _invites.Add(eventId, invite);

                _maxInviteId = Math.Max(_maxInviteId, inviteId);

                ++count;
            } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} calendar invites in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

        for (ulong i = 1; i < _maxEventId; ++i)
            if (GetEvent(i) == null)
                _freeEventIds.Add(i);

        for (ulong i = 1; i < _maxInviteId; ++i)
            if (GetInvite(i) == null)
                _freeInviteIds.Add(i);
    }

    public void RemoveAllPlayerEventsAndInvites(ObjectGuid guid)
    {
        foreach (var calendarEvent in _events.Where(calendarEvent => calendarEvent.OwnerGuid == guid))
            RemoveEvent(calendarEvent.EventId, ObjectGuid.Empty); // don't send mail if removing a character

        var playerInvites = GetPlayerInvites(guid);

        foreach (var calendarInvite in playerInvites)
            RemoveInvite(calendarInvite.InviteId, calendarInvite.EventId, guid);
    }

    public void RemoveEvent(ulong eventId, ObjectGuid remover)
    {
        var calendarEvent = GetEvent(eventId);

        if (calendarEvent == null)
        {
            SendCalendarCommandResult(remover, CalendarError.EventInvalid);

            return;
        }

        RemoveEvent(calendarEvent, remover);
    }

    public void RemoveInvite(ulong inviteId, ulong eventId, ObjectGuid remover)
    {
        var calendarEvent = GetEvent(eventId);

        if (calendarEvent == null)
            return;

        var calendarInvite = _invites[eventId].FirstOrDefault(invite => invite.InviteId == inviteId);

        if (calendarInvite == null)
            return;

        SQLTransaction trans = new();
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CALENDAR_INVITE);
        stmt.AddValue(0, calendarInvite.InviteId);
        trans.Append(stmt);
        _characterDatabase.CommitTransaction(trans);

        if (!calendarEvent.IsGuildEvent)
            SendCalendarEventInviteRemoveAlert(calendarInvite.InviteeGuid, calendarEvent, CalendarInviteStatus.Removed);

        SendCalendarEventInviteRemove(calendarEvent, calendarInvite, (uint)calendarEvent.Flags);

        // we need to find out how to use CALENDAR_INVITE_REMOVED_MAIL_SUBJECT to force client to display different mail
        //if (itr._invitee != remover)
        //    MailDraft(calendarEvent.BuildCalendarMailSubject(remover), calendarEvent.BuildCalendarMailBody())
        //        .SendMailTo(trans, MailReceiver(itr.GetInvitee()), calendarEvent, MAIL_CHECK_MASK_COPIED);

        _invites.Remove(eventId, calendarInvite);
    }

    public void RemovePlayerGuildEventsAndSignups(ObjectGuid guid, ulong guildId)
    {
        foreach (var calendarEvent in _events.Where(calendarEvent => calendarEvent.OwnerGuid == guid && (calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement)).ToList())
            RemoveEvent(calendarEvent.EventId, guid);

        var playerInvites = GetPlayerInvites(guid);

        foreach (var playerCalendarEvent in playerInvites)
        {
            var calendarEvent = GetEvent(playerCalendarEvent.EventId);

            if (calendarEvent == null)
                continue;

            if (calendarEvent.IsGuildEvent && calendarEvent.GuildId == guildId)
                RemoveInvite(playerCalendarEvent.InviteId, playerCalendarEvent.EventId, guid);
        }
    }

    public void SendCalendarClearPendingAction(ObjectGuid guid)
    {
        _objectAccessor.FindPlayer(guid)?.SendPacket(new CalendarClearPendingAction());
    }

    public void SendCalendarCommandResult(ObjectGuid guid, CalendarError err, string param = null)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player == null)
            return;

        CalendarCommandResult packet = new()
        {
            Command = 1, // FIXME
            Result = err
        };

        packet.Name = err switch
        {
            CalendarError.OtherInvitesExceeded   => param,
            CalendarError.AlreadyInvitedToEventS => param,
            CalendarError.IgnoringYouS           => param,
            _                                    => packet.Name
        };

        player.SendPacket(packet);
    }

    public void SendCalendarEvent(ObjectGuid guid, CalendarEvent calendarEvent, CalendarSendEventType sendType)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player == null)
            return;

        var eventInviteeList = _invites[calendarEvent.EventId];

        CalendarSendEvent packet = new()
        {
            Date = calendarEvent.Date,
            Description = calendarEvent.Description,
            EventID = calendarEvent.EventId,
            EventName = calendarEvent.Title,
            EventType = sendType,
            Flags = calendarEvent.Flags,
            GetEventType = calendarEvent.EventType,
            LockDate = calendarEvent.LockDate, // Always 0 ?
            OwnerGuid = calendarEvent.OwnerGuid,
            TextureID = calendarEvent.TextureId
        };

        var guild = _guildManager.GetGuildById(calendarEvent.GuildId);
        packet.EventGuildID = guild?.GetGUID() ?? ObjectGuid.Empty;

        foreach (var calendarInvite in eventInviteeList)
        {
            var inviteeGuid = calendarInvite.InviteeGuid;
            var invitee = _objectAccessor.FindPlayer(inviteeGuid);

            var inviteeLevel = invitee?.Level ?? _characterCache.GetCharacterLevelByGuid(inviteeGuid);
            var inviteeGuildId = invitee?.GuildId ?? _characterCache.GetCharacterGuildIdByGuid(inviteeGuid);

            CalendarEventInviteInfo inviteInfo = new()
            {
                Guid = inviteeGuid,
                Level = (byte)inviteeLevel,
                Status = calendarInvite.Status,
                Moderator = calendarInvite.Rank,
                InviteType = (byte)(calendarEvent.IsGuildEvent && calendarEvent.GuildId == inviteeGuildId ? 1 : 0),
                InviteID = calendarInvite.InviteId,
                ResponseTime = calendarInvite.ResponseTime,
                Notes = calendarInvite.Note
            };

            packet.Invites.Add(inviteInfo);
        }

        player.SendPacket(packet);
    }

    public void SendCalendarEventInvite(CalendarInvite invite)
    {
        var calendarEvent = GetEvent(invite.EventId);

        var invitee = invite.InviteeGuid;
        var player = _objectAccessor.FindPlayer(invitee);

        var level = player?.Level ?? _characterCache.GetCharacterLevelByGuid(invitee);

        CalendarInviteAdded packet = new()
        {
            EventID = calendarEvent?.EventId ?? 0,
            InviteGuid = invitee,
            InviteID = calendarEvent != null ? invite.InviteId : 0,
            Level = (byte)level,
            ResponseTime = invite.ResponseTime,
            Status = invite.Status,
            Type = (byte)(calendarEvent != null ? calendarEvent.IsGuildEvent ? 1 : 0 : 0), // Correct ?
            ClearPending = calendarEvent == null || !calendarEvent.IsGuildEvent            // Correct ?
        };

        if (calendarEvent == null) // Pre-invite
        {
            _objectAccessor.FindPlayer(invite.SenderGuid)?.SendPacket(packet);
        }
        else
        {
            if (calendarEvent.OwnerGuid != invite.InviteeGuid) // correct?
                SendPacketToAllEventRelatives(packet, calendarEvent);
        }
    }

    public void SendCalendarEventModeratorStatusAlert(CalendarEvent calendarEvent, CalendarInvite invite)
    {
        CalendarModeratorStatus packet = new()
        {
            ClearPending = true, // FIXME
            EventID = calendarEvent.EventId,
            InviteGuid = invite.InviteeGuid,
            Status = invite.Status
        };

        SendPacketToAllEventRelatives(packet, calendarEvent);
    }

    public void SendCalendarEventStatus(CalendarEvent calendarEvent, CalendarInvite invite)
    {
        CalendarInviteStatusPacket packet = new()
        {
            ClearPending = true, // FIXME
            Date = calendarEvent.Date,
            EventID = calendarEvent.EventId,
            Flags = calendarEvent.Flags,
            InviteGuid = invite.InviteeGuid,
            ResponseTime = invite.ResponseTime,
            Status = invite.Status
        };

        SendPacketToAllEventRelatives(packet, calendarEvent);
    }

    public void SendCalendarEventUpdateAlert(CalendarEvent calendarEvent, long originalDate)
    {
        CalendarEventUpdatedAlert packet = new()
        {
            ClearPending = true, // FIXME
            Date = calendarEvent.Date,
            Description = calendarEvent.Description,
            EventID = calendarEvent.EventId,
            EventName = calendarEvent.Title,
            EventType = calendarEvent.EventType,
            Flags = calendarEvent.Flags,
            LockDate = calendarEvent.LockDate, // Always 0 ?
            OriginalDate = originalDate,
            TextureID = calendarEvent.TextureId
        };

        SendPacketToAllEventRelatives(packet, calendarEvent);
    }

    public void UpdateEvent(CalendarEvent calendarEvent)
    {
        SQLTransaction trans = new();
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.REP_CALENDAR_EVENT);
        stmt.AddValue(0, calendarEvent.EventId);
        stmt.AddValue(1, calendarEvent.OwnerGuid.Counter);
        stmt.AddValue(2, calendarEvent.Title);
        stmt.AddValue(3, calendarEvent.Description);
        stmt.AddValue(4, (byte)calendarEvent.EventType);
        stmt.AddValue(5, calendarEvent.TextureId);
        stmt.AddValue(6, calendarEvent.Date);
        stmt.AddValue(7, (uint)calendarEvent.Flags);
        stmt.AddValue(8, calendarEvent.LockDate);
        trans.Append(stmt);
        _characterDatabase.CommitTransaction(trans);
    }

    public void UpdateInvite(CalendarInvite invite, SQLTransaction trans = null)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.REP_CALENDAR_INVITE);
        stmt.AddValue(0, invite.InviteId);
        stmt.AddValue(1, invite.EventId);
        stmt.AddValue(2, invite.InviteeGuid.Counter);
        stmt.AddValue(3, invite.SenderGuid.Counter);
        stmt.AddValue(4, (byte)invite.Status);
        stmt.AddValue(5, invite.ResponseTime);
        stmt.AddValue(6, (byte)invite.Rank);
        stmt.AddValue(7, invite.Note);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    private void RemoveEvent(CalendarEvent calendarEvent, ObjectGuid remover)
    {
        if (calendarEvent == null)
        {
            SendCalendarCommandResult(remover, CalendarError.EventInvalid);

            return;
        }

        SendCalendarEventRemovedAlert(calendarEvent);

        SQLTransaction trans = new();
        PreparedStatement stmt;
        var mail = _classFactory.ResolveWithPositionalParameters<MailDraft>(calendarEvent.BuildCalendarMailSubject(remover), calendarEvent.BuildCalendarMailBody());

        var eventInvites = _invites[calendarEvent.EventId];

        foreach (var invite in eventInvites)
        {
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CALENDAR_INVITE);
            stmt.AddValue(0, invite.InviteId);
            trans.Append(stmt);

            // guild events only? check invite status here?
            // When an event is deleted, all invited (accepted/declined? - verify) guildies are notified via in-GameInfo mail. (wowwiki)
            if (!remover.IsEmpty && invite.InviteeGuid != remover)
                mail.SendMailTo(trans, new MailReceiver(invite.InviteeGuid.Counter), new MailSender(calendarEvent), MailCheckMask.Copied);
        }

        _invites.Remove(calendarEvent.EventId);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CALENDAR_EVENT);
        stmt.AddValue(0, calendarEvent.EventId);
        trans.Append(stmt);
        _characterDatabase.CommitTransaction(trans);

        _events.Remove(calendarEvent);
        FreeInviteId(calendarEvent.EventId);
    }

    private void SendCalendarEventInviteAlert(CalendarEvent calendarEvent, CalendarInvite invite)
    {
        CalendarInviteAlert packet = new()
        {
            Date = calendarEvent.Date,
            EventID = calendarEvent.EventId,
            EventName = calendarEvent.Title,
            EventType = calendarEvent.EventType,
            Flags = calendarEvent.Flags,
            InviteID = invite.InviteId,
            InvitedByGuid = invite.SenderGuid,
            ModeratorStatus = invite.Rank,
            OwnerGuid = calendarEvent.OwnerGuid,
            Status = invite.Status,
            TextureID = calendarEvent.TextureId
        };
        
        packet.EventGuildID = _guildManager.GetGuildById(calendarEvent.GuildId)?.GetGUID() ?? ObjectGuid.Empty;

        if (calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement)
            _guildManager.GetGuildById(calendarEvent.GuildId)?.BroadcastPacket(packet);
        else
            _objectAccessor.FindPlayer(invite.InviteeGuid)?.SendPacket(packet);
    }

    private void SendCalendarEventInviteRemove(CalendarEvent calendarEvent, CalendarInvite invite, uint flags)
    {
        CalendarInviteRemoved packet = new()
        {
            ClearPending = true, // FIXME
            EventID = calendarEvent.EventId,
            Flags = flags,
            InviteGuid = invite.InviteeGuid
        };

        SendPacketToAllEventRelatives(packet, calendarEvent);
    }

    private void SendCalendarEventInviteRemoveAlert(ObjectGuid guid, CalendarEvent calendarEvent, CalendarInviteStatus status)
    {
        var player = _objectAccessor.FindPlayer(guid);

        if (player == null)
            return;

        CalendarInviteRemovedAlert packet = new()
        {
            Date = calendarEvent.Date,
            EventID = calendarEvent.EventId,
            Flags = calendarEvent.Flags,
            Status = status
        };

        player.SendPacket(packet);
    }

    private void SendCalendarEventRemovedAlert(CalendarEvent calendarEvent)
    {
        CalendarEventRemovedAlert packet = new()
        {
            ClearPending = true, // FIXME
            Date = calendarEvent.Date,
            EventID = calendarEvent.EventId
        };

        SendPacketToAllEventRelatives(packet, calendarEvent);
    }

    private void SendPacketToAllEventRelatives(ServerPacket packet, CalendarEvent calendarEvent)
    {
        // Send packet to all guild members
        if (calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement)
        {
            _guildManager.GetGuildById(calendarEvent.GuildId)?.BroadcastPacket(packet);
        }

        // Send packet to all invitees if event is non-guild, in other case only to non-guild invitees (packet was broadcasted for them)
        var invites = _invites[calendarEvent.EventId];

        foreach (var playerCalendarEvent in invites)
        {
            var player = _objectAccessor.FindPlayer(playerCalendarEvent.InviteeGuid);

            if (player == null)
                continue;

            if (!calendarEvent.IsGuildEvent || player.GuildId != calendarEvent.GuildId)
                player.SendPacket(packet);
        }
    }
}