﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting;
using Forged.MapServer.SupportSystem;
using Framework.Constants;
using Game.Common;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Local

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("go")]
internal class GoCommands
{
    private static bool DoTeleport(CommandHandler handler, Position pos, uint mapId = 0xFFFFFFFF)
    {
        var player = handler.Session.Player;

        mapId = mapId switch
        {
            0xFFFFFFFF => player.Location.MapId,
            _          => mapId
        };

        if (!handler.ClassFactory.Resolve<GridDefines>().IsValidMapCoord(mapId, pos) || handler.ObjectManager.IsTransportMap(mapId))
        {
            handler.SendSysMessage(CypherStrings.InvalidTargetCoord, pos.X, pos.Y, mapId);

            return false;
        }

        // stop flight if need
        if (player.IsInFlight)
            player.FinishTaxiFlight();
        else
            player.SaveRecallPosition(); // save only in non-flight case

        player.TeleportTo(new WorldLocation(mapId, pos));

        return true;
    }

    [Command("areatrigger", RBACPermissions.CommandGo)]
    private static bool HandleGoAreaTriggerCommand(CommandHandler handler, uint areaTriggerId)
    {
        if (handler.CliDB.AreaTriggerStorage.TryGetValue(areaTriggerId, out var at))
            return DoTeleport(handler, new Position(at.Pos.X, at.Pos.Y, at.Pos.Z), at.ContinentID);

        handler.SendSysMessage(CypherStrings.CommandGoareatrnotfound, areaTriggerId);

        return false;

    }

    [Command("boss", RBACPermissions.CommandGo)]
    private static bool HandleGoBossCommand(CommandHandler handler, string[] needles)
    {
        if (needles.Empty())
            return false;

        MultiMap<uint, CreatureTemplate> matches = new();
        Dictionary<uint, List<CreatureData>> spawnLookup = new();

        // find all boss flagged mobs that match our needles
        foreach (var pair in handler.ObjectManager.CreatureTemplates)
        {
            var data = pair.Value;

            if (!data.FlagsExtra.HasFlag(CreatureFlagsExtra.DungeonBoss))
                continue;

            uint count = 0;
            var scriptName = handler.ClassFactory.Resolve<ScriptManager>().GetScriptName(data.ScriptID);

            foreach (var label in needles)
                if (scriptName.Contains(label) || data.Name.Contains(label))
                    ++count;

            if (count != 0)
            {
                matches.Add(count, data);
                spawnLookup[data.Entry] = new List<CreatureData>(); // inserts default-constructed vector
            }
        }

        if (!matches.Empty())
        {
            // find the spawn points of any matches
            foreach (var pair in handler.ObjectManager.AllCreatureData)
            {
                var data = pair.Value;

                if (spawnLookup.ContainsKey(data.Id))
                    spawnLookup[data.Id].Add(data);
            }

            // remove any matches without spawns
            matches.RemoveIfMatching(pair => spawnLookup[pair.Value.Entry].Empty());
        }

        // check if we even have any matches left
        if (matches.Empty())
        {
            handler.SendSysMessage(CypherStrings.CommandNoBossesMatch);

            return false;
        }

        // see if we have multiple equal matches left
        var keyValueList = matches.KeyValueList.ToList();
        var maxCount = keyValueList.Last().Key;

        for (var i = keyValueList.Count; i > 0;)
            if (++i != 0 && keyValueList[i].Key == maxCount)
            {
                handler.SendSysMessage(CypherStrings.CommandMultipleBossesMatch);
                --i;

                do
                {
                    handler.SendSysMessage(CypherStrings.CommandMultipleBossesEntry, keyValueList[i].Value.Entry, keyValueList[i].Value.Name, handler.ClassFactory.Resolve<ScriptManager>().GetScriptName(keyValueList[i].Value.ScriptID));
                } while (++i != 0 && keyValueList[i].Key == maxCount);

                return false;
            }

        var boss = matches.KeyValueList.Last().Value;
        var spawns = spawnLookup[boss.Entry];

        if (spawns.Count > 1)
        {
            handler.SendSysMessage(CypherStrings.CommandBossMultipleSpawns, boss.Name, boss.Entry);

            foreach (var spawnData in spawns)
            {
                var map = handler.CliDB.MapStorage.LookupByKey(spawnData.MapId);
                handler.SendSysMessage(CypherStrings.CommandBossMultipleSpawnEty, spawnData.SpawnId, spawnData.MapId, map.MapName[handler.SessionDbcLocale], spawnData.SpawnPoint.ToString());
            }

            return false;
        }

        var player = handler.Session.Player;

        if (player.IsInFlight)
            player.FinishTaxiFlight();
        else
            player.SaveRecallPosition();

        var spawn = spawns.First();
        var mapId = spawn.MapId;

        if (!player.TeleportTo(new WorldLocation(mapId, spawn.SpawnPoint)))
        {
            var mapName = handler.CliDB.MapStorage.LookupByKey(mapId).MapName[handler.SessionDbcLocale];
            handler.SendSysMessage(CypherStrings.CommandGoBossFailed, spawn.SpawnId, boss.Name, boss.Entry, mapName);

            return false;
        }

        handler.SendSysMessage(CypherStrings.CommandWentToBoss, boss.Name, boss.Entry, spawn.SpawnId);

        return true;
    }

    [Command("bugticket", RBACPermissions.CommandGo)]
    private static bool HandleGoBugTicketCommand(CommandHandler handler, uint ticketId)
    {
        return HandleGoTicketCommand<BugTicket>(handler, ticketId);
    }

    [Command("complaintticket", RBACPermissions.CommandGo)]
    private static bool HandleGoComplaintTicketCommand(CommandHandler handler, uint ticketId)
    {
        return HandleGoTicketCommand<ComplaintTicket>(handler, ticketId);
    }

    [Command("graveyard", RBACPermissions.CommandGo)]
    private static bool HandleGoGraveyardCommand(CommandHandler handler, uint graveyardId)
    {
        var gy = handler.ObjectManager.GetWorldSafeLoc(graveyardId);

        if (gy == null)
        {
            handler.SendSysMessage(CypherStrings.CommandGraveyardnoexist, graveyardId);

            return false;
        }

        if (!handler.ClassFactory.Resolve<GridDefines>().IsValidMapCoord(gy.Location))
        {
            handler.SendSysMessage(CypherStrings.InvalidTargetCoord, gy.Location.X, gy.Location.Y, gy.Location.MapId);

            return false;
        }

        var player = handler.Session.Player;

        // stop flight if need
        if (player.IsInFlight)
            player.FinishTaxiFlight();
        else
            player.SaveRecallPosition(); // save only in non-flight case

        player.TeleportTo(gy.Location);

        return true;
    }

    [Command("grid", RBACPermissions.CommandGo)]
    private static bool HandleGoGridCommand(CommandHandler handler, float gridX, float gridY, uint? mapIdArg)
    {
        var player = handler.Session.Player;
        var mapId = mapIdArg.GetValueOrDefault(player.Location.MapId);

        // center of grid
        var x = (gridX - MapConst.CenterGridId + 0.5f) * MapConst.SizeofGrids;
        var y = (gridY - MapConst.CenterGridId + 0.5f) * MapConst.SizeofGrids;

        if (!handler.ClassFactory.Resolve<GridDefines>().IsValidMapCoord(mapId, x, y))
        {
            handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);

            return false;
        }

        // stop flight if need
        if (player.IsInFlight)
            player.FinishTaxiFlight();
        else
            player.SaveRecallPosition(); // save only in non-flight case

        var terrain = handler.ClassFactory.Resolve<TerrainManager>().LoadTerrain(mapId);
        var z = Math.Max(terrain.GetStaticHeight(handler.ClassFactory.Resolve<PhasingHandler>().EmptyPhaseShift, mapId, x, y, MapConst.MaxHeight), terrain.GetWaterLevel(handler.ClassFactory.Resolve<PhasingHandler>().EmptyPhaseShift, mapId, x, y));

        player.TeleportTo(mapId, x, y, z, player.Location.Orientation);

        return true;
    }

    [Command("instance", RBACPermissions.CommandGo)]
    private static bool HandleGoInstanceCommand(CommandHandler handler, string[] labels)
    {
        if (labels.Empty())
            return false;

        MultiMap<uint, Tuple<uint, string, string>> matches = new();

        foreach (var pair in handler.ClassFactory.Resolve<InstanceTemplateManager>().InstanceTemplates)
        {
            uint count = 0;
            var scriptName = handler.ClassFactory.Resolve<ScriptManager>().GetScriptName(pair.Value.ScriptId);
            var mapName1 = handler.CliDB.MapStorage.LookupByKey(pair.Key).MapName[handler.SessionDbcLocale];

            foreach (var label in labels)
                if (scriptName.Contains(label))
                    ++count;

            if (count != 0)
                matches.Add(count, Tuple.Create(pair.Key, mapName1, scriptName));
        }

        if (matches.Empty())
        {
            handler.SendSysMessage(CypherStrings.CommandNoInstancesMatch);

            return false;
        }

        // see if we have multiple equal matches left
        var keyValueList = matches.KeyValueList.ToList();
        var maxCount = keyValueList.Last().Key;

        for (var i = keyValueList.Count; i > 0;)
            if (++i != 0 && keyValueList[i].Key == maxCount)
            {
                handler.SendSysMessage(CypherStrings.CommandMultipleInstancesMatch);
                --i;

                do
                {
                    handler.SendSysMessage(CypherStrings.CommandMultipleInstancesEntry, keyValueList[i].Value.Item2, keyValueList[i].Value.Item1, keyValueList[i].Value.Item3);
                } while (++i != 0 && keyValueList[i].Key == maxCount);

                return false;
            }

        var it = matches.KeyValueList.Last();
        var mapId = it.Value.Item1;
        var mapName = it.Value.Item2;

        var player = handler.Session.Player;

        if (player.IsInFlight)
            player.FinishTaxiFlight();
        else
            player.SaveRecallPosition();

        // try going to entrance
        var exit = handler.ObjectManager.GetGoBackTrigger(mapId);

        if (exit != null)
        {
            if (player.TeleportTo(exit.TargetMapId, exit.TargetX, exit.TargetY, exit.TargetZ, exit.TargetOrientation + MathF.PI))
            {
                handler.SendSysMessage(CypherStrings.CommandWentToInstanceGate, mapName, mapId);

                return true;
            }

            var parentMapId = exit.TargetMapId;
            var parentMapName = handler.CliDB.MapStorage.LookupByKey(parentMapId).MapName[handler.SessionDbcLocale];
            handler.SendSysMessage(CypherStrings.CommandGoInstanceGateFailed, mapName, mapId, parentMapName, parentMapId);
        }
        else
            handler.SendSysMessage(CypherStrings.CommandInstanceNoExit, mapName, mapId);

        // try going to start
        var entrance = handler.ObjectManager.GetMapEntranceTrigger(mapId);

        if (entrance != null)
        {
            if (player.TeleportTo(entrance.TargetMapId, entrance.TargetX, entrance.TargetY, entrance.TargetZ, entrance.TargetOrientation))
            {
                handler.SendSysMessage(CypherStrings.CommandWentToInstanceStart, mapName, mapId);

                return true;
            }

            handler.SendSysMessage(CypherStrings.CommandGoInstanceStartFailed, mapName, mapId);
        }
        else
            handler.SendSysMessage(CypherStrings.CommandInstanceNoEntrance, mapName, mapId);

        return false;
    }

    [Command("offset", RBACPermissions.CommandGo)]
    private static bool HandleGoOffsetCommand(CommandHandler handler, float dX, float? dY, float? dZ, float? dO)
    {
        Position loc = handler.Session.Player.Location;
        loc.RelocateOffset(new Position(dX, dY.GetValueOrDefault(0f), dZ.GetValueOrDefault(0f), dO.GetValueOrDefault(0f)));

        return DoTeleport(handler, loc);
    }

    [Command("quest", RBACPermissions.CommandGo)]
    private static bool HandleGoQuestCommand(CommandHandler handler, uint questId)
    {
        var player = handler.Session.Player;

        if (handler.ObjectManager.GetQuestTemplate(questId) == null)
        {
            handler.SendSysMessage(CypherStrings.CommandQuestNotfound, questId);

            return false;
        }

        float x, y, z;
        uint mapId;

        var poiData = handler.ObjectManager.GetQuestPOIData(questId);

        if (poiData != null)
        {
            var data = poiData.Blobs[0];

            mapId = (uint)data.MapID;

            x = data.Points[0].X;
            y = data.Points[0].Y;
        }
        else
        {
            handler.SendSysMessage(CypherStrings.CommandQuestNotfound, questId);

            return false;
        }

        if (!handler.ClassFactory.Resolve<GridDefines>().IsValidMapCoord(mapId, x, y) || handler.ObjectManager.IsTransportMap(mapId))
        {
            handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);

            return false;
        }

        // stop flight if need
        if (player.IsInFlight)
            player.FinishTaxiFlight();
        else
            player.SaveRecallPosition(); // save only in non-flight case

        var terrain = handler.ClassFactory.Resolve<TerrainManager>().LoadTerrain(mapId);
        z = Math.Max(terrain.GetStaticHeight(handler.ClassFactory.Resolve<PhasingHandler>().EmptyPhaseShift, mapId, x, y, MapConst.MaxHeight), terrain.GetWaterLevel(handler.ClassFactory.Resolve<PhasingHandler>().EmptyPhaseShift, mapId, x, y));

        player.TeleportTo(mapId, x, y, z, 0.0f);

        return true;
    }

    [Command("suggestionticket", RBACPermissions.CommandGo)]
    private static bool HandleGoSuggestionTicketCommand(CommandHandler handler, uint ticketId)
    {
        return HandleGoTicketCommand<SuggestionTicket>(handler, ticketId);
    }

    [Command("taxinode", RBACPermissions.CommandGo)]
    private static bool HandleGoTaxinodeCommand(CommandHandler handler, uint nodeId)
    {
        if (!handler.CliDB.TaxiNodesStorage.TryGetValue(nodeId, out var node))
        {
            handler.SendSysMessage(CypherStrings.CommandGotaxinodenotfound, nodeId);

            return false;
        }

        return DoTeleport(handler, new Position(node.Pos.X, node.Pos.Y, node.Pos.Z), node.ContinentID);
    }

    private static bool HandleGoTicketCommand<T>(CommandHandler handler, uint ticketId) where T : Ticket
    {
        var ticket = handler.ClassFactory.Resolve<SupportManager>().GetTicket<T>(ticketId);

        if (ticket == null)
        {
            handler.SendSysMessage(CypherStrings.CommandTicketnotexist);

            return true;
        }

        var player = handler.Session.Player;

        // stop flight if need
        if (player.IsInFlight)
            player.FinishTaxiFlight();
        else
            player.SaveRecallPosition(); // save only in non-flight case

        ticket.TeleportTo(player);

        return true;
    }

    //teleport at coordinates, including Z and orientation
    [Command("xyz", RBACPermissions.CommandGo)]
    private static bool HandleGoXYZCommand(CommandHandler handler, float x, float y, float? z, uint? id, float? o)
    {
        var player = handler.Session.Player;
        var mapId = id.GetValueOrDefault(player.Location.MapId);

        if (z.HasValue)
        {
            if (!handler.ClassFactory.Resolve<GridDefines>().IsValidMapCoord(mapId, x, y, z.Value))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);

                return false;
            }
        }
        else
        {
            if (!handler.ClassFactory.Resolve<GridDefines>().IsValidMapCoord(mapId, x, y))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, mapId);

                return false;
            }

            var terrain = handler.ClassFactory.Resolve<TerrainManager>().LoadTerrain(mapId);
            z = Math.Max(terrain.GetStaticHeight(handler.ClassFactory.Resolve<PhasingHandler>().EmptyPhaseShift, mapId, x, y, MapConst.MaxHeight), terrain.GetWaterLevel(handler.ClassFactory.Resolve<PhasingHandler>().EmptyPhaseShift, mapId, x, y));
        }

        return DoTeleport(handler, new Position(x, y, z.Value, o ?? 0), mapId);
    }

    //teleport at coordinates
    [Command("zonexy", RBACPermissions.CommandGo)]
    private static bool HandleGoZoneXyCommand(CommandHandler handler, float x, float y, uint? areaIdArg)
    {
        var player = handler.Session.Player;

        var areaId = areaIdArg ?? player.Location.Zone;

        if (!handler.CliDB.AreaTableStorage.TryGetValue(areaId, out var areaEntry))
        {
            handler.SendSysMessage(CypherStrings.InvalidZoneCoord, x, y, areaId);

            return false;
        }

        // update to parent zone if exist (client map show only zones without parents)
        var zoneEntry = areaEntry.ParentAreaID != 0 ? handler.CliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID) : areaEntry;

        x /= 100.0f;
        y /= 100.0f;

        var terrain = handler.ClassFactory.Resolve<TerrainManager>().LoadTerrain(zoneEntry.ContinentID);

        if (!handler.ClassFactory.Resolve<DB2Manager>().Zone2MapCoordinates(areaEntry.ParentAreaID != 0 ? areaEntry.ParentAreaID : areaId, ref x, ref y))
        {
            handler.SendSysMessage(CypherStrings.InvalidZoneMap, areaId, areaEntry.AreaName[handler.SessionDbcLocale], terrain.GetId(), terrain.GetMapName());

            return false;
        }

        if (!handler.ClassFactory.Resolve<GridDefines>().IsValidMapCoord(zoneEntry.ContinentID, x, y))
        {
            handler.SendSysMessage(CypherStrings.InvalidTargetCoord, x, y, zoneEntry.ContinentID);

            return false;
        }

        // stop flight if need
        if (player.IsInFlight)
            player.FinishTaxiFlight();
        else
            player.SaveRecallPosition(); // save only in non-flight case

        var z = Math.Max(terrain.GetStaticHeight(handler.ClassFactory.Resolve<PhasingHandler>().EmptyPhaseShift, zoneEntry.ContinentID, x, y, MapConst.MaxHeight), terrain.GetWaterLevel(handler.ClassFactory.Resolve<PhasingHandler>().EmptyPhaseShift, zoneEntry.ContinentID, x, y));

        player.TeleportTo(zoneEntry.ContinentID, x, y, z, player.Location.Orientation);

        return true;
    }

    [CommandGroup("creature")]
    private class GoCommandCreature
    {
        [Command("id", RBACPermissions.CommandGo)]
        private static bool HandleGoCreatureCIdCommand(CommandHandler handler, uint id)
        {
            CreatureData spawnpoint = null;

            foreach (var pair in handler.ObjectManager.AllCreatureData)
            {
                if (pair.Value.Id != id)
                    continue;

                if (spawnpoint == null)
                    spawnpoint = pair.Value;
                else
                {
                    handler.SendSysMessage(CypherStrings.CommandGocreatmultiple);

                    break;
                }
            }

            if (spawnpoint == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);

                return false;
            }

            return DoTeleport(handler, spawnpoint.SpawnPoint, spawnpoint.MapId);
        }

        [Command("", RBACPermissions.CommandGo)]
        private static bool HandleGoCreatureSpawnIdCommand(CommandHandler handler, ulong spawnId)
        {
            var spawnpoint = handler.ObjectManager.GetCreatureData(spawnId);

            if (spawnpoint != null)
                return DoTeleport(handler, spawnpoint.SpawnPoint, spawnpoint.MapId);

            handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);

            return false;

        }
    }

    [CommandGroup("gameobject")]
    private class GoCommandGameobject
    {
        [Command("id", RBACPermissions.CommandGo)]
        private static bool HandleGoGameObjectGOIdCommand(CommandHandler handler, uint goId)
        {
            GameObjectData spawnpoint = null;

            foreach (var pair in handler.ObjectManager.AllGameObjectData)
            {
                if (pair.Value.Id != goId)
                    continue;

                if (spawnpoint == null)
                    spawnpoint = pair.Value;
                else
                {
                    handler.SendSysMessage(CypherStrings.CommandGocreatmultiple);

                    break;
                }
            }

            if (spawnpoint == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGoobjnotfound);

                return false;
            }

            return DoTeleport(handler, spawnpoint.SpawnPoint, spawnpoint.MapId);
        }

        [Command("", RBACPermissions.CommandGo)]
        private static bool HandleGoGameObjectSpawnIdCommand(CommandHandler handler, ulong spawnId)
        {
            var spawnpoint = handler.ObjectManager.GetGameObjectData(spawnId);

            if (spawnpoint == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGoobjnotfound);

                return false;
            }

            return DoTeleport(handler, spawnpoint.SpawnPoint, spawnpoint.MapId);
        }
    }
}