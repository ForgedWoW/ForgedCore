﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Movement;

namespace Game.AI
{
    public class EscortAI : ScriptedAI
    {
        private readonly WaypointPath _path;
        private bool _activeAttacker; // obsolete, determined by faction.
        private bool _despawnAtEnd;
        private bool _despawnAtFar;
        private bool _ended;

        private Quest _escortQuest; //generally passed in Start() when regular escort script.
        private EscortState _escortState;
        private bool _hasImmuneToNPCFlags;
        private bool _instantRespawn; // if creature should respawn instantly after escort over (if not, database respawntime are used)
        private bool _manualPath;
        private float _maxPlayerDistance;
        private TimeSpan _pauseTimer;
        private uint _playerCheckTimer;

        private ObjectGuid _playerGUID;
        private bool _resume;
        private bool _returnToStart; // if creature can walk same path (loop) without despawn. Not for regular escort quests.
        private bool _running;       // all creatures are walking by default (has flag MOVEMENTFLAG_WALK)
        private bool _started;

        public EscortAI(Creature creature) : base(creature)
        {
            _pauseTimer = TimeSpan.FromSeconds(2.5);
            _playerCheckTimer = 1000;
            _maxPlayerDistance = 100;
            _activeAttacker = true;
            _despawnAtEnd = true;
            _despawnAtFar = true;

            _path = new WaypointPath();
        }

        public Player GetPlayerForEscort()
        {
            return Global.ObjAccessor.GetPlayer(me, _playerGUID);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (who == null)
                return;

            if (HasEscortState(EscortState.Escorting) &&
                AssistPlayerInCombatAgainst(who))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void JustDied(Unit killer)
        {
            if (!HasEscortState(EscortState.Escorting) ||
                _playerGUID.IsEmpty() ||
                _escortQuest == null)
                return;

            Player player = GetPlayerForEscort();

            if (player)
            {
                Group group = player.GetGroup();

                if (group)
                    for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                    {
                        Player member = groupRef.GetSource();

                        if (member)
                            if (member.IsInMap(player))
                                member.FailQuest(_escortQuest.Id);
                    }
                else
                    player.FailQuest(_escortQuest.Id);
            }
        }

        public override void InitializeAI()
        {
            _escortState = EscortState.None;

            if (!IsCombatMovementAllowed())
                SetCombatMovement(true);

            //add a small delay before going to first waypoint, normal in near all cases
            _pauseTimer = TimeSpan.FromSeconds(2);

            if (me.GetFaction() != me.GetCreatureTemplate().Faction)
                me.RestoreFaction();

            Reset();
        }

        public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            me.RemoveAllAuras();
            me.CombatStop(true);
            me.SetTappedBy(null);

            EngagementOver();

            if (HasEscortState(EscortState.Escorting))
            {
                AddEscortState(EscortState.Returning);
                ReturnToLastPoint();
                Log.outDebug(LogFilter.ScriptsAi, $"EscortAI.EnterEvadeMode has left combat and is now returning to last point {me.GetGUID()}");
            }
            else
            {
                me.GetMotionMaster().MoveTargetedHome();

                if (_hasImmuneToNPCFlags)
                    me.SetImmuneToNPC(true);

                Reset();
            }
        }

        public override void UpdateAI(uint diff)
        {
            //Waypoint Updating
            if (HasEscortState(EscortState.Escorting) &&
                !me.IsEngaged() &&
                !HasEscortState(EscortState.Returning))
            {
                if (_pauseTimer.TotalMilliseconds <= diff)
                {
                    if (!HasEscortState(EscortState.Paused))
                    {
                        _pauseTimer = TimeSpan.Zero;

                        if (_ended)
                        {
                            _ended = false;
                            me.GetMotionMaster().MoveIdle();

                            if (_despawnAtEnd)
                            {
                                Log.outDebug(LogFilter.ScriptsAi, $"EscortAI::UpdateAI: reached end of waypoints, despawning at end ({me.GetGUID()})");

                                if (_returnToStart)
                                {
                                    var respawnPosition = me.GetRespawnPosition();
                                    respawnPosition.SetOrientation(respawnPosition.Orientation);
                                    me.GetMotionMaster().MovePoint(EscortPointIds.Home, respawnPosition);
                                    Log.outDebug(LogFilter.ScriptsAi, $"EscortAI::UpdateAI: returning to spawn location: {respawnPosition} ({me.GetGUID()})");
                                }
                                else if (_instantRespawn)
                                {
                                    me.Respawn();
                                }
                                else
                                {
                                    me.DespawnOrUnsummon();
                                }
                            }

                            Log.outDebug(LogFilter.ScriptsAi, $"EscortAI::UpdateAI: reached end of waypoints ({me.GetGUID()})");
                            RemoveEscortState(EscortState.Escorting);

                            return;
                        }

                        if (!_started)
                        {
                            _started = true;
                            me.GetMotionMaster().MovePath(_path, false);
                        }
                        else if (_resume)
                        {
                            _resume = false;
                            MovementGenerator movementGenerator = me.GetMotionMaster().GetCurrentMovementGenerator(MovementSlot.Default);

                            movementGenerator?.Resume(0);
                        }
                    }
                }
                else
                {
                    _pauseTimer -= TimeSpan.FromMilliseconds(diff);
                }
            }


            //Check if player or any member of his group is within range
            if (_despawnAtFar &&
                HasEscortState(EscortState.Escorting) &&
                !_playerGUID.IsEmpty() &&
                !me.IsEngaged() &&
                !HasEscortState(EscortState.Returning))
            {
                if (_playerCheckTimer <= diff)
                {
                    if (!IsPlayerOrGroupInRange())
                    {
                        Log.outDebug(LogFilter.ScriptsAi, $"EscortAI::UpdateAI: failed because player/group was to far away or not found ({me.GetGUID()})");

                        bool isEscort = false;
                        CreatureData creatureData = me.GetCreatureData();

                        if (creatureData != null)
                            isEscort = (WorldConfig.GetBoolValue(WorldCfg.RespawnDynamicEscortNpc) && creatureData.spawnGroupData.flags.HasAnyFlag(SpawnGroupFlags.EscortQuestNpc));

                        if (_instantRespawn)
                        {
                            if (!isEscort)
                                me.DespawnOrUnsummon(TimeSpan.Zero, TimeSpan.FromSeconds(1));
                            else
                                me.GetMap().Respawn(SpawnObjectType.Creature, me.GetSpawnId());
                        }
                        else
                        {
                            me.DespawnOrUnsummon();
                        }

                        return;
                    }

                    _playerCheckTimer = 1000;
                }
                else
                {
                    _playerCheckTimer -= diff;
                }
            }

            UpdateEscortAI(diff);
        }

        public virtual void UpdateEscortAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        public override void MovementInform(MovementGeneratorType moveType, uint Id)
        {
            // no Action allowed if there is no escort
            if (!HasEscortState(EscortState.Escorting))
                return;

            //Combat start position reached, continue waypoint movement
            if (moveType == MovementGeneratorType.Point)
            {
                if (_pauseTimer == TimeSpan.Zero)
                    _pauseTimer = TimeSpan.FromSeconds(2);

                if (Id == EscortPointIds.LastPoint)
                {
                    Log.outDebug(LogFilter.ScriptsAi, $"EscortAI::MovementInform has returned to original position before combat ({me.GetGUID()})");

                    me.SetWalk(!_running);
                    RemoveEscortState(EscortState.Returning);
                }
                else if (Id == EscortPointIds.Home)
                {
                    Log.outDebug(LogFilter.ScriptsAi, $"EscortAI::MovementInform: returned to home location and restarting waypoint path ({me.GetGUID()})");
                    _started = false;
                }
            }
            else if (moveType == MovementGeneratorType.Waypoint)
            {
                Cypher.Assert(Id < _path.Nodes.Count, $"EscortAI::MovementInform: referenced movement Id ({Id}) points to non-existing node in loaded path ({me.GetGUID()})");
                WaypointNode waypoint = _path.Nodes[(int)Id];

                Log.outDebug(LogFilter.ScriptsAi, $"EscortAI::MovementInform: waypoint node {waypoint.Id} reached ({me.GetGUID()})");

                // last point
                if (Id == _path.Nodes.Count - 1)
                {
                    _started = false;
                    _ended = true;
                    _pauseTimer = TimeSpan.FromSeconds(1);
                }
            }
        }

        public void AddWaypoint(uint id, float x, float y, float z, float orientation, TimeSpan waitTime)
        {
            x = GridDefines.NormalizeMapCoord(x);
            y = GridDefines.NormalizeMapCoord(y);

            WaypointNode waypoint = new();
            waypoint.Id = id;
            waypoint.X = x;
            waypoint.Y = y;
            waypoint.Z = z;
            waypoint.Orientation = orientation;
            waypoint.MoveType = _running ? WaypointMoveType.Run : WaypointMoveType.Walk;
            waypoint.Delay = (uint)waitTime.TotalMilliseconds;
            waypoint.EventId = 0;
            waypoint.EventChance = 100;
            _path.Nodes.Add(waypoint);

            _manualPath = true;
        }

        public void SetRun(bool on = true)
        {
            if (on == _running)
                return;

            foreach (var node in _path.Nodes)
                node.MoveType = on ? WaypointMoveType.Run : WaypointMoveType.Walk;

            me.SetWalk(!on);

            _running = on;
        }

        /// todo get rid of this many variables passed in function.
        public void Start(bool isActiveAttacker = true, bool run = false, ObjectGuid playerGUID = default, Quest quest = null, bool instantRespawn = false, bool canLoopPath = false, bool resetWaypoints = true)
        {
            // Queue respawn from the point it starts
            CreatureData cdata = me.GetCreatureData();

            if (cdata != null)
                if (WorldConfig.GetBoolValue(WorldCfg.RespawnDynamicEscortNpc) &&
                    cdata.spawnGroupData.flags.HasFlag(SpawnGroupFlags.EscortQuestNpc))
                    me.SaveRespawnTime(me.GetRespawnDelay());

            if (me.IsEngaged())
            {
                Log.outError(LogFilter.ScriptsAi, $"EscortAI::Start: (script: {me.GetScriptName()} attempts to Start while in combat ({me.GetGUID()})");

                return;
            }

            if (HasEscortState(EscortState.Escorting))
            {
                Log.outError(LogFilter.ScriptsAi, $"EscortAI::Start: (script: {me.GetScriptName()} attempts to Start while already escorting ({me.GetGUID()})");

                return;
            }

            _running = run;

            if (!_manualPath && resetWaypoints)
                FillPointMovementListForCreature();

            if (_path.Nodes.Empty())
            {
                Log.outError(LogFilter.ScriptsAi, $"EscortAI::Start: (script: {me.GetScriptName()} starts with 0 waypoints (possible missing entry in script_waypoint. Quest: {(quest != null ? quest.Id : 0)} ({me.GetGUID()})");

                return;
            }

            // set variables
            _activeAttacker = isActiveAttacker;
            _playerGUID = playerGUID;
            _escortQuest = quest;
            _instantRespawn = instantRespawn;
            _returnToStart = canLoopPath;

            if (_returnToStart && _instantRespawn)
                Log.outError(LogFilter.ScriptsAi, $"EscortAI::Start: (script: {me.GetScriptName()} is set to return home after waypoint end and instant respawn at waypoint end. Creature will never despawn ({me.GetGUID()})");

            me.GetMotionMaster().MoveIdle();
            me.GetMotionMaster().Clear(MovementGeneratorPriority.Normal);

            //disable npcflags
            me.ReplaceAllNpcFlags(NPCFlags.None);
            me.ReplaceAllNpcFlags2(NPCFlags2.None);

            if (me.IsImmuneToNPC())
            {
                _hasImmuneToNPCFlags = true;
                me.SetImmuneToNPC(false);
            }

            Log.outDebug(LogFilter.ScriptsAi, $"EscortAI::Start: (script: {me.GetScriptName()}, started with {_path.Nodes.Count} waypoints. ActiveAttacker = {_activeAttacker}, Run = {_running}, Player = {_playerGUID} ({me.GetGUID()})");

            // set initial speed
            me.SetWalk(!_running);

            _started = false;
            AddEscortState(EscortState.Escorting);
        }

        public void SetEscortPaused(bool on)
        {
            if (!HasEscortState(EscortState.Escorting))
                return;

            if (on)
            {
                AddEscortState(EscortState.Paused);
                MovementGenerator movementGenerator = me.GetMotionMaster().GetCurrentMovementGenerator(MovementSlot.Default);

                movementGenerator?.Pause(0);
            }
            else
            {
                RemoveEscortState(EscortState.Paused);
                _resume = true;
            }
        }

        public void SetPauseTimer(TimeSpan timer)
        {
            _pauseTimer = timer;
        }

        public bool HasEscortState(EscortState escortState)
        {
            return (_escortState & escortState) != 0;
        }

        public override bool IsEscorted()
        {
            return !_playerGUID.IsEmpty();
        }

        public void SetDespawnAtEnd(bool despawn)
        {
            _despawnAtEnd = despawn;
        }

        public void SetDespawnAtFar(bool despawn)
        {
            _despawnAtFar = despawn;
        }

        public bool IsActiveAttacker()
        {
            return _activeAttacker;
        } // used in EnterEvadeMode override

        public void SetActiveAttacker(bool attack)
        {
            _activeAttacker = attack;
        }

        //see followerAI
        private bool AssistPlayerInCombatAgainst(Unit who)
        {
            if (!who ||
                !who.GetVictim())
                return false;

            if (me.HasReactState(ReactStates.Passive))
                return false;

            //experimental (unknown) flag not present
            if (!me.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.CanAssist))
                return false;

            //not a player
            if (!who.GetVictim().GetCharmerOrOwnerPlayerOrPlayerItself())
                return false;

            //never attack friendly
            if (me.IsValidAssistTarget(who.GetVictim()))
                return false;

            //too far away and no free sight?
            if (me.IsWithinDistInMap(who, GetMaxPlayerDistance()) &&
                me.IsWithinLOSInMap(who))
            {
                me.EngageWithTarget(who);

                return true;
            }

            return false;
        }

        private void ReturnToLastPoint()
        {
            me.GetMotionMaster().MovePoint(0xFFFFFF, me.GetHomePosition());
        }

        private bool IsPlayerOrGroupInRange()
        {
            Player player = GetPlayerForEscort();

            if (player)
            {
                Group group = player.GetGroup();

                if (group)
                    for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                    {
                        Player member = groupRef.GetSource();

                        if (member)
                            if (me.IsWithinDistInMap(member, GetMaxPlayerDistance()))
                                return true;
                    }
                else if (me.IsWithinDistInMap(player, GetMaxPlayerDistance()))
                    return true;
            }

            return false;
        }

        private void FillPointMovementListForCreature()
        {
            WaypointPath path = Global.WaypointMgr.GetPath(me.GetEntry());

            if (path == null)
                return;

            foreach (WaypointNode value in path.Nodes)
            {
                WaypointNode node = value;
                node.X = GridDefines.NormalizeMapCoord(node.X);
                node.Y = GridDefines.NormalizeMapCoord(node.Y);
                node.MoveType = _running ? WaypointMoveType.Run : WaypointMoveType.Walk;

                _path.Nodes.Add(node);
            }
        }

        private void SetMaxPlayerDistance(float newMax)
        {
            _maxPlayerDistance = newMax;
        }

        private float GetMaxPlayerDistance()
        {
            return _maxPlayerDistance;
        }

        private ObjectGuid GetEventStarterGUID()
        {
            return _playerGUID;
        }

        private void AddEscortState(EscortState escortState)
        {
            _escortState |= escortState;
        }

        private void RemoveEscortState(EscortState escortState)
        {
            _escortState &= ~escortState;
        }
    }

    public enum EscortState
    {
        None = 0x00,      //nothing in progress
        Escorting = 0x01, //escort are in progress
        Returning = 0x02, //escort is returning after being in combat
        Paused = 0x04     //will not proceed with waypoints before State is removed
    }

    internal struct EscortPointIds
    {
        public const uint LastPoint = 0xFFFFFF;
        public const uint Home = 0xFFFFFE;
    }
}