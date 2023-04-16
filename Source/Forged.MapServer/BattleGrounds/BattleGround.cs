﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Events;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Miscellaneous;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.BattleGround;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Text;
using Framework.Constants;
using Framework.Database;
using Serilog;
using Transport = Forged.MapServer.Entities.Transport;

namespace Forged.MapServer.BattleGrounds;

public class Battleground : ZoneScript, IDisposable
{
    public Battleground(BattlegroundTemplate battlegroundTemplate)
    {
        _battlegroundTemplate = battlegroundTemplate;
        _randomTypeID = BattlegroundTypeId.None;
        Status = BattlegroundStatus.None;
        _winnerTeamId = PvPTeamId.Neutral;

        MHonorMode = BGHonorMode.Normal;

        StartDelayTimes[BattlegroundConst.EventIdFirst] = BattlegroundStartTimeIntervals.Delay2m;
        StartDelayTimes[BattlegroundConst.EventIdSecond] = BattlegroundStartTimeIntervals.Delay1m;
        StartDelayTimes[BattlegroundConst.EventIdThird] = BattlegroundStartTimeIntervals.Delay30s;
        StartDelayTimes[BattlegroundConst.EventIdFourth] = BattlegroundStartTimeIntervals.None;

        StartMessageIds[BattlegroundConst.EventIdFirst] = BattlegroundBroadcastTexts.StartTwoMinutes;
        StartMessageIds[BattlegroundConst.EventIdSecond] = BattlegroundBroadcastTexts.StartOneMinute;
        StartMessageIds[BattlegroundConst.EventIdThird] = BattlegroundBroadcastTexts.StartHalfMinute;
        StartMessageIds[BattlegroundConst.EventIdFourth] = BattlegroundBroadcastTexts.HasBegun;
    }

    public virtual void Dispose()
    {
        // remove objects and creatures
        // (this is done automatically in mapmanager update, when the instance is reset after the reset time)
        for (var i = 0; i < BgCreatures.Length; ++i)
            DelCreature(i);

        for (var i = 0; i < BgObjects.Length; ++i)
            DelObject(i);

        Global.BattlegroundMgr.RemoveBattleground(GetTypeID(), InstanceID);

        // unload map
        if (_map)
        {
            _map.UnloadAll(); // unload all objects (they may hold a reference to bg in their ZoneScript pointer)
            _map.SetUnload(); // mark for deletion by MMapManager

            //unlink to prevent crash, always unlink all pointer reference before destruction
            _map.SetBG(null);
            _map = null;
        }

        // remove from bg free slot queue
        RemoveFromBGFreeSlotQueue();
    }

    public static int GetTeamIndexByTeamId(TeamFaction team)
    {
        return team == TeamFaction.Alliance ? TeamIds.Alliance : TeamIds.Horde;
    }

    public Player _GetPlayer(ObjectGuid guid, bool offlineRemove, string context)
    {
        Player player = null;

        if (!offlineRemove)
        {
            player = Global.ObjAccessor.FindPlayer(guid);

            if (!player)
                Log.Logger.Error($"Battleground.{context}: player ({guid}) not found for BG (map: {MapId}, instance id: {_instanceID})!");
        }

        return player;
    }

    public Player _GetPlayer(KeyValuePair<ObjectGuid, BattlegroundPlayer> pair, string context)
    {
        return _GetPlayer(pair.Key, pair.Value.OfflineRemoveTime != 0, context);
    }

    public virtual Creature AddCreature(uint entry, int type, float x, float y, float z, float o, int teamIndex = TeamIds.Neutral, uint respawntime = 0, Transport transport = null)
    {
        Map map = FindBgMap();

        if (!map)
            return null;

        if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
        {
            Log.Logger.Error($"Battleground.AddCreature: creature template (entry: {entry}) does not exist for BG (map: {MapId}, instance id: {_instanceID})!");

            return null;
        }


        if (transport)
        {
            Creature transCreature = transport.SummonPassenger(entry, new Position(x, y, z, o), TempSummonType.ManualDespawn);

            if (transCreature)
            {
                BgCreatures[type] = transCreature.GUID;

                return transCreature;
            }

            return null;
        }

        Position pos = new(x, y, z, o);

        var creature = CreatureFactory.CreateCreature(entry, map, pos);

        if (!creature)
        {
            Log.Logger.Error($"Battleground.AddCreature: cannot create creature (entry: {entry}) for BG (map: {MapId}, instance id: {_instanceID})!");

            return null;
        }

        creature.HomePosition = pos;

        if (!map.AddToMap(creature))
            return null;

        BgCreatures[type] = creature.GUID;

        if (respawntime != 0)
            creature.RespawnDelay = respawntime;

        return creature;
    }

    public Creature AddCreature(uint entry, int type, Position pos, int teamIndex = TeamIds.Neutral, uint respawntime = 0, Transport transport = null)
    {
        return AddCreature(entry, type, pos.X, pos.Y, pos.Z, pos.Orientation, teamIndex, respawntime, transport);
    }

    public bool AddObject(int type, uint entry, float x, float y, float z, float o, float rotation0, float rotation1, float rotation2, float rotation3, uint respawnTime = 0, GameObjectState goState = GameObjectState.Ready)
    {
        Map map = FindBgMap();

        if (!map)
            return false;

        Quaternion rotation = new(rotation0, rotation1, rotation2, rotation3);

        // Temporally add safety check for bad spawns and send log (object rotations need to be rechecked in sniff)
        if (rotation0 == 0 && rotation1 == 0 && rotation2 == 0 && rotation3 == 0)
        {
            Log.Logger.Debug($"Battleground.AddObject: gameoobject [entry: {entry}, object type: {type}] for BG (map: {MapId}) has zeroed rotation fields, " +
                             "orientation used temporally, but please fix the spawn");

            rotation = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(o, 0.0f, 0.0f));
        }

        // Must be created this way, adding to godatamap would add it to the base map of the instance
        // and when loading it (in go.LoadFromDB()), a new guid would be assigned to the object, and a new object would be created
        // So we must create it specific for this instance
        var go = GameObjectFactory.CreateGameObject(entry, BgMap, new Position(x, y, z, o), rotation, 255, goState);

        if (!go)
        {
            Log.Logger.Error($"Battleground.AddObject: cannot create gameobject (entry: {entry}) for BG (map: {MapId}, instance id: {_instanceID})!");

            return false;
        }

        // Add to world, so it can be later looked up from HashMapHolder
        if (!map.AddToMap(go))
            return false;

        BgObjects[type] = go.GUID;

        return true;
    }

    public bool AddObject(int type, uint entry, Position pos, float rotation0, float rotation1, float rotation2, float rotation3, uint respawnTime = 0, GameObjectState goState = GameObjectState.Ready)
    {
        return AddObject(type, entry, pos.X, pos.Y, pos.Z, pos.Orientation, rotation0, rotation1, rotation2, rotation3, respawnTime, goState);
    }

    // this method adds player to his team's bg group, or sets his correct group if player is already in bg group
    public void AddOrSetPlayerToCorrectBgGroup(Player player, TeamFaction team)
    {
        var playerGuid = player.GUID;
        var group = GetBgRaid(team);

        if (!group) // first player joined
        {
            group = new PlayerGroup();
            SetBgRaid(team, group);
            group.Create(player);
        }
        else // raid already exist
        {
            if (group.IsMember(playerGuid))
            {
                var subgroup = group.GetMemberGroup(playerGuid);
                player.SetBattlegroundOrBattlefieldRaid(group, subgroup);
            }
            else
            {
                group.AddMember(player);
                var originalGroup = player.OriginalGroup;

                if (originalGroup)
                    if (originalGroup.IsLeader(playerGuid))
                    {
                        group.ChangeLeader(playerGuid);
                        group.SendUpdate();
                    }
            }
        }
    }

    public virtual void AddPlayer(Player player)
    {
        // remove afk from player
        if (player.IsAfk)
            player.ToggleAfk();

        // score struct must be created in inherited class

        var guid = player.GUID;
        var team = player.GetBgTeam();

        BattlegroundPlayer bp = new()
        {
            OfflineRemoveTime = 0,
            Team = team,
            ActiveSpec = (int)player.GetPrimarySpecialization(),
            Mercenary = player.IsMercenaryForBattlegroundQueueType(GetQueueId())
        };

        var isInBattleground = IsPlayerInBattleground(player.GUID);
        // Add to list/maps
        _players[guid] = bp;

        if (!isInBattleground)
            UpdatePlayersCountByTeam(team, false); // +1 player

        BattlegroundPlayerJoined playerJoined = new()
        {
            Guid = player.GUID
        };

        SendPacketToTeam(team, playerJoined, player);

        PVPMatchInitialize pvpMatchInitialize = new()
        {
            MapID = MapId
        };

        pvpMatchInitialize.State = Status switch
        {
            BattlegroundStatus.None       => PVPMatchInitialize.MatchState.Inactive,
            BattlegroundStatus.WaitQueue  => PVPMatchInitialize.MatchState.Inactive,
            BattlegroundStatus.WaitJoin   => PVPMatchInitialize.MatchState.InProgress,
            BattlegroundStatus.InProgress => PVPMatchInitialize.MatchState.InProgress,
            BattlegroundStatus.WaitLeave  => PVPMatchInitialize.MatchState.Complete,
            _                             => pvpMatchInitialize.State
        };

        if (ElapsedTime >= (int)BattlegroundStartTimeIntervals.Delay2m)
        {
            pvpMatchInitialize.Duration = (int)(ElapsedTime - (int)BattlegroundStartTimeIntervals.Delay2m) / Time.IN_MILLISECONDS;
            pvpMatchInitialize.StartTime = GameTime.CurrentTime - pvpMatchInitialize.Duration;
        }

        pvpMatchInitialize.ArenaFaction = (byte)(player.GetBgTeam() == TeamFaction.Horde ? PvPTeamId.Horde : PvPTeamId.Alliance);
        pvpMatchInitialize.BattlemasterListID = (uint)GetTypeID();
        pvpMatchInitialize.Registered = false;
        pvpMatchInitialize.AffectsRating = IsRated;

        player.SendPacket(pvpMatchInitialize);

        player.RemoveAurasByType(AuraType.Mounted);

        // add arena specific auras
        if (IsArena)
        {
            player.RemoveArenaEnchantments(EnchantmentSlot.Temp);

            player.DestroyConjuredItems(true);
            player.UnsummonPetTemporaryIfAny();

            if (Status == BattlegroundStatus.WaitJoin) // not started yet
            {
                player.CastSpell(player, BattlegroundConst.SpellArenaPreparation, true);
                player.ResetAllPowers();
            }
        }
        else
        {
            if (Status == BattlegroundStatus.WaitJoin) // not started yet
            {
                player.CastSpell(player, BattlegroundConst.SpellPreparation, true); // reduces all mana cost of spells.

                var countdownMaxForBGType = IsArena ? BattlegroundConst.ArenaCountdownMax : BattlegroundConst.BattlegroundCountdownMax;

                StartTimer timer = new()
                {
                    Type = TimerType.Pvp,
                    TimeLeft = countdownMaxForBGType - ElapsedTime / 1000,
                    TotalTime = countdownMaxForBGType
                };

                player.SendPacket(timer);
            }

            if (bp.Mercenary)
            {
                if (bp.Team == TeamFaction.Horde)
                {
                    player.CastSpell(player, BattlegroundConst.SpellMercenaryHorde1, true);
                    player.CastSpell(player, BattlegroundConst.SpellMercenaryHordeReactions, true);
                }
                else if (bp.Team == TeamFaction.Alliance)
                {
                    player.CastSpell(player, BattlegroundConst.SpellMercenaryAlliance1, true);
                    player.CastSpell(player, BattlegroundConst.SpellMercenaryAllianceReactions, true);
                }

                player.CastSpell(player, BattlegroundConst.SpellMercenaryShapeshift);
                player.SetPlayerFlagEx(PlayerFlagsEx.MercenaryMode);
            }
        }

        // reset all map criterias on map enter
        if (!isInBattleground)
            player.ResetCriteria(CriteriaFailEvent.LeaveBattleground, MapId, true);

        // setup BG group membership
        PlayerAddedToBGCheckIfBGIsRunning(player);
        AddOrSetPlayerToCorrectBgGroup(player, team);
    }

    public void AddPlayerPosition(BattlegroundPlayerPosition position)
    {
        _playerPositions.Add(position);
    }

    public void AddPlayerToResurrectQueue(ObjectGuid npcGUID, ObjectGuid playerGUID)
    {
        _reviveQueue.Add(npcGUID, playerGUID);

        var player = Global.ObjAccessor.FindPlayer(playerGUID);

        if (!player)
            return;

        player.CastSpell(player, BattlegroundConst.SpellWaitingForResurrect, true);
    }

    public bool AddSpiritGuide(int type, float x, float y, float z, float o, int teamIndex)
    {
        var entry = (uint)(teamIndex == TeamIds.Alliance ? BattlegroundCreatures.A_SpiritGuide : BattlegroundCreatures.H_SpiritGuide);

        var creature = AddCreature(entry, type, x, y, z, o);

        if (creature)
        {
            creature.SetDeathState(DeathState.Dead);
            creature.AddChannelObject(creature.GUID);

            // aura
            //todo Fix display here
            // creature.SetVisibleAura(0, SPELL_SPIRIT_HEAL_CHANNEL);
            // casting visual effect
            creature. // aura
                //todo Fix display here
                // creature.SetVisibleAura(0, SPELL_SPIRIT_HEAL_CHANNEL);
                // casting visual effect
                ChannelSpellId = BattlegroundConst.SpellSpiritHealChannel;

            creature.SetChannelVisual(new SpellCastVisual(BattlegroundConst.SpellSpiritHealChannelVisual, 0));

            //creature.CastSpell(creature, SPELL_SPIRIT_HEAL_CHANNEL, true);
            return true;
        }

        Log.Logger.Error($"Battleground.AddSpiritGuide: cannot create spirit guide (type: {type}, entry: {entry}) for BG (map: {MapId}, instance id: {_instanceID})!");
        EndNow();

        return false;
    }

    public bool AddSpiritGuide(int type, Position pos, int teamIndex = TeamIds.Neutral)
    {
        return AddSpiritGuide(type, pos.X, pos.Y, pos.Z, pos.Orientation, teamIndex);
    }

    public virtual void BuildPvPLogDataPacket(out PVPMatchStatistics pvpLogData)
    {
        pvpLogData = new PVPMatchStatistics();

        foreach (var score in PlayerScores)
        {
            score.Value.BuildPvPLogPlayerDataPacket(out var playerData);

            var player = Global.ObjAccessor.GetPlayer(BgMap, playerData.PlayerGUID);

            if (player)
            {
                playerData.IsInWorld = true;
                playerData.PrimaryTalentTree = (int)player.GetPrimarySpecialization();
                playerData.Sex = (int)player.Gender;
                playerData.PlayerRace = player.Race;
                playerData.PlayerClass = (int)player.Class;
                playerData.HonorLevel = (int)player.HonorLevel;
            }

            pvpLogData.Statistics.Add(playerData);
        }

        pvpLogData.PlayerCount[(int)PvPTeamId.Horde] = (sbyte)GetPlayersCountByTeam(TeamFaction.Horde);
        pvpLogData.PlayerCount[(int)PvPTeamId.Alliance] = (sbyte)GetPlayersCountByTeam(TeamFaction.Alliance);
    }

    public virtual bool CanActivateGO(int entry, uint team)
    {
        return true;
    }

    public void CastSpellOnTeam(uint spellID, TeamFaction team)
    {
        foreach (var pair in _players)
        {
            var player = _GetPlayerForTeam(team, pair, "CastSpellOnTeam");

            if (player)
                player.CastSpell(player, spellID, true);
        }
    }

    public virtual void CheckWinConditions() { }

    public void DecreaseInvitedCount(TeamFaction team)
    {
        if (team == TeamFaction.Alliance)
            --_invitedAlliance;
        else
            --_invitedHorde;
    }

    public bool DelCreature(int type)
    {
        if (BgCreatures[type].IsEmpty)
            return true;

        var creature = BgMap.GetCreature(BgCreatures[type]);

        if (creature)
        {
            creature.Location.AddObjectToRemoveList();
            BgCreatures[type].Clear();

            return true;
        }

        Log.Logger.Error($"Battleground.DelCreature: creature (type: {type}, {BgCreatures[type]}) not found for BG (map: {MapId}, instance id: {_instanceID})!");
        BgCreatures[type].Clear();

        return false;
    }

    public bool DelObject(int type)
    {
        if (BgObjects[type].IsEmpty)
            return true;

        var obj = BgMap.GetGameObject(BgObjects[type]);

        if (obj)
        {
            obj.SetRespawnTime(0); // not save respawn time
            obj.Delete();
            BgObjects[type].Clear();

            return true;
        }

        Log.Logger.Error($"Battleground.DelObject: gameobject (type: {type}, {BgObjects[type]}) not found for BG (map: {MapId}, instance id: {_instanceID})!");
        BgObjects[type].Clear();

        return false;
    }

    public virtual void DestroyGate(Player player, GameObject go) { }

    // this function can be used by spell to interact with the BG map
    public virtual void DoAction(uint action, ulong arg) { }

    // Some doors aren't despawned so we cannot handle their closing in gameobject.update()
    // It would be nice to correctly implement GO_ACTIVATED state and open/close doors in gameobject code
    public void DoorClose(int type)
    {
        var obj = BgMap.GetGameObject(BgObjects[type]);

        if (obj)
        {
            // If doors are open, close it
            if (obj.LootState == LootState.Activated && obj.GoState != GameObjectState.Ready)
            {
                obj.SetLootState(LootState.Ready);
                obj.SetGoState(GameObjectState.Ready);
            }
        }
        else
        {
            Log.Logger.Error($"Battleground.DoorClose: door gameobject (type: {type}, {BgObjects[type]}) not found for BG (map: {MapId}, instance id: {_instanceID})!");
        }
    }

    public void DoorOpen(int type)
    {
        var obj = BgMap.GetGameObject(BgObjects[type]);

        if (obj)
        {
            obj.SetLootState(LootState.Activated);
            obj.SetGoState(GameObjectState.Active);
        }
        else
        {
            Log.Logger.Error($"Battleground.DoorOpen: door gameobject (type: {type}, {BgObjects[type]}) not found for BG (map: {MapId}, instance id: {_instanceID})!");
        }
    }

    public virtual void EndBattleground(TeamFaction winner)
    {
        RemoveFromBGFreeSlotQueue();

        var guildAwarded = false;

        if (winner == TeamFaction.Alliance)
        {
            if (IsBattleground)
                SendBroadcastText(BattlegroundBroadcastTexts.AllianceWins, ChatMsg.BgSystemNeutral);

            PlaySoundToAll((uint)BattlegroundSounds.AllianceWins);
            SetWinner(PvPTeamId.Alliance);
        }
        else if (winner == TeamFaction.Horde)
        {
            if (IsBattleground)
                SendBroadcastText(BattlegroundBroadcastTexts.HordeWins, ChatMsg.BgSystemNeutral);

            PlaySoundToAll((uint)BattlegroundSounds.HordeWins);
            SetWinner(PvPTeamId.Horde);
        }
        else
        {
            SetWinner(PvPTeamId.Neutral);
        }

        PreparedStatement stmt;
        ulong battlegroundId = 1;

        if (IsBattleground && GetDefaultValue("Battleground:StoreStatistics:Enable", false))
        {
            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PVPSTATS_MAXID);
            var result = DB.Characters.Query(stmt);

            if (!result.IsEmpty())
                battlegroundId = result.Read<ulong>(0) + 1;

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PVPSTATS_BATTLEGROUND);
            stmt.AddValue(0, battlegroundId);
            stmt.AddValue(1, (byte)GetWinner());
            stmt.AddValue(2, GetUniqueBracketId());
            stmt.AddValue(3, (byte)GetTypeID(true));
            DB.Characters.Execute(stmt);
        }

        SetStatus(BattlegroundStatus.WaitLeave);
        //we must set it this way, because end time is sent in packet!
        SetRemainingTime(BattlegroundConst.AutocloseBattleground);

        PVPMatchComplete pvpMatchComplete = new()
        {
            Winner = (byte)GetWinner(),
            Duration = (int)Math.Max(0, (ElapsedTime - (int)BattlegroundStartTimeIntervals.Delay2m) / Time.IN_MILLISECONDS)
        };

        BuildPvPLogDataPacket(out pvpMatchComplete.LogData);
        pvpMatchComplete.Write();

        foreach (var pair in _players)
        {
            var team = pair.Value.Team;

            var player = _GetPlayer(pair, "EndBattleground");

            if (!player)
                continue;

            // should remove spirit of redemption
            if (player.HasAuraType(AuraType.SpiritOfRedemption))
                player.RemoveAurasByType(AuraType.ModShapeshift);

            if (!player.IsAlive)
            {
                player.ResurrectPlayer(1.0f);
                player.SpawnCorpseBones();
            }
            else
            {
                //needed cause else in av some creatures will kill the players at the end
                player.CombatStop();
            }

            // remove temporary currency bonus auras before rewarding player
            player.RemoveAura(BattlegroundConst.SpellHonorableDefender25y);
            player.RemoveAura(BattlegroundConst.SpellHonorableDefender60y);

            var winnerKills = player.GetRandomWinner() ? GetDefaultValue("Battleground:RewardWinnerHonorLast", 13500) : GetDefaultValue("Battleground:RewardWinnerHonorFirst", 27000);
            var loserKills = player.GetRandomWinner() ? GetDefaultValue("Battleground:RewardLoserHonorLast", 3500) : GetDefaultValue("Battleground:RewardLoserHonorFirst", 4500);

            if (IsBattleground && GetDefaultValue("Battleground:StoreStatistics:Enable", false))
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PVPSTATS_PLAYER);
                var score = PlayerScores.LookupByKey(player.GUID);

                stmt.AddValue(0, battlegroundId);
                stmt.AddValue(1, player.GUID.Counter);
                stmt.AddValue(2, team == winner);
                stmt.AddValue(3, score.KillingBlows);
                stmt.AddValue(4, score.Deaths);
                stmt.AddValue(5, score.HonorableKills);
                stmt.AddValue(6, score.BonusHonor);
                stmt.AddValue(7, score.DamageDone);
                stmt.AddValue(8, score.HealingDone);
                stmt.AddValue(9, score.GetAttr1());
                stmt.AddValue(10, score.GetAttr2());
                stmt.AddValue(11, score.GetAttr3());
                stmt.AddValue(12, score.GetAttr4());
                stmt.AddValue(13, score.GetAttr5());

                DB.Characters.Execute(stmt);
            }

            // Reward winner team
            if (team == winner)
            {
                if (IsRandom || Global.BattlegroundMgr.IsBGWeekend(GetTypeID()))
                {
                    UpdatePlayerScore(player, ScoreType.BonusHonor, GetBonusHonorFromKill(winnerKills));

                    if (!player.GetRandomWinner())
                        player.SetRandomWinner(true);
                    // TODO: win honor xp
                }
                else
                {
                    // TODO: lose honor xp
                }

                player.UpdateCriteria(CriteriaType.WinBattleground, player.Location.MapId);

                if (!guildAwarded)
                {
                    guildAwarded = true;
                    var guildId = BgMap.GetOwnerGuildId(player.GetBgTeam());

                    if (guildId != 0)
                    {
                        var guild = Global.GuildMgr.GetGuildById(guildId);

                        if (guild)
                            guild.UpdateCriteria(CriteriaType.WinBattleground, player.Location.MapId, 0, 0, null, player);
                    }
                }
            }
            else
            {
                if (IsRandom || Global.BattlegroundMgr.IsBGWeekend(GetTypeID()))
                    UpdatePlayerScore(player, ScoreType.BonusHonor, GetBonusHonorFromKill(loserKills));
            }

            player.ResetAllPowers();
            player.CombatStopWithPets(true);

            BlockMovement(player);

            player.SendPacket(pvpMatchComplete);

            player.UpdateCriteria(CriteriaType.ParticipateInBattleground, player.Location.MapId);
        }
    }

    public virtual void EventPlayerClickedOnFlag(Player player, GameObject targetObj) { }

    // Battleground events
    public virtual void EventPlayerDroppedFlag(Player player) { }

    // This method should be called when player logs into running Battleground
    public void EventPlayerLoggedIn(Player player)
    {
        var guid = player.GUID;

        // player is correct pointer
        foreach (var id in _offlineQueue)
            if (id == guid)
            {
                _offlineQueue.Remove(id);

                break;
            }

        _players[guid].OfflineRemoveTime = 0;
        PlayerAddedToBGCheckIfBGIsRunning(player);
        // if Battleground is starting, then add preparation aura
        // we don't have to do that, because preparation aura isn't removed when player logs out
    }

    // This method should be called when player logs out from running Battleground
    public void EventPlayerLoggedOut(Player player)
    {
        var guid = player.GUID;

        if (!IsPlayerInBattleground(guid)) // Check if this player really is in Battleground (might be a GM who teleported inside)
            return;

        // player is correct pointer, it is checked in WorldSession.LogoutPlayer()
        _offlineQueue.Add(player.GUID);
        _players[guid].OfflineRemoveTime = GameTime.CurrentTime + BattlegroundConst.MaxOfflineTime;

        if (Status == BattlegroundStatus.InProgress)
        {
            // drop Id and handle other cleanups
            RemovePlayer(player, guid, GetPlayerTeam(guid));

            // 1 player is logging out, if it is the last alive, then end arena!
            if (IsArena && player.IsAlive)
                if (GetAlivePlayersCountByTeam(player.GetBgTeam()) <= 1 && GetPlayersCountByTeam(GetOtherTeam(player.GetBgTeam())) != 0)
                    EndBattleground(GetOtherTeam(player.GetBgTeam()));
        }
    }

    public uint GetAlivePlayersCountByTeam(TeamFaction team)
    {
        uint count = 0;

        foreach (var pair in _players)
            if (pair.Value.Team == team)
            {
                var player = Global.ObjAccessor.FindPlayer(pair.Key);

                if (player && player.IsAlive)
                    ++count;
            }

        return count;
    }

    public uint GetArenaMatchmakerRating(TeamFaction team)
    {
        return _arenaTeamMmr[GetTeamIndexByTeamId(team)];
    }

    public uint GetArenaTeamIdByIndex(uint index)
    {
        return _arenaTeamIds[index];
    }

    public uint GetArenaTeamIdForTeam(TeamFaction team)
    {
        return _arenaTeamIds[GetTeamIndexByTeamId(team)];
    }

    public ArenaTypes ArenaType => _arenaType;

    public Creature GetBGCreature(int type)
    {
        if (BgCreatures[type].IsEmpty)
            return null;

        var creature = BgMap.GetCreature(BgCreatures[type]);

        if (!creature)
            Log.Logger.Error($"Battleground.GetBGCreature: creature (type: {type}, {BgCreatures[type]}) not found for BG (map: {MapId}, instance id: {_instanceID})!");

        return creature;
    }

    public BattlegroundMap BgMap => _map;

    public GameObject GetBGObject(int type)
    {
        if (BgObjects[type].IsEmpty)
            return null;

        var obj = BgMap.GetGameObject(BgObjects[type]);

        if (!obj)
            Log.Logger.Error($"Battleground.GetBGObject: gameobject (type: {type}, {BgObjects[type]}) not found for BG (map: {MapId}, instance id: {_instanceID})!");

        return obj;
    }

    public uint GetBonusHonorFromKill(uint kills)
    {
        //variable kills means how many honorable kills you scored (so we need kills * honor_for_one_kill)
        var maxLevel = Math.Min(MaxLevel, 80U);

        return Formulas.HkHonorAtLevel(maxLevel, kills);
    }

    public BattlegroundBracketId BracketId => _pvpDifficultyEntry.BracketId;

    public uint ClientInstanceID => _clientInstanceID;

    public virtual WorldSafeLocsEntry GetClosestGraveYard(Player player)
    {
        return Global.ObjectMgr.GetClosestGraveYard(player.Location, GetPlayerTeam(player.GUID), player);
    }

    public Battleground GetCopy()
    {
        return (Battleground)MemberwiseClone();
    }

    public uint ElapsedTime => _startTime;

    public virtual WorldSafeLocsEntry GetExploitTeleportLocation(TeamFaction team)
    {
        return null;
    }

    public virtual ObjectGuid GetFlagPickerGUID(int teamIndex = -1)
    {
        return ObjectGuid.Empty;
    }

    // get the number of free slots for team
    // returns the number how many players can join Battleground to MaxPlayersPerTeam
    public uint GetFreeSlotsForTeam(TeamFaction team)
    {
        // if BG is starting and WorldCfg.BattlegroundInvitationType == BattlegroundQueueInvitationTypeB.NoBalance, invite anyone
        if (Status == BattlegroundStatus.WaitJoin && GetDefaultValue("Battleground:InvitationType", 0) == (int)BattlegroundQueueInvitationType.NoBalance)
            return GetInvitedCount(team) < GetMaxPlayersPerTeam() ? GetMaxPlayersPerTeam() - GetInvitedCount(team) : 0;

        // if BG is already started or WorldCfg.BattlegroundInvitationType != BattlegroundQueueInvitationType.NoBalance, do not allow to join too much players of one faction
        uint otherTeamInvitedCount;
        uint thisTeamInvitedCount;
        uint otherTeamPlayersCount;
        uint thisTeamPlayersCount;

        if (team == TeamFaction.Alliance)
        {
            thisTeamInvitedCount = GetInvitedCount(TeamFaction.Alliance);
            otherTeamInvitedCount = GetInvitedCount(TeamFaction.Horde);
            thisTeamPlayersCount = GetPlayersCountByTeam(TeamFaction.Alliance);
            otherTeamPlayersCount = GetPlayersCountByTeam(TeamFaction.Horde);
        }
        else
        {
            thisTeamInvitedCount = GetInvitedCount(TeamFaction.Horde);
            otherTeamInvitedCount = GetInvitedCount(TeamFaction.Alliance);
            thisTeamPlayersCount = GetPlayersCountByTeam(TeamFaction.Horde);
            otherTeamPlayersCount = GetPlayersCountByTeam(TeamFaction.Alliance);
        }

        if (Status == BattlegroundStatus.InProgress || Status == BattlegroundStatus.WaitJoin)
        {
            // difference based on ppl invited (not necessarily entered battle)
            // default: allow 0
            uint diff = 0;

            // allow join one person if the sides are equal (to fill up bg to minPlayerPerTeam)
            if (otherTeamInvitedCount == thisTeamInvitedCount)
                diff = 1;
            // allow join more ppl if the other side has more players
            else if (otherTeamInvitedCount > thisTeamInvitedCount)
                diff = otherTeamInvitedCount - thisTeamInvitedCount;

            // difference based on max players per team (don't allow inviting more)
            var diff2 = thisTeamInvitedCount < GetMaxPlayersPerTeam() ? GetMaxPlayersPerTeam() - thisTeamInvitedCount : 0;
            // difference based on players who already entered
            // default: allow 0
            uint diff3 = 0;

            // allow join one person if the sides are equal (to fill up bg minPlayerPerTeam)
            if (otherTeamPlayersCount == thisTeamPlayersCount)
                diff3 = 1;
            // allow join more ppl if the other side has more players
            else if (otherTeamPlayersCount > thisTeamPlayersCount)
                diff3 = otherTeamPlayersCount - thisTeamPlayersCount;
            // or other side has less than minPlayersPerTeam
            else if (thisTeamInvitedCount <= MinPlayersPerTeam)
                diff3 = MinPlayersPerTeam - thisTeamInvitedCount + 1;

            // return the minimum of the 3 differences

            // min of diff and diff 2
            diff = Math.Min(diff, diff2);

            // min of diff, diff2 and diff3
            return Math.Min(diff, diff3);
        }

        return 0;
    }

    public uint InstanceID => _instanceID;

    public uint LastResurrectTime => _lastResurrectTime;

    public uint MapId => (uint)_battlegroundTemplate.BattlemasterEntry.MapId[0];

    public uint MaxLevel => _pvpDifficultyEntry?.MaxLevel ?? _battlegroundTemplate.MaxLevel;

    public int GetMaxPlayersPerTeam()
    {
        if (!IsArena)
            return _battlegroundTemplate.MaxPlayersPerTeam;

        return ArenaType switch
        {
            ArenaTypes.Team2v2 => 2,
            ArenaTypes.Team3v3 => 3,
            ArenaTypes.Team5v5 => // removed
                5,
            _ => _battlegroundTemplate.MaxPlayersPerTeam
        };
    }

    public uint MinLevel => _pvpDifficultyEntry?.MinLevel ?? _battlegroundTemplate.MinLevel;

    public int MinPlayersPerTeam => _battlegroundTemplate.MinPlayersPerTeam;

    public string Name => _battlegroundTemplate.BattlemasterEntry.Name[Global.WorldMgr.DefaultDbcLocale];

    public TeamFaction GetOtherTeam(TeamFaction teamId)
    {
        return teamId switch
        {
            TeamFaction.Alliance => TeamFaction.Horde,
            TeamFaction.Horde    => TeamFaction.Alliance,
            _                    => TeamFaction.Other
        };
    }

    public Dictionary<ObjectGuid, BattlegroundPlayer> GetPlayers()
    {
        return _players;
    }

    public uint GetPlayersCountByTeam(TeamFaction team)
    {
        return _playersCount[GetTeamIndexByTeamId(team)];
    }

    // Return the player's team based on Battlegroundplayer info
    // Used in same faction arena matches mainly
    public TeamFaction GetPlayerTeam(ObjectGuid guid)
    {
        if (_players.TryGetValue(guid, out var player))
            return player.Team;

        return 0;
    }

    public virtual TeamFaction GetPrematureWinner()
    {
        TeamFaction winner = 0;

        if (GetPlayersCountByTeam(TeamFaction.Alliance) >= MinPlayersPerTeam)
            winner = TeamFaction.Alliance;
        else if (GetPlayersCountByTeam(TeamFaction.Horde) >= MinPlayersPerTeam)
            winner = TeamFaction.Horde;

        return winner;
    }

    public BattlegroundQueueTypeId GetQueueId()
    {
        return _queueId;
    }

    public uint RemainingTime => (uint)_endTime;

    public BattlegroundStatus Status { get; private set; }

    public uint GetTeamScore(int teamIndex)
    {
        if (teamIndex is TeamIds.Alliance or TeamIds.Horde)
            return MTeamScores[teamIndex];

        Log.Logger.Error("GetTeamScore with wrong Team {0} for BG {1}", teamIndex, GetTypeID());

        return 0;
    }

    public WorldSafeLocsEntry GetTeamStartPosition(int teamId)
    {
        return _battlegroundTemplate.StartLocation[teamId];
    }

    public BattlegroundTypeId GetTypeID(bool getRandom = false)
    {
        return getRandom ? _randomTypeID : _battlegroundTemplate.Id;
    }

    public virtual void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        Log.Logger.Debug("Unhandled AreaTrigger {0} in Battleground {1}. Player coords (x: {2}, y: {3}, z: {4})",
                         trigger,
                         player.Location.MapId,
                         player.Location.X,
                         player.Location.Y,
                         player.Location.Z);
    }

    public virtual void HandleKillPlayer(Player victim, Player killer)
    {
        // Keep in mind that for arena this will have to be changed a bit

        // Add +1 deaths
        UpdatePlayerScore(victim, ScoreType.Deaths, 1);

        // Add +1 kills to group and +1 killing_blows to killer
        if (killer)
        {
            // Don't reward credit for killing ourselves, like fall damage of hellfire (warlock)
            if (killer == victim)
                return;

            var killerTeam = GetPlayerTeam(killer.GUID);

            UpdatePlayerScore(killer, ScoreType.HonorableKills, 1);
            UpdatePlayerScore(killer, ScoreType.KillingBlows, 1);

            foreach (var (guid, player) in _players)
            {
                var creditedPlayer = Global.ObjAccessor.FindPlayer(guid);

                if (!creditedPlayer || creditedPlayer == killer)
                    continue;

                if (player.Team == killerTeam && creditedPlayer.IsAtGroupRewardDistance(victim))
                    UpdatePlayerScore(creditedPlayer, ScoreType.HonorableKills, 1);
            }
        }

        if (!IsArena)
        {
            // To be able to remove insignia -- ONLY IN Battlegrounds
            victim.SetUnitFlag(UnitFlags.Skinnable);
            RewardXPAtKill(killer, victim);
        }
    }

    public virtual void HandleKillUnit(Creature creature, Player killer) { }

    public virtual void HandlePlayerResurrect(Player player) { }

    public virtual bool HandlePlayerUnderMap(Player player)
    {
        return false;
    }

    public virtual void HandleQuestComplete(uint questid, Player player) { }

    // IMPORTANT NOTICE:
    // buffs aren't spawned/despawned when players captures anything
    // buffs are in their positions when Battleground starts
    public void HandleTriggerBuff(ObjectGuid goGuid)
    {
        if (!FindBgMap())
        {
            Log.Logger.Error($"Battleground::HandleTriggerBuff called with null bg map, {goGuid}");

            return;
        }

        var obj = BgMap.GetGameObject(goGuid);

        if (!obj || obj.GoType != GameObjectTypes.Trap || !obj.IsSpawned)
            return;

        // Change buff type, when buff is used:
        var index = BgObjects.Length - 1;

        while (index >= 0 && BgObjects[index] != goGuid)
            index--;

        if (index < 0)
        {
            Log.Logger.Error($"Battleground.HandleTriggerBuff: cannot find buff gameobject ({goGuid}, entry: {obj.Entry}, type: {obj.GoType}) in internal data for BG (map: {MapId}, instance id: {_instanceID})!");

            return;
        }

        // Randomly select new buff
        var buff = RandomHelper.IRand(0, 2);
        var entry = obj.Entry;

        if (MBuffChange && entry != BuffEntries[buff])
        {
            // Despawn current buff
            SpawnBGObject(index, BattlegroundConst.RespawnOneDay);

            // Set index for new one
            for (byte currBuffTypeIndex = 0; currBuffTypeIndex < 3; ++currBuffTypeIndex)
                if (entry == BuffEntries[currBuffTypeIndex])
                {
                    index -= currBuffTypeIndex;
                    index += buff;
                }
        }

        SpawnBGObject(index, BattlegroundConst.BuffRespawnTime);
    }

    public bool HasFreeSlots => GetPlayersSize() < GetMaxPlayers();

    public void IncreaseInvitedCount(TeamFaction team)
    {
        if (team == TeamFaction.Alliance)
            ++_invitedAlliance;
        else
            ++_invitedHorde;
    }

    public bool IsArena => _battlegroundTemplate.IsArena;

    public bool IsBattleground => !IsArena;

    public bool IsPlayerInBattleground(ObjectGuid guid)
    {
        return _players.ContainsKey(guid);
    }

    public bool IsPlayerMercenaryInBattleground(ObjectGuid guid)
    {
        if (_players.TryGetValue(guid, out var player))
            return player.Mercenary;

        return false;
    }

    public bool IsRandom => _isRandom;

    public bool IsRated => _isRated;

    public virtual bool IsSpellAllowed(uint spellId, Player player)
    {
        return true;
    }

    public void PlaySoundToAll(uint soundID)
    {
        SendPacketToAll(new PlaySound(ObjectGuid.Empty, soundID, 0));
    }

    public virtual void PostUpdateImpl(uint diff) { }

    public virtual bool PreUpdateImpl(uint diff)
    {
        return true;
    }

    public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker = null) { }

    public void RelocateDeadPlayers(ObjectGuid guideGuid)
    {
        // Those who are waiting to resurrect at this node are taken to the closest own node's graveyard
        var ghostList = _reviveQueue[guideGuid];

        if (!ghostList.Empty())
        {
            WorldSafeLocsEntry closestGrave = null;

            foreach (var guid in ghostList)
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (!player)
                    continue;

                if (closestGrave == null)
                    closestGrave = GetClosestGraveYard(player);

                if (closestGrave != null)
                    player.TeleportTo(closestGrave.Location);
            }

            ghostList.Clear();
        }
    }

    // This method removes this Battleground from free queue - it must be called when deleting Battleground
    public void RemoveFromBGFreeSlotQueue()
    {
        if (_inBGFreeSlotQueue)
        {
            Global.BattlegroundMgr.RemoveFromBGFreeSlotQueue(GetQueueId(), _instanceID);
            _inBGFreeSlotQueue = false;
        }
    }

    public virtual void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team) { }

    public virtual void RemovePlayerAtLeave(ObjectGuid guid, bool transport, bool sendPacket)
    {
        var team = GetPlayerTeam(guid);
        var participant = false;
        // Remove from lists/maps
        if (_players.ContainsKey(guid))
        {
            UpdatePlayersCountByTeam(team, true); // -1 player
            _players.Remove(guid);
            // check if the player was a participant of the match, or only entered through gm command (goname)
            participant = true;
        }

        if (PlayerScores.ContainsKey(guid))
            PlayerScores.Remove(guid);

        RemovePlayerFromResurrectQueue(guid);

        var player = Global.ObjAccessor.FindPlayer(guid);

        if (player)
        {
            // should remove spirit of redemption
            if (player.HasAuraType(AuraType.SpiritOfRedemption))
                player.RemoveAurasByType(AuraType.ModShapeshift);

            player.RemoveAurasByType(AuraType.Mounted);
            player.RemoveAura(BattlegroundConst.SpellMercenaryHorde1);
            player.RemoveAura(BattlegroundConst.SpellMercenaryHordeReactions);
            player.RemoveAura(BattlegroundConst.SpellMercenaryAlliance1);
            player.RemoveAura(BattlegroundConst.SpellMercenaryAllianceReactions);
            player.RemoveAura(BattlegroundConst.SpellMercenaryShapeshift);
            player.RemovePlayerFlagEx(PlayerFlagsEx.MercenaryMode);

            if (!player.IsAlive) // resurrect on exit
            {
                player.ResurrectPlayer(1.0f);
                player.SpawnCorpseBones();
            }
        }
        else
        {
            PlayerComputators.OfflineResurrect(guid, null);
        }

        RemovePlayer(player, guid, team); // BG subclass specific code

        var bgQueueTypeId = GetQueueId();

        if (participant) // if the player was a match participant, remove auras, calc rating, update queue
        {
            if (player)
            {
                player.ClearAfkReports();

                // if arena, remove the specific arena auras
                if (IsArena)
                {
                    // unsummon current and summon old pet if there was one and there isn't a current pet
                    player.RemovePet(null, PetSaveMode.NotInSlot);
                    player.ResummonPetTemporaryUnSummonedIfAny();
                }

                if (sendPacket)
                {
                    Global.BattlegroundMgr.BuildBattlegroundStatusNone(out var battlefieldStatus, player, player.GetBattlegroundQueueIndex(bgQueueTypeId), player.GetBattlegroundQueueJoinTime(bgQueueTypeId));
                    player.SendPacket(battlefieldStatus);
                }

                // this call is important, because player, when joins to Battleground, this method is not called, so it must be called when leaving bg
                player.RemoveBattlegroundQueueId(bgQueueTypeId);
            }

            // remove from raid group if player is member
            var group = GetBgRaid(team);

            if (group)
                if (!group.RemoveMember(guid)) // group was disbanded
                    SetBgRaid(team, null);

            DecreaseInvitedCount(team);

            //we should update Battleground queue, but only if bg isn't ending
            if (IsBattleground && Status < BattlegroundStatus.WaitLeave)
            {
                // a player has left the Battleground, so there are free slots . add to queue
                AddToBGFreeSlotQueue();
                Global.BattlegroundMgr.ScheduleQueueUpdate(0, bgQueueTypeId, BracketId);
            }

            // Let others know
            BattlegroundPlayerLeft playerLeft = new()
            {
                Guid = guid
            };

            SendPacketToTeam(team, playerLeft, player);
        }

        if (player)
        {
            // Do next only if found in Battleground
            player.SetBattlegroundId(0, BattlegroundTypeId.None); // We're not in BG.
            // reset destination bg team
            player.SetBgTeam(0);

            // remove all criterias on bg leave
            player.ResetCriteria(CriteriaFailEvent.LeaveBattleground, MapId, true);

            if (transport)
                player.TeleportToBGEntryPoint();

            Log.Logger.Debug("Removed player {0} from Battleground.", player.GetName());
        }

        //Battleground object will be deleted next Battleground.Update() call
    }

    public void RemovePlayerFromResurrectQueue(ObjectGuid playerGUID)
    {
        _reviveQueue.RemoveIfMatching((pair) =>
        {
            if (pair.Value == playerGUID)
            {
                var player = Global.ObjAccessor.FindPlayer(playerGUID);

                if (player)
                    player.RemoveAura(BattlegroundConst.SpellWaitingForResurrect);

                return true;
            }

            return false;
        });
    }

    public void RemovePlayerPosition(ObjectGuid guid)
    {
        _playerPositions.RemoveAll(playerPosition => playerPosition.Guid == guid);
    }

    // this method is called when no players remains in Battleground
    public virtual void Reset()
    {
        SetWinner(PvPTeamId.Neutral);
        SetStatus(BattlegroundStatus.WaitQueue);
        SetElapsedTime(0);
        SetRemainingTime(0);
        SetLastResurrectTime(0);
        _battlegroundEventFlags = 0;

        if (_invitedAlliance > 0 || _invitedHorde > 0)
            Log.Logger.Error($"Battleground.Reset: one of the counters is not 0 (Team.Alliance: {_invitedAlliance}, Team.Horde: {_invitedHorde}) for BG (map: {MapId}, instance id: {_instanceID})!");

        _invitedAlliance = 0;
        _invitedHorde = 0;
        _inBGFreeSlotQueue = false;

        _players.Clear();

        PlayerScores.Clear();

        _playerPositions.Clear();
    }

    public void RewardHonorToTeam(uint honor, TeamFaction team)
    {
        foreach (var pair in _players)
        {
            var player = _GetPlayerForTeam(team, pair, "RewardHonorToTeam");

            if (player)
                UpdatePlayerScore(player, ScoreType.BonusHonor, honor);
        }
    }

    public void RewardReputationToTeam(uint factionID, uint reputation, TeamFaction team)
    {
        if (!CliDB.FactionStorage.TryGetValue(factionID, out var factionEntry))
            return;

        foreach (var pair in _players)
        {
            var player = _GetPlayerForTeam(team, pair, "RewardReputationToTeam");

            if (!player)
                continue;

            if (player.HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode))
                continue;

            var repGain = reputation;
            MathFunctions.AddPct(ref repGain, player.GetTotalAuraModifier(AuraType.ModReputationGain));
            MathFunctions.AddPct(ref repGain, player.GetTotalAuraModifierByMiscValue(AuraType.ModFactionReputationGain, (int)factionID));
            player.ReputationMgr.ModifyReputation(factionEntry, (int)repGain);
        }
    }

    public void SendBroadcastText(uint id, ChatMsg msgType, WorldObject target = null)
    {
        if (!CliDB.BroadcastTextStorage.ContainsKey(id))
        {
            Log.Logger.Error($"Battleground.SendBroadcastText: `broadcast_text` (ID: {id}) was not found");

            return;
        }

        BroadcastTextBuilder builder = new(null, msgType, id, Gender.Male, target);
        LocalizedDo localizer = new(builder);
        BroadcastWorker(localizer);
    }

    public void SendChatMessage(Creature source, byte textId, WorldObject target = null)
    {
        Global.CreatureTextMgr.SendChat(source, textId, target);
    }

    public void SendMessageToAll(CypherStrings entry, ChatMsg msgType, Player source = null)
    {
        if (entry == 0)
            return;

        CypherStringChatBuilder builder = new(null, msgType, entry, source);
        LocalizedDo localizer = new(builder);
        BroadcastWorker(localizer);
    }

    public void SendMessageToAll(CypherStrings entry, ChatMsg msgType, Player source, params object[] args)
    {
        if (entry == 0)
            return;

        CypherStringChatBuilder builder = new(null, msgType, entry, source, args);
        LocalizedDo localizer = new(builder);
        BroadcastWorker(localizer);
    }

    public void SendPacketToAll(ServerPacket packet)
    {
        foreach (var pair in _players)
        {
            var player = _GetPlayer(pair, "SendPacketToAll");

            if (player)
                player.SendPacket(packet);
        }
    }

    public void SetArenaMatchmakerRating(TeamFaction team, uint mmr)
    {
        _arenaTeamMmr[GetTeamIndexByTeamId(team)] = mmr;
    }

    public void SetArenaTeamIdForTeam(TeamFaction team, uint arenaTeamId)
    {
        _arenaTeamIds[GetTeamIndexByTeamId(team)] = arenaTeamId;
    }

    public void SetArenaType(ArenaTypes type)
    {
        _arenaType = type;
    }

    public void SetBgMap(BattlegroundMap map)
    {
        _map = map;
    }

    public void SetBracket(PvpDifficultyRecord bracketEntry)
    {
        _pvpDifficultyEntry = bracketEntry;
    }

    public void SetClientInstanceID(uint instanceID)
    {
        _clientInstanceID = instanceID;
    }

    public virtual void SetDroppedFlagGUID(ObjectGuid guid, int teamIndex = -1) { }

    public void SetElapsedTime(uint time)
    {
        _startTime = time;
    }

    public void SetHoliday(bool isHoliday)
    {
        MHonorMode = isHoliday ? BGHonorMode.Holiday : BGHonorMode.Normal;
    }

    //here we can count minlevel and maxlevel for players
    public void SetInstanceID(uint instanceID)
    {
        _instanceID = instanceID;
    }

    public void SetLastResurrectTime(uint time)
    {
        _lastResurrectTime = time;
    }

    public void SetQueueId(BattlegroundQueueTypeId queueId)
    {
        _queueId = queueId;
    }

    public void SetRandom(bool isRandom)
    {
        _isRandom = isRandom;
    }

    public void SetRandomTypeID(BattlegroundTypeId typeID)
    {
        _randomTypeID = typeID;
    }

    public void SetRated(bool state)
    {
        _isRated = state;
    }

    public void SetRemainingTime(uint time)
    {
        _endTime = (int)time;
    }

    public void SetStatus(BattlegroundStatus status)
    {
        this.Status = status;
    }

    public virtual bool SetupBattleground()
    {
        return true;
    }

    public void SetWinner(PvPTeamId winnerTeamId)
    {
        _winnerTeamId = winnerTeamId;
    }

    public void SpawnBGObject(int type, uint respawntime)
    {
        Map map = FindBgMap();

        if (map != null)
        {
            var obj = map.GetGameObject(BgObjects[type]);

            if (obj)
            {
                if (respawntime != 0)
                {
                    obj.SetLootState(LootState.JustDeactivated);

                    {
                        var goOverride = obj.GameObjectOverride;

                        if (goOverride != null)
                            if (goOverride.Flags.HasFlag(GameObjectFlags.NoDespawn))
                                // This function should be called in GameObject::Update() but in case of
                                // GO_FLAG_NODESPAWN Id the function is never called, so we call it here
                                obj.SendGameObjectDespawn();
                    }
                }
                else if (obj.LootState == LootState.JustDeactivated)
                {
                    // Change state from GO_JUST_DEACTIVATED to GO_READY in case battleground is starting again
                    obj.SetLootState(LootState.Ready);
                }

                obj.SetRespawnTime((int)respawntime);
                map.AddToMap(obj);
            }
        }
    }

    public void StartBattleground()
    {
        SetElapsedTime(0);
        SetLastResurrectTime(0);
        // add BG to free slot queue
        AddToBGFreeSlotQueue();

        // add bg to update list
        // This must be done here, because we need to have already invited some players when first BG.Update() method is executed
        // and it doesn't matter if we call StartBattleground() more times, because m_Battlegrounds is a map and instance id never changes
        Global.BattlegroundMgr.AddBattleground(this);

        if (_isRated)
            Log.Logger.Debug("Arena match type: {0} for Team1Id: {1} - Team2Id: {2} started.", _arenaType, _arenaTeamIds[TeamIds.Alliance], _arenaTeamIds[TeamIds.Horde]);
    }

    public virtual void StartingEventCloseDoors() { }

    public virtual void StartingEventOpenDoors() { }

    public void TeleportPlayerToExploitLocation(Player player)
    {
        var loc = GetExploitTeleportLocation(player.GetBgTeam());

        if (loc != null)
            player.TeleportTo(loc.Location);
    }

    public bool ToBeDeleted()
    {
        return _setDeleteThis;
    }

    public override void TriggerGameEvent(uint gameEventId, WorldObject source = null, WorldObject target = null)
    {
        ProcessEvent(target, gameEventId, source);
        GameEvents.TriggerForMap(gameEventId, BgMap, source, target);

        foreach (var guid in GetPlayers().Keys)
        {
            var player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                GameEvents.TriggerForPlayer(gameEventId, player);
        }
    }

    public void Update(uint diff)
    {
        if (!PreUpdateImpl(diff))
            return;

        if (GetPlayersSize() == 0)
        {
            //BG is empty
            // if there are no players invited, delete BG
            // this will delete arena or bg object, where any player entered
            // [[   but if you use Battleground object again (more battles possible to be played on 1 instance)
            //      then this condition should be removed and code:
            //      if (!GetInvitedCount(Team.Horde) && !GetInvitedCount(Team.Alliance))
            //          this.AddToFreeBGObjectsQueue(); // not yet implemented
            //      should be used instead of current
            // ]]
            // Battleground Template instance cannot be updated, because it would be deleted
            if (GetInvitedCount(TeamFaction.Horde) == 0 && GetInvitedCount(TeamFaction.Alliance) == 0)
                _setDeleteThis = true;

            return;
        }

        switch (Status)
        {
            case BattlegroundStatus.WaitJoin:
                if (GetPlayersSize() != 0)
                {
                    _ProcessJoin(diff);
                    _CheckSafePositions(diff);
                }

                break;
            case BattlegroundStatus.InProgress:
                _ProcessOfflineQueue();
                _ProcessPlayerPositionBroadcast(diff);

                // after 47 Time.Minutes without one team losing, the arena closes with no winner and no rating change
                if (IsArena)
                {
                    if (ElapsedTime >= 47 * Time.MINUTE * Time.IN_MILLISECONDS)
                    {
                        EndBattleground(0);

                        return;
                    }
                }
                else
                {
                    _ProcessRessurect(diff);

                    if (Global.BattlegroundMgr.GetPrematureFinishTime() != 0 && (GetPlayersCountByTeam(TeamFaction.Alliance) < MinPlayersPerTeam || GetPlayersCountByTeam(TeamFaction.Horde) < MinPlayersPerTeam))
                        _ProcessProgress(diff);
                    else if (_prematureCountDown)
                        _prematureCountDown = false;
                }

                break;
            case BattlegroundStatus.WaitLeave:
                _ProcessLeave(diff);

                break;
            
        }

        // Update start time and reset stats timer
        SetElapsedTime(ElapsedTime + diff);

        if (Status == BattlegroundStatus.WaitJoin)
        {
            _resetStatTimer += diff;
            _countdownTimer += diff;
        }

        PostUpdateImpl(diff);
    }

    public virtual bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
    {
        if (!PlayerScores.TryGetValue(player.GUID, out var bgScore)) // player not found...
            return false;

        if (type == ScoreType.BonusHonor && doAddHonor && IsBattleground)
            player.RewardHonor(null, 1, (int)value);
        else
            bgScore.UpdateScore(type, value);

        return true;
    }

    public void UpdateWorldState(int worldStateId, int value, bool hidden = false)
    {
        Global.WorldStateMgr.SetValue(worldStateId, value, hidden, BgMap);
    }

    public void UpdateWorldState(uint worldStateId, int value, bool hidden = false)
    {
        Global.WorldStateMgr.SetValue((int)worldStateId, value, hidden, BgMap);
    }

    private void _CheckSafePositions(uint diff)
    {
        var maxDist = GetStartMaxDist();

        if (maxDist == 0.0f)
            return;

        _validStartPositionTimer += diff;

        if (_validStartPositionTimer >= BattlegroundConst.CheckPlayerPositionInverval)
        {
            _validStartPositionTimer = 0;

            foreach (var guid in GetPlayers().Keys)
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                {
                    if (player.IsGameMaster)
                        continue;

                    Position pos = player.Location;
                    var startPos = GetTeamStartPosition(GetTeamIndexByTeamId(player.GetBgTeam()));

                    if (pos.GetExactDistSq(startPos.Location) > maxDist)
                    {
                        Log.Logger.Debug($"Battleground: Sending {player.GetName()} back to start location (map: {MapId}) (possible exploit)");
                        player.TeleportTo(startPos.Location);
                    }
                }
            }
        }
    }

    private Player _GetPlayerForTeam(TeamFaction teamId, KeyValuePair<ObjectGuid, BattlegroundPlayer> pair, string context)
    {
        var player = _GetPlayer(pair, context);

        if (player)
        {
            var team = pair.Value.Team;

            if (team == 0)
                team = player.EffectiveTeam;

            if (team != teamId)
                player = null;
        }

        return player;
    }

    private void _ProcessJoin(uint diff)
    {
        // *********************************************************
        // ***           Battleground STARTING SYSTEM            ***
        // *********************************************************
        ModifyStartDelayTime((int)diff);

        if (!IsArena)
            SetRemainingTime(300000);

        if (_resetStatTimer > 5000)
        {
            _resetStatTimer = 0;

            foreach (var guid in GetPlayers().Keys)
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.ResetAllPowers();
            }
        }

        // Send packet every 10 seconds until the 2nd field reach 0
        if (_countdownTimer >= 10000)
        {
            var countdownMaxForBGType = IsArena ? BattlegroundConst.ArenaCountdownMax : BattlegroundConst.BattlegroundCountdownMax;

            StartTimer timer = new()
            {
                Type = TimerType.Pvp,
                TimeLeft = countdownMaxForBGType - ElapsedTime / 1000,
                TotalTime = countdownMaxForBGType
            };

            foreach (var guid in GetPlayers().Keys)
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.SendPacket(timer);
            }

            _countdownTimer = 0;
        }

        if (!_battlegroundEventFlags.HasAnyFlag(BattlegroundEventFlags.Event1))
        {
            _battlegroundEventFlags |= BattlegroundEventFlags.Event1;

            if (!FindBgMap())
            {
                Log.Logger.Error($"Battleground._ProcessJoin: map (map id: {MapId}, instance id: {_instanceID}) is not created!");
                EndNow();

                return;
            }

            // Setup here, only when at least one player has ported to the map
            if (!SetupBattleground())
            {
                EndNow();

                return;
            }

            StartingEventCloseDoors();
            SetStartDelayTime(StartDelayTimes[BattlegroundConst.EventIdFirst]);

            // First start warning - 2 or 1 Minute
            if (StartMessageIds[BattlegroundConst.EventIdFirst] != 0)
                SendBroadcastText(StartMessageIds[BattlegroundConst.EventIdFirst], ChatMsg.BgSystemNeutral);
        }
        // After 1 Time.Minute or 30 seconds, warning is signaled
        else if (GetStartDelayTime() <= (int)StartDelayTimes[BattlegroundConst.EventIdSecond] && !_battlegroundEventFlags.HasAnyFlag(BattlegroundEventFlags.Event2))
        {
            _battlegroundEventFlags |= BattlegroundEventFlags.Event2;

            if (StartMessageIds[BattlegroundConst.EventIdSecond] != 0)
                SendBroadcastText(StartMessageIds[BattlegroundConst.EventIdSecond], ChatMsg.BgSystemNeutral);
        }
        // After 30 or 15 seconds, warning is signaled
        else if (GetStartDelayTime() <= (int)StartDelayTimes[BattlegroundConst.EventIdThird] && !_battlegroundEventFlags.HasAnyFlag(BattlegroundEventFlags.Event3))
        {
            _battlegroundEventFlags |= BattlegroundEventFlags.Event3;

            if (StartMessageIds[BattlegroundConst.EventIdThird] != 0)
                SendBroadcastText(StartMessageIds[BattlegroundConst.EventIdThird], ChatMsg.BgSystemNeutral);
        }
        // Delay expired (after 2 or 1 Time.Minute)
        else if (GetStartDelayTime() <= 0 && !_battlegroundEventFlags.HasAnyFlag(BattlegroundEventFlags.Event4))
        {
            _battlegroundEventFlags |= BattlegroundEventFlags.Event4;

            StartingEventOpenDoors();

            if (StartMessageIds[BattlegroundConst.EventIdFourth] != 0)
                SendBroadcastText(StartMessageIds[BattlegroundConst.EventIdFourth], ChatMsg.RaidBossEmote);

            SetStatus(BattlegroundStatus.InProgress);
            SetStartDelayTime(StartDelayTimes[BattlegroundConst.EventIdFourth]);

            // Remove preparation
            if (IsArena)
            {
                //todo add arena sound PlaySoundToAll(SOUND_ARENA_START);
                foreach (var guid in GetPlayers().Keys)
                {
                    var player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                    {
                        // Correctly display EnemyUnitFrame
                        player.SetArenaFaction((byte)player.GetBgTeam());

                        player.RemoveAura(BattlegroundConst.SpellArenaPreparation);
                        player.ResetAllPowers();

                        if (!player.IsGameMaster)
                            // remove auras with duration lower than 30s
                            player.GetAppliedAurasQuery()
                                  .IsPermanent(false)
                                  .IsPositive()
                                  .AlsoMatches(aurApp =>
                                  {
                                      var aura = aurApp.Base;

                                      return aura.Duration <= 30 * Time.IN_MILLISECONDS &&
                                             !aura.SpellInfo.HasAttribute(SpellAttr0.NoImmunities) &&
                                             !aura.HasEffectType(AuraType.ModInvisibility);
                                  })
                                  .Execute(player.RemoveAura);
                    }
                }

                CheckWinConditions();
            }
            else
            {
                PlaySoundToAll((uint)BattlegroundSounds.BgStart);

                foreach (var guid in GetPlayers().Keys)
                {
                    var player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                    {
                        player.RemoveAura(BattlegroundConst.SpellPreparation);
                        player.ResetAllPowers();
                    }
                }

                // Announce BG starting
                if (GetDefaultValue("Battleground:QueueAnnouncer:Enable", false))
                    Global.WorldMgr.SendWorldText(CypherStrings.BgStartedAnnounceWorld, Name, MinLevel, MaxLevel);
            }
        }

        if (RemainingTime > 0 && (_endTime -= (int)diff) > 0)
            SetRemainingTime(RemainingTime - diff);
    }

    private void _ProcessLeave(uint diff)
    {
        // *********************************************************
        // ***           Battleground ENDING SYSTEM              ***
        // *********************************************************
        // remove all players from Battleground after 2 Time.Minutes
        SetRemainingTime(RemainingTime - diff);

        if (RemainingTime <= 0)
        {
            SetRemainingTime(0);

            foreach (var guid in _players.Keys)
                RemovePlayerAtLeave(guid, true, true); // remove player from BG
            // do not change any Battleground's private variables
        }
    }

    private void _ProcessOfflineQueue()
    {
        // remove offline players from bg after 5 Time.Minutes
        if (!_offlineQueue.Empty())
        {
            var guid = _offlineQueue.FirstOrDefault();
            if (_players.TryGetValue(guid, out var bgPlayer))
                if (bgPlayer.OfflineRemoveTime <= GameTime.CurrentTime)
                {
                    RemovePlayerAtLeave(guid, true, true); // remove player from BG
                    _offlineQueue.RemoveAt(0);            // remove from offline queue
                }
        }
    }

    private void _ProcessPlayerPositionBroadcast(uint diff)
    {
        _lastPlayerPositionBroadcast += diff;

        if (_lastPlayerPositionBroadcast >= BattlegroundConst.PlayerPositionUpdateInterval)
        {
            _lastPlayerPositionBroadcast = 0;

            BattlegroundPlayerPositions playerPositions = new();

            for (var i = 0; i < _playerPositions.Count; ++i)
            {
                var playerPosition = _playerPositions[i];
                // Update position data if we found player.
                var player = Global.ObjAccessor.GetPlayer(BgMap, playerPosition.Guid);

                if (player != null)
                    playerPosition.Pos = player.Location;

                playerPositions.FlagCarriers.Add(playerPosition);
            }

            SendPacketToAll(playerPositions);
        }
    }

    private void _ProcessProgress(uint diff)
    {
        // *********************************************************
        // ***           Battleground BALLANCE SYSTEM            ***
        // *********************************************************
        // if less then minimum players are in on one side, then start premature finish timer
        if (!_prematureCountDown)
        {
            _prematureCountDown = true;
            _prematureCountDownTimer = Global.BattlegroundMgr.GetPrematureFinishTime();
        }
        else if (_prematureCountDownTimer < diff)
        {
            // time's up!
            EndBattleground(GetPrematureWinner());
            _prematureCountDown = false;
        }
        else if (!Global.BattlegroundMgr.IsTesting())
        {
            var newtime = _prematureCountDownTimer - diff;

            // announce every Time.Minute
            if (newtime > Time.MINUTE * Time.IN_MILLISECONDS)
            {
                if (newtime / (Time.MINUTE * Time.IN_MILLISECONDS) != _prematureCountDownTimer / (Time.MINUTE * Time.IN_MILLISECONDS))
                    SendMessageToAll(CypherStrings.BattlegroundPrematureFinishWarning, ChatMsg.System, null, _prematureCountDownTimer / (Time.MINUTE * Time.IN_MILLISECONDS));
            }
            else
            {
                //announce every 15 seconds
                if (newtime / (15 * Time.IN_MILLISECONDS) != _prematureCountDownTimer / (15 * Time.IN_MILLISECONDS))
                    SendMessageToAll(CypherStrings.BattlegroundPrematureFinishWarningSecs, ChatMsg.System, null, _prematureCountDownTimer / Time.IN_MILLISECONDS);
            }

            _prematureCountDownTimer = newtime;
        }
    }

    private void _ProcessRessurect(uint diff)
    {
        // *********************************************************
        // ***        Battleground RESSURECTION SYSTEM           ***
        // *********************************************************
        // this should be handled by spell system
        _lastResurrectTime += diff;

        if (_lastResurrectTime >= BattlegroundConst.ResurrectionInterval)
        {
            if (GetReviveQueueSize() != 0)
            {
                Creature sh = null;

                foreach (var pair in _reviveQueue.KeyValueList)
                {
                    var player = Global.ObjAccessor.FindPlayer(pair.Value);

                    if (!player)
                        continue;

                    if (!sh && player.IsInWorld)
                    {
                        sh = player.Map.GetCreature(pair.Key);

                        // only for visual effect
                        if (sh)
                            // Spirit Heal, effect 117
                            sh.CastSpell(sh, BattlegroundConst.SpellSpiritHeal, true);
                    }

                    // Resurrection visual
                    player.CastSpell(player, BattlegroundConst.SpellResurrectionVisual, true);
                    _resurrectQueue.Add(pair.Value);
                }

                _reviveQueue.Clear();
                _lastResurrectTime = 0;
            }
            else
                // queue is clear and time passed, just update last resurrection time
            {
                _lastResurrectTime = 0;
            }
        }
        else if (_lastResurrectTime > 500) // Resurrect players only half a second later, to see spirit heal effect on NPC
        {
            foreach (var guid in _resurrectQueue)
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (!player)
                    continue;

                player.ResurrectPlayer(1.0f);
                player.CastSpell(player, 6962, true);
                player.CastSpell(player, BattlegroundConst.SpellSpiritHealMana, true);
                player.SpawnCorpseBones(false);
            }

            _resurrectQueue.Clear();
        }
    }

    // This method should be called only once ... it adds pointer to queue
    private void AddToBGFreeSlotQueue()
    {
        if (!_inBGFreeSlotQueue && IsBattleground)
        {
            Global.BattlegroundMgr.AddToBGFreeSlotQueue(GetQueueId(), this);
            _inBGFreeSlotQueue = true;
        }
    }

    private void BlockMovement(Player player)
    {
        // movement disabled NOTE: the effect will be automatically removed by client when the player is teleported from the battleground, so no need to send with uint8(1) in RemovePlayerAtLeave()
        player.SetClientControl(player, false);
    }

    private void BroadcastWorker(IDoWork<Player> @do)
    {
        foreach (var pair in _players)
        {
            var player = _GetPlayer(pair, "BroadcastWorker");

            if (player)
                @do.Invoke(player);
        }
    }

    private bool CanAwardArenaPoints()
    {
        return MinLevel >= 71;
    }

    private void EndNow()
    {
        RemoveFromBGFreeSlotQueue();
        SetStatus(BattlegroundStatus.WaitLeave);
        SetRemainingTime(0);
    }

    private BattlegroundMap FindBgMap()
    {
        return _map;
    }

    private PlayerGroup GetBgRaid(TeamFaction team)
    {
        return _bgRaids[GetTeamIndexByTeamId(team)];
    }

    private int GetInvitedCount(TeamFaction team)
    {
        return team == TeamFaction.Alliance ? _invitedAlliance : _invitedHorde;
    }

    private int GetMaxPlayers()
    {
        return GetMaxPlayersPerTeam() * 2;
    }

    private int GetMinPlayers()
    {
        return MinPlayersPerTeam * 2;
    }

    private int GetObjectType(ObjectGuid guid)
    {
        for (var i = 0; i < BgObjects.Length; ++i)
            if (BgObjects[i] == guid)
                return i;

        Log.Logger.Error($"Battleground.GetObjectType: player used gameobject ({guid}) which is not in internal data for BG (map: {MapId}, instance id: {_instanceID}), cheating?");

        return -1;
    }

    private int GetPlayerScoresSize()
    {
        return PlayerScores.Count;
    }

    private int GetPlayersSize()
    {
        return _players.Count;
    }

    private int GetReviveQueueSize()
    {
        return _reviveQueue.Count;
    }

    private uint GetScriptId()
    {
        return _battlegroundTemplate.ScriptId;
    }

    private int GetStartDelayTime()
    {
        return _startDelayTime;
    }

    private float GetStartMaxDist()
    {
        return _battlegroundTemplate.MaxStartDistSq;
    }

    private byte GetUniqueBracketId()
    {
        return (byte)(MinLevel / 5 - 1); // 10 - 1, 15 - 2, 20 - 3, etc.
    }

    private PvPTeamId GetWinner()
    {
        return _winnerTeamId;
    }

    private void ModifyStartDelayTime(int diff)
    {
        _startDelayTime -= diff;
    }

    private void PlayerAddedToBGCheckIfBGIsRunning(Player player)
    {
        if (Status != BattlegroundStatus.WaitLeave)
            return;

        BlockMovement(player);

        PVPMatchStatisticsMessage pvpMatchStatistics = new();
        BuildPvPLogDataPacket(out pvpMatchStatistics.Data);
        player.SendPacket(pvpMatchStatistics);
    }

    private void PlaySoundToTeam(uint soundID, TeamFaction team)
    {
        SendPacketToTeam(team, new PlaySound(ObjectGuid.Empty, soundID, 0));
    }

    private void RemoveAuraOnTeam(uint spellID, TeamFaction team)
    {
        foreach (var pair in _players)
        {
            var player = _GetPlayerForTeam(team, pair, "RemoveAuraOnTeam");

            if (player)
                player.RemoveAura(spellID);
        }
    }

    private bool RemoveObjectFromWorld(uint type)
    {
        if (BgObjects[type].IsEmpty)
            return true;

        var obj = BgMap.GetGameObject(BgObjects[type]);

        if (obj != null)
        {
            obj.RemoveFromWorld();
            BgObjects[type].Clear();

            return true;
        }

        Log.Logger.Information($"Battleground::RemoveObjectFromWorld: gameobject (type: {type}, {BgObjects[type]}) not found for BG (map: {MapId}, instance id: {_instanceID})!");

        return false;
    }

    private void RewardXPAtKill(Player killer, Player victim)
    {
        if (GetDefaultValue("Battleground:GiveXPForKills", false) && killer && victim)
            new KillRewarder(new[]
                             {
                                 killer
                             },
                             victim,
                             true).Reward();
    }

    private void SendPacketToTeam(TeamFaction team, ServerPacket packet, Player except = null)
    {
        foreach (var pair in _players)
        {
            var player = _GetPlayerForTeam(team, pair, "SendPacketToTeam");

            if (player)
                if (player != except)
                    player.SendPacket(packet);
        }
    }

    private void SetBgRaid(TeamFaction team, PlayerGroup bgRaid)
    {
        var oldRaid = _bgRaids[GetTeamIndexByTeamId(team)];

        if (oldRaid)
            oldRaid.SetBattlegroundGroup(null);

        if (bgRaid)
            bgRaid.SetBattlegroundGroup(this);

        _bgRaids[GetTeamIndexByTeamId(team)] = bgRaid;
    }

    private void SetDeleteThis()
    {
        _setDeleteThis = true;
    }

    private void SetStartDelayTime(BattlegroundStartTimeIntervals time)
    {
        _startDelayTime = (int)time;
    }

    private void UpdatePlayersCountByTeam(TeamFaction team, bool remove)
    {
        if (remove)
            --_playersCount[GetTeamIndexByTeamId(team)];
        else
            ++_playersCount[GetTeamIndexByTeamId(team)];
    }
    
    public uint[] BuffEntries =
    {
        BattlegroundConst.SpeedBuff, BattlegroundConst.RegenBuff, BattlegroundConst.BerserkerBuff
    };

    public bool MBuffChange;
    public BGHonorMode MHonorMode;
    public uint[] MTeamScores = new uint[SharedConst.PvpTeamsCount];

    public BattlegroundStartTimeIntervals[] StartDelayTimes = new BattlegroundStartTimeIntervals[4];

    // this must be filled inructors!
    public uint[] StartMessageIds = new uint[4];

    protected ObjectGuid[] BgCreatures;
    protected ObjectGuid[] BgObjects;

    protected Dictionary<ObjectGuid, BattlegroundScore> PlayerScores = new(); // Player scores
    // Player lists, those need to be accessible by inherited classes

    private readonly BattlegroundTemplate _battlegroundTemplate;

    private readonly List<BattlegroundPlayerPosition> _playerPositions = new();

    // Arena team ids by team
    private readonly uint[] _arenaTeamIds = new uint[SharedConst.PvpTeamsCount];

    private readonly uint[] _arenaTeamMmr = new uint[SharedConst.PvpTeamsCount];

    // Raid Group
    private readonly PlayerGroup[] _bgRaids = new PlayerGroup[SharedConst.PvpTeamsCount];

    private readonly List<ObjectGuid> _offlineQueue = new();
    private readonly Dictionary<ObjectGuid, BattlegroundPlayer> _players = new();

    // Players count by team
    private readonly uint[] _playersCount = new uint[SharedConst.PvpTeamsCount];

    // Player lists
    private readonly List<ObjectGuid> _resurrectQueue = new();

    // Spirit Guide guid + Player list GUIDS
    private readonly MultiMap<ObjectGuid, ObjectGuid> _reviveQueue = new();

    private PvpDifficultyRecord _pvpDifficultyEntry;

    private PvPTeamId _winnerTeamId;

    private ArenaTypes _arenaType;

    private uint _clientInstanceID;

    private uint _countdownTimer;

    private int _endTime;

    // these are important variables used for starting messages
    private BattlegroundEventFlags _battlegroundEventFlags;

    // 2=2v2, 3=3v3, 5=5v5
    private bool _inBGFreeSlotQueue;

    private uint _instanceID;

    // Invited counters are useful for player invitation to BG - do not allow, if BG is started to one faction to have 2 more players than another faction
    // Invited counters will be changed only when removing already invited player from queue, removing player from Battleground and inviting player to BG
    // Invited players counters
    private uint _invitedAlliance;

    // Player GUID
    // Player GUID
    private uint _invitedHorde;

    private bool _isRandom;
    private bool _isRated;

    private uint _lastPlayerPositionBroadcast;

    // it is set to 120000 when bg is ending and it decreases itself
    private uint _lastResurrectTime;

    // 0 - Team.Alliance, 1 - Team.Horde
    // Start location
    private BattlegroundMap _map;

    // is this battle rated?
    private bool _prematureCountDown;

    private uint _prematureCountDownTimer;

    // = new Dictionary<int, ObjectGuid>();
    // = new Dictionary<int, ObjectGuid>();
    // Battleground
    private BattlegroundQueueTypeId _queueId;
    private BattlegroundTypeId _randomTypeID;
    private uint _resetStatTimer;

    // used to make sure that BG is only once inserted into the BattlegroundMgr.BGFreeSlotQueue[bgTypeId] deque
    private bool _setDeleteThis;

    // used for safe deletion of the bg after end / all players leave
    private int _startDelayTime;

    // the instance-id which is sent to the client and without any other internal use
    private uint _startTime;

    // Battleground Instance's GUID!
    private uint _validStartPositionTimer;
}