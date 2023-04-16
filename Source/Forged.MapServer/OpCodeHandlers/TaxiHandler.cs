﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Taxis;
using Forged.MapServer.Globals;
using Forged.MapServer.Movement.Generators;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Taxi;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.OpCodeHandlers;

public class TaxiHandler : IWorldSessionHandler
{
    public void SendActivateTaxiReply(ActivateTaxiReply reply = ActivateTaxiReply.Ok)
    {
        ActivateTaxiReplyPkt data = new();
        data.Reply = reply;
        SendPacket(data);
    }

    public void SendDiscoverNewTaxiNode(uint nodeid)
    {
        if (Player.Taxi.SetTaximaskNode(nodeid))
            SendPacket(new NewTaxiPath());
    }

    public void SendDoFlight(uint mountDisplayId, uint path, uint pathNode = 0)
    {
        // remove fake death
        if (Player.HasUnitState(UnitState.Died))
            Player.RemoveAurasByType(AuraType.FeignDeath);

        if (mountDisplayId != 0)
            Player.Mount(mountDisplayId);

        Player.MotionMaster.MoveTaxiFlight(path, pathNode);
    }

    public bool SendLearnNewTaxiNode(Creature unit)
    {
        // find current node
        var curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, Player.Team);

        if (curloc == 0)
            return true;

        if (Player.Taxi.SetTaximaskNode(curloc))
        {
            SendPacket(new NewTaxiPath());

            TaxiNodeStatusPkt data = new();
            data.Unit = unit.GUID;
            data.Status = TaxiNodeStatus.Learned;
            SendPacket(data);

            return true;
        }
        else
        {
            return false;
        }
    }

    public void SendTaxiMenu(Creature unit)
    {
        // find current node
        var curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, Player.Team);

        if (curloc == 0)
            return;

        var lastTaxiCheaterState = Player.IsTaxiCheater;

        if (unit.Entry == 29480)
            Player.SetTaxiCheater(true); // Grimwing in Ebon Hold, special case. NOTE: Not perfect, Zul'Aman should not be included according to WoWhead, and I think taxicheat includes it.

        ShowTaxiNodes data = new();
        ShowTaxiNodesWindowInfo windowInfo = new();
        windowInfo.UnitGUID = unit.GUID;
        windowInfo.CurrentNode = (int)curloc;

        data.WindowInfo = windowInfo;

        Player.Taxi.AppendTaximaskTo(data, lastTaxiCheaterState);

        var reachableNodes = new byte[CliDB.TaxiNodesMask.Length];
        TaxiPathGraph.GetReachableNodesMask(CliDB.TaxiNodesStorage.LookupByKey(curloc), reachableNodes);

        for (var i = 0; i < reachableNodes.Length; ++i)
        {
            data.CanLandNodes[i] &= reachableNodes[i];
            data.CanUseNodes[i] &= reachableNodes[i];
        }


        SendPacket(data);

        Player.SetTaxiCheater(lastTaxiCheaterState);
    }

    public void SendTaxiStatus(ObjectGuid guid)
    {
        // cheating checks
        var player = Player;
        var unit = ObjectAccessor.GetCreature(player, guid);

        if (!unit || unit.WorldObjectCombat.IsHostileTo(player) || !unit.HasNpcFlag(NPCFlags.FlightMaster))
        {
            Log.Logger.Debug("WorldSession.SendTaxiStatus - {0} not found.", guid.ToString());

            return;
        }

        // find taxi node
        var nearest = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, player.Team);

        TaxiNodeStatusPkt data = new();
        data.Unit = guid;

        if (nearest == 0)
            data.Status = TaxiNodeStatus.None;
        else if (unit.WorldObjectCombat.GetReactionTo(player) >= ReputationRank.Neutral)
            data.Status = player.Taxi.IsTaximaskNodeKnown(nearest) ? TaxiNodeStatus.Learned : TaxiNodeStatus.Unlearned;
        else
            data.Status = TaxiNodeStatus.NotEligible;

        SendPacket(data);
    }

    [WorldPacketHandler(ClientOpcodes.ActivateTaxi, Processing = PacketProcessing.ThreadSafe)]
    private void HandleActivateTaxi(ActivateTaxi activateTaxi)
    {
        var unit = Player.GetNPCIfCanInteractWith(activateTaxi.Vendor, NPCFlags.FlightMaster, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleActivateTaxiOpcode - {0} not found or you can't interact with it.", activateTaxi.Vendor.ToString());
            SendActivateTaxiReply(ActivateTaxiReply.TooFarAway);

            return;
        }

        var curloc = Global.ObjectMgr.GetNearestTaxiNode(unit.Location.X, unit.Location.Y, unit.Location.Z, unit.Location.MapId, Player.Team);

        if (curloc == 0)
            return;

        var from = CliDB.TaxiNodesStorage.LookupByKey(curloc);
        if (!CliDB.TaxiNodesStorage.TryGetValue(activateTaxi.Node, out var to))
            return;

        if (!Player.IsTaxiCheater)
            if (!Player.Taxi.IsTaximaskNodeKnown(curloc) || !Player.Taxi.IsTaximaskNodeKnown(activateTaxi.Node))
            {
                SendActivateTaxiReply(ActivateTaxiReply.NotVisited);

                return;
            }

        uint preferredMountDisplay = 0;
        if (CliDB.MountStorage.TryGetValue(activateTaxi.FlyingMountID, out var mount))
            if (Player.HasSpell(mount.SourceSpellID))
            {
                var mountDisplays = Global.DB2Mgr.GetMountDisplays(mount.Id);

                if (mountDisplays != null)
                {
                    var usableDisplays = mountDisplays.Where(mountDisplay =>
                                                      {
                                                          if (CliDB.PlayerConditionStorage.TryGetValue(mountDisplay.PlayerConditionID, out var playerCondition))
                                                              return ConditionManager.IsPlayerMeetingCondition(Player, playerCondition);

                                                          return true;
                                                      })
                                                      .ToList();

                    if (!usableDisplays.Empty())
                        preferredMountDisplay = usableDisplays.SelectRandom().CreatureDisplayInfoID;
                }
            }

        List<uint> nodes = new();
        TaxiPathGraph.GetCompleteNodeRoute(from, to, Player, nodes);
        Player.ActivateTaxiPathTo(nodes, unit, 0, preferredMountDisplay);
    }

    [WorldPacketHandler(ClientOpcodes.EnableTaxiNode, Processing = PacketProcessing.ThreadSafe)]
    private void HandleEnableTaxiNodeOpcode(EnableTaxiNode enableTaxiNode)
    {
        var unit = Player.GetNPCIfCanInteractWith(enableTaxiNode.Unit, NPCFlags.FlightMaster, NPCFlags2.None);

        if (unit)
            SendLearnNewTaxiNode(unit);
    }

    [WorldPacketHandler(ClientOpcodes.TaxiNodeStatusQuery, Processing = PacketProcessing.ThreadSafe)]
    private void HandleTaxiNodeStatusQuery(TaxiNodeStatusQuery taxiNodeStatusQuery)
    {
        SendTaxiStatus(taxiNodeStatusQuery.UnitGUID);
    }

    [WorldPacketHandler(ClientOpcodes.TaxiQueryAvailableNodes, Processing = PacketProcessing.ThreadSafe)]
    private void HandleTaxiQueryAvailableNodes(TaxiQueryAvailableNodes taxiQueryAvailableNodes)
    {
        // cheating checks
        var unit = Player.GetNPCIfCanInteractWith(taxiQueryAvailableNodes.Unit, NPCFlags.FlightMaster, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleTaxiQueryAvailableNodes - {0} not found or you can't interact with him.", taxiQueryAvailableNodes.Unit.ToString());

            return;
        }

        // remove fake death
        if (Player.HasUnitState(UnitState.Died))
            Player.RemoveAurasByType(AuraType.FeignDeath);

        // unknown taxi node case
        if (SendLearnNewTaxiNode(unit))
            return;

        // known taxi node case
        SendTaxiMenu(unit);
    }

    [WorldPacketHandler(ClientOpcodes.TaxiRequestEarlyLanding, Processing = PacketProcessing.ThreadSafe)]
    private void HandleTaxiRequestEarlyLanding(TaxiRequestEarlyLanding taxiRequestEarlyLanding)
    {
        if (Player.MotionMaster.GetCurrentMovementGenerator() is FlightPathMovementGenerator flight)
            if (Player.Taxi.RequestEarlyLanding())
            {
                flight.LoadPath(Player, (uint)flight.Path[(int)flight.GetCurrentNode()].NodeIndex);
                flight.Reset(Player);
            }
    }
}