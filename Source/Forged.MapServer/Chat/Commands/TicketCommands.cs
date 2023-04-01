﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.SupportSystem;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("ticket")]
internal class TicketCommands
{
    [Command("togglesystem", RBACPermissions.CommandTicketTogglesystem, true)]
    private static bool HandleToggleGMTicketSystem(CommandHandler handler)
    {
        if (!GetDefaultValue("Support.TicketsEnabled", false))
        {
            handler.SendSysMessage(CypherStrings.DisallowTicketsConfig);

            return true;
        }

        var status = !Global.SupportMgr.GetSupportSystemStatus();
        Global.SupportMgr.SetSupportSystemStatus(status);
        handler.SendSysMessage(status ? CypherStrings.AllowTickets : CypherStrings.DisallowTickets);

        return true;
    }

    private static bool HandleTicketAssignToCommand<T>(CommandHandler handler, uint ticketId, string targetName) where T : Ticket
    {
        if (targetName.IsEmpty())
            return false;

        if (!GameObjectManager.NormalizePlayerName(ref targetName))
            return false;

        var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

        if (ticket == null || ticket.IsClosed)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

            return true;
        }

        var targetGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(targetName);
        var accountId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(targetGuid);

        // Target must exist and have administrative rights
        if (!Global.AccountMgr.HasPermission(accountId, RBACPermissions.CommandsBeAssignedTicket, Global.WorldMgr.Realm.Id.Index))
        {
            handler.SendSysMessage(CypherStrings.CommandTicketassignerrorA);

            return true;
        }

        // If already assigned, leave
        if (ticket.IsAssignedTo(targetGuid))
        {
            handler.SendSysMessage(CypherStrings.CommandTicketassignerrorB, ticket.Id);

            return true;
        }

        // If assigned to different player other than current, leave
        //! Console can override though
        var player = handler.Session?.Player;

        if (player && ticket.IsAssignedNotTo(player.GUID))
        {
            handler.SendSysMessage(CypherStrings.CommandTicketalreadyassigned, ticket.Id);

            return true;
        }

        // Assign ticket
        ticket.SetAssignedTo(targetGuid, Global.AccountMgr.IsAdminAccount(Global.AccountMgr.GetSecurity(accountId, (int)Global.WorldMgr.Realm.Id.Index)));
        ticket.SaveToDB();

        var msg = ticket.FormatViewMessageString(handler, null, targetName, null, null);
        handler.SendGlobalGMSysMessage(msg);

        return true;
    }

    private static bool HandleCloseByIdCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
    {
        var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

        if (ticket == null || ticket.IsClosed)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

            return true;
        }

        // Ticket should be assigned to the player who tries to close it.
        // Console can override though
        var player = handler.Session?.Player;

        if (player && ticket.IsAssignedNotTo(player.GUID))
        {
            handler.SendSysMessage(CypherStrings.CommandTicketcannotclose, ticket.Id);

            return true;
        }

        var closedByGuid = ObjectGuid.Empty;

        if (player)
            closedByGuid = player.GUID;
        else
            closedByGuid.SetRawValue(0, ulong.MaxValue);

        Global.SupportMgr.CloseTicket<T>(ticket.Id, closedByGuid);

        var msg = ticket.FormatViewMessageString(handler, player ? player.GetName() : "Console", null, null, null);
        handler.SendGlobalGMSysMessage(msg);

        return true;
    }

    private static bool HandleClosedListCommand<T>(CommandHandler handler) where T : Ticket
    {
        Global.SupportMgr.ShowClosedList<T>(handler);

        return true;
    }

    private static bool HandleCommentCommand<T>(CommandHandler handler, uint ticketId, QuotedString comment) where T : Ticket
    {
        if (comment.IsEmpty())
            return false;

        var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

        if (ticket == null || ticket.IsClosed)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

            return true;
        }

        // Cannot comment ticket assigned to someone else
        //! Console excluded
        var player = handler.Session?.Player;

        if (player && ticket.IsAssignedNotTo(player.GUID))
        {
            handler.SendSysMessage(CypherStrings.CommandTicketalreadyassigned, ticket.Id);

            return true;
        }

        ticket.SetComment(comment);
        ticket.SaveToDB();
        Global.SupportMgr.UpdateLastChange();

        var msg = ticket.FormatViewMessageString(handler, null, ticket.AssignedToName, null, null);
        msg += string.Format(handler.GetCypherString(CypherStrings.CommandTicketlistaddcomment), player ? player.GetName() : "Console", comment);
        handler.SendGlobalGMSysMessage(msg);

        return true;
    }

    private static bool HandleDeleteByIdCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
    {
        var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

        if (ticket == null)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

            return true;
        }

        if (!ticket.IsClosed)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketclosefirst);

            return true;
        }

        var msg = ticket.FormatViewMessageString(handler, null, null, null, handler.Session != null ? handler.Session.Player.GetName() : "Console");
        handler.SendGlobalGMSysMessage(msg);

        Global.SupportMgr.RemoveTicket<T>(ticket.Id);

        return true;
    }

    private static bool HandleListCommand<T>(CommandHandler handler) where T : Ticket
    {
        Global.SupportMgr.ShowList<T>(handler);

        return true;
    }

    private static bool HandleResetCommand<T>(CommandHandler handler) where T : Ticket
    {
        if (Global.SupportMgr.GetOpenTicketCount<T>() != 0)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketpending);

            return true;
        }
        else
        {
            Global.SupportMgr.ResetTickets<T>();
            handler.SendSysMessage(CypherStrings.CommandTicketreset);
        }

        return true;
    }

    private static bool HandleUnAssignCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
    {
        var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

        if (ticket == null || ticket.IsClosed)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

            return true;
        }

        // Ticket must be assigned
        if (!ticket.IsAssigned)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketnotassigned, ticket.Id);

            return true;
        }

        // Get security level of player, whom this ticket is assigned to
        AccountTypes security;
        var assignedPlayer = ticket.AssignedPlayer;

        if (assignedPlayer && assignedPlayer.IsInWorld)
        {
            security = assignedPlayer.Session.Security;
        }
        else
        {
            var guid = ticket.AssignedToGUID;
            var accountId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(guid);
            security = Global.AccountMgr.GetSecurity(accountId, (int)Global.WorldMgr.Realm.Id.Index);
        }

        // Check security
        //! If no m_session present it means we're issuing this command from the console
        var mySecurity = handler.Session?.Security ?? AccountTypes.Console;

        if (security > mySecurity)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketunassignsecurity);

            return true;
        }

        var assignedTo = ticket.AssignedToName; // copy assignedto name because we need it after the ticket has been unnassigned

        ticket.SetUnassigned();
        ticket.SaveToDB();
        var msg = ticket.FormatViewMessageString(handler, null, assignedTo, handler.Session != null ? handler.Session.Player.GetName() : "Console", null);
        handler.SendGlobalGMSysMessage(msg);

        return true;
    }

    private static bool HandleGetByIdCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
    {
        var ticket = Global.SupportMgr.GetTicket<T>(ticketId);

        if (ticket == null || ticket.IsClosed)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

            return true;
        }

        handler.SendSysMessage(ticket.FormatViewMessageString(handler, true));

        return true;
    }

    [CommandGroup("bug")]
    private class TicketBugCommands
    {
        [Command("assign", RBACPermissions.CommandTicketBugAssign, true)]
        private static bool HandleTicketBugAssignCommand(CommandHandler handler, uint ticketId, string targetName)
        {
            return HandleTicketAssignToCommand<BugTicket>(handler, ticketId, targetName);
        }

        [Command("close", RBACPermissions.CommandTicketBugClose, true)]
        private static bool HandleTicketBugCloseCommand(CommandHandler handler, uint ticketId)
        {
            return HandleCloseByIdCommand<BugTicket>(handler, ticketId);
        }

        [Command("closedlist", RBACPermissions.CommandTicketBugClosedlist, true)]
        private static bool HandleTicketBugClosedListCommand(CommandHandler handler)
        {
            return HandleClosedListCommand<BugTicket>(handler);
        }

        [Command("comment", RBACPermissions.CommandTicketBugComment, true)]
        private static bool HandleTicketBugCommentCommand(CommandHandler handler, uint ticketId, QuotedString comment)
        {
            return HandleCommentCommand<BugTicket>(handler, ticketId, comment);
        }

        [Command("delete", RBACPermissions.CommandTicketBugDelete, true)]
        private static bool HandleTicketBugDeleteCommand(CommandHandler handler, uint ticketId)
        {
            return HandleDeleteByIdCommand<BugTicket>(handler, ticketId);
        }

        [Command("list", RBACPermissions.CommandTicketBugList, true)]
        private static bool HandleTicketBugListCommand(CommandHandler handler)
        {
            return HandleListCommand<BugTicket>(handler);
        }

        [Command("unassign", RBACPermissions.CommandTicketBugUnassign, true)]
        private static bool HandleTicketBugUnAssignCommand(CommandHandler handler, uint ticketId)
        {
            return HandleUnAssignCommand<BugTicket>(handler, ticketId);
        }

        [Command("view", RBACPermissions.CommandTicketBugView, true)]
        private static bool HandleTicketBugViewCommand(CommandHandler handler, uint ticketId)
        {
            return HandleGetByIdCommand<BugTicket>(handler, ticketId);
        }
    }

    [CommandGroup("complaint")]
    private class TicketComplaintCommands
    {
        [Command("assign", RBACPermissions.CommandTicketComplaintAssign, true)]
        private static bool HandleTicketComplaintAssignCommand(CommandHandler handler, uint ticketId, string targetName)
        {
            return HandleTicketAssignToCommand<ComplaintTicket>(handler, ticketId, targetName);
        }

        [Command("close", RBACPermissions.CommandTicketComplaintClose, true)]
        private static bool HandleTicketComplaintCloseCommand(CommandHandler handler, uint ticketId)
        {
            return HandleCloseByIdCommand<ComplaintTicket>(handler, ticketId);
        }

        [Command("closedlist", RBACPermissions.CommandTicketComplaintClosedlist, true)]
        private static bool HandleTicketComplaintClosedListCommand(CommandHandler handler)
        {
            return HandleClosedListCommand<ComplaintTicket>(handler);
        }

        [Command("comment", RBACPermissions.CommandTicketComplaintComment, true)]
        private static bool HandleTicketComplaintCommentCommand(CommandHandler handler, uint ticketId, QuotedString comment)
        {
            return HandleCommentCommand<ComplaintTicket>(handler, ticketId, comment);
        }

        [Command("delete", RBACPermissions.CommandTicketComplaintDelete, true)]
        private static bool HandleTicketComplaintDeleteCommand(CommandHandler handler, uint ticketId)
        {
            return HandleDeleteByIdCommand<ComplaintTicket>(handler, ticketId);
        }

        [Command("list", RBACPermissions.CommandTicketComplaintList, true)]
        private static bool HandleTicketComplaintListCommand(CommandHandler handler)
        {
            return HandleListCommand<ComplaintTicket>(handler);
        }

        [Command("unassign", RBACPermissions.CommandTicketComplaintUnassign, true)]
        private static bool HandleTicketComplaintUnAssignCommand(CommandHandler handler, uint ticketId)
        {
            return HandleUnAssignCommand<ComplaintTicket>(handler, ticketId);
        }

        [Command("view", RBACPermissions.CommandTicketComplaintView, true)]
        private static bool HandleTicketComplaintViewCommand(CommandHandler handler, uint ticketId)
        {
            return HandleGetByIdCommand<ComplaintTicket>(handler, ticketId);
        }
    }

    [CommandGroup("suggestion")]
    private class TicketSuggestionCommands
    {
        [Command("assign", RBACPermissions.CommandTicketSuggestionAssign, true)]
        private static bool HandleTicketSuggestionAssignCommand(CommandHandler handler, uint ticketId, string targetName)
        {
            return HandleTicketAssignToCommand<SuggestionTicket>(handler, ticketId, targetName);
        }

        [Command("close", RBACPermissions.CommandTicketSuggestionClose, true)]
        private static bool HandleTicketSuggestionCloseCommand(CommandHandler handler, uint ticketId)
        {
            return HandleCloseByIdCommand<SuggestionTicket>(handler, ticketId);
        }

        [Command("closedlist", RBACPermissions.CommandTicketSuggestionClosedlist, true)]
        private static bool HandleTicketSuggestionClosedListCommand(CommandHandler handler)
        {
            return HandleClosedListCommand<SuggestionTicket>(handler);
        }

        [Command("comment", RBACPermissions.CommandTicketSuggestionComment, true)]
        private static bool HandleTicketSuggestionCommentCommand(CommandHandler handler, uint ticketId, QuotedString comment)
        {
            return HandleCommentCommand<SuggestionTicket>(handler, ticketId, comment);
        }

        [Command("delete", RBACPermissions.CommandTicketSuggestionDelete, true)]
        private static bool HandleTicketSuggestionDeleteCommand(CommandHandler handler, uint ticketId)
        {
            return HandleDeleteByIdCommand<SuggestionTicket>(handler, ticketId);
        }

        [Command("list", RBACPermissions.CommandTicketSuggestionList, true)]
        private static bool HandleTicketSuggestionListCommand(CommandHandler handler)
        {
            return HandleListCommand<SuggestionTicket>(handler);
        }

        [Command("unassign", RBACPermissions.CommandTicketSuggestionUnassign, true)]
        private static bool HandleTicketSuggestionUnAssignCommand(CommandHandler handler, uint ticketId)
        {
            return HandleUnAssignCommand<SuggestionTicket>(handler, ticketId);
        }

        [Command("view", RBACPermissions.CommandTicketSuggestionView, true)]
        private static bool HandleTicketSuggestionViewCommand(CommandHandler handler, uint ticketId)
        {
            return HandleGetByIdCommand<SuggestionTicket>(handler, ticketId);
        }
    }

    [CommandGroup("reset")]
    private class TicketResetCommands
    {
        [Command("all", RBACPermissions.CommandTicketResetAll, true)]
        private static bool HandleTicketResetAllCommand(CommandHandler handler)
        {
            if (Global.SupportMgr.GetOpenTicketCount<BugTicket>() != 0 || Global.SupportMgr.GetOpenTicketCount<ComplaintTicket>() != 0 || Global.SupportMgr.GetOpenTicketCount<SuggestionTicket>() != 0)
            {
                handler.SendSysMessage(CypherStrings.CommandTicketpending);

                return true;
            }
            else
            {
                Global.SupportMgr.ResetTickets<BugTicket>();
                Global.SupportMgr.ResetTickets<ComplaintTicket>();
                Global.SupportMgr.ResetTickets<SuggestionTicket>();
                handler.SendSysMessage(CypherStrings.CommandTicketreset);
            }

            return true;
        }

        [Command("bug", RBACPermissions.CommandTicketResetBug, true)]
        private static bool HandleTicketResetBugCommand(CommandHandler handler)
        {
            return HandleResetCommand<BugTicket>(handler);
        }

        [Command("complaint", RBACPermissions.CommandTicketResetComplaint, true)]
        private static bool HandleTicketResetComplaintCommand(CommandHandler handler)
        {
            return HandleResetCommand<ComplaintTicket>(handler);
        }

        [Command("suggestion", RBACPermissions.CommandTicketResetSuggestion, true)]
        private static bool HandleTicketResetSuggestionCommand(CommandHandler handler)
        {
            return HandleResetCommand<SuggestionTicket>(handler);
        }
    }
}