﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Duel;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.OpCodeHandlers;

public class DuelHandler : IWorldSessionHandler
{
    [WorldPacketHandler(ClientOpcodes.CanDuel)]
    private void HandleCanDuel(CanDuel packet)
    {
        var player = Global.ObjAccessor.FindPlayer(packet.TargetGUID);

        if (!player)
            return;

        CanDuelResult response = new();
        response.TargetGUID = packet.TargetGUID;
        response.Result = player.Duel == null;
        SendPacket(response);

        if (response.Result)
        {
            if (Player.IsMounted)
                Player.CastSpell(player, 62875);
            else
                Player.CastSpell(player, 7266);
        }
    }

    private void HandleDuelAccepted(ObjectGuid arbiterGuid)
    {
        var player = Player;

        if (player.Duel == null || player == player.Duel.Initiator || player.Duel.State != DuelState.Challenged)
            return;

        var target = player.Duel.Opponent;

        if (target.PlayerData.DuelArbiter != arbiterGuid)
            return;

        Log.Logger.Debug("Player 1 is: {0} ({1})", player.GUID.ToString(), player.GetName());
        Log.Logger.Debug("Player 2 is: {0} ({1})", target.GUID.ToString(), target.GetName());

        var now = GameTime.CurrentTime;
        player.Duel.StartTime = now + 3;
        target.Duel.StartTime = now + 3;

        player.Duel.State = DuelState.Countdown;
        target.Duel.State = DuelState.Countdown;

        DuelCountdown packet = new(3000);

        player.SendPacket(packet);
        target.SendPacket(packet);

        player.EnablePvpRules();
        target.EnablePvpRules();
    }

    private void HandleDuelCancelled()
    {
        var player = Player;

        // no duel requested
        if (player.Duel == null || player.Duel.State == DuelState.Completed)
            return;

        // player surrendered in a duel using /forfeit
        if (player.Duel.State == DuelState.InProgress)
        {
            player.CombatStopWithPets(true);
            player.Duel.Opponent.CombatStopWithPets(true);

            player.CastSpell(Player, 7267, true); // beg
            player.DuelComplete(DuelCompleteType.Won);

            return;
        }

        player.DuelComplete(DuelCompleteType.Interrupted);
    }

    [WorldPacketHandler(ClientOpcodes.DuelResponse)]
    private void HandleDuelResponse(DuelResponse duelResponse)
    {
        if (duelResponse.Accepted && !duelResponse.Forfeited)
            HandleDuelAccepted(duelResponse.ArbiterGUID);
        else
            HandleDuelCancelled();
    }
}