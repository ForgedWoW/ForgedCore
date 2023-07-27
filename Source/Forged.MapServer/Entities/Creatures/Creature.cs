﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.AI;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Movement;
using Forged.MapServer.Networking.Packets.Combat;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Pools;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Entities.Creatures;

public partial class Creature : Unit
{
    public Creature(ClassFactory classFactory) : this(false, classFactory)
    {
    }

    public Creature(bool worldObject, ClassFactory classFactory) : base(worldObject, classFactory)
    {
        WaypointManager = classFactory.Resolve<WaypointManager>();
        WorldManager = classFactory.Resolve<WorldManager>();
        PoolManager = classFactory.Resolve<PoolManager>();
        WorldDatabase = classFactory.Resolve<WorldDatabase>();
        BattlegroundManager = classFactory.Resolve<BattlegroundManager>();
        CreatureFactory = classFactory.Resolve<CreatureFactory>();
        GameEventManager = classFactory.Resolve<GameEventManager>();
        FormationManager = classFactory.Resolve<FormationMgr>();
        CreatureTextManager = classFactory.Resolve<CreatureTextManager>();
        RespawnDelay = 300;
        CorpseDelay = 60;
        _boundaryCheckTime = 2500;
        ReactState = ReactStates.Aggressive;
        DefaultMovementType = MovementGeneratorType.Idle;
        _regenerateHealth = true;
        _meleeDamageSchoolMask = SpellSchoolMask.Normal;
        _triggerJustAppeared = true;

        RegenTimer = SharedConst.CreatureRegenInterval;

        SightDistance = SharedConst.SightRangeUnit;

        ResetLootMode(); // restore default loot mode

        _homePosition = new WorldLocation();

        CurrentWaypointInfo = new ValueTuple<uint, uint>();
    }

    public bool _IsTargetAcceptable(Unit target)
    {
        // if the target cannot be attacked, the target is not acceptable
        if (WorldObjectCombat.IsFriendlyTo(target) || !target.IsTargetableForAttack(false) || (Vehicle != null && (IsOnVehicle(target) || Vehicle.Base.IsOnVehicle(target))))
            return false;

        if (target.HasUnitState(UnitState.Died))
        {
            // some creatures can detect fake death
            if (CanIgnoreFeignDeath && target.HasUnitFlag2(UnitFlags2.FeignDeath))
                return true;

            return false;
        }

        // if I'm already fighting target, or I'm hostile towards the target, the target is acceptable
        if (IsEngagedBy(target) || WorldObjectCombat.IsHostileTo(target))
            return true;

        // if the target's victim is not friendly, or the target is friendly, the target is not acceptable
        return false;
    }

    public void AddLootMode(LootModes lootMode)
    {
        _lootMode |= lootMode;
    }

    public override void AddToWorld()
    {
        // Register the creature for guid lookup
        if (!Location.IsInWorld)
        {
            Location.Map.ObjectsStore.TryAdd(GUID, this);

            if (SpawnId != 0)
                Location.Map.CreatureBySpawnIdStore.Add(SpawnId, this);

            base.AddToWorld();
            SearchFormation();
            InitializeAI();

            if (IsVehicle)
                VehicleKit.Install();

            ZoneScript?.OnCreatureCreate(this);
        }
    }

    public void AllLootRemovedFromCorpse()
    {
        var now = GameTime.CurrentTime;

        // Do not reset corpse remove time if corpse is already removed
        if (CorpseRemoveTime <= now)
            return;

        // Scripts can choose to ignore RATE_CORPSE_DECAY_LOOTED by calling SetCorpseDelay(timer, true)
        var decayRate = _ignoreCorpseDecayRatio ? 1.0f : Configuration.GetDefaultValue("Rate:Corpse:Decay:Looted", 0.5f);

        // corpse skinnable, but without skinning Id, and then skinned, corpse will despawn next update
        bool IsFullySkinned()
        {
            if (Loot is { LootType: LootType.Skinning } && Loot.IsLooted())
                return true;

            foreach (var (_, loot) in PersonalLoot)
                if (loot != null && (loot.LootType != LootType.Skinning || !loot.IsLooted()))
                    return false;

            return true;
        }

        if (IsFullySkinned())
            CorpseRemoveTime = now;
        else
            CorpseRemoveTime = now + (uint)(CorpseDelay * decayRate);

        RespawnTime = Math.Max(CorpseRemoveTime + RespawnDelay, RespawnTime);
    }

    public void ApplyLevelScaling()
    {
        var scaling = Template.GetLevelScaling(Location.Map.DifficultyID);
        var levels = DB2Manager.GetContentTuningData(scaling.ContentTuningId, 0);

        if (levels.HasValue)
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ScalingLevelMin), levels.Value.MinLevel);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ScalingLevelMax), levels.Value.MaxLevel);
        }
        else if (Configuration.GetDefaultValue("CreatureScaling:DefaultMaxLevel", false))
        {
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ScalingLevelMin), 1);
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ScalingLevelMax), Configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel));
        }

        int mindelta = Math.Min(scaling.DeltaLevelMax, scaling.DeltaLevelMin);
        int maxdelta = Math.Max(scaling.DeltaLevelMax, scaling.DeltaLevelMin);
        var delta = mindelta == maxdelta ? mindelta : RandomHelper.IRand(mindelta, maxdelta);

        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ScalingLevelDelta), delta);
        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.ContentTuningID), scaling.ContentTuningId);
    }

    public override void AtDisengage()
    {
        base.AtDisengage();

        ClearUnitState(UnitState.AttackPlayer);

        if (IsAlive && HasDynamicFlag(UnitDynFlags.Tapped))
            ReplaceAllDynamicFlags((UnitDynFlags)Template.DynamicFlags);

        if (!IsPet && !IsGuardian) // update pets' speed for catchup OOC speed
            return;

        UpdateSpeed(UnitMoveType.Run);
        UpdateSpeed(UnitMoveType.Swim);
        UpdateSpeed(UnitMoveType.Flight);
    }

    public override void AtEngage(Unit target)
    {
        base.AtEngage(target);

        if (!Template.TypeFlags.HasAnyFlag(CreatureTypeFlags.AllowMountedCombat))
            Dismount();

        RefreshCanSwimFlag();

        if (IsPet || IsGuardian) // update pets' speed for catchup OOC speed
        {
            UpdateSpeed(UnitMoveType.Run);
            UpdateSpeed(UnitMoveType.Swim);
            UpdateSpeed(UnitMoveType.Flight);
        }

        var movetype = MotionMaster.GetCurrentMovementGeneratorType();

        if (movetype is MovementGeneratorType.Waypoint or MovementGeneratorType.Point || (IsAIEnabled && AI.IsEscorted()))
        {
            HomePosition = Location;
            // if its a vehicle, set the home positon of every creature passenger at engage
            // so that they are in combat range if hostile
            var vehicle = VehicleKit;

            if (vehicle != null)
                foreach (var (_, seat) in vehicle.Seats)
                {
                    var passenger = ObjectAccessor.GetUnit(this, seat.Passenger.Guid);

                    var creature = passenger?.AsCreature;

                    if (creature != null)
                        creature.HomePosition = Location;
                }
        }

        var ai = AI;

        ai?.JustEngagedWith(target);

        var formation = Formation;

        formation?.MemberEngagingTarget(this, target);
    }

    public void CallAssistance()
    {
        if (_alreadyCallAssistance || Victim == null || IsPet || IsCharmed)
            return;

        SetNoCallAssistance(true);

        var radius = Configuration.GetDefaultValue("CreatureFamilyAssistanceRadius", 10.0f);

        if (!(radius > 0))
            return;

        List<Creature> assistList = new();

        var uCheck = new AnyAssistCreatureInRangeCheck(this, Victim, radius);
        var searcher = new CreatureListSearcher(this, assistList, uCheck, GridType.Grid);
        CellCalculator.VisitGrid(this, searcher, radius);

        if (assistList.Empty())
            return;

        AssistDelayEvent e = new(Victim.GUID, this, ObjectAccessor);

        while (!assistList.Empty())
        {
            // Pushing guids because in delay can happen some creature gets despawned
            e.AddAssistant(assistList.First().GUID);
            assistList.Remove(assistList.First());
        }

        Events.AddEvent(e, Events.CalculateTime(TimeSpan.FromMilliseconds(Configuration.GetDefaultValue("CreatureFamilyAssistanceDelay", 1500))));
    }

    public void CallForHelp(float radius)
    {
        if (radius <= 0.0f || !IsEngaged || !IsAlive || IsPet || IsCharmed)
            return;

        var target = GetThreatManager().CurrentVictim;

        if (target == null)
            target = GetThreatManager().GetAnyTarget();

        if (target == null)
            target = CombatManager.GetAnyTarget();

        if (target == null)
        {
            Log.Logger.Error($"Creature {Entry} ({GetName()}) trying to call for help without being in combat.");

            return;
        }

        var uDo = new CallOfHelpCreatureInRangeDo(this, target, radius);
        var worker = new CreatureWorker(this, uDo, GridType.Grid);
        CellCalculator.VisitGrid(this, worker, radius);
    }

    public override bool CanAlwaysSee(WorldObject obj)
    {
        return IsAIEnabled && GetAI<CreatureAI>().CanSeeAlways(obj);
    }

    public bool CanAssistTo(Unit u, Unit enemy, bool checkfaction = true)
    {
        // is it true?
        if (!HasReactState(ReactStates.Aggressive))
            return false;

        // we don't need help from zombies :)
        if (!IsAlive)
            return false;

        // we cannot assist in evade mode
        if (IsInEvadeMode)
            return false;

        // or if enemy is in evade mode
        if (enemy.TypeId == TypeId.Unit && enemy.AsCreature.IsInEvadeMode)
            return false;

        // we don't need help from non-combatant ;)
        if (IsCivilian)
            return false;

        if (HasUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible) || HasUnitFlag(UnitFlags.ImmuneToNpc))
            return false;

        // skip fighting creature
        if (IsEngaged)
            return false;

        // only free creature
        if (!CharmerOrOwnerGUID.IsEmpty)
            return false;

        // only from same creature faction
        if (checkfaction)
        {
            if (Faction != u.Faction)
                return false;
        }
        else
        {
            if (!WorldObjectCombat.IsFriendlyTo(u))
                return false;
        }

        // skip non hostile to caster enemy creatures
        if (!WorldObjectCombat.IsHostileTo(enemy))
            return false;

        return true;
    }

    public bool CanCreatureAttack(Unit victim, bool force = true)
    {
        if (!victim.Location.IsInMap(this))
            return false;

        if (!WorldObjectCombat.IsValidAttackTarget(victim))
            return false;

        if (!victim.IsInAccessiblePlaceFor(this))
            return false;

        var ai = AI;

        if (ai != null)
            if (!ai.CanAIAttack(victim))
                return false;

        // we cannot attack in evade mode
        if (IsInEvadeMode)
            return false;

        // or if enemy is in evade mode
        if (victim.TypeId == TypeId.Unit && victim.AsCreature.IsInEvadeMode)
            return false;

        if (!CharmerOrOwnerGUID.IsPlayer)
        {
            if (Location.Map.IsDungeon)
                return true;

            // don't check distance to home position if recently damaged, this should include taunt auras
            if (!IsWorldBoss && (LastDamagedTime > GameTime.CurrentTime || HasAuraType(AuraType.ModTaunt)))
                return true;
        }

        // Map visibility range, but no more than 2*cell size
        var dist = Math.Min(Location.Map.VisibilityRange, MapConst.SizeofCells * 2);

        var unit = CharmerOrOwner;

        if (unit != null)
            return victim.Location.IsWithinDist(unit, dist);

        // include sizes for huge npcs
        dist += CombatReach + victim.CombatReach;

        // to prevent creatures in air ignore attacks because distance is already too high...
        if (MovementTemplate.IsFlightAllowed)
            return victim.Location.IsInDist2d(_homePosition, dist);

        return victim.Location.IsInDist(_homePosition, dist);
    }

    public bool CanInteractWithBattleMaster(Player player, bool msg)
    {
        if (!IsBattleMaster)
            return false;

        var bgTypeId = BattlegroundManager.GetBattleMasterBG(Entry);

        if (!msg)
            return player.GetBgAccessByLevel(bgTypeId);

        if (player.GetBgAccessByLevel(bgTypeId))
            return true;

        player.PlayerTalkClass.ClearMenus();

        switch (bgTypeId)
        {
            case BattlegroundTypeId.Av:
                player.PlayerTalkClass.SendGossipMenu(7616, GUID);

                break;

            case BattlegroundTypeId.Ws:
                player.PlayerTalkClass.SendGossipMenu(7599, GUID);

                break;

            case BattlegroundTypeId.AB:
                player.PlayerTalkClass.SendGossipMenu(7642, GUID);

                break;

            case BattlegroundTypeId.Ey:
            case BattlegroundTypeId.Na:
            case BattlegroundTypeId.Be:
            case BattlegroundTypeId.Aa:
            case BattlegroundTypeId.Rl:
            case BattlegroundTypeId.Sa:
            case BattlegroundTypeId.Ds:
            case BattlegroundTypeId.Rv:
                player.PlayerTalkClass.SendGossipMenu(10024, GUID);

                break;
        }

        return false;
    }

    public bool CanResetTalents(Player player)
    {
        return player.Level >= 15 && player.Class == Template.TrainerClass;
    }

    public bool CanStartAttack(Unit who, bool force)
    {
        if (IsCivilian)
            return false;

        // This set of checks is should be done only for creatures
        if ((HasUnitFlag(UnitFlags.ImmuneToNpc) && !who.HasUnitFlag(UnitFlags.PlayerControlled)) || (HasUnitFlag(UnitFlags.ImmuneToPc) && who.HasUnitFlag(UnitFlags.PlayerControlled)))
            return false;

        // Do not attack non-combat pets
        if (who.IsTypeId(TypeId.Unit) && who.CreatureType == CreatureType.NonCombatPet)
            return false;

        if (!CanFly && Location.GetDistanceZ(who) > SharedConst.CreatureAttackRangeZ + CombatDistance)
            return false;

        if (force)
            return CanCreatureAttack(who) && Location.IsWithinLOSInMap(who);

        if (!_IsTargetAcceptable(who))
            return false;

        if (WorldObjectCombat.IsNeutralToAll() || !Location.IsWithinDistInMap(who, (float)GetAttackDistance(who) + CombatDistance))
            return false;

        return CanCreatureAttack(who, false) && Location.IsWithinLOSInMap(who);
    }

    public void ClearTextRepeatGroup(byte textGroup)
    {
        var groupList = _textRepeat[textGroup];

        groupList?.Clear();
    }

    public bool Create(ulong guidlow, Map map, uint entry, Position pos, CreatureData data = null, uint vehId = 0, bool dynamic = false)
    {
        Location.Map = map;
        CheckAddToMap();

        if (data != null)
        {
            PhasingHandler.InitDbPhaseShift(Location.PhaseShift, data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
            PhasingHandler.InitDbVisibleMapId(Location.PhaseShift, data.TerrainSwapMap);
        }

        // Set if this creature can handle dynamic spawns
        if (!dynamic)
            RespawnCompatibilityMode = true;

        var cinfo = GameObjectManager.GetCreatureTemplate(entry);

        if (cinfo == null)
        {
            Log.Logger.Error("Creature.Create: creature template (guidlow: {0}, entry: {1}) does not exist.", guidlow, entry);

            return false;
        }

        //! Relocate before CreateFromProto, to initialize coords and allow
        //! returning correct zone id for selecting OutdoorPvP/Battlefield script
        Location.Relocate(pos);

        // Check if the position is valid before calling CreateFromProto(), otherwise we might add Auras to Creatures at
        // invalid position, triggering a crash about Auras not removed in the destructor
        if (!GridDefines.IsValidMapCoord(Location))
        {
            Log.Logger.Error($"Creature.Create: given coordinates for creature (guidlow {guidlow}, entry {entry}) are not valid ({pos})");

            return false;
        }

        {
            // area/zone id is needed immediately for ZoneScript::GetCreatureEntry hook before it is known which creature template to load (no model/scale available yet)
            PositionFullTerrainStatus positionData = new();
            Location.Map.GetFullTerrainStatusForPosition(Location.PhaseShift, Location.X, Location.Y, Location.Z, positionData, LiquidHeaderTypeFlags.AllLiquids);
            Location.ProcessPositionDataChanged(positionData);
        }

        // Allow players to see those units while dead, do it here (mayby altered by addon auras)
        if (cinfo.TypeFlags.HasAnyFlag(CreatureTypeFlags.VisibleToGhosts))
            Visibility.ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive | GhostVisibilityType.Ghost);

        if (!CreateFromProto(guidlow, entry, data, vehId))
            return false;

        cinfo = Template; // might be different than initially requested

        if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.DungeonBoss) && map.IsDungeon)
            RespawnDelay = 0; // special value, prevents respawn for dungeon bosses unless overridden

        CorpseDelay = cinfo.Rank switch
        {
            CreatureEliteType.Rare => Configuration.GetDefaultValue("Corpse:Decay:RARE", 300u),
            CreatureEliteType.Elite => Configuration.GetDefaultValue("Corpse:Decay:ELITE", 300u),
            CreatureEliteType.RareElite => Configuration.GetDefaultValue("Corpse:Decay:RAREELITE", 300u),
            CreatureEliteType.WorldBoss => Configuration.GetDefaultValue("Corpse:Decay:WORLDBOSS", 3600u),
            _ => Configuration.GetDefaultValue("Corpse:Decay:NORMAL", 60u)
        };

        LoadCreaturesAddon();

        //! Need to be called after LoadCreaturesAddon - MOVEMENTFLAG_HOVER is set there
        Location.Z += HoverOffset;

        LastUsedScriptID = GetScriptId();

        if (IsSpiritHealer || IsSpiritGuide || Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.GhostVisibility))
        {
            Visibility.ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
            Visibility.ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
        }

        if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.IgnorePathfinding))
            AddUnitState(UnitState.IgnorePathfinding);

        if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.ImmunityKnockback))
        {
            ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
            ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBackDest, true);
        }

        GetThreatManager().Initialize();

        return true;
    }

    public void DespawnOrUnsummon(TimeSpan msTimeToDespawn = default, TimeSpan forceRespawnTimer = default)
    {
        var summon = ToTempSummon();

        if (summon != null)
            summon.UnSummon(TimeSpan.FromMilliseconds(msTimeToDespawn.TotalMilliseconds));
        else
            ForcedDespawn((uint)msTimeToDespawn.TotalMilliseconds, forceRespawnTimer);
    }

    public void DisappearAndDie()
    {
        ForcedDespawn();
    }

    public void DoFleeToGetAssistance()
    {
        if (Victim == null)
            return;

        if (HasAuraType(AuraType.PreventsFleeing))
            return;

        var radius = Configuration.GetDefaultValue("CreatureFamilyFleeAssistanceRadius", 30.0f);

        if (radius > 0)
        {
            var uCheck = new NearestAssistCreatureInCreatureRangeCheck(this, Victim, radius);
            var searcher = new CreatureLastSearcher(this, uCheck, GridType.Grid);
            CellCalculator.VisitGrid(this, searcher, radius);

            var creature = searcher.Target;

            SetNoSearchAssistance(true);

            if (creature == null)
                SetControlled(true, UnitState.Fleeing);
            else
                MotionMaster.MoveSeekAssistance(creature.Location.X, creature.Location.Y, creature.Location.Z);
        }
    }

    public void DoImmediateBoundaryCheck() => _boundaryCheckTime = 0;

    public void DoNotReacquireSpellFocusTarget()
    {
        _spellFocusInfo.Delay = 0;
        _spellFocusInfo.Spell = null;
    }

    public override void ExitVehicle(Position exitPosition = null)
    {
        base.ExitVehicle(exitPosition);

        // if the creature exits a vehicle, set it's home position to the
        // exited position so it won't run away (home) and evade if it's hostile
        HomePosition = Location;
    }

    public void ForcedDespawn(uint timeMSToDespawn = 0, TimeSpan forceRespawnTimer = default)
    {
        if (timeMSToDespawn != 0)
        {
            Events.AddEvent(new ForcedDespawnDelayEvent(this, forceRespawnTimer), Events.CalculateTime(TimeSpan.FromMilliseconds(timeMSToDespawn)));

            return;
        }

        if (RespawnCompatibilityMode)
        {
            var corpseDelay = CorpseDelay;
            var respawnDelay = RespawnDelay;

            // do it before killing creature
            UpdateObjectVisibilityOnDestroy();

            var overrideRespawnTime = false;

            if (IsAlive)
            {
                if (forceRespawnTimer > TimeSpan.Zero)
                {
                    SetCorpseDelay(0);
                    RespawnDelay = (uint)forceRespawnTimer.TotalSeconds;
                    overrideRespawnTime = true;
                }

                SetDeathState(DeathState.JustDied);
            }

            // Skip corpse decay time
            RemoveCorpse(overrideRespawnTime, false);

            SetCorpseDelay(corpseDelay);
            RespawnDelay = respawnDelay;
        }
        else
        {
            if (forceRespawnTimer > TimeSpan.Zero)
                SaveRespawnTime((uint)forceRespawnTimer.TotalSeconds);
            else
            {
                var respawnDelay = RespawnDelay;
                var scalingMode = Configuration.GetDefaultValue("Respawn:DynamicMode", 0u);

                if (scalingMode != 0)
                    Location.Map.ApplyDynamicModeRespawnScaling(this, SpawnId, ref respawnDelay, scalingMode);

                RespawnTime = GameTime.CurrentTime + respawnDelay;
                SaveRespawnTime();
            }

            Location.AddObjectToRemoveList();
        }
    }

    public float GetAggroRange(Unit target)
    {
        // Determines the aggro range for creatures (usually pets), used mainly for aggressive pet target selection.
        // Based on data from wowwiki due to lack of 3.3.5a data

        if (target != null && IsPet)
        {
            uint targetLevel = 0;

            if (target.IsTypeId(TypeId.Player))
                targetLevel = target.GetLevelForTarget(this);
            else if (target.IsTypeId(TypeId.Unit))
                targetLevel = target.AsCreature.GetLevelForTarget(this);

            var myLevel = GetLevelForTarget(target);
            var levelDiff = (int)(targetLevel - myLevel);

            // The maximum Aggro Radius is capped at 45 yards (25 level difference)
            if (levelDiff < -25)
                levelDiff = -25;

            // The base aggro radius for mob of same level
            double aggroRadius = 20;

            // Aggro Radius varies with level difference at a rate of roughly 1 yard/level
            aggroRadius -= levelDiff;

            // detect range auras
            aggroRadius += GetTotalAuraModifier(AuraType.ModDetectRange);

            // detected range auras
            aggroRadius += target.GetTotalAuraModifier(AuraType.ModDetectedRange);

            // Just in case, we don't want pets running all over the map
            if (aggroRadius > SharedConst.MaxAggroRadius)
                aggroRadius = SharedConst.MaxAggroRadius;

            // Minimum Aggro Radius for a mob seems to be combat range (5 yards)
            //  hunter pets seem to ignore minimum aggro radius so we'll default it a little higher
            if (aggroRadius < 10)
                aggroRadius = 10;

            return (float)aggroRadius;
        }

        // Default
        return 0.0f;
    }

    public T GetAI<T>() where T : CreatureAI
    {
        return Ai as T;
    }

    public string GetAIName()
    {
        return GameObjectManager.GetCreatureTemplate(Entry).AIName;
    }

    public override float GetArmorMultiplierForTarget(WorldObject target)
    {
        if (!HasScalableLevels)
            return 1.0f;

        var levelForTarget = GetLevelForTarget(target);

        return GetBaseArmorForLevel(levelForTarget) / GetBaseArmorForLevel(Level);
    }

    public double GetAttackDistance(Unit player)
    {
        var aggroRate = Configuration.GetDefaultValue("Rate:Creature:Aggro", 1.0f);

        if (aggroRate == 0)
            return 0.0f;

        // WoW Wiki: the minimum radius seems to be 5 yards, while the maximum range is 45 yards
        var maxRadius = 45.0f * aggroRate;
        var minRadius = 5.0f * aggroRate;

        var expansionMaxLevel = (int)GameObjectManager.GetMaxLevelForExpansion((Expansion)Template.RequiredExpansion);
        var playerLevel = (int)player.GetLevelForTarget(this);
        var creatureLevel = (int)GetLevelForTarget(player);
        var levelDifference = creatureLevel - playerLevel;

        // The aggro radius for creatures with equal level as the player is 20 yards.
        // The combatreach should not get taken into account for the distance so we drop it from the range (see Supremus as expample)
        var baseAggroDistance = 20.0f - CombatReach;

        // + - 1 yard for each level difference between player and creature
        double aggroRadius = baseAggroDistance + levelDifference;

        // detect range auras
        if (creatureLevel + 5 <= Configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
        {
            aggroRadius += GetTotalAuraModifier(AuraType.ModDetectRange);
            aggroRadius += player.GetTotalAuraModifier(AuraType.ModDetectedRange);
        }

        // The aggro range of creatures with higher levels than the total player level for the expansion should get the maxlevel treatment
        // This makes sure that creatures such as bosses wont have a bigger aggro range than the rest of the npc's
        // The following code is used for blizzlike behaviour such as skippable bosses
        if (creatureLevel > expansionMaxLevel)
            aggroRadius = baseAggroDistance + (expansionMaxLevel - playerLevel);

        // Make sure that we wont go over the total range limits
        if (aggroRadius > maxRadius)
            aggroRadius = maxRadius;
        else if (aggroRadius < minRadius)
            aggroRadius = minRadius;

        return aggroRadius * aggroRate;
    }

    public float GetBaseDamageForLevel(uint level)
    {
        var cInfo = Template;
        var scaling = cInfo.GetLevelScaling(Location.Map.DifficultyID);

        return DB2Manager.EvaluateExpectedStat(ExpectedStatType.CreatureAutoAttackDps, level, cInfo.GetHealthScalingExpansion(), scaling.ContentTuningId, (PlayerClass)cInfo.UnitClass);
    }

    public override float GetDamageMultiplierForTarget(WorldObject target)
    {
        if (!HasScalableLevels)
            return 1.0f;

        var levelForTarget = GetLevelForTarget(target);

        return GetBaseDamageForLevel(levelForTarget) / GetBaseDamageForLevel(Level);
    }

    public override string GetDebugInfo()
    {
        return $"{base.GetDebugInfo()}\nAIName: {GetAIName()} ScriptName: {GetScriptName()} WaypointPath: {WaypointPath} SpawnId: {SpawnId}";
    }

    public override MovementGeneratorType GetDefaultMovementType()
    {
        return DefaultMovementType;
    }

    public float GetHealthMod(CreatureEliteType rank)
    {
        return rank switch // define rates for each elite rank
        {
            CreatureEliteType.Normal => Configuration.GetDefaultValue("Rate:Creature:Normal:HP", 1.0f),
            CreatureEliteType.Elite => Configuration.GetDefaultValue("Rate:Creature:Elite:Elite:HP", 1.0f),
            CreatureEliteType.RareElite => Configuration.GetDefaultValue("Rate:Creature:Elite:RAREELITE:HP", 1.0f),
            CreatureEliteType.WorldBoss => Configuration.GetDefaultValue("Rate:Creature:Elite:WORLDBOSS:HP", 1.0f),
            CreatureEliteType.Rare => Configuration.GetDefaultValue("Rate:Creature:Elite:RARE:HP", 1.0f),
            _ => Configuration.GetDefaultValue("Rate:Creature:Elite:RAREELITE:HP", 1.0f)
        };
    }

    public override float GetHealthMultiplierForTarget(WorldObject target)
    {
        if (!HasScalableLevels)
            return 1.0f;

        var levelForTarget = GetLevelForTarget(target);

        if (Level < levelForTarget)
            return 1.0f;

        return (float)GetMaxHealthByLevel(levelForTarget) / GetCreateHealth();
    }

    public override uint GetLevelForTarget(WorldObject target)
    {
        var unitTarget = target.AsUnit;

        if (unitTarget == null)
            return base.GetLevelForTarget(target);

        if (IsWorldBoss)
        {
            var level = (int)(unitTarget.Level + Configuration.GetDefaultValue("WorldBossLevelDiff", 3));

            return (uint)MathFunctions.RoundToInterval(ref level, 1u, 255u);
        }

        // If this creature should scale level, adapt level depending of target level
        // between UNIT_FIELD_SCALING_LEVEL_MIN and UNIT_FIELD_SCALING_LEVEL_MAX
        if (!HasScalableLevels)
            return base.GetLevelForTarget(target);

        int scalingLevelMin = UnitData.ScalingLevelMin;
        int scalingLevelMax = UnitData.ScalingLevelMax;
        int scalingLevelDelta = UnitData.ScalingLevelDelta;
        int scalingFactionGroup = UnitData.ScalingFactionGroup;
        int targetLevel = unitTarget.UnitData.EffectiveLevel;

        if (targetLevel == 0)
            targetLevel = (int)unitTarget.Level;

        var targetLevelDelta = 0;

        var playerTarget = target.AsPlayer;

        if (playerTarget != null)
        {
            if (scalingFactionGroup != 0 && CliDB.FactionTemplateStorage.LookupByKey((uint)CliDB.ChrRacesStorage.LookupByKey((uint)playerTarget.Race).FactionID).FactionGroup != scalingFactionGroup)
                scalingLevelMin = scalingLevelMax;

            int maxCreatureScalingLevel = playerTarget.ActivePlayerData.MaxCreatureScalingLevel;
            targetLevelDelta = Math.Min(maxCreatureScalingLevel > 0 ? maxCreatureScalingLevel - targetLevel : 0, playerTarget.ActivePlayerData.ScalingPlayerLevelDelta);
        }

        var levelWithDelta = targetLevel + targetLevelDelta;
        var scaledLevel = MathFunctions.RoundToInterval(ref levelWithDelta, scalingLevelMin, scalingLevelMax) + scalingLevelDelta;

        return (uint)MathFunctions.RoundToInterval(ref scaledLevel, 1, SharedConst.MaxLevel + 3);
    }

    public override Loot GetLootForPlayer(Player player)
    {
        if (PersonalLoot.Empty())
            return Loot;

        var loot = PersonalLoot.LookupByKey(player.GUID);

        return loot;
    }

    public LootModes GetLootMode()
    {
        return _lootMode;
    }

    public override SpellSchoolMask GetMeleeDamageSchoolMask(WeaponAttackType attackType = WeaponAttackType.BaseAttack)
    {
        return _meleeDamageSchoolMask;
    }

    public override string GetName(Locale locale = Locale.enUS)
    {
        if (locale == Locale.enUS)
            return base.GetName(locale);

        var cl = GameObjectManager.GetCreatureLocale(Entry);

        if (cl == null)
            return base.GetName(locale);

        if (cl.Name.Length > (int)locale && !cl.Name[(int)locale].IsEmpty())
            return cl.Name[(int)locale];

        return base.GetName(locale);
    }

    public virtual uint GetPetAutoSpellOnPos(byte pos)
    {
        if (pos >= SharedConst.MaxSpellCharm || GetCharmInfo() == null || GetCharmInfo().GetCharmSpell(pos).ActiveState != ActiveStates.Enabled)
            return 0;

        return GetCharmInfo().GetCharmSpell(pos).Action;
    }

    public float GetPetChaseDistance()
    {
        var range = 0f;

        for (byte i = 0; i < PetAutoSpellSize; ++i)
        {
            var spellID = GetPetAutoSpellOnPos(i);

            if (spellID == 0)
                continue;

            var spellInfo = SpellManager.GetSpellInfo(spellID, Location.Map.DifficultyID);

            if (spellInfo == null)
                continue;

            if (spellInfo.RecoveryTime1 == 0 && spellInfo.RangeEntry.Id != 1 /*Self*/ && spellInfo.RangeEntry.Id != 2 /*Combat Range*/ && spellInfo.GetMaxRange() > range)
                range = spellInfo.GetMaxRange();
        }

        return range;
    }

    public Position GetRespawnPosition(out float dist)
    {
        if (CreatureData != null)
        {
            dist = CreatureData.WanderDistance;

            return CreatureData.SpawnPoint.Copy();
        }

        dist = 0;

        return HomePosition.Copy();
    }

    public uint GetScriptId()
    {
        var creatureData = CreatureData;

        if (creatureData != null)
        {
            var scriptId = creatureData.ScriptId;

            if (scriptId != 0)
                return scriptId;
        }

        if (Template.ScriptID != 0)
            return Template.ScriptID;

        return GameObjectManager.GetCreatureTemplate(Entry) != null ? GameObjectManager.GetCreatureTemplate(Entry).ScriptID : 0;
    }

    public string GetScriptName()
    {
        return ScriptManager.GetScriptName(GetScriptId());
    }

    public float GetSpellDamageMod(CreatureEliteType rank)
    {
        return rank switch // define rates for each elite rank
        {
            CreatureEliteType.Normal => Configuration.GetDefaultValue("Rate:Creature:Normal:SpellDamage", 1.0f),
            CreatureEliteType.Elite => Configuration.GetDefaultValue("Rate:Creature:Elite:Elite:SpellDamage", 1.0f),
            CreatureEliteType.RareElite => Configuration.GetDefaultValue("Rate:Creature:Elite:RAREELITE:SpellDamage", 1.0f),
            CreatureEliteType.WorldBoss => Configuration.GetDefaultValue("Rate:Creature:Elite:WORLDBOSS:SpellDamage", 1.0f),
            CreatureEliteType.Rare => Configuration.GetDefaultValue("Rate:Creature:Elite:RARE:SpellDamage", 1.0f),
            _ => Configuration.GetDefaultValue("Rate:Creature:Elite:Elite:SpellDamage", 1.0f)
        };
    }

    public List<byte> GetTextRepeatGroup(byte textGroup)
    {
        return _textRepeat.LookupByKey(textGroup);
    }

    public uint GetVendorItemCurrentCount(VendorItem vItem)
    {
        if (vItem.Maxcount == 0)
            return vItem.Maxcount;

        if (!_vendorItemCounts.TryGetValue(vItem.Item, out var vCount))
            return vItem.Maxcount;

        var ptime = GameTime.CurrentTime;

        if (vCount.LastIncrementTime + vItem.Incrtime > ptime)
            return vCount.Count;

        var pProto = GameObjectManager.ItemTemplateCache.GetItemTemplate(vItem.Item);

        var diff = (uint)((ptime - vCount.LastIncrementTime) / vItem.Incrtime);

        if (vCount.Count + diff * pProto.BuyCount >= vItem.Maxcount)
        {
            _vendorItemCounts.Remove(vCount.ItemId);

            return vItem.Maxcount;
        }

        vCount.Count += diff * pProto.BuyCount;
        vCount.LastIncrementTime = ptime;

        return vCount.Count;
    }

    public override bool HasInvolvedQuest(uint questId)
    {
        return GameObjectManager.GetCreatureQuestInvolvedRelations(Entry).HasQuest(questId);
    }

    public bool HasLootMode(LootModes lootMode)
    {
        return Convert.ToBoolean(_lootMode & lootMode);
    }

    public override bool HasQuest(uint questId)
    {
        return GameObjectManager.GetCreatureQuestRelations(Entry).HasQuest(questId);
    }

    public bool HasReactState(ReactStates state)
    {
        return ReactState == state;
    }

    public override bool HasSpell(uint spellId)
    {
        return Spells.Contains(spellId);
    }

    public override bool HasSpellFocus(Spell focusSpell = null)
    {
        if (IsDead) // dead creatures cannot focus
        {
            if (_spellFocusInfo.Spell != null || _spellFocusInfo.Delay != 0)
                Log.Logger.Warning($"Creature '{GetName()}' (entry {Entry}) has spell focus (spell id {(_spellFocusInfo.Spell != null ? _spellFocusInfo.Spell.SpellInfo.Id : 0)}, delay {_spellFocusInfo.Delay}ms) despite being dead.");

            return false;
        }

        if (focusSpell != null)
            return focusSpell == _spellFocusInfo.Spell;

        return _spellFocusInfo.Spell != null || _spellFocusInfo.Delay != 0;
    }

    public bool HasStringId(string id)
    {
        return StringIds.Contains(id);
    }

    public bool InitEntry(uint entry, CreatureData data = null)
    {
        var normalInfo = GameObjectManager.GetCreatureTemplate(entry);

        if (normalInfo == null)
        {
            Log.Logger.Error("Creature.InitEntry creature entry {0} does not exist.", entry);

            return false;
        }

        // get difficulty 1 mode entry
        CreatureTemplate cInfo = null;
        var difficultyEntry = CliDB.DifficultyStorage.LookupByKey((uint)Location.Map.DifficultyID);

        while (difficultyEntry != null)
        {
            var idx = CreatureTemplate.DifficultyIDToDifficultyEntryIndex(difficultyEntry.Id);

            if (idx == -1)
                break;

            if (normalInfo.DifficultyEntry[idx] != 0)
            {
                cInfo = GameObjectManager.GetCreatureTemplate(normalInfo.DifficultyEntry[idx]);

                break;
            }

            if (difficultyEntry.FallbackDifficultyID == 0)
                break;

            difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficultyEntry.FallbackDifficultyID);
        }

        cInfo ??= normalInfo;

        Entry = entry;    // normal entry always
        Template = cInfo; // map mode related always

        // equal to player Race field, but creature does not have race
        Race = 0;
        Class = (PlayerClass)cInfo.UnitClass;

        // Cancel load if no model defined
        if (cInfo.GetFirstValidModel() == null)
        {
            Log.Logger.Error("Creature (Entry: {0}) has no model defined in table `creature_template`, can't load. ", entry);

            return false;
        }

        var model = GameObjectManager.ChooseDisplayId(cInfo, data);
        var minfo = GameObjectManager.GetCreatureModelRandomGender(ref model, cInfo);

        if (minfo == null) // Cancel load if no model defined
        {
            Log.Logger.Error("Creature (Entry: {0}) has invalid model {1} defined in table `creature_template`, can't load.", entry, model.CreatureDisplayId);

            return false;
        }

        SetDisplayId(model.CreatureDisplayId, model.DisplayScale);
        SetNativeDisplayId(model.CreatureDisplayId, model.DisplayScale);

        // Load creature equipment
        if (data == null)
            LoadEquipment(); // use default equipment (if available) for summons
        else if (data.EquipmentId == 0)
            LoadEquipment(0); // 0 means no equipment for creature table
        else
        {
            OriginalEquipmentId = data.EquipmentId;
            LoadEquipment(data.EquipmentId);
        }

        SetName(normalInfo.Name); // at normal entry always

        SetModCastingSpeed(1.0f);
        SetModSpellHaste(1.0f);
        SetModHaste(1.0f);
        SetModRangedHaste(1.0f);
        SetModHasteRegen(1.0f);
        SetModTimeRate(1.0f);

        SetSpeedRate(UnitMoveType.Walk, cInfo.SpeedWalk);
        SetSpeedRate(UnitMoveType.Run, cInfo.SpeedRun);
        SetSpeedRate(UnitMoveType.Swim, 1.0f);   // using 1.0 rate
        SetSpeedRate(UnitMoveType.Flight, 1.0f); // using 1.0 rate

        ObjectScale = NativeObjectScale;

        SetHoverHeight(cInfo.HoverHeight);

        SetCanDualWield(cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.UseOffhandAttack));

        // checked at loading
        DefaultMovementType = (MovementGeneratorType)(data?.MovementType ?? cInfo.MovementType);

        if (WanderDistance == 0 && DefaultMovementType == MovementGeneratorType.Random)
            DefaultMovementType = MovementGeneratorType.Idle;

        for (byte i = 0; i < SharedConst.MaxCreatureSpells; ++i)
            Spells[i] = Template.Spells[i];

        StaticFlags.ModifyFlag(CreatureStaticFlags.NO_XP, IsCritter && IsPet && IsTotem && Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoXP));

        return true;
    }

    public bool InitializeAI(CreatureAI ai = null)
    {
        InitializeMovementAI();

        AI = ai ?? AISelector.SelectAI(this);

        Ai.InitializeAI();

        // Initialize vehicle
        VehicleKit?.Reset();

        return true;
    }

    public void InitializeReactState()
    {
        if (IsTotem || IsTrigger || IsCritter || IsSpiritService)
            ReactState = ReactStates.Passive;
        else
            ReactState = ReactStates.Aggressive;
    }

    public override bool IsImmunedToSpellEffect(SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, WorldObject caster, bool requireImmunityPurgesEffectAttribute = false)
    {
        if (Template.CreatureType == CreatureType.Mechanical && spellEffectInfo.IsEffectName(SpellEffectName.Heal))
            return true;

        return base.IsImmunedToSpellEffect(spellInfo, spellEffectInfo, caster, requireImmunityPurgesEffectAttribute);
    }

    public override bool IsInvisibleDueToDespawn(WorldObject seer)
    {
        if (base.IsInvisibleDueToDespawn(seer))
            return true;

        if (IsAlive || CorpseRemoveTime > GameTime.CurrentTime)
            return false;

        return true;
    }

    public override bool IsMovementPreventedByCasting()
    {
        // first check if currently a movement allowed channel is active and we're not casting
        var spell = GetCurrentSpell(CurrentSpellTypes.Channeled);

        if (spell != null)
            if (spell.State != SpellState.Finished && spell.IsChannelActive)
                if (spell.CheckMovement() != SpellCastResult.SpellCastOk)
                    return true;

        if (HasSpellFocus())
            return true;

        if (HasUnitState(UnitState.Casting))
            return true;

        return false;
    }

    public bool IsSkinnedBy(Player player)
    {
        var loot = GetLootForPlayer(player);

        if (loot != null)
            return loot.LootType == LootType.Skinning;

        return false;
    }

    public bool IsTappedBy(Player player)
    {
        return TapList.Contains(player.GUID);
    }

    public bool LoadCreaturesAddon()
    {
        var creatureAddon = CreatureAddon;

        if (creatureAddon == null)
            return false;

        if (creatureAddon.Mount != 0)
            Mount(creatureAddon.Mount);

        SetStandState((UnitStandStateType)creatureAddon.StandState);
        ReplaceAllVisFlags((UnitVisFlags)creatureAddon.VisFlags);
        SetAnimTier((AnimTier)creatureAddon.AnimTier, false);

        //! Suspected correlation between UNIT_FIELD_BYTES_1, offset 3, value 0x2:
        //! If no inhabittype_fly (if no MovementFlag_DisableGravity or MovementFlag_CanFly Id found in sniffs)
        //! Check using InhabitType as movement flags are assigned dynamically
        //! basing on whether the creature is in air or not
        //! Set MovementFlag_Hover. Otherwise do nothing.
        if (CanHover)
            MovementInfo.AddMovementFlag(MovementFlag.Hover);

        Sheath = (SheathState)creatureAddon.SheathState;
        ReplaceAllPvpFlags((UnitPVPStateFlags)creatureAddon.PvpFlags);

        // These fields must only be handled by core internals and must not be modified via scripts/DB dat
        ReplaceAllPetFlags(UnitPetFlags.None);
        ShapeshiftForm = ShapeShiftForm.None;

        if (creatureAddon.Emote != 0)
            EmoteState = (Emote)creatureAddon.Emote;

        SetAIAnimKitId(creatureAddon.AiAnimKit);
        SetMovementAnimKitId(creatureAddon.MovementAnimKit);
        SetMeleeAnimKitId(creatureAddon.MeleeAnimKit);

        // Check if visibility distance different
        if (creatureAddon.VisibilityDistanceType != VisibilityDistanceType.Normal)
            Visibility.SetVisibilityDistanceOverride(creatureAddon.VisibilityDistanceType);

        //Load Path
        if (creatureAddon.PathId != 0)
            WaypointPath = creatureAddon.PathId;

        if (creatureAddon.Auras != null)
            foreach (var id in creatureAddon.Auras)
            {
                var additionalSpellInfo = SpellManager.GetSpellInfo(id, Location.Map.DifficultyID);

                if (additionalSpellInfo == null)
                {
                    Log.Logger.Error("Creature ({0}) has wrong spell {1} defined in `auras` field.", GUID.ToString(), id);

                    continue;
                }

                // skip already applied aura
                if (HasAura(id))
                    continue;

                AddAura(id, this);
                Log.Logger.Debug("Spell: {0} added to creature ({1})", id, GUID.ToString());
            }

        return true;
    }

    public void LoadEquipment(int id = 1, bool force = true)
    {
        if (id == 0)
        {
            if (!force)
                return;

            for (byte i = 0; i < SharedConst.MaxEquipmentItems; ++i)
                SetVirtualItem(i, 0);

            CurrentEquipmentId = 0;

            return;
        }

        var einfo = GameObjectManager.GetEquipmentInfo(Entry, id);

        if (einfo == null)
            return;

        CurrentEquipmentId = (byte)id;

        for (byte i = 0; i < SharedConst.MaxEquipmentItems; ++i)
            SetVirtualItem(i, einfo.Items[i].ItemId, einfo.Items[i].AppearanceModId, einfo.Items[i].ItemVisual);
    }

    public override bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool allowDuplicate)
    {
        if (!allowDuplicate)
        {
            // If an alive instance of this spawnId is already found, skip creation
            // If only dead instance(s) exist, despawn them and spawn a new (maybe also dead) version
            var creatureBounds = map.CreatureBySpawnIdStore.LookupByKey(spawnId);
            List<Creature> despawnList = new();

            foreach (var creature in creatureBounds)
                if (creature.IsAlive)
                {
                    Log.Logger.Debug("Would have spawned {0} but {1} already exists", spawnId, creature.GUID.ToString());

                    return false;
                }
                else
                {
                    despawnList.Add(creature);
                    Log.Logger.Debug("Despawned dead instance of spawn {0} ({1})", spawnId, creature.GUID.ToString());
                }

            foreach (var despawnCreature in despawnList)
                despawnCreature.Location.AddObjectToRemoveList();
        }

        var data = GameObjectManager.GetCreatureData(spawnId);

        if (data == null)
        {
            Log.Logger.Error($"Creature (SpawnID: {spawnId}) not found in table `creature`, can't load.");

            return false;
        }

        SpawnId = spawnId;
        RespawnCompatibilityMode = data.SpawnGroupData.Flags.HasAnyFlag(SpawnGroupFlags.CompatibilityMode);
        CreatureData = data;
        WanderDistance = data.WanderDistance;
        RespawnDelay = (uint)data.Spawntimesecs;

        if (!Create(map.GenerateLowGuid(HighGuid.Creature), map, data.Id, data.SpawnPoint, data, 0, !RespawnCompatibilityMode))
            return false;

        //We should set first home position, because then AI calls home movement
        HomePosition = Location;

        DeathState = DeathState.Alive;

        RespawnTime = Location.Map.GetCreatureRespawnTime(SpawnId);

        if (RespawnTime == 0 && !map.IsSpawnGroupActive(data.SpawnGroupData.GroupId))
        {
            if (!RespawnCompatibilityMode)
                // @todo pools need fixing! this is just a temporary thing, but they violate dynspawn principles
                if (data.PoolId == 0)
                {
                    Log.Logger.Error($"Creature (SpawnID {spawnId}) trying to load in inactive spawn group '{data.SpawnGroupData.Name}':\n{GetDebugInfo()}");

                    return false;
                }

            RespawnTime = GameTime.CurrentTime + RandomHelper.URand(4, 7);
        }

        if (RespawnTime != 0)
        {
            if (!RespawnCompatibilityMode)
            {
                // @todo same as above
                if (data.PoolId == 0)
                {
                    Log.Logger.Error($"Creature (SpawnID {spawnId}) trying to load despite a respawn timer in progress:\n{GetDebugInfo()}");

                    return false;
                }
            }
            else
                // compatibility mode creatures will be respawned in ::Update()
                DeathState = DeathState.Dead;

            if (CanFly)
            {
                var tz = map.GetHeight(Location.PhaseShift, data.SpawnPoint, true, MapConst.MaxFallDistance);

                if (data.SpawnPoint.Z - tz > 0.1f && GridDefines.IsValidMapCoord(tz))
                    Location.Relocate(data.SpawnPoint.X, data.SpawnPoint.Y, tz);
            }
        }

        SetSpawnHealth();

        SelectWildBattlePetLevel();

        // checked at creature_template loading
        DefaultMovementType = (MovementGeneratorType)data.MovementType;

        StringIds[1] = data.StringId;

        if (addToMap && !Location.Map.AddToMap(this))
            return false;

        return true;
    }

    public void LoadPath(uint pathid)
    {
        WaypointPath = pathid;
    }

    public void LoadTemplateImmunities()
    {
        // uint32 max used for "spell id", the immunity system will not perform SpellInfo checks against invalid spells
        // used so we know which immunities were loaded from template
        var placeholderSpellId = uint.MaxValue;

        // unapply template immunities (in case we're updating entry)
        for (uint i = 0; i < (int)Mechanics.Max; ++i)
            ApplySpellImmune(placeholderSpellId, SpellImmunity.Mechanic, i, false);

        for (var i = (int)SpellSchools.Normal; i < (int)SpellSchools.Max; ++i)
            ApplySpellImmune(placeholderSpellId, SpellImmunity.School, 1u << i, false);

        // don't inherit immunities for hunter pets
        if (OwnerGUID.IsPlayer && IsHunterPet)
            return;

        var mechanicMask = Template.MechanicImmuneMask;

        if (mechanicMask != 0)
            for (uint i = 0 + 1; i < (int)Mechanics.Max; ++i)
                if ((mechanicMask & (1ul << ((int)i - 1))) != 0)
                    ApplySpellImmune(placeholderSpellId, SpellImmunity.Mechanic, i, true);

        var schoolMask = Template.SpellSchoolImmuneMask;

        if (schoolMask != 0)
            for (var i = (int)SpellSchools.Normal; i <= (int)SpellSchools.Max; ++i)
                if ((schoolMask & (1 << i)) != 0)
                    ApplySpellImmune(placeholderSpellId, SpellImmunity.School, 1u << i, true);
    }

    public void LowerPlayerDamageReq(double unDamage)
    {
        LowerPlayerDamageReq((ulong)unDamage);
    }

    public void LowerPlayerDamageReq(ulong unDamage)
    {
        if (PlayerDamageReq != 0)
        {
            if (PlayerDamageReq > unDamage)
                PlayerDamageReq -= unDamage;
            else
                PlayerDamageReq = 0;
        }
    }

    public void RefreshCanSwimFlag(bool recheck = false)
    {
        if (!_isMissingCanSwimFlagOutOfCombat || recheck)
            _isMissingCanSwimFlagOutOfCombat = !HasUnitFlag(UnitFlags.CanSwim);

        // Check if the creature has UNIT_FLAG_CAN_SWIM and add it if it's missing
        // Creatures must be able to chase a target in water if they can enter water
        if (_isMissingCanSwimFlagOutOfCombat && CanEnterWater)
            SetUnitFlag(UnitFlags.CanSwim);
    }

    public void Regenerate(PowerType power)
    {
        var curValue = GetPower(power);
        var maxValue = GetMaxPower(power);

        if (!HasUnitFlag2(UnitFlags2.RegeneratePower))
            return;

        if (curValue >= maxValue)
            return;

        double addvalue;

        switch (power)
        {
            case PowerType.Focus:
            {
                // For hunter pets.
                addvalue = 24 * Configuration.GetDefaultValue("Rate:Focus", 1.0f);

                break;
            }
            case PowerType.Energy:
            {
                // For deathknight's ghoul.
                addvalue = 20;

                break;
            }
            case PowerType.Mana:
            {
                // Combat and any controlled creature
                if (IsInCombat || CharmerOrOwnerGUID.IsEmpty)
                {
                    var manaIncreaseRate = Configuration.GetDefaultValue("Rate:Mana", 1.0f);
                    addvalue = (27.0f / 5.0f + 17.0f) * manaIncreaseRate;
                }
                else
                    addvalue = (float)maxValue / 3;

                break;
            }
            default:
                return;
        }

        // Apply modifiers (if any).
        addvalue *= GetTotalAuraMultiplierByMiscValue(AuraType.ModPowerRegenPercent, (int)power);
        addvalue += GetTotalAuraModifierByMiscValue(AuraType.ModPowerRegen, (int)power) * (IsHunterPet ? SharedConst.PetFocusRegenInterval : SharedConst.CreatureRegenInterval) / (5 * Time.IN_MILLISECONDS);

        ModifyPower(power, (int)addvalue);
    }

    public void ReleaseSpellFocus(Spell focusSpell = null, bool withDelay = true)
    {
        if (_spellFocusInfo.Spell == null)
            return;

        // focused to something else
        if (focusSpell != null && focusSpell != _spellFocusInfo.Spell)
            return;

        if (_spellFocusInfo.Spell.SpellInfo.HasAttribute(SpellAttr5.AiDoesntFaceTarget))
            ClearUnitState(UnitState.Focusing);

        if (IsPet) // player pets do not use delay system
        {
            if (!HasUnitFlag2(UnitFlags2.CannotTurn))
                ReacquireSpellFocusTarget();
        }
        else // don't allow re-target right away to prevent visual bugs
            _spellFocusInfo.Delay = withDelay ? 1000 : 1u;

        _spellFocusInfo.Spell = null;
    }

    public void RemoveCorpse(bool setSpawnTime = true, bool destroyForNearbyPlayers = true)
    {
        if (DeathState != DeathState.Corpse)
            return;

        if (RespawnCompatibilityMode)
        {
            CorpseRemoveTime = GameTime.CurrentTime;
            SetDeathState(DeathState.Dead);
            RemoveAllAuras();
            //DestroyForNearbyPlayers(); // old UpdateObjectVisibility()
            Loot = null;
            var respawnDelay = RespawnDelay;
            var ai = AI;

            ai?.CorpseRemoved(respawnDelay);

            if (destroyForNearbyPlayers)
                UpdateObjectVisibilityOnDestroy();

            // Should get removed later, just keep "compatibility" with scripts
            if (setSpawnTime)
                RespawnTime = Math.Max(GameTime.CurrentTime + respawnDelay, RespawnTime);

            // if corpse was removed during falling, the falling will continue and override relocation to respawn position
            if (IsFalling)
                StopMoving();

            var respawn = RespawnPosition;

            // We were spawned on transport, calculate real position
            if (IsSpawnedOnTransport)
            {
                MovementInfo.Transport.Pos.Relocate(respawn);

                var transport = DirectTransport;

                transport?.CalculatePassengerPosition(respawn);
            }

            respawn.Z = Location.UpdateAllowedPositionZ(respawn.X, respawn.Y, respawn.Z);
            HomePosition = respawn;
            Location.Map.CreatureRelocation(this, respawn);
        }
        else
        {
            var ai = AI;

            ai?.CorpseRemoved(RespawnDelay);

            // In case this is called directly and normal respawn timer not set
            // Since this timer will be longer than the already present time it
            // will be ignored if the correct place added a respawn timer
            if (setSpawnTime)
            {
                var respawnDelay = RespawnDelay;
                RespawnTime = Math.Max(GameTime.CurrentTime + respawnDelay, RespawnTime);

                SaveRespawnTime();
            }

            var summon = ToTempSummon();

            if (summon != null)
                summon.UnSummon();
            else
                Location.AddObjectToRemoveList();
        }
    }

    public override void RemoveFromWorld()
    {
        if (Location.IsInWorld)
            try
            {
                ZoneScript?.OnCreatureRemove(this);

                if (Formation != null)
                    FormationManager.RemoveCreatureFromGroup(Formation, this);

                base.RemoveFromWorld();

                if (SpawnId != 0)
                    Location.Map.CreatureBySpawnIdStore.Remove(SpawnId, this);

                Location.Map.ObjectsStore.TryRemove(GUID, out _);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void RemoveLootMode(LootModes lootMode)
    {
        _lootMode &= ~lootMode;
    }

    public void ResetLootMode()
    {
        _lootMode = LootModes.Default;
    }

    public void ResetPickPocketRefillTimer()
    {
        _pickpocketLootRestore = 0;
    }

    public void ResetPlayerDamageReq()
    {
        PlayerDamageReq = (uint)(Health / 2);
    }

    public void Respawn(bool force = false)
    {
        if (force)
        {
            if (IsAlive)
                SetDeathState(DeathState.JustDied);
            else if (DeathState != DeathState.Corpse)
                SetDeathState(DeathState.Corpse);
        }

        if (RespawnCompatibilityMode)
        {
            UpdateObjectVisibilityOnDestroy();
            RemoveCorpse(false, false);

            if (DeathState == DeathState.Dead)
            {
                Log.Logger.Debug("Respawning creature {0} ({1})", GetName(), GUID.ToString());
                RespawnTime = 0;
                ResetPickPocketRefillTimer();
                Loot = null;

                if (OriginalEntry != Entry)
                    UpdateEntry(OriginalEntry);

                SelectLevel();

                SetDeathState(DeathState.JustRespawned);

                CreatureModel display = new(NativeDisplayId, NativeDisplayScale, 1.0f);

                if (GameObjectManager.GetCreatureModelRandomGender(ref display, Template) != null)
                {
                    SetDisplayId(display.CreatureDisplayId, display.DisplayScale);
                    SetNativeDisplayId(display.CreatureDisplayId, display.DisplayScale);
                }

                MotionMaster.InitializeDefault();

                //Re-initialize reactstate that could be altered by movementgenerators
                InitializeReactState();

                IUnitAI ai = AI;

                ai?.Reset();

                _triggerJustAppeared = true;

                var poolid = CreatureData?.PoolId ?? 0;

                if (poolid != 0)
                    PoolManager.UpdatePool<Creature>(Location.Map.PoolData, poolid, SpawnId);
            }

            UpdateObjectVisibility();
        }
        else
        {
            if (SpawnId != 0)
                Location.Map.Respawn(SpawnObjectType.Creature, SpawnId);
        }

        Log.Logger.Debug($"Respawning creature {GetName()} ({GUID})");
    }

    public void SaveRespawnTime(uint forceDelay = 0)
    {
        if (IsSummon || SpawnId == 0 || CreatureData is { DbData: false })
            return;

        if (RespawnCompatibilityMode)
        {
            RespawnInfo ri = new()
            {
                ObjectType = SpawnObjectType.Creature,
                SpawnId = SpawnId,
                RespawnTime = RespawnTime
            };

            Location.Map.SaveRespawnInfoDB(ri);

            return;
        }

        var thisRespawnTime = forceDelay != 0 ? GameTime.CurrentTime + forceDelay : RespawnTime;
        Location.Map.SaveRespawnTime(SpawnObjectType.Creature, SpawnId, Entry, thisRespawnTime, GridDefines.ComputeGridCoord(HomePosition.X, HomePosition.Y).GetId());
    }

    public void SaveToDB()
    {
        // this should only be used when the creature has already been loaded
        // preferably after adding to map, because mapid may not be valid otherwise
        var data = GameObjectManager.GetCreatureData(SpawnId);

        if (data == null)
        {
            Log.Logger.Error("Creature.SaveToDB failed, cannot get creature data!");

            return;
        }

        var mapId = Location.MapId;
        var transport = Transport;

        if (transport != null)
            if (transport.MapIdForSpawning >= 0)
                mapId = (uint)transport.MapIdForSpawning;

        SaveToDB(mapId, data.SpawnDifficulties);
    }

    public virtual void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties)
    {
        // update in loaded data
        if (SpawnId == 0)
            SpawnId = GameObjectManager.GenerateCreatureSpawnId();

        var data = GameObjectManager.NewOrExistCreatureData(SpawnId);

        var displayId = NativeDisplayId;
        var npcflag = ((ulong)UnitData.NpcFlags[1] << 32) | UnitData.NpcFlags[0];
        uint unitFlags = UnitData.Flags;
        uint unitFlags2 = UnitData.Flags2;
        uint unitFlags3 = UnitData.Flags3;
        var dynamicflags = (UnitDynFlags)(uint)ObjectData.DynamicFlags;

        // check if it's a custom model and if not, use 0 for displayId
        var cinfo = Template;

        if (cinfo != null)
        {
            foreach (var model in cinfo.Models)
                if (displayId != 0 && displayId == model.CreatureDisplayId)
                    displayId = 0;

            if (npcflag == (uint)cinfo.Npcflag)
                npcflag = 0;

            if (unitFlags == (uint)cinfo.UnitFlags)
                unitFlags = 0;

            if (unitFlags2 == cinfo.UnitFlags2)
                unitFlags2 = 0;

            if (unitFlags3 == cinfo.UnitFlags3)
                unitFlags3 = 0;

            if (dynamicflags == (UnitDynFlags)cinfo.DynamicFlags)
                dynamicflags = 0;
        }

        if (data.SpawnId == 0)
            data.SpawnId = SpawnId;

        data.Id = Entry;
        data.Displayid = displayId;
        data.EquipmentId = (sbyte)CurrentEquipmentId;

        if (Transport == null)
        {
            data.MapId = Location.MapId;
            data.SpawnPoint.Relocate(Location);
        }
        else
        {
            data.MapId = mapid;
            data.SpawnPoint.Relocate(MovementInfo.Transport.Pos.X, MovementInfo.Transport.Pos.Y, MovementInfo.Transport.Pos.Z, MovementInfo.Transport.Pos.Orientation);
        }

        data.Spawntimesecs = (int)RespawnDelay;
        // prevent add data integrity problems
        data.WanderDistance = GetDefaultMovementType() == MovementGeneratorType.Idle ? 0.0f : WanderDistance;
        data.Currentwaypoint = 0;
        data.Curhealth = (uint)Health;
        data.Curmana = (uint)GetPower(PowerType.Mana);

        // prevent add data integrity problems
        data.MovementType = (byte)(WanderDistance == 0 && GetDefaultMovementType() == MovementGeneratorType.Random
                                       ? MovementGeneratorType.Idle
                                       : GetDefaultMovementType());

        data.SpawnDifficulties = spawnDifficulties;
        data.Npcflag = npcflag;
        data.UnitFlags = unitFlags;
        data.UnitFlags2 = unitFlags2;
        data.UnitFlags3 = unitFlags3;
        data.Dynamicflags = (uint)dynamicflags;

        data.SpawnGroupData ??= GameObjectManager.GetDefaultSpawnGroup();

        data.PhaseId = Location.DBPhase > 0 ? (uint)Location.DBPhase : data.PhaseId;
        data.PhaseGroup = Location.DBPhase < 0 ? (uint)-Location.DBPhase : data.PhaseGroup;

        // update in DB
        SQLTransaction trans = new();

        var stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_CREATURE);
        stmt.AddValue(0, SpawnId);
        trans.Append(stmt);

        byte index = 0;

        stmt = WorldDatabase.GetPreparedStatement(WorldStatements.INS_CREATURE);
        stmt.AddValue(index++, SpawnId);
        stmt.AddValue(index++, Entry);
        stmt.AddValue(index++, mapid);
        stmt.AddValue(index++, data.SpawnDifficulties.Empty() ? "" : string.Join(',', data.SpawnDifficulties));
        stmt.AddValue(index++, data.PhaseId);
        stmt.AddValue(index++, data.PhaseGroup);
        stmt.AddValue(index++, displayId);
        stmt.AddValue(index++, CurrentEquipmentId);
        stmt.AddValue(index++, Location.X);
        stmt.AddValue(index++, Location.Y);
        stmt.AddValue(index++, Location.Z);
        stmt.AddValue(index++, Location.Orientation);
        stmt.AddValue(index++, RespawnDelay);
        stmt.AddValue(index++, WanderDistance);
        stmt.AddValue(index++, 0);
        stmt.AddValue(index++, Health);
        stmt.AddValue(index++, GetPower(PowerType.Mana));
        stmt.AddValue(index++, (byte)GetDefaultMovementType());
        stmt.AddValue(index++, npcflag);
        stmt.AddValue(index++, unitFlags);
        stmt.AddValue(index++, unitFlags2);
        stmt.AddValue(index++, unitFlags3);
        stmt.AddValue(index, (uint)dynamicflags);
        trans.Append(stmt);

        WorldDatabase.CommitTransaction(trans);
    }

    public void SearchFormation()
    {
        if (IsSummon)
            return;

        var lowguid = SpawnId;

        if (lowguid == 0)
            return;

        var formationInfo = FormationManager.GetFormationInfo(lowguid);

        if (formationInfo != null)
            FormationManager.AddCreatureToGroup(formationInfo.LeaderSpawnId, this);
    }

    public void SelectLevel()
    {
        var cInfo = Template;

        // level
        var minMaxLevels = cInfo.GetMinMaxLevel();
        var minlevel = Math.Min(minMaxLevels[0], minMaxLevels[1]);
        var maxlevel = Math.Max(minMaxLevels[0], minMaxLevels[1]);
        var level = minlevel == maxlevel ? minlevel : RandomHelper.IRand(minlevel, maxlevel);
        SetLevel((uint)level);

        ApplyLevelScaling();

        UpdateLevelDependantStats();
    }

    public Unit SelectNearestHostileUnitInAggroRange(bool useLOS = false, bool ignoreCivilians = false)
    {
        // Selects nearest hostile target within creature's aggro range. Used primarily by
        //  pets set to aggressive. Will not return neutral or friendly targets
        var uCheck = new NearestHostileUnitInAggroRangeCheck(this, useLOS, ignoreCivilians);
        var searcher = new UnitSearcher(this, uCheck, GridType.Grid);
        CellCalculator.VisitGrid(this, searcher, SharedConst.MaxAggroRadius);

        return searcher.GetTarget();
    }

    // select nearest hostile unit within the given distance (regardless of threat list).
    public Unit SelectNearestTarget(float dist = 0)
    {
        if (dist == 0.0f)
            dist = SharedConst.MaxVisibilityDistance;

        var uCheck = new NearestHostileUnitCheck(this, dist);
        var searcher = new UnitLastSearcher(this, uCheck, GridType.All);
        CellCalculator.VisitGrid(this, searcher, dist);

        return searcher.GetTarget();
    }

    // select nearest hostile unit within the given attack distance (i.e. distance is ignored if > than ATTACK_DISTANCE), regardless of threat list.
    public Unit SelectNearestTargetInAttackDistance(float dist = 0)
    {
        if (dist > SharedConst.MaxVisibilityDistance)
        {
            Log.Logger.Error("Creature ({0}) SelectNearestTargetInAttackDistance called with dist > MAX_VISIBILITY_DISTANCE. Distance set to ATTACK_DISTANCE.", GUID.ToString());
            dist = SharedConst.AttackDistance;
        }

        var uCheck = new NearestHostileUnitInAttackDistanceCheck(this, dist);
        var searcher = new UnitLastSearcher(this, uCheck, GridType.All);

        CellCalculator.VisitGrid(this, searcher, Math.Max(dist, SharedConst.AttackDistance));

        return searcher.GetTarget();
    }

    public Unit SelectVictim()
    {
        Unit target;

        if (CanHaveThreatList)
            target = GetThreatManager().CurrentVictim;
        else if (!HasReactState(ReactStates.Passive))
        {
            // We're a player pet, probably
            target = GetAttackerForHelper();

            if (target == null && IsSummon)
            {
                var owner = ToTempSummon().OwnerUnit;

                if (owner != null)
                {
                    if (owner.IsInCombat)
                        target = owner.GetAttackerForHelper();

                    if (target == null)
                        foreach (var itr in owner.Controlled)
                            if (itr.IsInCombat)
                            {
                                target = itr.GetAttackerForHelper();

                                if (target != null)
                                    break;
                            }
                }
            }
        }
        else
            return null;

        if (target != null && _IsTargetAcceptable(target) && CanCreatureAttack(target))
        {
            if (!HasSpellFocus())
                SetInFront(target);

            return target;
        }

        // @todo a vehicle may eat some mob, so mob should not evade
        if (Vehicle != null)
            return null;

        var iAuras = GetAuraEffectsByType(AuraType.ModInvisibility);

        if (!iAuras.Empty())
        {
            foreach (var itr in iAuras)
                if (itr.Base.IsPermanent)
                {
                    AI.EnterEvadeMode();

                    break;
                }

            return null;
        }

        // enter in evade mode in other case
        AI.EnterEvadeMode(EvadeReason.NoHostiles);

        return null;
    }

    public void SendAIReaction(AiReaction reactionType)
    {
        AIReaction packet = new()
        {
            UnitGUID = GUID,
            Reaction = reactionType
        };

        SendMessageToSet(packet, true);
    }

    // Send a message to LocalDefense channel for players opposition team in the zone
    public void SendZoneUnderAttackMessage(Player attacker)
    {
        var enemyTeam = attacker.Team;

        ZoneUnderAttack packet = new()
        {
            AreaID = (int)Location.Area
        };

        WorldManager.SendGlobalMessage(packet, null, enemyTeam == TeamFaction.Alliance ? TeamFaction.Horde : TeamFaction.Alliance);
    }

    public override void SetCanDualWield(bool value)
    {
        base.SetCanDualWield(value);
        UpdateDamagePhysical(WeaponAttackType.OffAttack);
    }

    public void SetCannotReachTarget(bool cannotReach)
    {
        if (cannotReach == CannotReachTarget)
            return;

        CannotReachTarget = cannotReach;
        _cannotReachTimer = 0;

        if (cannotReach)
            Log.Logger.Debug($"Creature::SetCannotReachTarget() called with true. Details: {GetDebugInfo()}");
    }

    public void SetCorpseDelay(uint delay, bool ignoreCorpseDecayRatio = false)
    {
        CorpseDelay = delay;

        if (ignoreCorpseDecayRatio)
            _ignoreCorpseDecayRatio = true;
    }

    public override void SetDeathState(DeathState s)
    {
        base.SetDeathState(s);

        if (s == DeathState.JustDied)
        {
            CorpseRemoveTime = GameTime.CurrentTime + CorpseDelay;
            var respawnDelay = RespawnDelay;
            var scalingMode = Configuration.GetDefaultValue("Respawn:DynamicMode", 0u);

            if (scalingMode != 0)
                Location.Map.ApplyDynamicModeRespawnScaling(this, SpawnId, ref respawnDelay, scalingMode);

            // @todo remove the boss respawn time hack in a dynspawn follow-up once we have creature groups in instances
            if (RespawnCompatibilityMode)
            {
                if (IsDungeonBoss && RespawnDelay == 0)
                    RespawnTime = long.MaxValue; // never respawn in this instance
                else
                    RespawnTime = GameTime.CurrentTime + respawnDelay + CorpseDelay;
            }
            else
            {
                if (IsDungeonBoss && RespawnDelay == 0)
                    RespawnTime = long.MaxValue; // never respawn in this instance
                else
                    RespawnTime = GameTime.CurrentTime + respawnDelay;
            }

            SaveRespawnTime();

            ReleaseSpellFocus(null, false);   // remove spellcast focus
            DoNotReacquireSpellFocusTarget(); // cancel delayed re-target
            SetTarget(ObjectGuid.Empty);      // drop target - dead mobs shouldn't ever target things

            ReplaceAllNpcFlags(NPCFlags.None);
            ReplaceAllNpcFlags2(NPCFlags2.None);

            MountDisplayId = 0; // if creature is mounted on a virtual mount, remove it at death

            SetActive(false);
            SetNoSearchAssistance(false);

            //Dismiss group if is leader
            if (Formation != null && Formation.Leader == this)
                Formation.FormationReset(true);

            var needsFalling = (IsFlying || IsHovering) && !Location.IsUnderWater;
            SetHover(false, false);
            SetDisableGravity(false, false);

            if (needsFalling)
                MotionMaster.MoveFall();

            base.SetDeathState(DeathState.Corpse);
        }
        else if (s == DeathState.JustRespawned)
        {
            if (IsPet)
                SetFullHealth();
            else
                SetSpawnHealth();

            SetTappedBy(null);
            ResetPlayerDamageReq();

            SetCannotReachTarget(false);
            UpdateMovementFlags();

            ClearUnitState(UnitState.AllErasable);

            if (!IsPet)
            {
                var creatureData = CreatureData;
                var cInfo = Template;

                GameObjectManager.ChooseCreatureFlags(cInfo, out var npcFlags, out var unitFlags, out var unitFlags2, out var unitFlags3, out var dynamicFlags, creatureData);

                if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Worldevent))
                    npcFlags |= GameEventManager.GetNPCFlag(this);

                ReplaceAllNpcFlags((NPCFlags)(npcFlags & 0xFFFFFFFF));
                ReplaceAllNpcFlags2((NPCFlags2)(npcFlags >> 32));

                ReplaceAllUnitFlags((UnitFlags)unitFlags);
                ReplaceAllUnitFlags2((UnitFlags2)unitFlags2);
                ReplaceAllUnitFlags3((UnitFlags3)unitFlags3);
                ReplaceAllDynamicFlags((UnitDynFlags)dynamicFlags);

                RemoveUnitFlag(UnitFlags.InCombat);

                SetMeleeDamageSchool((SpellSchools)cInfo.DmgSchool);
            }

            InitializeMovementAI();
            base.SetDeathState(DeathState.Alive);
            LoadCreaturesAddon();
        }
    }

    public void SetDefaultMovementType(MovementGeneratorType mgt)
    {
        DefaultMovementType = mgt;
    }

    public void SetDisplayFromModel(int modelIdx)
    {
        var model = Template.GetModelByIdx(modelIdx);

        if (model != null)
            SetDisplayId(model.CreatureDisplayId, model.DisplayScale);
    }

    public override void SetDisplayId(uint modelId, float displayScale = 1f)
    {
        base.SetDisplayId(modelId, displayScale);

        var minfo = GameObjectManager.GetCreatureModelInfo(modelId);

        if (minfo != null)
        {
            BoundingRadius = (IsPet ? 1.0f : minfo.BoundingRadius) * ObjectScale;
            SetCombatReach((IsPet ? SharedConst.DefaultPlayerCombatReach : minfo.CombatReach) * ObjectScale);
        }
    }

    public void SetHomePosition(float x, float y, float z, float o)
    {
        _homePosition.Relocate(x, y, z, o);
    }

    public override void SetImmuneToAll(bool apply)
    {
        SetImmuneToAll(apply, HasReactState(ReactStates.Passive));
    }

    public override void SetImmuneToNPC(bool apply)
    {
        SetImmuneToNPC(apply, HasReactState(ReactStates.Passive));
    }

    public override void SetImmuneToPc(bool apply)
    {
        SetImmuneToPc(apply, HasReactState(ReactStates.Passive));
    }

    public void SetLootMode(LootModes lootMode)
    {
        _lootMode = lootMode;
    }

    public void SetMeleeDamageSchool(SpellSchools school)
    {
        _meleeDamageSchoolMask = (SpellSchoolMask)(1 << (int)school);
    }

    public void SetNoCallAssistance(bool val)
    {
        _alreadyCallAssistance = val;
    }

    public void SetNoSearchAssistance(bool val)
    {
        HasSearchedAssistance = val;
    }

    public void SetRegenerateHealth(bool value)
    {
        StaticFlags.ModifyFlag(CreatureStaticFlags5.NO_HEALTH_REGEN, !value);
    }

    public void SetRespawnTime(uint respawn)
    {
        RespawnTime = respawn != 0 ? GameTime.CurrentTime + respawn : 0;
    }

    public void SetSpawnHealth()
    {
        if (StaticFlags.HasFlag(CreatureStaticFlags5.NO_HEALTH_REGEN))
            return;

        long curhealth;

        if (CreatureData != null && !_regenerateHealth)
        {
            curhealth = CreatureData.Curhealth;

            if (curhealth != 0)
            {
                curhealth = (long)(curhealth * GetHealthMod(Template.Rank));

                if (curhealth < 1)
                    curhealth = 1;
            }

            SetPower(PowerType.Mana, (int)CreatureData.Curmana);
        }
        else
        {
            curhealth = MaxHealth;
            SetFullPower(PowerType.Mana);
        }

        SetHealth(DeathState is DeathState.Alive or DeathState.JustRespawned ? curhealth : 0);
    }

    public void SetSpellFocus(Spell focusSpell, WorldObject target)
    {
        // Pointer validation and checking for a already existing focus
        if (_spellFocusInfo.Spell != null || focusSpell == null)
            return;

        // Prevent dead / feign death creatures from setting a focus target
        if (!IsAlive || HasUnitFlag2(UnitFlags2.FeignDeath) || HasAuraType(AuraType.FeignDeath))
            return;

        // Don't allow stunned creatures to set a focus target
        if (HasUnitFlag(UnitFlags.Stunned))
            return;

        // some spells shouldn't track targets
        if (focusSpell.IsFocusDisabled)
            return;

        var spellInfo = focusSpell.SpellInfo;

        // don't use spell focus for vehicle spells
        if (spellInfo.HasAura(AuraType.ControlVehicle))
            return;

        // instant non-channeled casts and non-target spells don't need facing updates
        if (target == null && focusSpell.CastTime == 0 && !spellInfo.IsChanneled)
            return;

        // store pre-cast values for target and orientation (used to later restore)
        if (_spellFocusInfo.Delay == 0)
        {
            // only overwrite these fields if we aren't transitioning from one spell focus to another
            _spellFocusInfo.Target = Target;
            _spellFocusInfo.Orientation = Location.Orientation;
        }
        else // don't automatically reacquire target for the previous spellcast
            _spellFocusInfo.Delay = 0;

        _spellFocusInfo.Spell = focusSpell;

        var noTurnDuringCast = spellInfo.HasAttribute(SpellAttr5.AiDoesntFaceTarget);
        var turnDisabled = HasUnitFlag2(UnitFlags2.CannotTurn);
        // set target, then force send update packet to players if it changed to provide appropriate facing
        var newTarget = target != null && !noTurnDuringCast && !turnDisabled ? target.GUID : ObjectGuid.Empty;

        if (Target != newTarget)
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Target), newTarget);

        // If we are not allowed to turn during cast but have a focus target, face the target
        if (!turnDisabled && noTurnDuringCast && target != null)
            SetFacingToObject(target, false);

        if (!noTurnDuringCast)
            AddUnitState(UnitState.Focusing);
    }

    public void SetTapList(HashSet<ObjectGuid> tapList)
    {
        TapList = tapList;
    }

    public void SetTappedBy(Unit unit, bool withGroup = true)
    {
        // set the player whose group should receive the right
        // to loot the creature after it dies
        // should be set to NULL after the loot disappears

        if (unit == null)
        {
            TapList.Clear();
            RemoveDynamicFlag(UnitDynFlags.Lootable | UnitDynFlags.Tapped);

            return;
        }

        if (TapList.Count >= SharedConst.CreatureTappersSoftCap)
            return;

        if (!unit.IsTypeId(TypeId.Player) && !unit.IsVehicle)
            return;

        var player = unit.CharmerOrOwnerPlayerOrPlayerItself;

        if (player == null) // normal creature, no player involved
            return;

        TapList.Add(player.GUID);

        if (withGroup)
        {
            var group = player.Group;

            if (group != null)
                for (var itr = group.FirstMember; itr != null; itr = itr.Next())
                    if (Location.Map.IsRaid || group.SameSubGroup(player, itr.Source))
                        TapList.Add(itr.Source.GUID);
        }

        if (TapList.Count >= SharedConst.CreatureTappersSoftCap)
            SetDynamicFlag(UnitDynFlags.Tapped);
    }

    public override void SetTarget(ObjectGuid guid)
    {
        if (HasSpellFocus())
            _spellFocusInfo.Target = guid;
        else
            SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Target), guid);
    }

    public void SetTextRepeatId(byte textGroup, byte id)
    {
        if (!_textRepeat.ContainsKey(textGroup))
        {
            _textRepeat.Add(textGroup, id);

            return;
        }

        var repeats = _textRepeat[textGroup];

        if (!repeats.Contains(id))
            repeats.Add(id);
        else
            Log.Logger.Error("CreatureTextMgr: TextGroup {0} for ({1}) {2}, id {3} already added", textGroup, GetName(), GUID.ToString(), id);
    }

    public void SetTransportHomePosition(float x, float y, float z, float o)
    {
        _transportHomePosition.Relocate(x, y, z, o);
    }

    public void SetUnkillable(bool value)
    {
        StaticFlags.ModifyFlag(CreatureStaticFlags.UNKILLABLE, value);
    }

    public void SignalFormationMovement()
    {
        if (Formation == null)
            return;

        if (!Formation.IsLeader(this))
            return;

        Formation.LeaderStartedMoving();
    }

    public void StartPickPocketRefillTimer()
    {
        _pickpocketLootRestore = GameTime.CurrentTime + Configuration.GetDefaultValue("Creature:PickPocketRefillDelay", 10 * Time.MINUTE);
    }

    public override void Update(uint diff)
    {
        if (IsAIEnabled && _triggerJustAppeared && DeathState != DeathState.Dead)
        {
            if (RespawnCompatibilityMode && VehicleKit != null)
                VehicleKit.Reset();

            _triggerJustAppeared = false;
            AI.JustAppeared();
        }

        UpdateMovementFlags();

        switch (DeathState)
        {
            case DeathState.JustRespawned:
            case DeathState.JustDied:
                Log.Logger.Error($"Creature ({GUID}) in wrong state: {DeathState}");

                break;

            case DeathState.Dead:
            {
                if (!RespawnCompatibilityMode)
                {
                    Log.Logger.Error($"Creature (GUID: {GUID.Counter} Entry: {Entry}) in wrong state: DEAD (3)");

                    break;
                }

                var now = GameTime.CurrentTime;

                if (RespawnTime <= now)
                {
                    // Delay respawn if spawn group is not active
                    if (CreatureData != null && !Location.Map.IsSpawnGroupActive(CreatureData.SpawnGroupData.GroupId))
                    {
                        RespawnTime = now + RandomHelper.LRand(4, 7);

                        break; // Will be rechecked on next Update call after delay expires
                    }

                    var dbtableHighGuid = ObjectGuid.Create(HighGuid.Creature, Location.MapId, Entry, SpawnId);
                    var linkedRespawnTime = Location.Map.GetLinkedRespawnTime(dbtableHighGuid);

                    if (linkedRespawnTime == 0) // Can respawn
                        Respawn();
                    else // the master is dead
                    {
                        var targetGuid = GameObjectManager.GetLinkedRespawnGuid(dbtableHighGuid);

                        if (targetGuid == dbtableHighGuid) // if linking self, never respawn (check delayed to next day)
                            SetRespawnTime(Time.WEEK);
                        else
                        {
                            // else copy time from master and add a little
                            var baseRespawnTime = Math.Max(linkedRespawnTime, now);
                            var offset = RandomHelper.LRand(5, Time.MINUTE);

                            // linked guid can be a boss, uses std::numeric_limits<time_t>::max to never respawn in that instance
                            // we shall inherit it instead of adding and causing an overflow
                            if (baseRespawnTime <= long.MaxValue - offset)
                                RespawnTime = baseRespawnTime + offset;
                            else
                                RespawnTime = long.MaxValue;
                        }

                        SaveRespawnTime(); // also save to DB immediately
                    }
                }

                break;
            }
            case DeathState.Corpse:
                base.Update(diff);

                if (DeathState != DeathState.Corpse)
                    break;

                if (IsEngaged)
                    AIUpdateTick(diff);

                Loot?.Update();

                foreach (var (_, loot) in PersonalLoot)
                    loot?.Update();

                if (CorpseRemoveTime <= GameTime.CurrentTime)
                {
                    RemoveCorpse(false);
                    Log.Logger.Debug("Removing corpse... {0} ", Entry);
                }

                break;

            case DeathState.Alive:
                base.Update(diff);

                if (!IsAlive)
                    break;

                GetThreatManager().Update(diff);

                if (IsFeared)
                    return;

                if (_spellFocusInfo.Delay != 0)
                {
                    if (_spellFocusInfo.Delay <= diff)
                        ReacquireSpellFocusTarget();
                    else
                        _spellFocusInfo.Delay -= diff;
                }

                // periodic check to see if the creature has passed an evade boundary
                if (IsAIEnabled && !IsInEvadeMode && IsEngaged)
                {
                    if (diff >= _boundaryCheckTime)
                    {
                        AI.CheckInRoom();
                        _boundaryCheckTime = 2500;
                    }
                    else
                        _boundaryCheckTime -= diff;
                }

                // if periodic combat pulse is enabled and we are both in combat and in a dungeon, do this now
                if (_combatPulseDelay > 0 && IsEngaged && Location.Map.IsDungeon)
                {
                    if (diff > _combatPulseTime)
                        _combatPulseTime = 0;
                    else
                        _combatPulseTime -= diff;

                    if (_combatPulseTime == 0)
                    {
                        var players = Location.Map.Players;

                        foreach (var player in players)
                        {
                            if (player.IsGameMaster)
                                continue;

                            if (player.IsAlive && WorldObjectCombat.IsHostileTo(player))
                                EngageWithTarget(player);
                        }

                        _combatPulseTime = _combatPulseDelay * Time.IN_MILLISECONDS;
                    }
                }

                AIUpdateTick(diff);

                if (!IsAlive)
                    break;

                if (RegenTimer > 0)
                {
                    if (diff >= RegenTimer)
                        RegenTimer = 0;
                    else
                        RegenTimer -= diff;
                }

                if (RegenTimer == 0)
                {
                    if (!IsInEvadeMode)
                    {
                        // regenerate health if not in combat or if polymorphed)
                        if (!IsEngaged || IsPolymorphed)
                            RegenerateHealth();
                        else if (CannotReachTarget)
                        {
                            // regenerate health if cannot reach the target and the setting is set to do so.
                            // this allows to disable the health regen of raid bosses if pathfinding has issues for whatever reason
                            if (Configuration.GetDefaultValue("Creature:RegenHPCannotReachTargetInRaid", true) || !Location.Map.IsRaid)
                            {
                                RegenerateHealth();
                                Log.Logger.Debug($"RegenerateHealth() enabled because Creature cannot reach the target. Detail: {GetDebugInfo()}");
                            }
                            else
                                Log.Logger.Debug($"RegenerateHealth() disabled even if the Creature cannot reach the target. Detail: {GetDebugInfo()}");
                        }
                    }

                    Regenerate(DisplayPowerType == PowerType.Energy ? PowerType.Energy : PowerType.Mana);

                    RegenTimer = SharedConst.CreatureRegenInterval;
                }

                if (CannotReachTarget && !IsInEvadeMode && !Location.Map.IsRaid)
                {
                    _cannotReachTimer += diff;

                    if (_cannotReachTimer >= SharedConst.CreatureNoPathEvadeTime)
                    {
                        var ai = AI;

                        ai?.EnterEvadeMode(EvadeReason.NoPath);
                    }
                }

                break;
        }
    }

    public void UpdateCurrentWaypointInfo(uint nodeId, uint pathId)
    {
        CurrentWaypointInfo = (nodeId, pathId);
    }

    public bool UpdateEntry(uint entry, CreatureData data = null, bool updateLevel = true)
    {
        if (!InitEntry(entry, data))
            return false;

        var cInfo = Template;

        _regenerateHealth = cInfo.RegenHealth;

        // creatures always have melee weapon ready if any unless specified otherwise
        if (CreatureAddon == null)
            Sheath = SheathState.Melee;

        Faction = cInfo.Faction;

        GameObjectManager.ChooseCreatureFlags(cInfo, out var npcFlags, out var unitFlags, out var unitFlags2, out var unitFlags3, out var dynamicFlags, data);

        if (cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Worldevent))
            npcFlags |= GameEventManager.GetNPCFlag(this);

        ReplaceAllNpcFlags((NPCFlags)(npcFlags & 0xFFFFFFFF));
        ReplaceAllNpcFlags2((NPCFlags2)(npcFlags >> 32));

        // if unit is in combat, keep this Id
        unitFlags &= ~(uint)UnitFlags.InCombat;

        if (IsInCombat)
            unitFlags |= (uint)UnitFlags.InCombat;

        ReplaceAllUnitFlags((UnitFlags)unitFlags);
        ReplaceAllUnitFlags2((UnitFlags2)unitFlags2);
        ReplaceAllUnitFlags3((UnitFlags3)unitFlags3);

        ReplaceAllDynamicFlags((UnitDynFlags)dynamicFlags);

        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.StateAnimID), DB2Manager.GetEmptyAnimStateID());

        SetCanDualWield(cInfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.UseOffhandAttack));

        SetBaseAttackTime(WeaponAttackType.BaseAttack, cInfo.BaseAttackTime);
        SetBaseAttackTime(WeaponAttackType.OffAttack, cInfo.BaseAttackTime);
        SetBaseAttackTime(WeaponAttackType.RangedAttack, cInfo.RangeAttackTime);

        if (updateLevel)
            SelectLevel();
        else if (!IsGuardian)
        {
            var previousHealth = Health;
            UpdateLevelDependantStats(); // We still re-initialize level dependant stats on entry update

            if (previousHealth > 0)
                SetHealth(previousHealth);
        }

        // Do not update guardian stats here - they are handled in Guardian::InitStatsForLevel()
        if (!IsGuardian)
        {
            SetMeleeDamageSchool((SpellSchools)cInfo.DmgSchool);
            SetStatFlatModifier(UnitMods.ResistanceHoly, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Holy]);
            SetStatFlatModifier(UnitMods.ResistanceFire, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Fire]);
            SetStatFlatModifier(UnitMods.ResistanceNature, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Nature]);
            SetStatFlatModifier(UnitMods.ResistanceFrost, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Frost]);
            SetStatFlatModifier(UnitMods.ResistanceShadow, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Shadow]);
            SetStatFlatModifier(UnitMods.ResistanceArcane, UnitModifierFlatType.Base, cInfo.Resistance[(int)SpellSchools.Arcane]);

            SetCanModifyStats(true);
            UpdateAllStats();
        }

        // checked and error show at loading templates
        if (CliDB.FactionTemplateStorage.TryGetValue(cInfo.Faction, out var factionTemplate))
            SetPvP(factionTemplate.Flags.HasAnyFlag((ushort)FactionTemplateFlags.PVP));

        // updates spell bars for vehicles and set player's faction - should be called here, to overwrite faction that is set from the new template
        if (IsVehicle)
        {
            var owner = CharmerOrOwnerPlayerOrPlayerItself;

            if (owner != null) // this check comes in case we don't have a player
            {
                Faction = owner.Faction; // vehicles should have same as owner faction
                owner.VehicleSpellInitialize();
            }
        }

        // trigger creature is always not selectable and can not be attacked
        if (IsTrigger)
            SetUnitFlag(UnitFlags.Uninteractible);

        InitializeReactState();

        if (Convert.ToBoolean(cInfo.FlagsExtra & CreatureFlagsExtra.NoTaunt))
        {
            ApplySpellImmune(0, SpellImmunity.State, AuraType.ModTaunt, true);
            ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.AttackMe, true);
        }

        SetIsCombatDisallowed(cInfo.FlagsExtra.HasFlag(CreatureFlagsExtra.CannotEnterCombat));

        LoadTemplateRoot();
        InitializeMovementFlags();

        LoadCreaturesAddon();

        LoadTemplateImmunities();
        GetThreatManager().EvaluateSuppressed();

        //We must update last scriptId or it looks like we reloaded a script, breaking some things such as gossip temporarily
        LastUsedScriptID = GetScriptId();

        StringIds[0] = cInfo.StringId;

        return true;
    }

    public void UpdateLevelDependantStats()
    {
        var cInfo = Template;
        var rank = IsPet ? 0 : cInfo.Rank;
        var level = Level;
        var stats = GameObjectManager.GetCreatureBaseStats(level, cInfo.UnitClass);

        // health
        var healthmod = GetHealthMod(rank);

        var basehp = (uint)GetMaxHealthByLevel(level);
        var health = (uint)(basehp * healthmod);

        SetCreateHealth(health);
        SetMaxHealth(health);
        SetHealth(health);
        ResetPlayerDamageReq();

        // mana
        var mana = stats.GenerateMana(cInfo);
        SetCreateMana(mana);

        switch (Class)
        {
            case PlayerClass.Paladin:
            case PlayerClass.Mage:
                SetMaxPower(PowerType.Mana, (int)mana);
                SetPower(PowerType.Mana, (int)mana);

                break;
        }

        SetStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Base, health);

        //Damage
        var basedamage = GetBaseDamageForLevel(level);
        var weaponBaseMaxDamage = basedamage * 1.5f;

        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, basedamage);
        SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

        SetBaseWeaponDamage(WeaponAttackType.OffAttack, WeaponDamageRange.MinDamage, basedamage);
        SetBaseWeaponDamage(WeaponAttackType.OffAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

        SetBaseWeaponDamage(WeaponAttackType.RangedAttack, WeaponDamageRange.MinDamage, basedamage);
        SetBaseWeaponDamage(WeaponAttackType.RangedAttack, WeaponDamageRange.MaxDamage, weaponBaseMaxDamage);

        SetStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Base, stats.AttackPower);
        SetStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Base, stats.RangedAttackPower);

        var armor = GetBaseArmorForLevel(level);
        SetStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, armor);
    }

    public void UpdateMovementFlags()
    {
        // Do not update movement flags if creature is controlled by a player (charm/vehicle)
        if (PlayerMovingMe != null)
            return;

        // Creatures with CREATURE_FLAG_EXTRA_NO_MOVE_FLAGS_UPDATE should control MovementFlags in your own scripts
        if (Template.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.NoMoveFlagsUpdate))
            return;

        // Set the movement flags if the creature is in that mode. (Only fly if actually in air, only swim if in water, etc)
        var ground = Location.FloorZ;

        var canHover = CanHover;
        var isInAir = MathFunctions.fuzzyGt(Location.Z, ground + (canHover ? UnitData.HoverHeight : 0.0f) + MapConst.GroundHeightTolerance) || MathFunctions.fuzzyLt(Location.Z, ground - MapConst.GroundHeightTolerance); // Can be underground too, prevent the falling

        if (MovementTemplate.IsFlightAllowed && (isInAir || !MovementTemplate.IsGroundAllowed) && !IsFalling)
        {
            if (MovementTemplate.Flight == CreatureFlightMovementType.CanFly)
                SetCanFly(true);
            else
                SetDisableGravity(true);

            if (!HasAuraType(AuraType.Hover) && MovementTemplate.Ground != CreatureGroundMovementType.Hover)
                SetHover(false);
        }
        else
        {
            SetCanFly(false);
            SetDisableGravity(false);

            if (IsAlive && (CanHover || HasAuraType(AuraType.Hover)))
                SetHover(true);
        }

        if (!isInAir)
            SetFall(false);

        SetSwim(CanSwim && Location.IsInWater);
    }

    public uint UpdateVendorItemCurrentCount(VendorItem vItem, uint usedCount)
    {
        if (vItem.Maxcount == 0)
            return 0;

        if (!_vendorItemCounts.TryGetValue(vItem.Item, out var vCount))
        {
            var newCount = vItem.Maxcount > usedCount ? vItem.Maxcount - usedCount : 0;
            _vendorItemCounts[vItem.Item] = new VendorItemCount(vItem.Item, newCount);

            return newCount;
        }

        var ptime = GameTime.CurrentTime;

        if (vCount.LastIncrementTime + vItem.Incrtime <= ptime)
        {
            var pProto = GameObjectManager.ItemTemplateCache.GetItemTemplate(vItem.Item);

            var diff = (uint)((ptime - vCount.LastIncrementTime) / vItem.Incrtime);

            if (vCount.Count + diff * pProto.BuyCount < vItem.Maxcount)
                vCount.Count += diff * pProto.BuyCount;
            else
                vCount.Count = vItem.Maxcount;
        }

        vCount.Count = vCount.Count > usedCount ? vCount.Count - usedCount : 0;
        vCount.LastIncrementTime = ptime;

        return vCount.Count;
    }

    private bool CreateFromProto(ulong guidlow, uint entry, CreatureData data = null, uint vehId = 0)
    {
        Location.SetZoneScript();

        if (ZoneScript != null && data != null)
        {
            entry = ZoneScript.GetCreatureEntry(guidlow, data);

            if (entry == 0)
                return false;
        }

        var cinfo = GameObjectManager.GetCreatureTemplate(entry);

        if (cinfo == null)
        {
            Log.Logger.Error("Creature.CreateFromProto: creature template (guidlow: {0}, entry: {1}) does not exist.", guidlow, entry);

            return false;
        }

        OriginalEntry = entry;

        if (vehId != 0 || cinfo.VehicleId != 0)
            Create(ObjectGuid.Create(HighGuid.Vehicle, Location.MapId, entry, guidlow));
        else
            Create(ObjectGuid.Create(HighGuid.Creature, Location.MapId, entry, guidlow));

        if (!UpdateEntry(entry, data))
            return false;

        if (vehId == 0)
        {
            if (Template.VehicleId != 0)
            {
                vehId = Template.VehicleId;
                entry = Template.Entry;
            }
            else
                vehId = cinfo.VehicleId;
        }

        if (vehId != 0)
            if (CreateVehicleKit(vehId, entry, true))
                UpdateDisplayPower();

        return true;
    }

    private float GetBaseArmorForLevel(uint level)
    {
        var cInfo = Template;
        var scaling = cInfo.GetLevelScaling(Location.Map.DifficultyID);
        var baseArmor = DB2Manager.EvaluateExpectedStat(ExpectedStatType.CreatureArmor, level, cInfo.GetHealthScalingExpansion(), scaling.ContentTuningId, (PlayerClass)cInfo.UnitClass);

        return baseArmor * cInfo.ModArmor;
    }

    private ulong GetMaxHealthByLevel(uint level)
    {
        var cInfo = Template;
        var scaling = cInfo.GetLevelScaling(Location.Map.DifficultyID);
        var baseHealth = DB2Manager.EvaluateExpectedStat(ExpectedStatType.CreatureHealth, level, cInfo.GetHealthScalingExpansion(), scaling.ContentTuningId, (PlayerClass)cInfo.UnitClass);

        return (ulong)(baseHealth * cInfo.ModHealth * cInfo.ModHealthExtra);
    }

    private void InitializeMovementAI()
    {
        if (Formation != null)
        {
            if (Formation.Leader == this)
                Formation.FormationReset(false);
            else if (Formation.IsFormed)
            {
                MotionMaster.MoveIdle(); //wait the order of leader

                return;
            }
        }

        MotionMaster.Initialize();
    }

    private void InitializeMovementFlags()
    {
        // It does the same, for now
        UpdateMovementFlags();
    }

    private void LoadTemplateRoot()
    {
        if (MovementTemplate.Rooted)
            SetControlled(true, UnitState.Root);
    }

    private void ReacquireSpellFocusTarget()
    {
        if (!HasSpellFocus())
        {
            Log.Logger.Error($"Creature::ReacquireSpellFocusTarget() being called with HasSpellFocus() returning false. {GetDebugInfo()}");

            return;
        }

        SetUpdateFieldValue(Values.ModifyValue(UnitData).ModifyValue(UnitData.Target), _spellFocusInfo.Target);

        if (!HasUnitFlag2(UnitFlags2.CannotTurn))
        {
            if (!_spellFocusInfo.Target.IsEmpty)
            {
                var objTarget = ObjectAccessor.GetWorldObject(this, _spellFocusInfo.Target);

                if (objTarget != null)
                    SetFacingToObject(objTarget, false);
            }
            else
                SetFacingTo(_spellFocusInfo.Orientation, false);
        }

        _spellFocusInfo.Delay = 0;
    }

    private void RegenerateHealth()
    {
        if (!CanRegenerateHealth)
            return;

        var curValue = Health;
        var maxValue = MaxHealth;

        if (curValue >= maxValue)
            return;

        double addvalue;

        // Not only pet, but any controlled creature (and not polymorphed)
        if (!CharmerOrOwnerGUID.IsEmpty && !IsPolymorphed)
        {
            var healthIncreaseRate = Configuration.GetDefaultValue("Rate:Health", 1.0f);
            addvalue = 0.015f * MaxHealth * healthIncreaseRate;
        }
        else
            addvalue = (float)maxValue / 3;

        // Apply modifiers (if any).
        addvalue *= GetTotalAuraMultiplier(AuraType.ModHealthRegenPercent);
        addvalue += GetTotalAuraModifier(AuraType.ModRegen) * SharedConst.CreatureRegenInterval / (5 * Time.IN_MILLISECONDS);

        ModifyHealth(addvalue);
    }

    private void SelectWildBattlePetLevel()
    {
        if (!IsWildBattlePet)
            return;

        byte wildBattlePetLevel = 1;

        var areaTable = CliDB.AreaTableStorage.LookupByKey(Location.Zone);

        if (areaTable is { WildBattlePetLevelMin: > 0 })
            wildBattlePetLevel = (byte)RandomHelper.URand(areaTable.WildBattlePetLevelMin, areaTable.WildBattlePetLevelMax);

        WildBattlePetLevel = wildBattlePetLevel;
    }
}