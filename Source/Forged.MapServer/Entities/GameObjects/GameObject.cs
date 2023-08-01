﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.AI;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Chrono;
using Forged.MapServer.Collision.Management;
using Forged.MapServer.Collision.Models;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Artifact;
using Forged.MapServer.Networking.Packets.BattleGround;
using Forged.MapServer.Networking.Packets.GameObject;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Pools;
using Forged.MapServer.Questing;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Framework.GameMath;
using Framework.Util;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Entities.GameObjects;

public class GameObject : WorldObject
{
    protected GameObjectTemplate GoInfoProtected;
    protected GameObjectTemplateAddon GoTemplateAddonProtected;
    protected GameObjectValue GoValueProtected; // TODO: replace with m_goTypeImpl
    private readonly Dictionary<uint, ObjectGuid> _chairListSlots = new();
    private readonly List<ObjectGuid> _skillupList = new();
    private readonly List<ObjectGuid> _uniqueUsers = new();
    private ushort _animKitId;
    private long _cooldownTime;
    private CreatureDataCache _creatureDataCache;
    private uint _despawnDelay;
    private TimeSpan _despawnRespawnTime;
    private uint? _gossipMenuId;
    private GameObjectTypeBase _goTypeImpl;
    private ObjectGuid _linkedTrap;
    private Quaternion _localRotation;

    // override respawn time after delayed despawn
    private ObjectGuid _lootStateUnitGuid;

    private Dictionary<ObjectGuid, PerPlayerState> _perPlayerState;
    private Dictionary<ObjectGuid, Loot> _personalLoot = new();
    private GameObjectState _prevGoState;

    // GUID of the unit passed with SetLootState(LootState, Unit*)
    private long _restockTime;

    private Player _ritualOwner;
    private uint _spellId;

    public GameObject(LootFactory lootFactory, ClassFactory classFactory, LootStoreBox lootStoreBox, PoolManager poolManager,
                      DB2Manager db2Manager, WorldDatabase worldDatabase, LootManager lootManager, OutdoorPvPManager outdoorPvPManager,
                      CreatureDataCache creatureDataCache) : base(false, classFactory)
    {
        LootFactory = lootFactory;
        LootStoreBox = lootStoreBox;
        PoolManager = poolManager;
        DB2Manager = db2Manager;
        WorldDatabase = worldDatabase;
        LootManager = lootManager;
        OutdoorPvPManager = outdoorPvPManager;
        _creatureDataCache = creatureDataCache;
        ObjectTypeMask |= TypeMask.GameObject;
        ObjectTypeId = TypeId.GameObject;

        UpdateFlag.Stationary = true;
        UpdateFlag.Rotation = true;

        RespawnDelay = 300;
        _despawnDelay = 0;
        LootState = LootState.NotReady;
        IsSpawnedByDefault = true;

        ResetLootMode(); // restore default loot mode
        StationaryPosition = new Position();

        GameObjectFieldData = new GameObjectFieldData();
    }

    // used as internal reaction delay time store (not state change reaction).
    // For traps this: spell casting cooldown, for doors/buttons: reset time.
    public GameObjectAI AI { get; private set; }

    public override ushort AIAnimKitId => _animKitId;

    public string AiName
    {
        get
        {
            var got = GameObjectManager.GameObjectTemplateCache.GetGameObjectTemplate(Entry);

            return got != null ? got.AIName : "";
        }
    }

    public Transport AsTransport => Template.type == GameObjectTypes.MapObjTransport ? this as Transport : null;
    public DB2Manager DB2Manager { get; }

    public uint DisplayId
    {
        get => GameObjectFieldData.DisplayID;
        set
        {
            SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.DisplayID), value);
            UpdateModel();
        }
    }

    public override uint Faction
    {
        get => GameObjectFieldData.FactionTemplate;
        set => SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.FactionTemplate), value);
    }

    public GameObjectData GameObjectData { get; private set; }

    // used for GAMEOBJECT_TYPE_SUMMONING_RITUAL where GO is not summoned (no owner)
    // What state to set whenever resetting
    public GameObjectFieldData GameObjectFieldData { get; set; }

    public GameObjectOverride GameObjectOverride
    {
        get
        {
            if (SpawnId == 0)
                return GoTemplateAddonProtected;

            var goOverride = GameObjectManager.GetGameObjectOverride(SpawnId);

            return goOverride ?? GoTemplateAddonProtected;
        }
    }

    public uint GoArtKit
    {
        get => GameObjectFieldData.ArtKit;
        set
        {
            SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.ArtKit), value);
            var data = GameObjectManager.SpawnDataCacheRouter.GameObjectCache.GetGameObjectData(SpawnId);

            if (data != null)
                data.ArtKit = value;
        }
    }

    public uint GossipMenuId
    {
        get => _gossipMenuId ?? Template.GetGossipMenuId();
        set => _gossipMenuId = value;
    }

    public GameObjectState GoState => (GameObjectState)(sbyte)GameObjectFieldData.State;

    public GameObjectTypes GoType
    {
        get => (GameObjectTypes)(sbyte)GameObjectFieldData.TypeID;
        set => SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.TypeID), (sbyte)value);
    }

    public GameObjectValue GoValue => GoValueProtected;

    public bool IsDestructibleBuilding
    {
        get
        {
            var gInfo = Template;

            if (gInfo == null)
                return false;

            return gInfo.type == GameObjectTypes.DestructibleBuilding;
        }
    }

    // is Dynamic transport = non-stop Transport
    public bool IsDynTransport
    {
        get
        {
            // If something is marked as a transport, don't transmit an out of range packet for it.
            var gInfo = Template;

            return gInfo?.type is GameObjectTypes.MapObjTransport or GameObjectTypes.Transport;
        }
    }

    public bool IsFullyLooted
    {
        get
        {
            if (Loot != null && !Loot.IsLooted())
                return false;

            foreach (var (_, loot) in _personalLoot)
                if (!loot.IsLooted())
                    return false;

            return true;
        }
    }

    public bool IsSpawned => RespawnDelay == 0 ||
                             (RespawnTime > 0 && !IsSpawnedByDefault) ||
                             (RespawnTime == 0 && IsSpawnedByDefault);

    public bool IsSpawnedByDefault { get; private set; }

    public bool IsTransport
    {
        get
        {
            // If something is marked as a transport, don't transmit an out of range packet for it.
            var gInfo = Template;

            return gInfo?.type is GameObjectTypes.Transport or GameObjectTypes.MapObjTransport;
        }
    }

    public GameObject LinkedTrap
    {
        get => ObjectAccessor.GetGameObject(this, _linkedTrap);
        set => _linkedTrap = value.GUID;
    }

    public Quaternion LocalRotation => _localRotation;
    public Loot Loot { get; set; }
    public LootFactory LootFactory { get; }
    public LootManager LootManager { get; }
    public LootModes LootMode { get; private set; }
    public LootState LootState { get; private set; }
    public LootStoreBox LootStoreBox { get; }
    public GameObjectModel Model { get; set; }
    public override ObjectGuid OwnerGUID => GameObjectFieldData.CreatedBy;
    public long PackedLocalRotation { get; private set; }
    public PoolManager PoolManager { get; }

    // There's many places not ready for dynamic spawns. This allows them to live on for now.
    public bool RespawnCompatibilityMode { get; private set; }

    public uint RespawnDelay { get; private set; }
    public long RespawnTime { get; private set; }
    public long RespawnTimeEx => RespawnTime > GameTime.CurrentTime ? RespawnTime : GameTime.CurrentTime;

    public uint ScriptId
    {
        get
        {
            if (GameObjectData == null)
                return Template.ScriptId;

            var scriptId = GameObjectData.ScriptId;

            return scriptId != 0 ? scriptId : Template.ScriptId;
        }
    }

    public ulong SpawnId { get; private set; }

    public uint SpellId
    {
        get => _spellId;
        set
        {
            IsSpawnedByDefault = false; // all summoned object is despawned after delay
            _spellId = value;
        }
    }

    public Position StationaryPosition { get; set; }
    public GameObjectTemplate Template => GoInfoProtected;
    public GameObjectTemplateAddon TemplateAddon => GoTemplateAddonProtected;
    public uint UseCount { get; private set; }
    public WorldDatabase WorldDatabase { get; }
    public uint WorldEffectID { get; set; }

    private GameObjectDestructibleState DestructibleState
    {
        get
        {
            if ((GameObjectFieldData.Flags & (uint)GameObjectFlags.Destroyed) != 0)
                return GameObjectDestructibleState.Destroyed;

            return (GameObjectFieldData.Flags & (uint)GameObjectFlags.Damaged) != 0 ? GameObjectDestructibleState.Damaged : GameObjectDestructibleState.Intact;
        }
    }

    private byte GoAnimProgress => GameObjectFieldData.PercentHealth;

    private List<ObjectGuid> TapList { get; } = new();

    //! Object distance/size - overridden from Object.IsWithinDist. Needs to take in account proper GO size.
    public override bool IsWithinDistance(WorldObject obj, float dist2Compare, bool is3D, bool incOwnRadius = true, bool incTargetRadius = true)
    {
        //! Following check does check 3d distance
        return IsInRange(obj.Location.X, obj.Location.Y, obj.Location.Z, dist2Compare);
    }

    public void ActivateObject(GameObjectActions action, int param, WorldObject spellCaster = null, uint spellId = 0, int effectIndex = -1)
    {
        var unitCaster = spellCaster?.AsUnit;

        switch (action)
        {
            case GameObjectActions.None:
                Log.Logger.Fatal($"Spell {spellId} has action type NONE in effect {effectIndex}");

                break;

            case GameObjectActions.AnimateCustom0:
            case GameObjectActions.AnimateCustom1:
            case GameObjectActions.AnimateCustom2:
            case GameObjectActions.AnimateCustom3:
                SendCustomAnim((uint)(action - GameObjectActions.AnimateCustom0));

                break;

            case GameObjectActions.Disturb: // What's the difference with Open?
                if (unitCaster != null)
                    Use(unitCaster);

                break;

            case GameObjectActions.Unlock:
                RemoveFlag(GameObjectFlags.Locked);

                break;

            case GameObjectActions.Lock:
                SetFlag(GameObjectFlags.Locked);

                break;

            case GameObjectActions.Open:
                if (unitCaster != null)
                    Use(unitCaster);

                break;

            case GameObjectActions.OpenAndUnlock:
                if (unitCaster != null)
                    UseDoorOrButton(0, false, unitCaster);

                RemoveFlag(GameObjectFlags.Locked);

                break;

            case GameObjectActions.Close:
                ResetDoorOrButton();

                break;

            case GameObjectActions.ToggleOpen:
                // No use cases, implementation unknown
                break;

            case GameObjectActions.Destroy:
                if (unitCaster != null)
                    UseDoorOrButton(0, true, unitCaster);

                break;

            case GameObjectActions.Rebuild:
                ResetDoorOrButton();

                break;

            case GameObjectActions.Creation:
                // No use cases, implementation unknown
                break;

            case GameObjectActions.Despawn:
                DespawnOrUnsummon();

                break;

            case GameObjectActions.MakeInert:
                SetFlag(GameObjectFlags.NotSelectable);

                break;

            case GameObjectActions.MakeActive:
                RemoveFlag(GameObjectFlags.NotSelectable);

                break;

            case GameObjectActions.CloseAndLock:
                ResetDoorOrButton();
                SetFlag(GameObjectFlags.Locked);

                break;

            case GameObjectActions.UseArtKit0:
            case GameObjectActions.UseArtKit1:
            case GameObjectActions.UseArtKit2:
            case GameObjectActions.UseArtKit3:
            case GameObjectActions.UseArtKit4:
            {
                var templateAddon = TemplateAddon;

                var artKitIndex = action != GameObjectActions.UseArtKit4 ? (uint)(action - GameObjectActions.UseArtKit0) : 4;

                uint artKitValue = 0;

                if (templateAddon != null)
                    artKitValue = templateAddon.ArtKits[artKitIndex];

                if (artKitValue == 0)
                    Log.Logger.Error($"GameObject {Entry} hit by spell {spellId} needs `artkit{artKitIndex}` in `gameobject_template_addon`");
                else
                    GoArtKit = artKitValue;

                break;
            }
            case GameObjectActions.GoTo1stFloor:
            case GameObjectActions.GoTo2ndFloor:
            case GameObjectActions.GoTo3rdFloor:
            case GameObjectActions.GoTo4thFloor:
            case GameObjectActions.GoTo5thFloor:
            case GameObjectActions.GoTo6thFloor:
            case GameObjectActions.GoTo7thFloor:
            case GameObjectActions.GoTo8thFloor:
            case GameObjectActions.GoTo9thFloor:
            case GameObjectActions.GoTo10thFloor:
                if (GoType == GameObjectTypes.Transport)
                    SetGoState((GameObjectState)action);
                else
                    Log.Logger.Error($"Spell {spellId} targeted non-transport gameobject for transport only action \"Go to Floor\" {action} in effect {effectIndex}");

                break;

            case GameObjectActions.PlayAnimKit:
                SetAnimKitId((ushort)param, false);

                break;

            case GameObjectActions.OpenAndPlayAnimKit:
                if (unitCaster != null)
                    UseDoorOrButton(0, false, unitCaster);

                SetAnimKitId((ushort)param, false);

                break;

            case GameObjectActions.CloseAndPlayAnimKit:
                ResetDoorOrButton();
                SetAnimKitId((ushort)param, false);

                break;

            case GameObjectActions.PlayOneShotAnimKit:
                SetAnimKitId((ushort)param, true);

                break;

            case GameObjectActions.StopAnimKit:
                SetAnimKitId(0, false);

                break;

            case GameObjectActions.OpenAndStopAnimKit:
                if (unitCaster != null)
                    UseDoorOrButton(0, false, unitCaster);

                SetAnimKitId(0, false);

                break;

            case GameObjectActions.CloseAndStopAnimKit:
                ResetDoorOrButton();
                SetAnimKitId(0, false);

                break;

            case GameObjectActions.PlaySpellVisual:
                if (spellCaster != null)
                    SetSpellVisualId((uint)param, spellCaster.GUID);

                break;

            case GameObjectActions.StopSpellVisual:
                SetSpellVisualId(0);

                break;

            default:
                Log.Logger.Error($"Spell {spellId} has unhandled action {action} in effect {effectIndex}");

                break;
        }
    }

    public bool ActivateToQuest(Player target)
    {
        if (target.HasQuestForGO((int)Entry))
            return true;

        if (!GameObjectManager.IsGameObjectForQuests(Entry))
            return false;

        switch (GoType)
        {
            case GameObjectTypes.QuestGiver:
                var questStatus = target.GetQuestDialogStatus(this);

                if (questStatus != QuestGiverStatus.None && questStatus != QuestGiverStatus.Future)
                    return true;

                break;

            case GameObjectTypes.Chest:
            {
                // Chests become inactive while not ready to be looted
                if (LootState == LootState.NotReady)
                    return false;

                // scan GO chest with loot including quest items
                if (target.GetQuestStatus(Template.Chest.questID) == QuestStatus.Incomplete || LootStoreBox.Gameobject.HaveQuestLootForPlayer(Template.Chest.chestLoot, target) || LootStoreBox.Gameobject.HaveQuestLootForPlayer(Template.Chest.chestPersonalLoot, target) || LootStoreBox.Gameobject.HaveQuestLootForPlayer(Template.Chest.chestPushLoot, target))
                    return target.Battleground == null || target.Battleground.CanActivateGO((int)Entry, (uint)target.Battleground.GetPlayerTeam(target.GUID));

                break;
            }
            case GameObjectTypes.Generic:
            {
                if (target.GetQuestStatus(Template.Generic.questID) == QuestStatus.Incomplete)
                    return true;

                break;
            }
            case GameObjectTypes.Goober:
            {
                if (target.GetQuestStatus(Template.Goober.questID) == QuestStatus.Incomplete)
                    return true;

                break;
            }
        }

        return false;
    }

    public void AddToSkillupList(ObjectGuid playerGuid)
    {
        _skillupList.Add(playerGuid);
    }

    public override void AddToWorld()
    {
        //- Register the gameobject for guid lookup
        if (!Location.IsInWorld)
        {
            ZoneScript?.OnGameObjectCreate(this);

            Location.Map.ObjectsStore.TryAdd(GUID, this);

            if (SpawnId != 0)
                Location.Map.GameObjectBySpawnIdStore.Add(SpawnId, this);

            // The state can be changed after GameObject.Create but before GameObject.AddToWorld
            var toggledState = GoType == GameObjectTypes.Chest ? LootState == LootState.Ready : GoState == GameObjectState.Ready || IsTransport;

            if (Model != null)
            {
                var trans = AsTransport;

                if (trans != null)
                    trans.SetDelayedAddModelToMap();
                else
                    Location.Map.InsertGameObjectModel(Model);
            }

            EnableCollision(toggledState);
            base.AddToWorld();
        }
    }

    public void AddUniqueUse(Player player)
    {
        AddUse();
        _uniqueUsers.Add(player.GUID);
    }

    public void AddUse()
    {
        ++UseCount;
    }

    public void AfterRelocation()
    {
        UpdateModelPosition();
        Location.UpdatePositionData();

        _goTypeImpl?.OnRelocated();

        UpdateObjectVisibility(false);
    }

    public bool AIM_Initialize()
    {
        AI = AISelector.SelectGameObjectAI(this);

        if (AI == null)
            return false;

        AI.InitializeAI();

        return true;
    }

    public void AssaultCapturePoint(Player player)
    {
        if (!CanInteractWithCapturePoint(player))
            return;

        var ai = AI;

        if (ai != null)
            if (ai.OnCapturePointAssaulted(player))
                return;

        // only supported in battlegrounds
        Battleground battleground = null;
        var map = Location.Map.ToBattlegroundMap;

        var bg = map?.BG;

        if (bg != null)
            battleground = bg;

        if (battleground == null)
            return;

        // Cancel current timer
        GoValueProtected.CapturePoint.AssaultTimer = 0;

        if (player.GetBgTeam() == TeamFaction.Horde)
        {
            if (GoValueProtected.CapturePoint.LastTeamCapture == TeamIds.Horde)
            {
                // defended. capture instantly.
                GoValueProtected.CapturePoint.State = BattlegroundCapturePointState.HordeCaptured;
                battleground.SendBroadcastText(Template.CapturePoint.DefendedBroadcastHorde, ChatMsg.BgSystemHorde, player);
                UpdateCapturePoint();

                if (Template.CapturePoint.DefendedEventHorde != 0)
                    GameEvents.Trigger(Template.CapturePoint.DefendedEventHorde, player, this);

                return;
            }

            switch (GoValueProtected.CapturePoint.State)
            {
                case BattlegroundCapturePointState.Neutral:
                case BattlegroundCapturePointState.AllianceCaptured:
                case BattlegroundCapturePointState.ContestedAlliance:
                    GoValueProtected.CapturePoint.State = BattlegroundCapturePointState.ContestedHorde;
                    battleground.SendBroadcastText(Template.CapturePoint.AssaultBroadcastHorde, ChatMsg.BgSystemHorde, player);
                    UpdateCapturePoint();

                    if (Template.CapturePoint.ContestedEventHorde != 0)
                        GameEvents.Trigger(Template.CapturePoint.ContestedEventHorde, player, this);

                    GoValueProtected.CapturePoint.AssaultTimer = Template.CapturePoint.CaptureTime;

                    break;
            }
        }
        else
        {
            if (GoValueProtected.CapturePoint.LastTeamCapture == TeamIds.Alliance)
            {
                // defended. capture instantly.
                GoValueProtected.CapturePoint.State = BattlegroundCapturePointState.AllianceCaptured;
                battleground.SendBroadcastText(Template.CapturePoint.DefendedBroadcastAlliance, ChatMsg.BgSystemAlliance, player);
                UpdateCapturePoint();

                if (Template.CapturePoint.DefendedEventAlliance != 0)
                    GameEvents.Trigger(Template.CapturePoint.DefendedEventAlliance, player, this);

                return;
            }

            switch (GoValueProtected.CapturePoint.State)
            {
                case BattlegroundCapturePointState.Neutral:
                case BattlegroundCapturePointState.HordeCaptured:
                case BattlegroundCapturePointState.ContestedHorde:
                    GoValueProtected.CapturePoint.State = BattlegroundCapturePointState.ContestedAlliance;
                    battleground.SendBroadcastText(Template.CapturePoint.AssaultBroadcastAlliance, ChatMsg.BgSystemAlliance, player);
                    UpdateCapturePoint();

                    if (Template.CapturePoint.ContestedEventAlliance != 0)
                        GameEvents.Trigger(Template.CapturePoint.ContestedEventAlliance, player, this);

                    GoValueProtected.CapturePoint.AssaultTimer = Template.CapturePoint.CaptureTime;

                    break;
            }
        }
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt8((byte)flags);
        ObjectData.WriteCreate(buffer, flags, this, target);
        GameObjectFieldData.WriteCreate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

        if (Values.HasChanged(TypeId.Object))
            ObjectData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.GameObject))
            GameObjectFieldData.WriteUpdate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedGameObjectMask, Player target)
    {
        UpdateMask valuesMask = new((int)TypeId.Max);

        if (requestedObjectMask.IsAnySet())
            valuesMask.Set((int)TypeId.Object);

        if (requestedGameObjectMask.IsAnySet())
            valuesMask.Set((int)TypeId.GameObject);

        WorldPacket buffer = new();
        buffer.WriteUInt32(valuesMask.GetBlock(0));

        if (valuesMask[(int)TypeId.Object])
            ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

        if (valuesMask[(int)TypeId.GameObject])
            GameObjectFieldData.WriteUpdate(buffer, requestedGameObjectMask, true, this, target);

        WorldPacket buffer1 = new();
        buffer1.WriteUInt8((byte)UpdateType.Values);
        buffer1.WritePackedGuid(GUID);
        buffer1.WriteUInt32(buffer.GetSize());
        buffer1.WriteBytes(buffer.GetData());

        data.AddUpdateBlock(buffer1);
    }

    public bool CanInteractWithCapturePoint(Player target)
    {
        if (GoInfoProtected.type != GameObjectTypes.CapturePoint)
            return false;

        if (GoValueProtected.CapturePoint.State == BattlegroundCapturePointState.Neutral)
            return true;

        if (target.GetBgTeam() == TeamFaction.Horde)
            return GoValueProtected.CapturePoint.State is BattlegroundCapturePointState.ContestedAlliance or BattlegroundCapturePointState.AllianceCaptured;

        // For Alliance players
        return GoValueProtected.CapturePoint.State is BattlegroundCapturePointState.ContestedHorde or BattlegroundCapturePointState.HordeCaptured;
    }

    public override void CleanupsBeforeDelete(bool finalCleanup = true)
    {
        base.CleanupsBeforeDelete(finalCleanup);

        RemoveFromOwner();
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(GameObjectFieldData);
        base.ClearUpdateMask(remove);
    }

    public void CreateModel()
    {
        Model = GameObjectModel.Create(new GameObjectModelOwnerImpl(this), ClassFactory.Resolve<VMapManager>());

        if (Model is { IsMapObject: true })
            SetFlag(GameObjectFlags.MapObject);
    }

    public void Delete()
    {
        SetLootState(LootState.NotReady);
        RemoveFromOwner();

        if (GoInfoProtected.type == GameObjectTypes.CapturePoint)
            SendMessageToSet(new CapturePointRemoved(GUID), true);

        SendGameObjectDespawn();

        if (GoInfoProtected.type != GameObjectTypes.Transport)
            SetGoState(GameObjectState.Ready);

        var goOverride = GameObjectOverride;

        if (goOverride != null)
            ReplaceAllFlags(goOverride.Flags);

        var poolid = GameObjectData?.PoolId ?? 0;

        if (RespawnCompatibilityMode && poolid != 0)
            PoolManager.UpdatePool<GameObject>(Location.Map.PoolData, poolid, SpawnId);
        else
            Location.AddObjectToRemoveList();
    }

    public void DespawnOrUnsummon(TimeSpan delay = default, TimeSpan forceRespawnTime = default)
    {
        if (delay > TimeSpan.Zero)
        {
            if (_despawnDelay == 0 || _despawnDelay > delay.TotalMilliseconds)
            {
                _despawnDelay = (uint)delay.TotalMilliseconds;
                _despawnRespawnTime = forceRespawnTime;
            }
        }
        else
        {
            if (GameObjectData != null)
            {
                var respawnDelay = (uint)(forceRespawnTime > TimeSpan.Zero ? forceRespawnTime.TotalSeconds : RespawnDelay);
                SaveRespawnTime(respawnDelay);
            }

            Delete();
        }
    }

    public override void Dispose()
    {
        AI = null;
        Model = null;

        base.Dispose();
    }

    public T GetAI<T>() where T : GameObjectAI
    {
        return (T)AI;
    }

    public override string GetDebugInfo()
    {
        return $"{base.GetDebugInfo()}\nSpawnId: {SpawnId} GoState: {GoState} ScriptId: {ScriptId} AIName: {AiName}";
    }

    public GameObjectDynamicLowFlags GetDynamicFlags()
    {
        return (GameObjectDynamicLowFlags)(uint)ObjectData.DynamicFlags;
    }

    public Loot GetFishLoot(Player lootOwner)
    {
        uint defaultzone = 1;

        var fishLoot = LootFactory.GenerateLoot(Location.Map, GUID, LootType.Fishing);

        var areaId = Location.Area;

        while (CliDB.AreaTableStorage.LookupByKey(areaId) is { } areaEntry)
        {
            fishLoot.FillLoot(areaId, LootStorageType.Fishing, lootOwner, true, true);

            if (!fishLoot.IsLooted())
                break;

            areaId = areaEntry.ParentAreaID;
        }

        if (fishLoot.IsLooted())
            fishLoot.FillLoot(defaultzone, LootStorageType.Fishing, lootOwner, true, true);

        return fishLoot;
    }

    public Loot GetFishLootJunk(Player lootOwner)
    {
        uint defaultzone = 1;

        var fishLoot = LootFactory.GenerateLoot(Location.Map, GUID, LootType.FishingJunk);

        var areaId = Location.Area;

        while (CliDB.AreaTableStorage.LookupByKey(areaId) is { } areaEntry)
        {
            fishLoot.FillLoot(areaId, LootStorageType.Fishing, lootOwner, true, true, LootModes.JunkFish);

            if (!fishLoot.IsLooted())
                break;

            areaId = areaEntry.ParentAreaID;
        }

        if (fishLoot.IsLooted())
            fishLoot.FillLoot(defaultzone, LootStorageType.Fishing, lootOwner, true, true, LootModes.JunkFish);

        return fishLoot;
    }

    public GameObjectState GetGoStateFor(ObjectGuid viewer)
    {
        var state = _perPlayerState?.LookupByKey(viewer);

        if (state is { State: { } })
            return state.State.Value;

        return GoState;
    }

    public float GetInteractionDistance()
    {
        if (Template.GetInteractRadiusOverride() != 0)
            return Template.GetInteractRadiusOverride() / 100.0f;

        return GoType switch
        {
            GameObjectTypes.AreaDamage => 0.0f,
            GameObjectTypes.QuestGiver => 5.5555553f,
            GameObjectTypes.Text => 5.5555553f,
            GameObjectTypes.FlagStand => 5.5555553f,
            GameObjectTypes.FlagDrop => 5.5555553f,
            GameObjectTypes.MiniGame => 5.5555553f,
            GameObjectTypes.Chair => 3.0f,
            GameObjectTypes.BarberChair => 3.0f,
            GameObjectTypes.FishingNode => 100.0f,
            GameObjectTypes.FishingHole => 20.0f + SharedConst.ContactDistance // max spell range
            ,
            GameObjectTypes.Camera => 5.0f,
            GameObjectTypes.MapObject => 5.0f,
            GameObjectTypes.DungeonDifficulty => 5.0f,
            GameObjectTypes.DestructibleBuilding => 5.0f,
            GameObjectTypes.Door => 5.0f,
            // Following values are not blizzlike
            GameObjectTypes.GuildBank =>
                // Successful mailbox interaction is rather critical to the client, failing it will start a minute-long cooldown until the next mail query may be executed.
                // And since movement info update is not sent with mailbox interaction query, server may find the player outside of interaction range. Thus we increase it.
                10.0f // 5.0f is blizzlike
            ,
            GameObjectTypes.Mailbox =>
                // Successful mailbox interaction is rather critical to the client, failing it will start a minute-long cooldown until the next mail query may be executed.
                // And since movement info update is not sent with mailbox interaction query, server may find the player outside of interaction range. Thus we increase it.
                10.0f // 5.0f is blizzlike
            ,
            _ => SharedConst.InteractionDistance
        };
    }

    public override uint GetLevelForTarget(WorldObject target)
    {
        var owner = OwnerUnit;

        if (owner != null)
            return owner.GetLevelForTarget(target);

        if (GoType != GameObjectTypes.Trap)
            return 1;

        var player = target.AsPlayer;

        if (player != null)
        {
            var userLevels = DB2Manager.GetContentTuningData(Template.ContentTuningId, player.PlayerData.CtrOptions.Value.ContentTuningConditionMask);

            if (userLevels.HasValue)
                return (byte)Math.Clamp(player.Level, userLevels.Value.MinLevel, userLevels.Value.MaxLevel);
        }

        var targetUnit = target.AsUnit;

        return targetUnit?.Level ?? 1;
    }

    public override Loot GetLootForPlayer(Player player)
    {
        return _personalLoot.Empty() ? Loot : _personalLoot.LookupByKey(player.GUID);
    }

    public override string GetName(Locale locale = Locale.enUS)
    {
        if (locale == Locale.enUS)
            return base.GetName(locale);

        var cl = GameObjectManager.GetGameObjectLocale(Entry);

        if (cl == null)
            return base.GetName(locale);

        if (cl.Name.Length > (int)locale && !cl.Name[(int)locale].IsEmpty())
            return cl.Name[(int)locale];

        return base.GetName(locale);
    }

    public byte GetNameSetId()
    {
        switch (GoType)
        {
            case GameObjectTypes.DestructibleBuilding:
                if (CliDB.DestructibleModelDataStorage.TryGetValue(GoInfoProtected.DestructibleBuilding.DestructibleModelRec, out var modelData))
                    switch (DestructibleState)
                    {
                        case GameObjectDestructibleState.Intact:
                            return modelData.State0NameSet;

                        case GameObjectDestructibleState.Damaged:
                            return modelData.State1NameSet;

                        case GameObjectDestructibleState.Destroyed:
                            return modelData.State2NameSet;

                        case GameObjectDestructibleState.Rebuilding:
                            return modelData.State3NameSet;
                    }

                break;

            case GameObjectTypes.GarrisonBuilding:
            case GameObjectTypes.GarrisonPlot:
            case GameObjectTypes.PhaseableMo:
                var flags = (GameObjectFlags)(uint)GameObjectFieldData.Flags;

                return (byte)(((int)flags >> 8) & 0xF);
        }

        return 0;
    }

    public List<uint> GetPauseTimes()
    {
        var transport = _goTypeImpl as TransportGameObject;

        return transport?.GetPauseTimes();
    }

    public Position GetRespawnPosition()
    {
        return GameObjectData != null ? GameObjectData.SpawnPoint.Copy() : Location.Copy();
    }

    public SpellInfo GetSpellForLock(Player player)
    {
        if (player == null)
            return null;

        var lockId = Template.GetLockId();

        if (lockId == 0)
            return null;

        if (!CliDB.LockStorage.TryGetValue(lockId, out var lockEntry))
            return null;

        for (byte i = 0; i < SharedConst.MaxLockCase; ++i)
        {
            switch (lockEntry.LockType[i])
            {
                case 0:
                    continue;
                case (byte)LockKeyType.Spell:
                {
                    var spell = SpellManager.GetSpellInfo((uint)lockEntry.Index[i], Location.Map.DifficultyID);

                    if (spell != null)
                        return spell;

                    break;
                }
            }

            if (lockEntry.LockType[i] != (byte)LockKeyType.Skill)
                break;

            foreach (var playerSpell in player.GetSpellMap())
            {
                var spell = SpellManager.GetSpellInfo(playerSpell.Key, Location.Map.DifficultyID);

                if (spell == null)
                    continue;

                if (spell.Effects.Where(effect => effect.Effect == SpellEffectName.OpenLock && effect.MiscValue == lockEntry.Index[i]).Any(effect => effect.CalcValue(player) >= lockEntry.Skill[i]))
                    return spell;
            }
        }

        return null;
    }

    public Quaternion GetWorldRotation()
    {
        var localRotation = LocalRotation;

        var transport = GetTransport<Transport>();

        if (transport != null)
        {
            var worldRotation = transport.GetWorldRotation();

            Quaternion worldRotationQuat = new(worldRotation.X, worldRotation.Y, worldRotation.Z, worldRotation.W);
            Quaternion localRotationQuat = new(localRotation.X, localRotation.Y, localRotation.Z, localRotation.W);

            var resultRotation = localRotationQuat * worldRotationQuat;

            return resultRotation;
        }

        return localRotation;
    }

    public bool HasDynamicFlag(GameObjectDynamicLowFlags flag)
    {
        return (ObjectData.DynamicFlags & (uint)flag) != 0;
    }

    public bool HasFlag(GameObjectFlags flags)
    {
        return (GameObjectFieldData.Flags & (uint)flags) != 0;
    }

    public override bool HasInvolvedQuest(uint questId)
    {
        return GameObjectManager.GetGOQuestInvolvedRelations(Entry).HasQuest(questId);
    }

    public override bool HasQuest(uint questId)
    {
        return GameObjectManager.GetGOQuestRelations(Entry).HasQuest(questId);
    }

    public override bool IsAlwaysVisibleFor(WorldObject seer)
    {
        if (base.IsAlwaysVisibleFor(seer))
            return true;

        if (IsTransport || IsDestructibleBuilding)
            return true;

        if (seer == null)
            return false;

        // Always seen by owner and friendly units
        var guid = OwnerGUID;

        if (guid.IsEmpty)
            return false;

        if (seer.GUID == guid)
            return true;

        var owner = OwnerUnit;

        return owner != null && seer.IsTypeMask(TypeMask.Unit) && owner.WorldObjectCombat.IsFriendlyTo(seer.AsUnit);
    }

    public bool IsAtInteractDistance(Player player, SpellInfo spell = null)
    {
        if (spell == null && (spell = GetSpellForLock(player)) == null)
            return IsAtInteractDistance(player.Location, GetInteractionDistance());

        var maxRange = spell.GetMaxRange(spell.IsPositive);

        if (GoType == GameObjectTypes.SpellFocus)
            return maxRange * maxRange >= Location.GetExactDistSq(player.Location);

        return IsAtInteractDistance(player.Location, CliDB.GameObjectDisplayInfoStorage.ContainsKey(Template.displayId) ? maxRange : GetInteractionDistance());
    }

    public bool IsInRange(float x, float y, float z, float radius)
    {
        if (!CliDB.GameObjectDisplayInfoStorage.TryGetValue(GoInfoProtected.displayId, out var info))
            return Location.IsWithinDist3d(x, y, z, radius);

        var sinA = (float)Math.Sin(Location.Orientation);
        var cosA = (float)Math.Cos(Location.Orientation);
        var dx = x - Location.X;
        var dy = y - Location.Y;
        var dz = z - Location.Z;
        var dist = (float)Math.Sqrt(dx * dx + dy * dy);

        //! Check if the distance between the 2 objects is 0, can happen if both objects are on the same position.
        //! The code below this check wont crash if dist is 0 because 0/0 in float operations is valid, and returns infinite
        if (MathFunctions.fuzzyEq(dist, 0.0f))
            return true;

        var sinB = dx / dist;
        var cosB = dy / dist;
        dx = dist * (cosA * cosB + sinA * sinB);
        dy = dist * (cosA * sinB - sinA * cosB);

        return dx < info.GeoBoxMax.X + radius && dx > info.GeoBoxMin.X - radius && dy < info.GeoBoxMax.Y + radius && dy > info.GeoBoxMin.Y - radius && dz < info.GeoBoxMax.Z + radius && dz > info.GeoBoxMin.Z - radius;
    }

    public bool IsInSkillupList(ObjectGuid playerGuid)
    {
        return _skillupList.Any(i => i == playerGuid);
    }

    public override bool IsInvisibleDueToDespawn(WorldObject seer)
    {
        if (base.IsInvisibleDueToDespawn(seer))
            return true;

        // Despawned
        if (!IsSpawned)
            return true;

        var state = _perPlayerState?.LookupByKey(seer.GUID);

        return state is { Despawned: true };
    }

    public bool IsLootAllowedFor(Player player)
    {
        var loot = GetLootForPlayer(player);

        if (loot != null) // check only if loot was already generated
        {
            if (loot.IsLooted()) // nothing to loot or everything looted.
                return false;

            if (!loot.HasAllowedLooter(GUID) || (!loot.HasItemForAll() && !loot.HasItemFor(player))) // no loot in chest for this player
                return false;
        }

        if (!TapList.Empty())
            return TapList.Contains(player.GUID); // if go doesnt have group bound it means it was solo killed by someone else

        return true;
    }

    public override bool IsNeverVisibleFor(WorldObject seer)
    {
        if (base.IsNeverVisibleFor(seer))
            return true;

        if (Template.GetServerOnly() != 0)
            return true;

        if (DisplayId == 0)
            return true;

        return false;
    }

    public bool IsWithinDistInMap(Player player)
    {
        return Location.IsInMap(player) && Location.InSamePhase(player) && IsAtInteractDistance(player);
    }

    public override bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool unused = true)
    {
        var data = GameObjectManager.SpawnDataCacheRouter.GameObjectCache.GetGameObjectData(spawnId);

        if (data == null)
        {
            Log.Logger.Error("Gameobject (SpawnId: {0}) not found in table `gameobject`, can't load. ", spawnId);

            return false;
        }

        var entry = data.Id;

        var animprogress = data.Animprogress;
        var goState = data.GoState;
        var artKit = data.ArtKit;

        SpawnId = spawnId;
        RespawnCompatibilityMode = (data.SpawnGroupData.Flags & SpawnGroupFlags.CompatibilityMode) != 0;

        if (!Create(entry, map, data.SpawnPoint, data.Rotation, animprogress, goState, artKit, !RespawnCompatibilityMode, spawnId))
            return false;

        PhasingHandler.InitDbPhaseShift(Location.PhaseShift, data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
        PhasingHandler.InitDbVisibleMapId(Location.PhaseShift, data.TerrainSwapMap);

        if (data.Spawntimesecs >= 0)
        {
            IsSpawnedByDefault = true;

            if (!Template.GetDespawnPossibility() && !Template.IsDespawnAtAction())
            {
                SetFlag(GameObjectFlags.NoDespawn);
                RespawnDelay = 0;
                RespawnTime = 0;
            }
            else
            {
                RespawnDelay = (uint)data.Spawntimesecs;
                RespawnTime = Location.Map.GetGORespawnTime(SpawnId);

                // ready to respawn
                if (RespawnTime != 0 && RespawnTime <= GameTime.CurrentTime)
                {
                    RespawnTime = 0;
                    Location.Map.RemoveRespawnTime(SpawnObjectType.GameObject, SpawnId);
                }
            }
        }
        else
        {
            if (!RespawnCompatibilityMode)
            {
                Log.Logger.Warning($"GameObject {entry} (SpawnID {spawnId}) is not spawned by default, but tries to use a non-hack spawn system. This will not work. Defaulting to compatibility mode.");
                RespawnCompatibilityMode = true;
            }

            IsSpawnedByDefault = false;
            RespawnDelay = (uint)-data.Spawntimesecs;
            RespawnTime = 0;
        }

        GameObjectData = data;

        if (addToMap && !Location.Map.AddToMap(this))
            return false;

        return true;
    }

    public bool MeetsInteractCondition(Player user)
    {
        if (GoInfoProtected.GetConditionID1() == 0)
            return true;

        if (CliDB.PlayerConditionStorage.TryGetValue(GoInfoProtected.GetConditionID1(), out var playerCondition))
            if (!ConditionManager.IsPlayerMeetingCondition(user, playerCondition))
                return false;

        return true;
    }

    public void ModifyHealth(double change, WorldObject attackerOrHealer = null, uint spellId = 0)
    {
        ModifyHealth((int)change, attackerOrHealer, spellId);
    }

    public void ModifyHealth(int change, WorldObject attackerOrHealer = null, uint spellId = 0)
    {
        if (GoValueProtected.Building.MaxHealth == 0 || change == 0)
            return;

        // prevent double destructions of the same object
        if (change < 0 && GoValueProtected.Building.Health == 0)
            return;

        if (GoValueProtected.Building.Health + change <= 0)
            GoValueProtected.Building.Health = 0;
        else if (GoValueProtected.Building.Health + change >= GoValueProtected.Building.MaxHealth)
            GoValueProtected.Building.Health = GoValueProtected.Building.MaxHealth;
        else
            GoValueProtected.Building.Health += (uint)change;

        // Set the health bar, value = 255 * healthPct;
        SetGoAnimProgress(GoValueProtected.Building.Health * 255 / GoValueProtected.Building.MaxHealth);

        // dealing damage, send packet
        var player = attackerOrHealer?.CharmerOrOwnerPlayerOrPlayerItself;

        if (player != null)
        {
            DestructibleBuildingDamage packet = new()
            {
                Caster = attackerOrHealer.GUID, // todo: this can be a GameObject
                Target = GUID,
                Damage = -change,
                Owner = player.GUID,
                SpellID = spellId
            };

            player.SendPacket(packet);
        }

        if (change < 0 && Template.DestructibleBuilding.DamageEvent != 0)
            GameEvents.Trigger(Template.DestructibleBuilding.DamageEvent, attackerOrHealer, this);

        var newState = DestructibleState;

        if (GoValueProtected.Building.Health == 0)
            newState = GameObjectDestructibleState.Destroyed;
        else if (GoValueProtected.Building.Health < GoValueProtected.Building.MaxHealth / 2)
            newState = GameObjectDestructibleState.Damaged;
        else if (GoValueProtected.Building.Health == GoValueProtected.Building.MaxHealth)
            newState = GameObjectDestructibleState.Intact;

        if (newState == DestructibleState)
            return;

        SetDestructibleState(newState, attackerOrHealer);
    }

    public void OnLootRelease(Player looter)
    {
        switch (GoType)
        {
            case GameObjectTypes.Chest:
            {
                var goInfo = Template;

                if (goInfo.Chest.consumable == 0 && goInfo.Chest.chestPersonalLoot != 0)
                    DespawnForPlayer(looter,
                                     goInfo.Chest.chestRestockTime != 0
                                         ? TimeSpan.FromSeconds(goInfo.Chest.chestRestockTime)
                                         : TimeSpan.FromSeconds(RespawnDelay)); // not hiding this object permanently to prevent infinite growth of m_perPlayerState

                // while also maintaining some sort of cheater protection (not getting rid of entries on logout)
                break;
            }
            case GameObjectTypes.GatheringNode:
            {
                SetGoStateFor(GameObjectState.Active, looter);

                ObjectFieldData objMask = new();
                GameObjectFieldData goMask = new();
                objMask.MarkChanged(objMask.DynamicFlags);

                UpdateData udata = new(Location.MapId);
                BuildValuesUpdateForPlayerWithMask(udata, objMask.GetUpdateMask(), goMask.GetUpdateMask(), looter);
                udata.BuildPacket(out var packet);
                looter.SendPacket(packet);

                break;
            }
        }
    }

    public void Refresh()
    {
        // not refresh despawned not casted GO (despawned casted GO destroyed in all cases anyway)
        if (RespawnTime > 0 && IsSpawnedByDefault)
            return;

        if (IsSpawned)
            Location.Map.AddToMap(this);
    }

    public void RelocateStationaryPosition(Position pos)
    {
        StationaryPosition.Relocate(pos);
    }

    public void RemoveDynamicFlag(GameObjectDynamicLowFlags flag)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
    }

    public void RemoveFlag(GameObjectFlags flags)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.Flags), (uint)flags);
    }

    public override void RemoveFromWorld()
    {
        //- Remove the gameobject from the accessor
        if (Location.IsInWorld)
            try
            {
                ZoneScript?.OnGameObjectRemove(this);

                RemoveFromOwner();

                if (Model != null)
                    if (Location.Map.ContainsGameObjectModel(Model))
                        Location.Map.RemoveGameObjectModel(Model);

                // If linked trap exists, despawn it
                var linkedTrap = LinkedTrap;

                linkedTrap?.DespawnOrUnsummon();

                base.RemoveFromWorld();

                if (SpawnId != 0)
                    Location.Map.GameObjectBySpawnIdStore.Remove(SpawnId, this);

                Location.Map.ObjectsStore.TryRemove(GUID, out _);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex);
            }
    }

    public void ReplaceAllDynamicFlags(GameObjectDynamicLowFlags flag)
    {
        SetUpdateFieldValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
    }

    public void ReplaceAllFlags(GameObjectFlags flags)
    {
        SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.Flags), (uint)flags);
    }

    public void ResetDoorOrButton()
    {
        if (LootState is LootState.Ready or LootState.JustDeactivated)
            return;

        RemoveFlag(GameObjectFlags.InUse);
        SetGoState(_prevGoState);

        SetLootState(LootState.JustDeactivated);
        _cooldownTime = 0;
    }

    public void Respawn()
    {
        if (IsSpawnedByDefault && RespawnTime > 0)
        {
            RespawnTime = GameTime.CurrentTime;
            Location.Map.Respawn(SpawnObjectType.GameObject, SpawnId);
        }
    }

    public void SaveRespawnTime(uint forceDelay = 0)
    {
        if (GameObjectData != null && (forceDelay != 0 || RespawnTime > GameTime.CurrentTime) && IsSpawnedByDefault)
        {
            if (RespawnCompatibilityMode)
            {
                RespawnInfo ri = new()
                {
                    ObjectType = SpawnObjectType.GameObject,
                    SpawnId = SpawnId,
                    RespawnTime = RespawnTime
                };

                Location.Map.SaveRespawnInfoDB(ri);

                return;
            }

            var thisRespawnTime = forceDelay != 0 ? GameTime.CurrentTime + forceDelay : RespawnTime;
            Location.Map.SaveRespawnTime(SpawnObjectType.GameObject, SpawnId, Entry, thisRespawnTime, GridDefines.ComputeGridCoord(Location.X, Location.Y).GetId());
        }
    }

    public void SaveToDB()
    {
        // this should only be used when the gameobject has already been loaded
        // preferably after adding to map, because mapid may not be valid otherwise
        var data = GameObjectManager.SpawnDataCacheRouter.GameObjectCache.GetGameObjectData(SpawnId);

        if (data == null)
        {
            Log.Logger.Error("GameObject.SaveToDB failed, cannot get gameobject data!");

            return;
        }

        var mapId = Location.MapId;
        var transport = Transport;

        if (transport != null)
            if (transport.MapIdForSpawning >= 0)
                mapId = (uint)transport.MapIdForSpawning;

        SaveToDB(mapId, data.SpawnDifficulties);
    }

    public void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties)
    {
        var goI = Template;

        if (goI == null)
            return;

        if (SpawnId == 0)
            SpawnId = GameObjectManager.GenerateGameObjectSpawnId();

        // update in loaded data (changing data only in this place)
        var data = GameObjectManager.SpawnDataCacheRouter.GameObjectCache.NewOrExistGameObjectData(SpawnId);

        if (data.SpawnId == 0)
            data.SpawnId = SpawnId;

        data.Id = Entry;
        data.MapId = Location.MapId;
        data.SpawnPoint.Relocate(Location);
        data.Rotation = _localRotation;
        data.Spawntimesecs = (int)(IsSpawnedByDefault ? RespawnDelay : -RespawnDelay);
        data.Animprogress = GoAnimProgress;
        data.GoState = GoState;
        data.SpawnDifficulties = spawnDifficulties;
        data.ArtKit = (byte)GoArtKit;

        data.SpawnGroupData ??= GameObjectManager.SpawnGroupDataCache.GetDefaultSpawnGroup();

        data.PhaseId = Location.DBPhase > 0 ? (uint)Location.DBPhase : data.PhaseId;
        data.PhaseGroup = Location.DBPhase < 0 ? (uint)-Location.DBPhase : data.PhaseGroup;

        // Update in DB
        byte index = 0;
        var stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT);
        stmt.AddValue(0, SpawnId);
        WorldDatabase.Execute(stmt);

        stmt = WorldDatabase.GetPreparedStatement(WorldStatements.INS_GAMEOBJECT);
        stmt.AddValue(index++, SpawnId);
        stmt.AddValue(index++, Entry);
        stmt.AddValue(index++, mapid);
        stmt.AddValue(index++, data.SpawnDifficulties.Empty() ? "" : string.Join(",", data.SpawnDifficulties));
        stmt.AddValue(index++, data.PhaseId);
        stmt.AddValue(index++, data.PhaseGroup);
        stmt.AddValue(index++, Location.X);
        stmt.AddValue(index++, Location.Y);
        stmt.AddValue(index++, Location.Z);
        stmt.AddValue(index++, Location.Orientation);
        stmt.AddValue(index++, _localRotation.X);
        stmt.AddValue(index++, _localRotation.Y);
        stmt.AddValue(index++, _localRotation.Z);
        stmt.AddValue(index++, _localRotation.W);
        stmt.AddValue(index++, RespawnDelay);
        stmt.AddValue(index++, GoAnimProgress);
        stmt.AddValue(index++, (byte)GoState);
        WorldDatabase.Execute(stmt);
    }

    public void SendCustomAnim(uint anim)
    {
        GameObjectCustomAnim customAnim = new()
        {
            ObjectGUID = GUID,
            CustomAnim = anim
        };

        SendMessageToSet(customAnim, true);
    }

    public void SendGameObjectDespawn()
    {
        GameObjectDespawn packet = new()
        {
            ObjectGUID = GUID
        };

        SendMessageToSet(packet, true);
    }

    public void SetAnimKitId(ushort animKitId, bool oneshot)
    {
        if (_animKitId == animKitId)
            return;

        if (animKitId != 0 && !CliDB.AnimKitStorage.ContainsKey(animKitId))
            return;

        _animKitId = !oneshot ? animKitId : (ushort)0;

        GameObjectActivateAnimKit activateAnimKit = new()
        {
            ObjectGUID = GUID,
            AnimKitID = animKitId,
            Maintain = !oneshot
        };

        SendMessageToSet(activateAnimKit, true);
    }

    public void SetDestructibleState(GameObjectDestructibleState state, WorldObject attackerOrHealer = null, bool setHealth = false)
    {
        // the user calling this must know he is already operating on destructible gameobject
        switch (state)
        {
            case GameObjectDestructibleState.Intact:
                RemoveFlag(GameObjectFlags.Damaged | GameObjectFlags.Destroyed);
                DisplayId = GoInfoProtected.displayId;

                if (setHealth)
                {
                    GoValueProtected.Building.Health = GoValueProtected.Building.MaxHealth;
                    SetGoAnimProgress(255);
                }

                EnableCollision(true);

                break;

            case GameObjectDestructibleState.Damaged:
            {
                if (Template.DestructibleBuilding.DamagedEvent != 0)
                    GameEvents.Trigger(Template.DestructibleBuilding.DamagedEvent, attackerOrHealer, this);

                AI.Damaged(attackerOrHealer, GoInfoProtected.DestructibleBuilding.DamagedEvent);

                RemoveFlag(GameObjectFlags.Destroyed);
                SetFlag(GameObjectFlags.Damaged);

                var modelId = GoInfoProtected.displayId;

                if (CliDB.DestructibleModelDataStorage.TryGetValue(GoInfoProtected.DestructibleBuilding.DestructibleModelRec, out var modelData))
                    if (modelData.State1Wmo != 0)
                        modelId = modelData.State1Wmo;

                DisplayId = modelId;

                if (setHealth)
                {
                    GoValueProtected.Building.Health = 10000; //m_goInfo.DestructibleBuilding.damagedNumHits;
                    var maxHealth = GoValueProtected.Building.MaxHealth;

                    // in this case current health is 0 anyway so just prevent crashing here
                    if (maxHealth == 0)
                        maxHealth = 1;

                    SetGoAnimProgress(GoValueProtected.Building.Health * 255 / maxHealth);
                }

                break;
            }
            case GameObjectDestructibleState.Destroyed:
            {
                if (Template.DestructibleBuilding.DestroyedEvent != 0)
                    GameEvents.Trigger(Template.DestructibleBuilding.DestroyedEvent, attackerOrHealer, this);

                AI.Destroyed(attackerOrHealer, GoInfoProtected.DestructibleBuilding.DestroyedEvent);

                var player = attackerOrHealer?.CharmerOrOwnerPlayerOrPlayerItself;

                if (player != null)
                {
                    var bg = player.Battleground;

                    bg?.DestroyGate(player, this);
                }

                RemoveFlag(GameObjectFlags.Damaged);
                SetFlag(GameObjectFlags.Destroyed);

                var modelId = GoInfoProtected.displayId;

                if (CliDB.DestructibleModelDataStorage.TryGetValue(GoInfoProtected.DestructibleBuilding.DestructibleModelRec, out var modelData))
                    if (modelData.State2Wmo != 0)
                        modelId = modelData.State2Wmo;

                DisplayId = modelId;

                if (setHealth)
                {
                    GoValueProtected.Building.Health = 0;
                    SetGoAnimProgress(0);
                }

                EnableCollision(false);

                break;
            }
            case GameObjectDestructibleState.Rebuilding:
            {
                if (Template.DestructibleBuilding.RebuildingEvent != 0)
                    GameEvents.Trigger(Template.DestructibleBuilding.RebuildingEvent, attackerOrHealer, this);

                RemoveFlag(GameObjectFlags.Damaged | GameObjectFlags.Destroyed);

                var modelId = GoInfoProtected.displayId;

                if (CliDB.DestructibleModelDataStorage.TryGetValue(GoInfoProtected.DestructibleBuilding.DestructibleModelRec, out var modelData))
                    if (modelData.State3Wmo != 0)
                        modelId = modelData.State3Wmo;

                DisplayId = modelId;

                // restores to full health
                if (setHealth)
                {
                    GoValueProtected.Building.Health = GoValueProtected.Building.MaxHealth;
                    SetGoAnimProgress(255);
                }

                EnableCollision(true);

                break;
            }
        }
    }

    public void SetDynamicFlag(GameObjectDynamicLowFlags flag)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
    }

    public void SetFlag(GameObjectFlags flags)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.Flags), (uint)flags);
    }

    public void SetGoAnimProgress(uint animprogress)
    {
        SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.PercentHealth), (byte)animprogress);
    }

    public void SetGoArtKit(uint artkit, GameObject go, uint lowguid)
    {
        GameObjectData data = null;

        if (go != null)
        {
            go.GoArtKit = artkit;
            data = go.GameObjectData;
        }
        else if (lowguid != 0)
            data = GameObjectManager.SpawnDataCacheRouter.GameObjectCache.GetGameObjectData(lowguid);

        if (data != null)
            data.ArtKit = artkit;
    }

    public void SetGoState(GameObjectState state)
    {
        var oldState = GoState;
        SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.State), (sbyte)state);

        AI?.OnStateChanged(state);

        _goTypeImpl?.OnStateChanged(oldState, state);

        if (Model == null || IsTransport)
            return;

        if (!Location.IsInWorld)
            return;

        // startOpen determines whether we are going to add or remove the LoS on activation
        EnableCollision(state == GameObjectState.Ready);
    }

    public void SetLevel(uint level)
    {
        SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.Level), level);
    }

    public void SetLocalRotation(float qx, float qy, float qz, float qw)
    {
        var rotation = new Quaternion(qx, qy, qz, qw);
        rotation = Quaternion.Multiply(rotation, 1.0f / MathF.Sqrt(Quaternion.Dot(rotation, rotation)));

        _localRotation.X = rotation.X;
        _localRotation.Y = rotation.Y;
        _localRotation.Z = rotation.Z;
        _localRotation.W = rotation.W;
        UpdatePackedRotation();
    }

    public void SetLocalRotationAngles(float zRot, float yRot, float xRot)
    {
        var quat = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(zRot, yRot, xRot));
        SetLocalRotation(quat.X, quat.Y, quat.Z, quat.W);
    }

    public void SetLootState(LootState state, Unit unit = null)
    {
        LootState = state;
        _lootStateUnitGuid = unit?.GUID ?? ObjectGuid.Empty;
        AI.OnLootStateChanged((uint)state, unit);

        // Start restock timer if the chest is partially looted or not looted at all
        if (GoType == GameObjectTypes.Chest && state == LootState.Activated && Template.Chest.chestRestockTime > 0 && _restockTime == 0)
            _restockTime = GameTime.CurrentTime + Template.Chest.chestRestockTime;

        // only set collision for doors on SetGoState
        if (GoType == GameObjectTypes.Door)
            return;

        if (Model != null)
            EnableCollision((GoState != GameObjectState.Ready && state is LootState.Activated or LootState.JustDeactivated) || state == LootState.Ready);
    }

    public void SetOwnerGUID(ObjectGuid owner)
    {
        // Owner already found and different than expected owner - remove object from old owner
        if (!owner.IsEmpty && !OwnerGUID.IsEmpty && OwnerGUID != owner)
            Log.Logger.Warning("Owner already found and different than expected owner - remove object from old owner");
        else
        {
            IsSpawnedByDefault = false; // all object with owner is despawned after delay
            SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.CreatedBy), owner);
        }
    }

    public void SetParentRotation(Quaternion rotation)
    {
        SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.ParentRotation), rotation);
    }

    public void SetPathProgressForClient(float progress)
    {
        DoWithSuppressingObjectUpdates(() =>
        {
            ObjectFieldData dynflagMask = new();
            dynflagMask.MarkChanged(ObjectData.DynamicFlags);
            var marked = (ObjectData.GetUpdateMask() & dynflagMask.GetUpdateMask()).IsAnySet();

            var dynamicFlags = (uint)GetDynamicFlags();
            dynamicFlags &= 0xFFFF; // remove high bits
            dynamicFlags |= (uint)(progress * 65535.0f) << 16;
            ReplaceAllDynamicFlags((GameObjectDynamicLowFlags)dynamicFlags);

            if (!marked)
                ObjectData.ClearChanged(ObjectData.DynamicFlags);
        });
    }

    public void SetRespawnTime(int respawn)
    {
        RespawnTime = respawn > 0 ? GameTime.CurrentTime + respawn : 0;
        RespawnDelay = (uint)(respawn > 0 ? respawn : 0);

        if (respawn != 0 && !IsSpawnedByDefault)
            UpdateObjectVisibility();
    }

    public void SetSpawnedByDefault(bool b)
    {
        IsSpawnedByDefault = b;
    }

    public void SetSpellVisualId(uint spellVisualId, ObjectGuid activatorGuid = default)
    {
        SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.SpellVisualID), spellVisualId);

        GameObjectPlaySpellVisual packet = new()
        {
            ObjectGUID = GUID,
            ActivatorGUID = activatorGuid,
            SpellVisualID = spellVisualId
        };

        SendMessageToSet(packet, true);
    }

    public ITransport ToTransportBase()
    {
        return GoType switch
        {
            GameObjectTypes.Transport => (TransportGameObject)_goTypeImpl,
            GameObjectTypes.MapObjTransport => (Transport)this,
            _ => null
        };
    }

    public void TriggeringLinkedGameObject(uint trapEntry, Unit target)
    {
        var trapInfo = GameObjectManager.GameObjectTemplateCache.GetGameObjectTemplate(trapEntry);

        if (trapInfo is not { type: GameObjectTypes.Trap })
            return;

        var trapSpell = SpellManager.GetSpellInfo(trapInfo.Trap.spell, Location.Map.DifficultyID);

        if (trapSpell == null) // checked at load already
            return;

        LinkedTrap?.SpellFactory.CastSpell(target, trapSpell.Id);
    }

    public override void Update(uint diff)
    {
        base.Update(diff);

        if (AI != null)
            AI.UpdateAI(diff);
        else if (!AIM_Initialize())
            Log.Logger.Error("Could not initialize GameObjectAI");

        if (_despawnDelay != 0)
        {
            if (_despawnDelay > diff)
                _despawnDelay -= diff;
            else
            {
                _despawnDelay = 0;
                DespawnOrUnsummon(TimeSpan.FromMilliseconds(0), _despawnRespawnTime);
            }
        }

        _goTypeImpl?.Update(diff);

        if (_perPlayerState != null)
            foreach (var (guid, playerState) in _perPlayerState.ToList())
            {
                if (playerState.ValidUntil > GameTime.SystemTime)
                    continue;

                var seer = ObjectAccessor.GetPlayer(this, guid);
                var needsStateUpdate = playerState.State != GoState;
                var despawned = playerState.Despawned;

                _perPlayerState.Remove(guid);

                if (seer == null)
                    continue;

                if (despawned)
                    seer.UpdateVisibilityOf(this);
                else if (needsStateUpdate)
                {
                    ObjectFieldData objMask = new();
                    GameObjectFieldData goMask = new();
                    goMask.MarkChanged(GameObjectFieldData.State);

                    UpdateData udata = new(Location.MapId);
                    BuildValuesUpdateForPlayerWithMask(udata, objMask.GetUpdateMask(), goMask.GetUpdateMask(), seer);
                    udata.BuildPacket(out var packet);
                    seer.SendPacket(packet);
                }
            }

        switch (LootState)
        {
            case LootState.NotReady:
            {
                switch (GoType)
                {
                    case GameObjectTypes.Trap:
                    {
                        // Arming Time for GAMEOBJECT_TYPE_TRAP (6)
                        var goInfo = Template;

                        // Bombs
                        var owner = OwnerUnit;

                        if (goInfo.Trap.charges == 2)
                            _cooldownTime = GameTime.CurrentTimeMS + 10 * Time.IN_MILLISECONDS; // Hardcoded tooltip value
                        else if (owner != null)
                            if (owner.IsInCombat)
                                _cooldownTime = GameTime.CurrentTimeMS + goInfo.Trap.startDelay * Time.IN_MILLISECONDS;

                        LootState = LootState.Ready;

                        break;
                    }
                    case GameObjectTypes.FishingNode:
                    {
                        // fishing code (bobber ready)
                        if (GameTime.CurrentTime <= RespawnTime - 5)
                            return;

                        // splash bobber (bobber ready now)
                        var caster = OwnerUnit;

                        if (caster != null && caster.IsTypeId(TypeId.Player))
                            SendCustomAnim(0);

                        LootState = LootState.Ready; // can be successfully open with some chance

                        return;
                    }
                    case GameObjectTypes.Chest:
                        if (_restockTime > GameTime.CurrentTime)
                            return;

                        // If there is no restock timer, or if the restock timer passed, the chest becomes ready to loot
                        _restockTime = 0;
                        LootState = LootState.Ready;
                        ClearLoot();
                        UpdateDynamicFlagsForNearbyPlayers();

                        break;

                    default:
                        LootState = LootState.Ready; // for other GOis same switched without delay to GO_READY

                        break;
                }
            }

            goto case LootState.Ready;
            case LootState.Ready:
            {
                if (RespawnCompatibilityMode)
                    if (RespawnTime > 0) // timer on
                    {
                        var now = GameTime.CurrentTime;

                        if (RespawnTime <= now) // timer expired
                        {
                            var dbtableHighGuid = ObjectGuid.Create(HighGuid.GameObject, Location.MapId, Entry, SpawnId);
                            var linkedRespawntime = Location.Map.GetLinkedRespawnTime(dbtableHighGuid);

                            if (linkedRespawntime != 0) // Can't respawn, the master is dead
                            {
                                var targetGuid = CreatureDataCache.GetLinkedRespawnGuid(dbtableHighGuid);

                                if (targetGuid == dbtableHighGuid) // if linking self, never respawn (check delayed to next day)
                                    SetRespawnTime(Time.WEEK);
                                else
                                    RespawnTime = (now > linkedRespawntime ? now : linkedRespawntime) + RandomHelper.IRand(5, Time.MINUTE); // else copy time from master and add a little

                                SaveRespawnTime();

                                return;
                            }

                            RespawnTime = 0;
                            _skillupList.Clear();
                            UseCount = 0;

                            switch (GoType)
                            {
                                case GameObjectTypes.FishingNode: //  can't fish now
                                {
                                    var caster = OwnerUnit;

                                    if (caster != null && caster.IsTypeId(TypeId.Player))
                                    {
                                        caster.AsPlayer.RemoveGameObject(this, false);
                                        caster.AsPlayer.SendPacket(new FishEscaped());
                                    }

                                    // can be delete
                                    LootState = LootState.JustDeactivated;

                                    return;
                                }
                                case GameObjectTypes.Door:
                                case GameObjectTypes.Button:
                                    //we need to open doors if they are closed (add there another condition if this code breaks some usage, but it need to be here for Battlegrounds)
                                    if (GoState != GameObjectState.Ready)
                                        ResetDoorOrButton();

                                    break;

                                case GameObjectTypes.FishingHole:
                                    // Initialize a new max fish count on respawn
                                    GoValueProtected.FishingHole.MaxOpens = RandomHelper.URand(Template.FishingHole.minRestock, Template.FishingHole.maxRestock);

                                    break;
                            }

                            if (!IsSpawnedByDefault) // despawn timer
                            {
                                // can be despawned or destroyed
                                SetLootState(LootState.JustDeactivated);

                                return;
                            }

                            // Call AI Reset (required for example in SmartAI to clear one time events)
                            AI?.Reset();

                            // respawn timer
                            var poolid = GameObjectData?.PoolId ?? 0;

                            if (poolid != 0)
                                PoolManager.UpdatePool<GameObject>(Location.Map.PoolData, poolid, SpawnId);
                            else
                                Location.Map.AddToMap(this);
                        }
                    }

                // Set respawn timer
                if (!RespawnCompatibilityMode && RespawnTime > 0)
                    SaveRespawnTime();

                if (IsSpawned)
                {
                    var goInfo = Template;
                    uint maxCharges;

                    if (goInfo.type == GameObjectTypes.Trap)
                    {
                        if (GameTime.CurrentTimeMS < _cooldownTime)
                            break;

                        // Type 2 (bomb) does not need to be triggered by a unit and despawns after casting its spell.
                        if (goInfo.Trap.charges == 2)
                        {
                            SetLootState(LootState.Activated);

                            break;
                        }

                        // Type 0 despawns after being triggered, type 1 does not.
                        // @todo This is activation radius. Casting radius must be selected from spell
                        float radius;

                        if (goInfo.Trap.radius == 0f)
                        {
                            // Battlegroundgameobjects have data2 == 0 && data5 == 3
                            if (goInfo.Trap.cooldown != 3)
                                break;

                            radius = 3.0f;
                        }
                        else
                            radius = goInfo.Trap.radius / 2.0f;

                        Unit target;

                        // @todo this hack with search required until GO casting not implemented
                        if (OwnerUnit != null || goInfo.Trap.Checkallunits != 0)
                        {
                            // Hunter trap: Search units which are unfriendly to the trap's owner
                            var checker = new NearestAttackableNoTotemUnitInObjectRangeCheck(this, radius);
                            var searcher = new UnitLastSearcher(this, checker, GridType.All);
                            CellCalculator.VisitGrid(this, searcher, radius);
                            target = searcher.GetTarget();
                        }
                        else
                        {
                            // Environmental trap: Any player
                            var check = new AnyPlayerInObjectRangeCheck(this, radius);
                            var searcher = new PlayerSearcher(this, check, GridType.World);
                            CellCalculator.VisitGrid(this, searcher, radius);
                            target = searcher.GetTarget();
                        }

                        if (target != null)
                            SetLootState(LootState.Activated, target);
                    }
                    else if (goInfo.type == GameObjectTypes.CapturePoint)
                    {
                        var hordeCapturing = GoValueProtected.CapturePoint.State == BattlegroundCapturePointState.ContestedHorde;
                        var allianceCapturing = GoValueProtected.CapturePoint.State == BattlegroundCapturePointState.ContestedAlliance;

                        if (hordeCapturing || allianceCapturing)
                        {
                            if (GoValueProtected.CapturePoint.AssaultTimer <= diff)
                            {
                                GoValueProtected.CapturePoint.State = hordeCapturing ? BattlegroundCapturePointState.HordeCaptured : BattlegroundCapturePointState.AllianceCaptured;

                                if (hordeCapturing)
                                {
                                    GoValueProtected.CapturePoint.State = BattlegroundCapturePointState.HordeCaptured;
                                    var map = Location.Map.ToBattlegroundMap;

                                    var bg = map?.BG;

                                    if (bg != null)
                                    {
                                        if (goInfo.CapturePoint.CaptureEventHorde != 0)
                                            GameEvents.Trigger(goInfo.CapturePoint.CaptureEventHorde, this, this);

                                        bg.SendBroadcastText(Template.CapturePoint.CaptureBroadcastHorde, ChatMsg.BgSystemHorde);
                                    }
                                }
                                else
                                {
                                    GoValueProtected.CapturePoint.State = BattlegroundCapturePointState.AllianceCaptured;
                                    var map = Location.Map.ToBattlegroundMap;

                                    var bg = map?.BG;

                                    if (bg != null)
                                    {
                                        if (goInfo.CapturePoint.CaptureEventAlliance != 0)
                                            GameEvents.Trigger(goInfo.CapturePoint.CaptureEventAlliance, this, this);

                                        bg.SendBroadcastText(Template.CapturePoint.CaptureBroadcastAlliance, ChatMsg.BgSystemAlliance);
                                    }
                                }

                                GoValueProtected.CapturePoint.LastTeamCapture = hordeCapturing ? TeamIds.Horde : TeamIds.Alliance;
                                UpdateCapturePoint();
                            }
                            else
                                GoValueProtected.CapturePoint.AssaultTimer -= diff;
                        }
                    }
                    else if ((maxCharges = goInfo.GetCharges()) != 0)
                        if (UseCount >= maxCharges)
                        {
                            UseCount = 0;
                            SetLootState(LootState.JustDeactivated); // can be despawned or destroyed
                        }
                }

                break;
            }
            case LootState.Activated:
            {
                switch (GoType)
                {
                    case GameObjectTypes.Door:
                    case GameObjectTypes.Button:
                        if (_cooldownTime != 0 && GameTime.CurrentTimeMS >= _cooldownTime)
                            ResetDoorOrButton();

                        break;

                    case GameObjectTypes.Goober:
                        if (GameTime.CurrentTimeMS >= _cooldownTime)
                        {
                            RemoveFlag(GameObjectFlags.InUse);

                            SetLootState(LootState.JustDeactivated);
                            _cooldownTime = 0;
                        }

                        break;

                    case GameObjectTypes.Chest:
                        Loot?.Update();

                        foreach (var (_, loot) in _personalLoot)
                            loot.Update();

                        // Non-consumable chest was partially looted and restock time passed, restock all loot now
                        if (Template.Chest.consumable == 0 && Template.Chest.chestRestockTime != 0 && GameTime.CurrentTime >= _restockTime)
                        {
                            _restockTime = 0;
                            LootState = LootState.Ready;
                            ClearLoot();
                            UpdateDynamicFlagsForNearbyPlayers();
                        }

                        break;

                    case GameObjectTypes.Trap:
                    {
                        var goInfo = Template;
                        var target = ObjectAccessor.GetUnit(this, _lootStateUnitGuid);

                        if (goInfo.Trap.charges == 2 && goInfo.Trap.spell != 0)
                        {
                            //todo NULL target won't work for target type 1
                            SpellFactory.CastSpell(goInfo.Trap.spell);
                            SetLootState(LootState.JustDeactivated);
                        }
                        else if (target != null)
                        {
                            // Some traps do not have a spell but should be triggered
                            CastSpellExtraArgs args = new();
                            args.SetOriginalCaster(OwnerGUID);

                            if (goInfo.Trap.spell != 0)
                                SpellFactory.CastSpell(target, goInfo.Trap.spell, args);

                            // Template value or 4 seconds
                            _cooldownTime = (GameTime.CurrentTimeMS + (goInfo.Trap.cooldown != 0 ? goInfo.Trap.cooldown : 4u)) * Time.IN_MILLISECONDS;

                            switch (goInfo.Trap.charges)
                            {
                                case 1:
                                    SetLootState(LootState.JustDeactivated);

                                    break;

                                case 0:
                                    SetLootState(LootState.Ready);

                                    break;
                            }

                            // Battleground gameobjects have data2 == 0 && data5 == 3
                            if (goInfo.Trap is { radius: 0, cooldown: 3 })
                                target.AsPlayer?.Battleground?.HandleTriggerBuff(GUID);
                        }

                        break;
                    }
                }

                break;
            }
            case LootState.JustDeactivated:
            {
                // If nearby linked trap exists, despawn it
                LinkedTrap?.DespawnOrUnsummon();

                //if Gameobject should cast spell, then this, but some GOs (type = 10) should be destroyed
                if (GoType == GameObjectTypes.Goober)
                {
                    var spellId = Template.Goober.spell;

                    if (spellId != 0)
                    {
                        foreach (var id in _uniqueUsers)
                            ObjectAccessor.GetPlayer(this, id)?.SpellFactory?.CastSpell(spellId);

                        _uniqueUsers.Clear();
                        UseCount = 0;
                    }

                    // Only goobers with a lock id or a reset time may reset their go state
                    if (Template.GetLockId() != 0 || Template.GetAutoCloseTime() != 0)
                        SetGoState(GameObjectState.Ready);

                    //any return here in case Battleground traps
                    var goOverride = GameObjectOverride;

                    if (goOverride != null && goOverride.Flags.HasFlag(GameObjectFlags.NoDespawn))
                        return;
                }

                ClearLoot();

                // Do not delete chests or goobers that are not consumed on loot, while still allowing them to despawn when they expire if summoned
                var isSummonedAndExpired = (OwnerUnit != null || SpellId != 0) && RespawnTime == 0;

                if (GoType is GameObjectTypes.Chest or GameObjectTypes.Goober && !Template.IsDespawnAtAction() && !isSummonedAndExpired)
                {
                    if (GoType == GameObjectTypes.Chest && Template.Chest.chestRestockTime > 0)
                    {
                        // Start restock timer when the chest is fully looted
                        _restockTime = GameTime.CurrentTime + Template.Chest.chestRestockTime;
                        SetLootState(LootState.NotReady);
                        UpdateDynamicFlagsForNearbyPlayers();
                    }
                    else
                        SetLootState(LootState.Ready);

                    UpdateObjectVisibility();

                    return;
                }

                if (!OwnerGUID.IsEmpty || SpellId != 0)
                {
                    SetRespawnTime(0);
                    Delete();

                    return;
                }

                SetLootState(LootState.NotReady);

                //burning flags in some Battlegrounds, if you find better condition, just add it
                if (Template.IsDespawnAtAction() || GoAnimProgress > 0)
                {
                    SendGameObjectDespawn();
                    //reset flags
                    var goOverride = GameObjectOverride;

                    if (goOverride != null)
                        ReplaceAllFlags(goOverride.Flags);
                }

                if (RespawnDelay == 0)
                    return;

                if (!IsSpawnedByDefault)
                {
                    RespawnTime = 0;

                    if (SpawnId != 0)
                        UpdateObjectVisibilityOnDestroy();
                    else
                        Delete();

                    return;
                }

                var respawnDelay = RespawnDelay;
                var scalingMode = Configuration.GetDefaultValue("Respawn:DynamicMode", 0u);

                if (scalingMode != 0)
                    Location.Map.ApplyDynamicModeRespawnScaling(this, SpawnId, ref respawnDelay, scalingMode);

                RespawnTime = GameTime.CurrentTime + respawnDelay;

                // if option not set then object will be saved at grid unload
                // Otherwise just save respawn time to map object memory
                SaveRespawnTime();

                if (RespawnCompatibilityMode)
                    UpdateObjectVisibilityOnDestroy();
                else
                    Location.AddObjectToRemoveList();

                break;
            }
        }
    }

    public void UpdateModelPosition()
    {
        if (Model == null)
            return;

        if (Location.Map.ContainsGameObjectModel(Model))
        {
            Location.Map.RemoveGameObjectModel(Model);
            Model.UpdatePosition();
            Location.Map.InsertGameObjectModel(Model);
        }
    }

    public void UpdatePackedRotation()
    {
        const int packYz = 1 << 20;
        const int packX = packYz << 1;

        const int packYzMask = (packYz << 1) - 1;
        const int packXMask = (packX << 1) - 1;

        var wSign = (sbyte)(_localRotation.W >= 0.0f ? 1 : -1);
        long x = (int)(_localRotation.X * packX) * wSign & packXMask;
        long y = (int)(_localRotation.Y * packYz) * wSign & packYzMask;
        long z = (int)(_localRotation.Z * packYz) * wSign & packYzMask;
        PackedLocalRotation = z | (y << 21) | (x << 42);
    }

    public void Use(Unit user)
    {
        // by default spell caster is user
        var spellCaster = user;
        uint spellId = 0;
        var triggered = false;

        var playerUser = user.AsPlayer;

        if (playerUser != null)
        {
            if (GoInfoProtected.GetNoDamageImmune() != 0 && playerUser.HasUnitFlag(UnitFlags.Immune))
                return;

            if (!GoInfoProtected.IsUsableMounted())
                playerUser.RemoveAurasByType(AuraType.Mounted);

            playerUser.PlayerTalkClass.ClearMenus();

            if (AI.OnGossipHello(playerUser))
                return;
        }

        // If cooldown data present in template
        var cooldown = Template.GetCooldown();

        if (cooldown != 0)
        {
            if (_cooldownTime > GameTime.CurrentTime)
                return;

            _cooldownTime = GameTime.CurrentTimeMS + cooldown * Time.IN_MILLISECONDS;
        }

        switch (GoType)
        {
            case GameObjectTypes.Door:   //0
            case GameObjectTypes.Button: //1
                //doors/buttons never really despawn, only reset to default state/flags
                UseDoorOrButton(0, false, user);

                return;

            case GameObjectTypes.QuestGiver: //2
            {
                if (!user.IsTypeId(TypeId.Player))
                    return;

                var player = user.AsPlayer;

                player.PrepareGossipMenu(this, Template.QuestGiver.gossipID, true);
                player.SendPreparedGossip(this);

                return;
            }
            case GameObjectTypes.Chest: //3
            {
                var player = user.AsPlayer;

                if (player == null)
                    return;

                var bg = player.Battleground;

                if (bg != null && !bg.CanActivateGO((int)Entry, (uint)bg.GetPlayerTeam(user.GUID)))
                    return;

                var info = Template;

                if (Loot == null && info.GetLootId() != 0)
                {
                    if (info.GetLootId() != 0)
                    {
                        var group = player.Group;
                        var groupRules = group != null && info.Chest.usegrouplootrules != 0;

                        Loot = LootFactory.GenerateLoot(Location.Map, GUID, LootType.Chest, groupRules ? group : null, info.Chest.DungeonEncounter, info.GetLootId(), LootStorageType.Gameobject, player, !groupRules, false, LootMode, Location.Map.GetDifficultyLootItemContext());

                        if (LootMode > 0)
                        {
                            var addon = TemplateAddon;

                            if (addon != null)
                                Loot.GenerateMoneyLoot(addon.Mingold, addon.Maxgold);
                        }
                    }

                    // @todo possible must be moved to loot release (in different from linked triggering)
                    if (info.Chest.triggeredEvent != 0)
                        GameEvents.Trigger(info.Chest.triggeredEvent, player, this);

                    // triggering linked GO
                    var trapEntry = info.Chest.linkedTrap;

                    if (trapEntry != 0)
                        TriggeringLinkedGameObject(trapEntry, player);
                }
                else if (!_personalLoot.ContainsKey(player.GUID))
                    if (info.Chest.chestPersonalLoot != 0)
                    {
                        var addon = TemplateAddon;

                        if (info.Chest.DungeonEncounter != 0)
                        {
                            List<Player> tappers = new();

                            foreach (var tapperGuid in TapList)
                            {
                                var tapper = ObjectAccessor.GetPlayer(this, tapperGuid);

                                if (tapper != null)
                                    tappers.Add(tapper);
                            }

                            if (tappers.Empty())
                                tappers.Add(player);

                            _personalLoot = LootManager.GenerateDungeonEncounterPersonalLoot(info.Chest.DungeonEncounter,
                                                                                              info.Chest.chestPersonalLoot,
                                                                                              LootStoreBox.Gameobject,
                                                                                              LootType.Chest,
                                                                                              this,
                                                                                              addon?.Mingold ?? 0,
                                                                                              addon?.Maxgold ?? 0,
                                                                                              (ushort)LootMode,
                                                                                              Location.Map.GetDifficultyLootItemContext(),
                                                                                              tappers);
                        }
                        else
                        {
                            var loot = LootFactory.GenerateLoot(Location.Map, GUID, LootType.Chest);
                            _personalLoot[player.GUID] = loot;

                            loot.DungeonEncounterId = info.Chest.DungeonEncounter;
                            loot.FillLoot(info.Chest.chestPersonalLoot, LootStoreBox.Gameobject, player, true, false, LootMode, Location.Map.GetDifficultyLootItemContext());

                            if (LootMode > 0 && addon != null)
                                loot.GenerateMoneyLoot(addon.Mingold, addon.Maxgold);
                        }
                    }

                if (!_uniqueUsers.Contains(player.GUID) && info.GetLootId() == 0)
                {
                    if (info.Chest.chestPushLoot != 0)
                    {
                        var pushLoot = LootFactory.GenerateLoot(Location.Map, GUID, LootType.Chest);
                        pushLoot.FillLoot(info.Chest.chestPushLoot, LootStoreBox.Gameobject, player, true, false, LootMode, Location.Map.GetDifficultyLootItemContext());
                        pushLoot.AutoStore(player, ItemConst.NullBag, ItemConst.NullSlot);
                    }

                    if (info.Chest.triggeredEvent != 0)
                        GameEvents.Trigger(info.Chest.triggeredEvent, player, this);

                    // triggering linked GO
                    var trapEntry = info.Chest.linkedTrap;

                    if (trapEntry != 0)
                        TriggeringLinkedGameObject(trapEntry, player);

                    AddUniqueUse(player);
                }

                if (LootState != LootState.Activated)
                    SetLootState(LootState.Activated, player);

                // Send loot
                var playerLoot = GetLootForPlayer(player);

                if (playerLoot != null)
                    player.SendLoot(playerLoot);

                break;
            }
            case GameObjectTypes.Trap: //6
            {
                var goInfo = Template;

                if (goInfo.Trap.spell != 0)
                    SpellFactory.CastSpell(user, goInfo.Trap.spell);

                _cooldownTime = GameTime.CurrentTimeMS + (goInfo.Trap.cooldown != 0 ? goInfo.Trap.cooldown : 4) * Time.IN_MILLISECONDS; // template or 4 seconds

                if (goInfo.Trap.charges == 1) // Deactivate after trigger
                    SetLootState(LootState.JustDeactivated);

                return;
            }
            //Sitting: Wooden bench, chairs enzz
            case GameObjectTypes.Chair: //7
            {
                var info = Template;

                if (_chairListSlots.Empty()) // this is called once at first chair use to make list of available slots
                {
                    if (info.Chair.chairslots > 0) // sometimes chairs in DB have error in fields and we dont know number of slots
                        for (uint i = 0; i < info.Chair.chairslots; ++i)
                            _chairListSlots[i] = default; // Last user of current slot set to 0 (none sit here yet)
                    else
                        _chairListSlots[0] = default; // error in DB, make one default slot
                }

                // a chair may have n slots. we have to calculate their positions and teleport the player to the nearest one
                var lowestDist = SharedConst.DefaultVisibilityDistance;

                uint nearestSlot = 0;
                var xLowest = Location.X;
                var yLowest = Location.Y;

                // the object orientation + 1/2 pi
                // every slot will be on that straight line
                var orthogonalOrientation = Location.Orientation + MathFunctions.PI * 0.5f;
                // find nearest slot
                var foundFreeSlot = false;

                foreach (var (slot, sittingUnit) in _chairListSlots.ToList())
                {
                    // the distance between this slot and the center of the go - imagine a 1D space
                    var relativeDistance = info.size * slot - info.size * (info.Chair.chairslots - 1) / 2.0f;

                    var xI = (float)(Location.X + relativeDistance * Math.Cos(orthogonalOrientation));
                    var yI = (float)(Location.Y + relativeDistance * Math.Sin(orthogonalOrientation));

                    if (!sittingUnit.IsEmpty)
                    {
                        var chairUser = ObjectAccessor.GetUnit(this, sittingUnit);

                        if (chairUser != null)
                        {
                            if (chairUser.IsSitState && chairUser.StandState != UnitStandStateType.Sit && chairUser.Location.GetExactDist2d(xI, yI) < 0.1f)
                                continue; // This seat is already occupied by ChairUser. NOTE: Not sure if the ChairUser.getStandState() != UNIT_STAND_STATE_SIT check is required.

                            _chairListSlots[slot].Clear(); // This seat is unoccupied.
                        }
                        else
                            _chairListSlots[slot].Clear(); // The seat may of had an occupant, but they're offline.
                    }

                    foundFreeSlot = true;

                    // calculate the distance between the player and this slot
                    var thisDistance = user.Location.GetDistance2d(xI, yI);

                    if (thisDistance <= lowestDist)
                    {
                        nearestSlot = slot;
                        lowestDist = thisDistance;
                        xLowest = xI;
                        yLowest = yI;
                    }
                }

                if (foundFreeSlot)
                    if (!_chairListSlots.ContainsKey(nearestSlot))
                    {
                        _chairListSlots[nearestSlot] = user.GUID; //this slot in now used by player
                        user.NearTeleportTo(xLowest, yLowest, Location.Z, Location.Orientation);
                        user.SetStandState(UnitStandStateType.SitLowChair + (byte)info.Chair.chairheight);

                        if (info.Chair.triggeredEvent != 0)
                            GameEvents.Trigger(info.Chair.triggeredEvent, user, this);

                        return;
                    }

                return;
            }
            case GameObjectTypes.SpellFocus: //8
            {
                // triggering linked GO
                var trapEntry = Template.SpellFocus.linkedTrap;

                if (trapEntry != 0)
                    TriggeringLinkedGameObject(trapEntry, user);

                break;
            }
            //big gun, its a spell/aura
            case GameObjectTypes.Goober: //10
            {
                var info = Template;
                var player = user.AsPlayer;

                if (player != null)
                {
                    if (info.Goober.pageID != 0) // show page...
                    {
                        PageTextPkt data = new()
                        {
                            GameObjectGUID = GUID
                        };

                        player.SendPacket(data);
                    }
                    else if (info.Goober.gossipID != 0)
                    {
                        player.PrepareGossipMenu(this, info.Goober.gossipID);
                        player.SendPreparedGossip(this);
                    }

                    if (info.Goober.eventID != 0)
                    {
                        Log.Logger.Debug("Goober ScriptStart id {0} for GO entry {1} (GUID {2}).", info.Goober.eventID, Entry, SpawnId);
                        GameEvents.Trigger(info.Goober.eventID, player, this);
                    }

                    // possible quest objective for active quests
                    if (info.Goober.questID != 0 && GameObjectManager.QuestTemplateCache.GetQuestTemplate(info.Goober.questID) != null)
                        //QuestId require to be active for GO using
                        if (player.GetQuestStatus(info.Goober.questID) != QuestStatus.Incomplete)
                            break;

                    var group = player.Group;

                    if (group != null)
                        for (var refe = group.FirstMember; refe != null; refe = refe.Next())
                        {
                            var member = refe.Source;

                            if (member == null)
                                continue;

                            if (member.IsAtGroupRewardDistance(this))
                                member.KillCreditGO(info.entry, GUID);
                        }
                    else
                        player.KillCreditGO(info.entry, GUID);
                }

                var trapEntry = info.Goober.linkedTrap;

                if (trapEntry != 0)
                    TriggeringLinkedGameObject(trapEntry, user);

                if (info.Goober.AllowMultiInteract != 0 && player != null)
                {
                    if (info.IsDespawnAtAction())
                        DespawnForPlayer(player, TimeSpan.FromSeconds(RespawnDelay));
                    else
                        SetGoStateFor(GameObjectState.Active, player);
                }
                else
                {
                    SetFlag(GameObjectFlags.InUse);
                    SetLootState(LootState.Activated, user);

                    // this appear to be ok, however others exist in addition to this that should have custom (ex: 190510, 188692, 187389)
                    if (info.Goober.customAnim != 0)
                        SendCustomAnim(GoAnimProgress);
                    else
                        SetGoState(GameObjectState.Active);

                    _cooldownTime = GameTime.CurrentTimeMS + info.GetAutoCloseTime();
                }

                // cast this spell later if provided
                spellId = info.Goober.spell;

                if (info.Goober.playerCast == 0)
                    spellCaster = null;

                break;
            }
            case GameObjectTypes.Camera: //13
            {
                var info = Template;

                if (info == null)
                    return;

                if (!user.IsTypeId(TypeId.Player))
                    return;

                var player = user.AsPlayer;

                if (info.Camera._camera != 0)
                    player.SendCinematicStart(info.Camera._camera);

                if (info.Camera.eventID != 0)
                    GameEvents.Trigger(info.Camera.eventID, player, this);

                return;
            }
            //fishing bobber
            case GameObjectTypes.FishingNode: //17
            {
                var player = user.AsPlayer;

                if (player == null)
                    return;

                if (player.GUID != OwnerGUID)
                    return;

                switch (LootState)
                {
                    case LootState.Ready: // ready for loot
                    {
                        SetLootState(LootState.Activated, player);

                        SetGoState(GameObjectState.Active);
                        ReplaceAllFlags(GameObjectFlags.InMultiUse);

                        SendUpdateToPlayer(player);
                        var zoneSkill = GameObjectManager.GetFishingBaseSkillLevel(Location.Area);

                        if (zoneSkill == 0)
                            zoneSkill = GameObjectManager.GetFishingBaseSkillLevel(Location.Zone);

                        //provide error, no fishable zone or area should be 0
                        if (zoneSkill == 0)
                            Log.Logger.Error("Fishable areaId {0} are not properly defined in `skill_fishing_base_level`.", Location.Area);

                        int skill = player.GetSkillValue(SkillType.ClassicFishing);

                        int chance;

                        if (skill < zoneSkill)
                        {
                            chance = (int)(Math.Pow((double)skill / zoneSkill, 2) * 100);

                            if (chance < 1)
                                chance = 1;
                        }
                        else
                            chance = 100;

                        var roll = RandomHelper.IRand(1, 100);

                        Log.Logger.Debug("Fishing check (skill: {0} zone min skill: {1} chance {2} roll: {3}", skill, zoneSkill, chance, roll);

                        player.UpdateFishingSkill();

                        // @todo find reasonable value for fishing hole search
                        var fishingPool = LookupFishingHoleAround(20.0f + SharedConst.ContactDistance);

                        // If fishing skill is high enough, or if fishing on a pool, send correct loot.
                        // Fishing pools have no skill requirement as of patch 3.3.0 (undocumented change).
                        if (chance >= roll || fishingPool != null)
                        {
                            // @todo I do not understand this hack. Need some explanation.
                            // prevent removing GO at spell cancel
                            RemoveFromOwner();
                            SetOwnerGUID(player.GUID);

                            if (fishingPool != null)
                            {
                                fishingPool.Use(player);
                                SetLootState(LootState.JustDeactivated);
                            }
                            else
                            {
                                Loot = GetFishLoot(player);
                                player.SendLoot(Loot);
                            }
                        }
                        else // If fishing skill is too low, send junk loot.
                        {
                            Loot = GetFishLootJunk(player);
                            player.SendLoot(Loot);
                        }

                        break;
                    }
                    case LootState.JustDeactivated: // nothing to do, will be deleted at next update
                        break;

                    default:
                    {
                        SetLootState(LootState.JustDeactivated);
                        player.SendPacket(new FishNotHooked());

                        break;
                    }
                }

                player.FinishSpell(CurrentSpellTypes.Channeled);

                return;
            }

            case GameObjectTypes.Ritual: //18
            {
                if (!user.IsTypeId(TypeId.Player))
                    return;

                var player = user.AsPlayer;

                var owner = OwnerUnit;

                var info = Template;

                // ritual owner is set for GO's without owner (not summoned)
                if (_ritualOwner == null && owner == null)
                    _ritualOwner = player;

                if (owner != null)
                {
                    if (!owner.IsTypeId(TypeId.Player))
                        return;

                    // accept only use by player from same group as owner, excluding owner itself (unique use already added in spell effect)
                    if (player == owner.AsPlayer || (info.Ritual.castersGrouped != 0 && !player.IsInSameRaidWith(owner.AsPlayer)))
                        return;

                    // expect owner to already be channeling, so if not...
                    if (owner.GetCurrentSpell(CurrentSpellTypes.Channeled) == null)
                        return;

                    // in case summoning ritual caster is GO creator
                    spellCaster = owner;
                }
                else
                {
                    if (player != _ritualOwner && info.Ritual.castersGrouped != 0 && !player.IsInSameRaidWith(_ritualOwner))
                        return;

                    spellCaster = player;
                }

                AddUniqueUse(player);

                if (info.Ritual.animSpell != 0)
                {
                    player.SpellFactory.CastSpell(player, info.Ritual.animSpell, true);

                    // for this case, summoningRitual.spellId is always triggered
                    triggered = true;
                }

                // full amount unique participants including original summoner
                if (GetUniqueUseCount() == info.Ritual.casters)
                {
                    if (_ritualOwner != null)
                        spellCaster = _ritualOwner;

                    spellId = info.Ritual.spell;

                    if (spellId == 62330) // GO store nonexistent spell, replace by expected
                    {
                        // spell have reagent and mana cost but it not expected use its
                        // it triggered spell in fact casted at currently channeled GO
                        spellId = 61993;
                        triggered = true;
                    }

                    // Cast casterTargetSpell at a random GO user
                    // on the current DB there is only one gameobject that uses this (Ritual of Doom)
                    // and its required target number is 1 (outter for loop will run once)
                    if (info.Ritual.casterTargetSpell != 0 && info.Ritual.casterTargetSpell != 1) // No idea why this field is a bool in some cases
                        for (uint i = 0; i < info.Ritual.casterTargetSpellTargets; i++)
                        {
                            // m_unique_users can contain only player GUIDs
                            var target = ObjectAccessor.GetPlayer(this, _uniqueUsers.SelectRandom());

                            if (target != null)
                                spellCaster.SpellFactory.CastSpell(target, info.Ritual.casterTargetSpell, true);
                        }

                    // finish owners spell
                    owner?.FinishSpell(CurrentSpellTypes.Channeled);

                    // can be deleted now, if
                    if (info.Ritual.ritualPersistent == 0)
                        SetLootState(LootState.JustDeactivated);
                    else
                    {
                        // reset ritual for this GO
                        _ritualOwner = null;
                        _uniqueUsers.Clear();
                        UseCount = 0;
                    }
                }
                else
                    return;

                // go to end function to spell casting
                break;
            }
            case GameObjectTypes.SpellCaster: //22
            {
                var info = Template;

                if (info == null)
                    return;

                if (info.SpellCaster.partyOnly != 0)
                {
                    var caster = OwnerUnit;

                    if (caster == null || !caster.IsTypeId(TypeId.Player))
                        return;

                    if (!user.IsTypeId(TypeId.Player) || !user.AsPlayer.IsInSameRaidWith(caster.AsPlayer))
                        return;
                }

                user.RemoveAurasByType(AuraType.Mounted);
                spellId = info.SpellCaster.spell;

                AddUse();

                break;
            }
            case GameObjectTypes.MeetingStone: //23
            {
                var info = Template;

                if (!user.IsTypeId(TypeId.Player))
                    return;

                var player = user.AsPlayer;

                var targetPlayer = ObjectAccessor.FindPlayer(player.Target);

                // accept only use by player from same raid as caster, except caster itself
                if (targetPlayer == null || targetPlayer == player || !targetPlayer.IsInSameRaidWith(player))
                    return;

                //required lvl checks!
                var userLevels = DB2Manager.GetContentTuningData(info.ContentTuningId, player.PlayerData.CtrOptions.Value.ContentTuningConditionMask);

                if (userLevels.HasValue)
                    if (player.Level < userLevels.Value.MaxLevel)
                        return;

                var targetLevels = DB2Manager.GetContentTuningData(info.ContentTuningId, targetPlayer.PlayerData.CtrOptions.Value.ContentTuningConditionMask);

                if (targetLevels.HasValue)
                    if (targetPlayer.Level < targetLevels.Value.MaxLevel)
                        return;

                if (info.entry == 194097)
                    spellId = 61994; // Ritual of Summoning
                else
                    spellId = 23598; // 59782;                            // Summoning Stone Effect

                break;
            }

            case GameObjectTypes.FlagStand: // 24
            {
                if (!user.IsTypeId(TypeId.Player))
                    return;

                var player = user.AsPlayer;

                if (player.CanUseBattlegroundObject(this))
                {
                    // in Battlegroundcheck
                    var bg = player.Battleground;

                    if (bg == null)
                        return;

                    if (player.Vehicle != null)
                        return;

                    player.RemoveAurasByType(AuraType.ModStealth);
                    player.RemoveAurasByType(AuraType.ModInvisibility);
                    // BG Id click
                    // AB:
                    // 15001
                    // 15002
                    // 15003
                    // 15004
                    // 15005
                    bg.EventPlayerClickedOnFlag(player, this);

                    return; //we don;t need to delete Id ... it is despawned!
                }

                break;
            }

            case GameObjectTypes.FishingHole: // 25
            {
                if (!user.IsTypeId(TypeId.Player))
                    return;

                var player = user.AsPlayer;

                var loot = LootFactory.GenerateLoot(Location.Map, GUID, LootType.Fishinghole, Template.GetLootId(), LootStorageType.Gameobject, player, true);

                _personalLoot[player.GUID] = loot;

                player.SendLoot(loot);
                player.UpdateCriteria(CriteriaType.CatchFishInFishingHole, Template.entry);

                return;
            }

            case GameObjectTypes.FlagDrop: // 26
            {
                if (!user.IsTypeId(TypeId.Player))
                    return;

                var player = user.AsPlayer;

                if (player.CanUseBattlegroundObject(this))
                {
                    // in Battlegroundcheck
                    var bg = player.Battleground;

                    if (bg == null)
                        return;

                    if (player.Vehicle != null)
                        return;

                    player.RemoveAurasByType(AuraType.ModStealth);
                    player.RemoveAurasByType(AuraType.ModInvisibility);
                    // BG Id dropped
                    // WS:
                    // 179785 - Silverwing Flag
                    // 179786 - Warsong Flag
                    // EotS:
                    // 184142 - Netherstorm Flag
                    var info = Template;

                    if (info != null)
                    {
                        switch (info.entry)
                        {
                            case 179785: // Silverwing Flag
                            case 179786: // Warsong Flag
                                if (bg.GetTypeID(true) == BattlegroundTypeId.Ws)
                                    bg.EventPlayerClickedOnFlag(player, this);

                                break;

                            case 184142: // Netherstorm Flag
                                if (bg.GetTypeID(true) == BattlegroundTypeId.Ey)
                                    bg.EventPlayerClickedOnFlag(player, this);

                                break;
                        }

                        if (info.FlagDrop.eventID != 0)
                            GameEvents.Trigger(info.FlagDrop.eventID, player, this);
                    }

                    //this cause to call return, all flags must be deleted here!!
                    spellId = 0;
                    Delete();
                }

                break;
            }
            case GameObjectTypes.BarberChair: //32
            {
                var info = Template;

                if (info == null)
                    return;

                if (!user.IsTypeId(TypeId.Player))
                    return;

                var player = user.AsPlayer;

                player.SendPacket(new EnableBarberShop());

                // fallback, will always work
                player.TeleportTo(Location.MapId, Location.X, Location.Y, Location.Z, Location.Orientation, TeleportToOptions.NotLeaveTransport | TeleportToOptions.NotLeaveCombat | TeleportToOptions.NotUnSummonPet);
                player.SetStandState(UnitStandStateType.SitLowChair + (byte)info.BarberChair.chairheight, info.BarberChair.SitAnimKit);

                return;
            }
            case GameObjectTypes.NewFlag:
            {
                var info = Template;

                if (info == null)
                    return;

                if (!user.IsPlayer)
                    return;

                spellId = info.NewFlag.pickupSpell;

                break;
            }
            case GameObjectTypes.ItemForge:
            {
                var info = Template;

                if (info == null)
                    return;

                if (!user.IsTypeId(TypeId.Player))
                    return;

                var player = user.AsPlayer;

                if (CliDB.PlayerConditionStorage.TryGetValue(info.ItemForge.conditionID1, out var playerCondition))
                    if (!ConditionManager.IsPlayerMeetingCondition(player, playerCondition))
                        return;

                switch (info.ItemForge.ForgeType)
                {
                    case 0: // Artifact Forge
                    case 1: // Relic Forge
                    {
                        var artifactAura = player.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                        var item = artifactAura != null ? player.GetItemByGuid(artifactAura.CastItemGuid) : null;

                        if (item == null)
                        {
                            player.SendPacket(new DisplayGameError(GameError.MustEquipArtifact));

                            return;
                        }

                        OpenArtifactForge openArtifactForge = new()
                        {
                            ArtifactGUID = item.GUID,
                            ForgeGUID = GUID
                        };

                        player.SendPacket(openArtifactForge);

                        break;
                    }
                    case 2: // Heart Forge
                    {
                        var item = player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

                        if (item == null)
                            return;

                        GameObjectInteraction openHeartForge = new()
                        {
                            ObjectGUID = GUID,
                            InteractionType = PlayerInteractionType.AzeriteForge
                        };

                        player.SendPacket(openHeartForge);

                        break;
                    }
                }

                break;
            }
            case GameObjectTypes.UILink:
            {
                var player = user.AsPlayer;

                if (player == null)
                    return;

                GameObjectInteraction gameObjectUiLink = new()
                {
                    ObjectGUID = GUID
                };

                gameObjectUiLink.InteractionType = Template.UILink.UILinkType switch
                {
                    0 => PlayerInteractionType.AdventureJournal,
                    1 => PlayerInteractionType.ObliterumForge,
                    2 => PlayerInteractionType.ScrappingMachine,
                    3 => PlayerInteractionType.ItemInteraction,
                    _ => gameObjectUiLink.InteractionType
                };

                player.SendPacket(gameObjectUiLink);

                return;
            }
            case GameObjectTypes.GatheringNode: //50
            {
                var player = user.AsPlayer;

                if (player == null)
                    return;

                var info = Template;

                if (!_personalLoot.ContainsKey(player.GUID))
                {
                    if (info.GatheringNode.chestLoot != 0)
                    {
                        var newLoot = LootFactory.GenerateLoot(Location.Map, GUID, LootType.Chest);
                        _personalLoot[player.GUID] = newLoot;

                        newLoot.FillLoot(info.GatheringNode.chestLoot, LootStoreBox.Gameobject, player, true, false, LootMode, Location.Map.GetDifficultyLootItemContext());
                    }

                    if (info.GatheringNode.triggeredEvent != 0)
                        GameEvents.Trigger(info.GatheringNode.triggeredEvent, player, this);

                    // triggering linked GO
                    var trapEntry = info.GatheringNode.linkedTrap;

                    if (trapEntry != 0)
                        TriggeringLinkedGameObject(trapEntry, player);

                    if (info.GatheringNode.xpDifficulty != 0 && info.GatheringNode.xpDifficulty < 10)
                        if (CliDB.QuestXPStorage.TryGetValue(player.Level, out var questXp))
                        {
                            var xp = Quest.RoundXPValue(questXp.Difficulty[info.GatheringNode.xpDifficulty]);

                            if (xp != 0)
                                player.GiveXP(xp, null);
                        }

                    spellId = info.GatheringNode.spell;
                }

                if (_personalLoot.Count >= info.GatheringNode.MaxNumberofLoots)
                {
                    SetGoState(GameObjectState.Active);
                    SetDynamicFlag(GameObjectDynamicLowFlags.NoInterract);
                }

                if (LootState != LootState.Activated)
                {
                    SetLootState(LootState.Activated, player);

                    if (info.GatheringNode.ObjectDespawnDelay != 0)
                        DespawnOrUnsummon(TimeSpan.FromSeconds(info.GatheringNode.ObjectDespawnDelay));
                }

                // Send loot
                var loot = GetLootForPlayer(player);

                if (loot != null)
                    player.SendLoot(loot);

                break;
            }
            default:
                if (GoType >= GameObjectTypes.Max)
                    Log.Logger.Error("GameObject.Use(): unit (type: {0}, guid: {1}, name: {2}) tries to use object (guid: {3}, entry: {4}, name: {5}) of unknown type ({6})",
                                     user.TypeId,
                                     user.GUID.ToString(),
                                     user.GetName(),
                                     GUID.ToString(),
                                     Entry,
                                     Template.name,
                                     GoType);

                break;
        }

        if (spellId == 0)
            return;

        if (!SpellManager.HasSpellInfo(spellId, Location.Map.DifficultyID))
        {
            if (!user.IsTypeId(TypeId.Player) || !OutdoorPvPManager.HandleCustomSpell(user.AsPlayer, spellId, this))
                Log.Logger.Error("WORLD: unknown spell id {0} at use action for gameobject (Entry: {1} GoType: {2})", spellId, Entry, GoType);
            else
                Log.Logger.Debug("WORLD: {0} non-dbc spell was handled by OutdoorPvP", spellId);

            return;
        }

        var player1 = user.AsPlayer;

        if (player1 != null)
            OutdoorPvPManager.HandleCustomSpell(player1, spellId, this);

        if (spellCaster != null)
            spellCaster.SpellFactory.CastSpell(user, spellId, triggered);
        else
            SpellFactory.CastSpell(user, spellId);
    }

    public void UseDoorOrButton(uint timeToRestore = 0, bool alternative = false, Unit user = null)
    {
        if (LootState != LootState.Ready)
            return;

        if (timeToRestore == 0)
            timeToRestore = Template.GetAutoCloseTime();

        SwitchDoorOrButton(true, alternative);
        SetLootState(LootState.Activated, user);

        _cooldownTime = timeToRestore != 0 ? GameTime.CurrentTimeMS + timeToRestore : 0;
    }

    internal bool Create(uint entry, Map map, Position pos, Quaternion rotation, uint animProgress, GameObjectState goState, uint artKit, bool dynamic, ulong spawnid)
    {
        Location.WorldRelocate(map, pos);
        CheckAddToMap();
        StationaryPosition.Relocate(pos);

        if (!GridDefines.IsValidMapCoord(Location))
        {
            Log.Logger.Error("Gameobject (Spawn id: {0} Entry: {1}) not created. Suggested coordinates isn't valid (X: {2} Y: {3})", SpawnId, entry, pos.X, pos.Y);

            return false;
        }

        // Set if this object can handle dynamic spawns
        if (!dynamic)
            RespawnCompatibilityMode = true;

        Location.UpdatePositionData();

        Location.SetZoneScript();

        if (ZoneScript != null)
        {
            entry = ZoneScript.GetGameObjectEntry(SpawnId, entry);

            if (entry == 0)
                return false;
        }

        var goInfo = GameObjectManager.GameObjectTemplateCache.GetGameObjectTemplate(entry);

        if (goInfo == null)
        {
            Log.Logger.Error("Gameobject (Spawn id: {0} Entry: {1}) not created: non-existing entry in `gameobject_template`. Map: {2} (X: {3} Y: {4} Z: {5})", SpawnId, entry, map.Id, pos.X, pos.Y, pos.Z);

            return false;
        }

        if (goInfo.type == GameObjectTypes.MapObjTransport)
        {
            Log.Logger.Error("Gameobject (Spawn id: {0} Entry: {1}) not created: gameobject type GAMEOBJECT_TYPE_MAP_OBJ_TRANSPORT cannot be manually created.", SpawnId, entry);

            return false;
        }

        ObjectGuid guid;

        if (goInfo.type != GameObjectTypes.Transport)
            guid = ObjectGuid.Create(HighGuid.GameObject, map.Id, goInfo.entry, map.GenerateLowGuid(HighGuid.GameObject));
        else
        {
            guid = ObjectGuid.Create(HighGuid.Transport, map.GenerateLowGuid(HighGuid.Transport));
            UpdateFlag.ServerTime = true;
        }

        Create(guid);

        GoInfoProtected = goInfo;
        GoTemplateAddonProtected = GameObjectManager.GetGameObjectTemplateAddon(entry);

        if (goInfo.type >= GameObjectTypes.Max)
        {
            Log.Logger.Error("Gameobject (Spawn id: {0} Entry: {1}) not created: non-existing GO type '{2}' in `gameobject_template`. It will crash client if created.", SpawnId, entry, goInfo.type);

            return false;
        }

        SetLocalRotation(rotation.X, rotation.Y, rotation.Z, rotation.W);
        var gameObjectAddon = GameObjectManager.GetGameObjectAddon(SpawnId);

        // For most of gameobjects is (0, 0, 0, 1) quaternion, there are only some transports with not standard rotation
        var parentRotation = Quaternion.Identity;

        if (gameObjectAddon != null)
            parentRotation = gameObjectAddon.ParentRotation;

        SetParentRotation(parentRotation);

        ObjectScale = goInfo.size;

        var goOverride = GameObjectOverride;

        if (goOverride != null)
        {
            Faction = goOverride.Faction;
            ReplaceAllFlags(goOverride.Flags);
        }

        if (GoTemplateAddonProtected != null)
        {
            if (GoTemplateAddonProtected.WorldEffectId != 0)
            {
                UpdateFlag.GameObject = true;
                WorldEffectID = GoTemplateAddonProtected.WorldEffectId;
            }

            if (GoTemplateAddonProtected.AiAnimKitId != 0)
                _animKitId = (ushort)GoTemplateAddonProtected.AiAnimKitId;
        }

        Entry = goInfo.entry;

        // set name for logs usage, doesn't affect anything ingame
        SetName(goInfo.name);

        DisplayId = goInfo.displayId;

        CreateModel();

        // GAMEOBJECT_BYTES_1, index at 0, 1, 2 and 3
        GoType = goInfo.type;
        _prevGoState = goState;
        SetGoState(goState);
        GoArtKit = artKit;

        SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.SpawnTrackingStateAnimID), DB2Manager.GetEmptyAnimStateID());

        switch (goInfo.type)
        {
            case GameObjectTypes.FishingHole:
                SetGoAnimProgress(animProgress);
                GoValueProtected.FishingHole.MaxOpens = RandomHelper.URand(Template.FishingHole.minRestock, Template.FishingHole.maxRestock);

                break;

            case GameObjectTypes.DestructibleBuilding:
                GoValueProtected.Building.Health = Template.DestructibleBuilding.InteriorVisible != 0 ? Template.DestructibleBuilding.InteriorVisible : 20000;
                GoValueProtected.Building.MaxHealth = GoValueProtected.Building.Health;
                SetGoAnimProgress(255);
                // yes, even after the updatefield rewrite this garbage hack is still in client
                SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.ParentRotation), new Quaternion(goInfo.DestructibleBuilding.DestructibleModelRec, 0f, 0f, 0f));

                break;

            case GameObjectTypes.Transport:
                _goTypeImpl = new TransportGameObject(this);

                if (goInfo.Transport.startOpen != 0)
                    SetGoState(GameObjectState.TransportStopped);
                else
                    SetGoState(GameObjectState.TransportActive);

                SetGoAnimProgress(animProgress);
                SetActive(true);

                break;

            case GameObjectTypes.FishingNode:
                SetLevel(0);
                SetGoAnimProgress(255);

                break;

            case GameObjectTypes.Trap:
                if (goInfo.Trap.stealthed != 0)
                {
                    Visibility.Stealth.AddFlag(StealthType.Trap);
                    Visibility.Stealth.AddValue(StealthType.Trap, 70);
                }

                if (goInfo.Trap.stealthAffected != 0)
                {
                    Visibility.Invisibility.AddFlag(InvisibilityType.Trap);
                    Visibility.Invisibility.AddValue(InvisibilityType.Trap, 300);
                }

                break;

            case GameObjectTypes.PhaseableMo:
                RemoveFlag((GameObjectFlags)0xF00);
                SetFlag((GameObjectFlags)((GoInfoProtected.PhaseableMO.AreaNameSet & 0xF) << 8));

                break;

            case GameObjectTypes.CapturePoint:
                SetUpdateFieldValue(Values.ModifyValue(GameObjectFieldData).ModifyValue(GameObjectFieldData.SpellVisualID), GoInfoProtected.CapturePoint.SpellVisual1);
                GoValueProtected.CapturePoint.AssaultTimer = 0;
                GoValueProtected.CapturePoint.LastTeamCapture = TeamIds.Neutral;
                GoValueProtected.CapturePoint.State = BattlegroundCapturePointState.Neutral;
                UpdateCapturePoint();

                break;

            default:
                SetGoAnimProgress(animProgress);

                break;
        }

        if (gameObjectAddon != null)
        {
            if (gameObjectAddon.InvisibilityValue != 0)
            {
                Visibility.Invisibility.AddFlag(gameObjectAddon.InvisibilityType);
                Visibility.Invisibility.AddValue(gameObjectAddon.InvisibilityType, gameObjectAddon.InvisibilityValue);
            }

            if (gameObjectAddon.WorldEffectID != 0)
            {
                UpdateFlag.GameObject = true;
                WorldEffectID = gameObjectAddon.WorldEffectID;
            }

            if (gameObjectAddon.AIAnimKitID != 0)
                _animKitId = (ushort)gameObjectAddon.AIAnimKitID;
        }

        LastUsedScriptID = Template.ScriptId;
        AIM_Initialize();

        if (spawnid != 0)
            SpawnId = spawnid;

        var linkedEntry = Template.GetLinkedGameObjectEntry();

        if (linkedEntry != 0)
        {
            var linkedGo = GameObjectFactory.CreateGameObject(linkedEntry, map, pos, rotation, 255, GameObjectState.Ready);

            if (linkedGo != null)
            {
                LinkedTrap = linkedGo;

                if (!map.AddToMap(linkedGo))
                    linkedGo.Dispose();
            }
        }

        // Check if GameObject is Infinite
        if (goInfo.IsInfiniteGameObject())
            Visibility.SetVisibilityDistanceOverride(VisibilityDistanceType.Infinite);

        // Check if GameObject is Gigantic
        if (goInfo.IsGiganticGameObject())
            Visibility.SetVisibilityDistanceOverride(VisibilityDistanceType.Gigantic);

        // Check if GameObject is Large
        if (goInfo.IsLargeGameObject())
            Visibility.SetVisibilityDistanceOverride(VisibilityDistanceType.Large);

        return true;
    }

    private void ClearLoot()
    {
        // Unlink loot objects from this GameObject before destroying to avoid accessing freed memory from Loot destructor
        Loot = null;
        _personalLoot.Clear();
        _uniqueUsers.Clear();
        UseCount = 0;
    }

    private void DespawnForPlayer(Player seer, TimeSpan respawnTime)
    {
        var perPlayerState = GetOrCreatePerPlayerStates(seer.GUID);
        perPlayerState.ValidUntil = GameTime.SystemTime + respawnTime;
        perPlayerState.Despawned = true;
        seer.UpdateVisibilityOf(this);
    }

    private void EnableCollision(bool enable)
    {
        Model?.EnableCollision(enable);
    }

    private PerPlayerState GetOrCreatePerPlayerStates(ObjectGuid guid)
    {
        if (_perPlayerState == null)
            _perPlayerState = new Dictionary<ObjectGuid, PerPlayerState>();

        if (!_perPlayerState.ContainsKey(guid))
            _perPlayerState[guid] = new PerPlayerState();

        return _perPlayerState[guid];
    }

    private uint GetUniqueUseCount()
    {
        return (uint)_uniqueUsers.Count;
    }

    private bool IsAtInteractDistance(Position pos, float radius)
    {
        if (!CliDB.GameObjectDisplayInfoStorage.TryGetValue(Template.displayId, out var displayInfo))
            return Location.GetExactDist(pos) <= radius;

        var scale = ObjectScale;

        var minX = displayInfo.GeoBoxMin.X * scale - radius;
        var minY = displayInfo.GeoBoxMin.Y * scale - radius;
        var minZ = displayInfo.GeoBoxMin.Z * scale - radius;
        var maxX = displayInfo.GeoBoxMax.X * scale + radius;
        var maxY = displayInfo.GeoBoxMax.Y * scale + radius;
        var maxZ = displayInfo.GeoBoxMax.Z * scale + radius;

        var worldRotation = GetWorldRotation();

        //Todo Test this. Needs checked.
        var worldSpaceBox = MathFunctions.toWorldSpace(worldRotation.ToMatrix(), new Vector3(Location.X, Location.Y, Location.Z), new Box(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ)));

        return worldSpaceBox.Contains(new Vector3(pos.X, pos.Y, pos.Z));
    }

    private GameObject LookupFishingHoleAround(float range)
    {
        var uCheck = new NearestGameObjectFishingHole(this, range);
        var checker = new GameObjectSearcher(this, uCheck, GridType.Grid);

        CellCalculator.VisitGrid(this, checker, range);

        return checker.GetTarget();
    }

    private void RemoveFromOwner()
    {
        var ownerGUID = OwnerGUID;

        if (ownerGUID.IsEmpty)
            return;

        var owner = ObjectAccessor.GetUnit(this, ownerGUID);

        if (owner != null)
        {
            owner.RemoveGameObject(this, false);

            return;
        }

        // This happens when a mage portal is despawned after the caster changes map (for example using the portal)
        Log.Logger.Debug("Removed GameObject (GUID: {0} Entry: {1} SpellId: {2} LinkedGO: {3}) that just lost any reference to the owner {4} GO list",
                         GUID.ToString(),
                         Template.entry,
                         _spellId,
                         Template.GetLinkedGameObjectEntry(),
                         ownerGUID.ToString());

        SetOwnerGUID(ObjectGuid.Empty);
    }

    private void ResetLootMode()
    {
        LootMode = LootModes.Default;
    }

    private void SetGoStateFor(GameObjectState state, Player viewer)
    {
        var perPlayerState = GetOrCreatePerPlayerStates(viewer.GUID);
        perPlayerState.ValidUntil = GameTime.SystemTime + TimeSpan.FromSeconds(RespawnDelay);
        perPlayerState.State = state;

        GameObjectSetStateLocal setStateLocal = new()
        {
            ObjectGUID = GUID,
            State = (byte)state
        };

        viewer.SendPacket(setStateLocal);
    }

    private void SwitchDoorOrButton(bool activate, bool alternative = false)
    {
        if (activate)
            SetFlag(GameObjectFlags.InUse);
        else
            RemoveFlag(GameObjectFlags.InUse);

        if (GoState == GameObjectState.Ready) //if closed . open
            SetGoState(alternative ? GameObjectState.Destroyed : GameObjectState.Active);
        else //if open . close
            SetGoState(GameObjectState.Ready);
    }

    private void UpdateCapturePoint()
    {
        if (GoType != GameObjectTypes.CapturePoint)
            return;

        var ai = AI;

        if (ai != null)
            if (ai.OnCapturePointUpdated(GoValueProtected.CapturePoint.State))
                return;

        uint spellVisualId = 0;
        uint customAnim = 0;

        switch (GoValueProtected.CapturePoint.State)
        {
            case BattlegroundCapturePointState.Neutral:
                spellVisualId = Template.CapturePoint.SpellVisual1;

                break;

            case BattlegroundCapturePointState.ContestedHorde:
                customAnim = 1;
                spellVisualId = Template.CapturePoint.SpellVisual2;

                break;

            case BattlegroundCapturePointState.ContestedAlliance:
                customAnim = 2;
                spellVisualId = Template.CapturePoint.SpellVisual3;

                break;

            case BattlegroundCapturePointState.HordeCaptured:
                customAnim = 3;
                spellVisualId = Template.CapturePoint.SpellVisual4;

                break;

            case BattlegroundCapturePointState.AllianceCaptured:
                customAnim = 4;
                spellVisualId = Template.CapturePoint.SpellVisual5;

                break;
        }

        if (customAnim != 0)
            SendCustomAnim(customAnim);

        SetSpellVisualId(spellVisualId);
        UpdateDynamicFlagsForNearbyPlayers();

        var map = Location.Map.ToBattlegroundMap;

        var bg = map?.BG;

        if (bg != null)
        {
            UpdateCapturePoint packet = new()
            {
                CapturePointInfo =
                {
                    State = GoValueProtected.CapturePoint.State,
                    Pos = Location,
                    Guid = GUID,
                    CaptureTotalDuration = TimeSpan.FromMilliseconds(Template.CapturePoint.CaptureTime),
                    CaptureTime = GoValueProtected.CapturePoint.AssaultTimer
                }
            };

            bg.SendPacketToAll(packet);
            bg.UpdateWorldState((int)Template.CapturePoint.worldState1, (byte)GoValueProtected.CapturePoint.State);
        }

        Location.Map.UpdateSpawnGroupConditions();
    }

    private void UpdateDynamicFlagsForNearbyPlayers()
    {
        Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags);
        AddToObjectUpdateIfNeeded();
    }

    private void UpdateModel()
    {
        if (!Location.IsInWorld)
            return;

        if (Model != null)
            if (Location.Map.ContainsGameObjectModel(Model))
                Location.Map.RemoveGameObjectModel(Model);

        RemoveFlag(GameObjectFlags.MapObject);
        Model = null;
        CreateModel();

        if (Model != null)
            Location.Map.InsertGameObjectModel(Model);
    }
}