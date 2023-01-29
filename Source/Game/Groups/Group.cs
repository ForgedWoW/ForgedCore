﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.DungeonFinding;
using Game.Entities;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IGroup;

namespace Game.Groups
{
    public class Group
    {
        private uint _activeMarkers;
        private BattleField _bfGroup;
        private Battleground _bgGroup;
        private uint _dbStoreId;
        private Difficulty _dungeonDifficulty;
        private GroupCategory _groupCategory;
        private GroupFlags _groupFlags;
        private ObjectGuid _guid;
        private readonly List<Player> _invitees = new();
        private bool _isLeaderOffline;
        private byte _leaderFactionGroup;
        private ObjectGuid _leaderGuid;
        private string _leaderName;
        private readonly TimeTracker _leaderOfflineTimer = new();
        private Difficulty _legacyRaidDifficulty;
        private ObjectGuid _looterGuid;
        private LootMethod _lootMethod;
        private ItemQuality _lootThreshold;

        // Raid markers
        private readonly RaidMarker[] _markers = new RaidMarker[MapConst.RaidMarkersCount];
        private ObjectGuid _masterLooterGuid;
        private readonly GroupRefManager _memberMgr = new();

        private readonly List<MemberSlot> _memberSlots = new();
        private readonly GroupInstanceRefManager _ownedInstancesMgr = new();
        private Difficulty _raidDifficulty;

        // Ready Check
        private bool _readyCheckStarted;
        private TimeSpan _readyCheckTimer;
        private readonly Dictionary<uint, Tuple<ObjectGuid, uint>> _recentInstances = new();
        private byte[] _subGroupsCounts;
        private readonly ObjectGuid[] _targetIcons = new ObjectGuid[MapConst.TargetIconsCount];

        public Group()
        {
            _leaderName = "";
            _groupFlags = GroupFlags.None;
            _dungeonDifficulty = Difficulty.Normal;
            _raidDifficulty = Difficulty.NormalRaid;
            _legacyRaidDifficulty = Difficulty.Raid10N;
            _lootMethod = LootMethod.PersonalLoot;
            _lootThreshold = ItemQuality.Uncommon;
        }

        public void Update(uint diff)
        {
            if (_isLeaderOffline)
            {
                _leaderOfflineTimer.Update(diff);

                if (_leaderOfflineTimer.Passed())
                {
                    SelectNewPartyOrRaidLeader();
                    _isLeaderOffline = false;
                }
            }

            UpdateReadyCheck(diff);
        }

        private void SelectNewPartyOrRaidLeader()
        {
            Player newLeader = null;

            // Attempt to give leadership to main assistant first
            if (IsRaidGroup())
                foreach (var memberSlot in _memberSlots)
                    if (memberSlot.flags.HasFlag(GroupMemberFlags.Assistant))
                    {
                        Player player = Global.ObjAccessor.FindPlayer(memberSlot.guid);

                        if (player != null)
                        {
                            newLeader = player;

                            break;
                        }
                    }

            // If there aren't assistants in raid, or if the group is not a raid, pick the first available member
            if (!newLeader)
                foreach (var memberSlot in _memberSlots)
                {
                    Player player = Global.ObjAccessor.FindPlayer(memberSlot.guid);

                    if (player != null)
                    {
                        newLeader = player;

                        break;
                    }
                }

            if (newLeader)
            {
                ChangeLeader(newLeader.GetGUID());
                SendUpdate();
            }
        }

        public bool Create(Player leader)
        {
            ObjectGuid leaderGuid = leader.GetGUID();

            _guid = ObjectGuid.Create(HighGuid.Party, Global.GroupMgr.GenerateGroupId());
            _leaderGuid = leaderGuid;
            _leaderFactionGroup = Player.GetFactionGroupForRace(leader.GetRace());
            _leaderName = leader.GetName();
            leader.SetPlayerFlag(PlayerFlags.GroupLeader);

            if (IsBGGroup() ||
                IsBFGroup())
            {
                _groupFlags = GroupFlags.MaskBgRaid;
                _groupCategory = GroupCategory.Instance;
            }

            if (_groupFlags.HasAnyFlag(GroupFlags.Raid))
                _initRaidSubGroupsCounter();

            _lootThreshold = ItemQuality.Uncommon;
            _looterGuid = leaderGuid;

            _dungeonDifficulty = Difficulty.Normal;
            _raidDifficulty = Difficulty.NormalRaid;
            _legacyRaidDifficulty = Difficulty.Raid10N;

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                _dungeonDifficulty = leader.GetDungeonDifficultyID();
                _raidDifficulty = leader.GetRaidDifficultyID();
                _legacyRaidDifficulty = leader.GetLegacyRaidDifficultyID();

                _dbStoreId = Global.GroupMgr.GenerateNewGroupDbStoreId();

                Global.GroupMgr.RegisterGroupDbStoreId(_dbStoreId, this);

                // Store group in database
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GROUP);

                byte index = 0;

                stmt.AddValue(index++, _dbStoreId);
                stmt.AddValue(index++, _leaderGuid.GetCounter());
                stmt.AddValue(index++, (byte)_lootMethod);
                stmt.AddValue(index++, _looterGuid.GetCounter());
                stmt.AddValue(index++, (byte)_lootThreshold);
                stmt.AddValue(index++, _targetIcons[0].GetRawValue());
                stmt.AddValue(index++, _targetIcons[1].GetRawValue());
                stmt.AddValue(index++, _targetIcons[2].GetRawValue());
                stmt.AddValue(index++, _targetIcons[3].GetRawValue());
                stmt.AddValue(index++, _targetIcons[4].GetRawValue());
                stmt.AddValue(index++, _targetIcons[5].GetRawValue());
                stmt.AddValue(index++, _targetIcons[6].GetRawValue());
                stmt.AddValue(index++, _targetIcons[7].GetRawValue());
                stmt.AddValue(index++, (byte)_groupFlags);
                stmt.AddValue(index++, (byte)_dungeonDifficulty);
                stmt.AddValue(index++, (byte)_raidDifficulty);
                stmt.AddValue(index++, (byte)_legacyRaidDifficulty);
                stmt.AddValue(index++, _masterLooterGuid.GetCounter());

                DB.Characters.Execute(stmt);

                InstanceMap leaderInstance = leader.GetMap().ToInstanceMap();

                leaderInstance?.TrySetOwningGroup(this);

                Cypher.Assert(AddMember(leader)); // If the leader can't be added to a new group because it appears full, something is clearly wrong.
            }
            else if (!AddMember(leader))
            {
                return false;
            }

            return true;
        }

        public void LoadGroupFromDB(SQLFields field)
        {
            _dbStoreId = field.Read<uint>(17);
            _guid = ObjectGuid.Create(HighGuid.Party, Global.GroupMgr.GenerateGroupId());
            _leaderGuid = ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(0));

            // group leader not exist
            var leader = Global.CharacterCacheStorage.GetCharacterCacheByGuid(_leaderGuid);

            if (leader == null)
                return;

            _leaderFactionGroup = Player.GetFactionGroupForRace(leader.RaceId);
            _leaderName = leader.Name;
            _lootMethod = (LootMethod)field.Read<byte>(1);
            _looterGuid = ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(2));
            _lootThreshold = (ItemQuality)field.Read<byte>(3);

            for (byte i = 0; i < MapConst.TargetIconsCount; ++i)
                _targetIcons[i].SetRawValue(field.Read<byte[]>(4 + i));

            _groupFlags = (GroupFlags)field.Read<byte>(12);

            if (_groupFlags.HasAnyFlag(GroupFlags.Raid))
                _initRaidSubGroupsCounter();

            _dungeonDifficulty = Player.CheckLoadedDungeonDifficultyID((Difficulty)field.Read<byte>(13));
            _raidDifficulty = Player.CheckLoadedRaidDifficultyID((Difficulty)field.Read<byte>(14));
            _legacyRaidDifficulty = Player.CheckLoadedLegacyRaidDifficultyID((Difficulty)field.Read<byte>(15));

            _masterLooterGuid = ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(16));

            if (_groupFlags.HasAnyFlag(GroupFlags.Lfg))
                Global.LFGMgr._LoadFromDB(field, GetGUID());
        }

        public void LoadMemberFromDB(ulong guidLow, byte memberFlags, byte subgroup, LfgRoles roles)
        {
            MemberSlot member = new();
            member.guid = ObjectGuid.Create(HighGuid.Player, guidLow);

            // skip non-existed member
            var character = Global.CharacterCacheStorage.GetCharacterCacheByGuid(member.guid);

            if (character == null)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GROUP_MEMBER);
                stmt.AddValue(0, guidLow);
                DB.Characters.Execute(stmt);

                return;
            }

            member.name = character.Name;
            member.race = character.RaceId;
            member._class = (byte)character.ClassId;
            member.group = subgroup;
            member.flags = (GroupMemberFlags)memberFlags;
            member.roles = roles;
            member.readyChecked = false;

            _memberSlots.Add(member);

            SubGroupCounterIncrease(subgroup);

            Global.LFGMgr.SetupGroupMember(member.guid, GetGUID());
        }

        public void ConvertToLFG()
        {
            _groupFlags = (_groupFlags | GroupFlags.Lfg | GroupFlags.LfgRestricted);
            _groupCategory = GroupCategory.Instance;
            _lootMethod = LootMethod.PersonalLoot;

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

                stmt.AddValue(0, (byte)_groupFlags);
                stmt.AddValue(1, _dbStoreId);

                DB.Characters.Execute(stmt);
            }

            SendUpdate();
        }

        public void ConvertToRaid()
        {
            _groupFlags |= GroupFlags.Raid;

            _initRaidSubGroupsCounter();

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

                stmt.AddValue(0, (byte)_groupFlags);
                stmt.AddValue(1, _dbStoreId);

                DB.Characters.Execute(stmt);
            }

            SendUpdate();

            // update quest related GO states (quest activity dependent from raid membership)
            foreach (var member in _memberSlots)
            {
                Player player = Global.ObjAccessor.FindPlayer(member.guid);

                player?.UpdateVisibleGameobjectsOrSpellClicks();
            }
        }

        public void ConvertToGroup()
        {
            if (_memberSlots.Count > 5)
                return; // What message error should we send?

            _groupFlags = GroupFlags.None;

            _subGroupsCounts = null;

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

                stmt.AddValue(0, (byte)_groupFlags);
                stmt.AddValue(1, _dbStoreId);

                DB.Characters.Execute(stmt);
            }

            SendUpdate();

            // update quest related GO states (quest activity dependent from raid membership)
            foreach (var member in _memberSlots)
            {
                Player player = Global.ObjAccessor.FindPlayer(member.guid);

                player?.UpdateVisibleGameobjectsOrSpellClicks();
            }
        }

        public bool AddInvite(Player player)
        {
            if (player == null ||
                player.GetGroupInvite())
                return false;

            Group group = player.GetGroup();

            if (group && (group.IsBGGroup() || group.IsBFGroup()))
                group = player.GetOriginalGroup();

            if (group)
                return false;

            RemoveInvite(player);

            _invitees.Add(player);

            player.SetGroupInvite(this);

            Global.ScriptMgr.ForEach<IGroupOnInviteMember>(p => p.OnInviteMember(this, player.GetGUID()));

            return true;
        }

        public bool AddLeaderInvite(Player player)
        {
            if (!AddInvite(player))
                return false;

            _leaderGuid = player.GetGUID();
            _leaderFactionGroup = Player.GetFactionGroupForRace(player.GetRace());
            _leaderName = player.GetName();

            return true;
        }

        public void RemoveInvite(Player player)
        {
            if (player != null)
            {
                _invitees.Remove(player);
                player.SetGroupInvite(null);
            }
        }

        public void RemoveAllInvites()
        {
            foreach (var pl in _invitees)
                pl?.SetGroupInvite(null);

            _invitees.Clear();
        }

        public Player GetInvited(ObjectGuid guid)
        {
            foreach (var pl in _invitees)
                if (pl != null &&
                    pl.GetGUID() == guid)
                    return pl;

            return null;
        }

        public Player GetInvited(string name)
        {
            foreach (var pl in _invitees)
                if (pl != null &&
                    pl.GetName() == name)
                    return pl;

            return null;
        }

        public bool AddMember(Player player)
        {
            // Get first not-full group
            byte subGroup = 0;

            if (_subGroupsCounts != null)
            {
                bool groupFound = false;

                for (; subGroup < MapConst.MaxRaidSubGroups; ++subGroup)
                    if (_subGroupsCounts[subGroup] < MapConst.MaxGroupSize)
                    {
                        groupFound = true;

                        break;
                    }

                // We are raid group and no one Slot is free
                if (!groupFound)
                    return false;
            }

            MemberSlot member = new();
            member.guid = player.GetGUID();
            member.name = player.GetName();
            member.race = player.GetRace();
            member._class = (byte)player.GetClass();
            member.group = subGroup;
            member.flags = 0;
            member.roles = 0;
            member.readyChecked = false;
            _memberSlots.Add(member);

            SubGroupCounterIncrease(subGroup);

            player.SetGroupInvite(null);

            if (player.GetGroup() != null)
            {
                if (IsBGGroup() ||
                    IsBFGroup()) // if player is in group and he is being added to BG raid group, then call SetBattlegroundRaid()
                    player.SetBattlegroundOrBattlefieldRaid(this, subGroup);
                else //if player is in bg raid and we are adding him to normal group, then call SetOriginalGroup()
                    player.SetOriginalGroup(this, subGroup);
            }
            else //if player is not in group, then call set group
            {
                player.SetGroup(this, subGroup);
            }

            player.SetPartyType(_groupCategory, GroupType.Normal);
            player.ResetGroupUpdateSequenceIfNeeded(this);

            // if the same group invites the player back, cancel the _homebind timer
            player.InstanceValid = player.CheckInstanceValidity(false);

            if (!IsRaidGroup()) // reset targetIcons for non-raid-groups
                for (byte i = 0; i < MapConst.TargetIconsCount; ++i)
                    _targetIcons[i].Clear();

            // insert into the table if we're not a Battlegroundgroup
            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GROUP_MEMBER);

                stmt.AddValue(0, _dbStoreId);
                stmt.AddValue(1, member.guid.GetCounter());
                stmt.AddValue(2, (byte)member.flags);
                stmt.AddValue(3, member.group);
                stmt.AddValue(4, (byte)member.roles);

                DB.Characters.Execute(stmt);
            }

            SendUpdate();
            Global.ScriptMgr.ForEach<IGroupOnAddMember>(p => p.OnAddMember(this, player.GetGUID()));

            if (!IsLeader(player.GetGUID()) &&
                !IsBGGroup() &&
                !IsBFGroup())
            {
                if (player.GetDungeonDifficultyID() != GetDungeonDifficultyID())
                {
                    player.SetDungeonDifficultyID(GetDungeonDifficultyID());
                    player.SendDungeonDifficulty();
                }

                if (player.GetRaidDifficultyID() != GetRaidDifficultyID())
                {
                    player.SetRaidDifficultyID(GetRaidDifficultyID());
                    player.SendRaidDifficulty(false);
                }

                if (player.GetLegacyRaidDifficultyID() != GetLegacyRaidDifficultyID())
                {
                    player.SetLegacyRaidDifficultyID(GetLegacyRaidDifficultyID());
                    player.SendRaidDifficulty(true);
                }
            }

            player.SetGroupUpdateFlag(GroupUpdateFlags.Full);
            Pet pet = player.GetPet();

            if (pet)
                pet.SetGroupUpdateFlag(GroupUpdatePetFlags.Full);

            UpdatePlayerOutOfRange(player);

            // quest related GO State dependent from raid membership
            if (IsRaidGroup())
                player.UpdateVisibleGameobjectsOrSpellClicks();

            {
                // Broadcast new player group member fields to rest of the group
                UpdateData groupData = new(player.GetMapId());
                UpdateObject groupDataPacket;

                // Broadcast group members' fields to player
                for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
                {
                    if (refe.GetSource() == player)
                        continue;

                    Player existingMember = refe.GetSource();

                    if (existingMember != null)
                    {
                        if (player.HaveAtClient(existingMember))
                            existingMember.BuildValuesUpdateBlockForPlayerWithFlag(groupData, UpdateFieldFlag.PartyMember, player);

                        if (existingMember.HaveAtClient(player))
                        {
                            UpdateData newData = new(player.GetMapId());
                            UpdateObject newDataPacket;
                            player.BuildValuesUpdateBlockForPlayerWithFlag(newData, UpdateFieldFlag.PartyMember, existingMember);

                            if (newData.HasData())
                            {
                                newData.BuildPacket(out newDataPacket);
                                existingMember.SendPacket(newDataPacket);
                            }
                        }
                    }
                }

                if (groupData.HasData())
                {
                    groupData.BuildPacket(out groupDataPacket);
                    player.SendPacket(groupDataPacket);
                }
            }

            return true;
        }

        public bool RemoveMember(ObjectGuid guid, RemoveMethod method = RemoveMethod.Default, ObjectGuid kicker = default, string reason = null)
        {
            BroadcastGroupUpdate();

            Global.ScriptMgr.ForEach<IGroupOnRemoveMember>(p => p.OnRemoveMember(this, guid, method, kicker, reason));

            Player player = Global.ObjAccessor.FindConnectedPlayer(guid);

            if (player)
                for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player groupMember = refe.GetSource();

                    if (groupMember)
                    {
                        if (groupMember.GetGUID() == guid)
                            continue;

                        groupMember.RemoveAllGroupBuffsFromCaster(guid);
                        player.RemoveAllGroupBuffsFromCaster(groupMember.GetGUID());
                    }
                }

            // LFG group vote kick handled in scripts
            if (IsLFGGroup() &&
                method == RemoveMethod.Kick)
                return _memberSlots.Count != 0;

            // remove member and change leader (if need) only if strong more 2 members _before_ member remove (BG/BF allow 1 member group)
            if (GetMembersCount() > ((IsBGGroup() || IsLFGGroup() || IsBFGroup()) ? 1 : 2))
            {
                if (player)
                {
                    // Battlegroundgroup handling
                    if (IsBGGroup() ||
                        IsBFGroup())
                    {
                        player.RemoveFromBattlegroundOrBattlefieldRaid();
                    }
                    else
                    // Regular group
                    {
                        if (player.GetOriginalGroup() == this)
                            player.SetOriginalGroup(null);
                        else
                            player.SetGroup(null);

                        // quest related GO State dependent from raid membership
                        player.UpdateVisibleGameobjectsOrSpellClicks();
                    }

                    player.SetPartyType(_groupCategory, GroupType.None);

                    if (method == RemoveMethod.Kick ||
                        method == RemoveMethod.KickLFG)
                        player.SendPacket(new GroupUninvite());

                    _homebindIfInstance(player);
                }

                // Remove player from group in DB
                if (!IsBGGroup() &&
                    !IsBFGroup())
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GROUP_MEMBER);
                    stmt.AddValue(0, guid.GetCounter());
                    DB.Characters.Execute(stmt);
                    DelinkMember(guid);
                }

                // Update subgroups
                var slot = _getMemberSlot(guid);

                if (slot != null)
                {
                    SubGroupCounterDecrease(slot.group);
                    _memberSlots.Remove(slot);
                }

                // Pick new leader if necessary
                if (_leaderGuid == guid)
                    foreach (var member in _memberSlots)
                        if (Global.ObjAccessor.FindPlayer(member.guid) != null)
                        {
                            ChangeLeader(member.guid);

                            break;
                        }

                SendUpdate();

                if (IsLFGGroup() &&
                    GetMembersCount() == 1)
                {
                    Player leader = Global.ObjAccessor.FindPlayer(GetLeaderGUID());
                    uint mapId = Global.LFGMgr.GetDungeonMapId(GetGUID());

                    if (mapId == 0 ||
                        leader == null ||
                        (leader.IsAlive() && leader.GetMapId() != mapId))
                    {
                        Disband();

                        return false;
                    }
                }

                if (_memberMgr.GetSize() < ((IsLFGGroup() || IsBGGroup()) ? 1 : 2))
                    Disband();
                else if (player)
                    // send update to removed player too so party frames are destroyed clientside
                    SendUpdateDestroyGroupToPlayer(player);

                return true;
            }
            // If group size before player removal <= 2 then disband it
            else
            {
                Disband();

                return false;
            }
        }

        public void ChangeLeader(ObjectGuid newLeaderGuid, sbyte partyIndex = 0)
        {
            var slot = _getMemberSlot(newLeaderGuid);

            if (slot == null)
                return;

            Player newLeader = Global.ObjAccessor.FindPlayer(slot.guid);

            // Don't allow switching leader to offline players
            if (newLeader == null)
                return;

            Global.ScriptMgr.ForEach<IGroupOnChangeLeader>(p => p.OnChangeLeader(this, newLeaderGuid, _leaderGuid));

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt;
                SQLTransaction trans = new();

                // Update the group leader
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_LEADER);

                stmt.AddValue(0, newLeader.GetGUID().GetCounter());
                stmt.AddValue(1, _dbStoreId);

                trans.Append(stmt);

                DB.Characters.CommitTransaction(trans);
            }

            Player oldLeader = Global.ObjAccessor.FindConnectedPlayer(_leaderGuid);

            if (oldLeader)
                oldLeader.RemovePlayerFlag(PlayerFlags.GroupLeader);

            newLeader.SetPlayerFlag(PlayerFlags.GroupLeader);
            _leaderGuid = newLeader.GetGUID();
            _leaderFactionGroup = Player.GetFactionGroupForRace(newLeader.GetRace());
            _leaderName = newLeader.GetName();
            ToggleGroupMemberFlag(slot, GroupMemberFlags.Assistant, false);

            GroupNewLeader groupNewLeader = new();
            groupNewLeader.Name = _leaderName;
            groupNewLeader.PartyIndex = partyIndex;
            BroadcastPacket(groupNewLeader, true);
        }

        public void Disband(bool hideDestroy = false)
        {
            Global.ScriptMgr.ForEach<IGroupOnDisband>(p => p.OnDisband(this));

            Player player;

            foreach (var member in _memberSlots)
            {
                player = Global.ObjAccessor.FindPlayer(member.guid);

                if (player == null)
                    continue;

                //we cannot call _removeMember because it would invalidate member iterator
                //if we are removing player from Battlegroundraid
                if (IsBGGroup() ||
                    IsBFGroup())
                {
                    player.RemoveFromBattlegroundOrBattlefieldRaid();
                }
                else
                {
                    //we can remove player who is in Battlegroundfrom his original group
                    if (player.GetOriginalGroup() == this)
                        player.SetOriginalGroup(null);
                    else
                        player.SetGroup(null);
                }

                player.SetPartyType(_groupCategory, GroupType.None);

                // quest related GO State dependent from raid membership
                if (IsRaidGroup())
                    player.UpdateVisibleGameobjectsOrSpellClicks();

                if (!hideDestroy)
                    player.SendPacket(new GroupDestroyed());

                SendUpdateDestroyGroupToPlayer(player);

                _homebindIfInstance(player);
            }

            _memberSlots.Clear();

            RemoveAllInvites();

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                SQLTransaction trans = new();

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GROUP);
                stmt.AddValue(0, _dbStoreId);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GROUP_MEMBER_ALL);
                stmt.AddValue(0, _dbStoreId);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_LFG_DATA);
                stmt.AddValue(0, _dbStoreId);
                trans.Append(stmt);

                DB.Characters.CommitTransaction(trans);

                Global.GroupMgr.FreeGroupDbStoreId(this);
            }

            Global.GroupMgr.RemoveGroup(this);
        }

        public void SetTargetIcon(byte symbol, ObjectGuid target, ObjectGuid changedBy, sbyte partyIndex)
        {
            if (symbol >= MapConst.TargetIconsCount)
                return;

            // clean other icons
            if (!target.IsEmpty())
                for (byte i = 0; i < MapConst.TargetIconsCount; ++i)
                    if (_targetIcons[i] == target)
                        SetTargetIcon(i, ObjectGuid.Empty, changedBy, partyIndex);

            _targetIcons[symbol] = target;

            SendRaidTargetUpdateSingle updateSingle = new();
            updateSingle.PartyIndex = partyIndex;
            updateSingle.Target = target;
            updateSingle.ChangedBy = changedBy;
            updateSingle.Symbol = (sbyte)symbol;
            BroadcastPacket(updateSingle, true);
        }

        public void SendTargetIconList(WorldSession session, sbyte partyIndex)
        {
            if (session == null)
                return;

            SendRaidTargetUpdateAll updateAll = new();
            updateAll.PartyIndex = partyIndex;

            for (byte i = 0; i < MapConst.TargetIconsCount; i++)
                updateAll.TargetIcons.Add(i, _targetIcons[i]);

            session.SendPacket(updateAll);
        }

        public void SendUpdate()
        {
            foreach (var member in _memberSlots)
                SendUpdateToPlayer(member.guid, member);
        }

        public void SendUpdateToPlayer(ObjectGuid playerGUID, MemberSlot memberSlot = null)
        {
            Player player = Global.ObjAccessor.FindPlayer(playerGUID);

            if (player == null ||
                player.GetSession() == null ||
                player.GetGroup() != this)
                return;

            // if MemberSlot wasn't provided
            if (memberSlot == null)
            {
                var slot = _getMemberSlot(playerGUID);

                if (slot == null) // if there is no MemberSlot for such a player
                    return;

                memberSlot = slot;
            }

            PartyUpdate partyUpdate = new();

            partyUpdate.PartyFlags = _groupFlags;
            partyUpdate.PartyIndex = (byte)_groupCategory;
            partyUpdate.PartyType = IsCreated() ? GroupType.Normal : GroupType.None;

            partyUpdate.PartyGUID = _guid;
            partyUpdate.LeaderGUID = _leaderGuid;
            partyUpdate.LeaderFactionGroup = _leaderFactionGroup;

            partyUpdate.SequenceNum = player.NextGroupUpdateSequenceNumber(_groupCategory);

            partyUpdate.MyIndex = -1;
            byte index = 0;

            for (var i = 0; i < _memberSlots.Count; ++i, ++index)
            {
                var member = _memberSlots[i];

                if (memberSlot.guid == member.guid)
                    partyUpdate.MyIndex = index;

                Player memberPlayer = Global.ObjAccessor.FindConnectedPlayer(member.guid);

                PartyPlayerInfo playerInfos = new();

                playerInfos.GUID = member.guid;
                playerInfos.Name = member.name;
                playerInfos.Class = member._class;

                playerInfos.FactionGroup = Player.GetFactionGroupForRace(member.race);

                playerInfos.Connected = memberPlayer?.GetSession() != null && !memberPlayer.GetSession().PlayerLogout();

                playerInfos.Subgroup = member.group;       // groupid
                playerInfos.Flags = (byte)member.flags; // See enum GroupMemberFlags
                playerInfos.RolesAssigned = (byte)member.roles; // Lfg Roles

                partyUpdate.PlayerList.Add(playerInfos);
            }

            if (GetMembersCount() > 1)
            {
                // LootSettings
                PartyLootSettings lootSettings = new();

                lootSettings.Method = (byte)_lootMethod;
                lootSettings.Threshold = (byte)_lootThreshold;
                lootSettings.LootMaster = _lootMethod == LootMethod.MasterLoot ? _masterLooterGuid : ObjectGuid.Empty;

                partyUpdate.LootSettings = lootSettings;

                // Difficulty Settings
                PartyDifficultySettings difficultySettings = new();

                difficultySettings.DungeonDifficultyID = (uint)_dungeonDifficulty;
                difficultySettings.RaidDifficultyID = (uint)_raidDifficulty;
                difficultySettings.LegacyRaidDifficultyID = (uint)_legacyRaidDifficulty;

                partyUpdate.DifficultySettings = difficultySettings;
            }

            // LfgInfos
            if (IsLFGGroup())
            {
                PartyLFGInfo lfgInfos = new();

                lfgInfos.Slot = Global.LFGMgr.GetLFGDungeonEntry(Global.LFGMgr.GetDungeon(_guid));
                lfgInfos.BootCount = 0;
                lfgInfos.Aborted = false;

                lfgInfos.MyFlags = (byte)(Global.LFGMgr.GetState(_guid) == LfgState.FinishedDungeon ? 2 : 0);
                lfgInfos.MyRandomSlot = Global.LFGMgr.GetSelectedRandomDungeon(player.GetGUID());

                lfgInfos.MyPartialClear = 0;
                lfgInfos.MyGearDiff = 0.0f;
                lfgInfos.MyFirstReward = false;

                LfgReward reward = Global.LFGMgr.GetRandomDungeonReward(partyUpdate.LfgInfos.Value.MyRandomSlot, player.GetLevel());

                if (reward != null)
                {
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(reward.firstQuest);

                    if (quest != null)
                        lfgInfos.MyFirstReward = player.CanRewardQuest(quest, false);
                }

                lfgInfos.MyStrangerCount = 0;
                lfgInfos.MyKickVoteCount = 0;

                partyUpdate.LfgInfos = lfgInfos;
            }

            player.SendPacket(partyUpdate);
        }

        private void SendUpdateDestroyGroupToPlayer(Player player)
        {
            PartyUpdate partyUpdate = new();
            partyUpdate.PartyFlags = GroupFlags.Destroyed;
            partyUpdate.PartyIndex = (byte)_groupCategory;
            partyUpdate.PartyType = GroupType.None;
            partyUpdate.PartyGUID = _guid;
            partyUpdate.MyIndex = -1;
            partyUpdate.SequenceNum = player.NextGroupUpdateSequenceNumber(_groupCategory);
            player.SendPacket(partyUpdate);
        }

        public void UpdatePlayerOutOfRange(Player player)
        {
            if (!player ||
                !player.IsInWorld)
                return;

            PartyMemberFullState packet = new();
            packet.Initialize(player);

            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player member = refe.GetSource();

                if (member &&
                    member != player &&
                    (!member.IsInMap(player) || !member.IsWithinDist(player, member.GetSightRange(), false)))
                    member.SendPacket(packet);
            }
        }

        public void BroadcastAddonMessagePacket(ServerPacket packet, string prefix, bool ignorePlayersInBGRaid, int group = -1, ObjectGuid ignore = default)
        {
            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();

                if (player == null ||
                    (!ignore.IsEmpty() && player.GetGUID() == ignore) ||
                    (ignorePlayersInBGRaid && player.GetGroup() != this))
                    continue;

                if ((group == -1 || refe.GetSubGroup() == group))
                    if (player.GetSession().IsAddonRegistered(prefix))
                        player.SendPacket(packet);
            }
        }

        public void BroadcastPacket(ServerPacket packet, bool ignorePlayersInBGRaid, int group = -1, ObjectGuid ignore = default)
        {
            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();

                if (!player ||
                    (!ignore.IsEmpty() && player.GetGUID() == ignore) ||
                    (ignorePlayersInBGRaid && player.GetGroup() != this))
                    continue;

                if (player.GetSession() != null &&
                    (group == -1 || refe.GetSubGroup() == group))
                    player.SendPacket(packet);
            }
        }

        private bool _setMembersGroup(ObjectGuid guid, byte group)
        {
            var slot = _getMemberSlot(guid);

            if (slot == null)
                return false;

            slot.group = group;

            SubGroupCounterIncrease(group);

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_SUBGROUP);

                stmt.AddValue(0, group);
                stmt.AddValue(1, guid.GetCounter());

                DB.Characters.Execute(stmt);
            }

            return true;
        }

        public bool SameSubGroup(Player member1, Player member2)
        {
            if (!member1 ||
                !member2)
                return false;

            if (member1.GetGroup() != this ||
                member2.GetGroup() != this)
                return false;
            else
                return member1.GetSubGroup() == member2.GetSubGroup();
        }

        public void ChangeMembersGroup(ObjectGuid guid, byte group)
        {
            // Only raid groups have sub groups
            if (!IsRaidGroup())
                return;

            // Check if player is really in the raid
            var slot = _getMemberSlot(guid);

            if (slot == null)
                return;

            byte prevSubGroup = slot.group;

            // Abort if the player is already in the Target sub group
            if (prevSubGroup == group)
                return;

            // Update the player Slot with the new sub group setting
            slot.group = group;

            // Increase the counter of the new sub group..
            SubGroupCounterIncrease(group);

            // ..and decrease the counter of the previous one
            SubGroupCounterDecrease(prevSubGroup);

            // Preserve new sub group in database for non-raid groups
            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_SUBGROUP);

                stmt.AddValue(0, group);
                stmt.AddValue(1, guid.GetCounter());

                DB.Characters.Execute(stmt);
            }

            // In case the moved player is online, update the player object with the new sub group references
            Player player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
            {
                if (player.GetGroup() == this)
                    player.GetGroupRef().SetSubGroup(group);
                else
                    // If player is in BG raid, it is possible that he is also in normal raid - and that normal raid is stored in _originalGroup reference
                    player.GetOriginalGroupRef().SetSubGroup(group);
            }

            // Broadcast the changes to the group
            SendUpdate();
        }

        public void SwapMembersGroups(ObjectGuid firstGuid, ObjectGuid secondGuid)
        {
            if (!IsRaidGroup())
                return;

            MemberSlot[] slots = new MemberSlot[2];
            slots[0] = _getMemberSlot(firstGuid);
            slots[1] = _getMemberSlot(secondGuid);

            if (slots[0] == null ||
                slots[1] == null)
                return;

            if (slots[0].group == slots[1].group)
                return;

            byte tmp = slots[0].group;
            slots[0].group = slots[1].group;
            slots[1].group = tmp;

            SQLTransaction trans = new();

            for (byte i = 0; i < 2; i++)
            {
                // Preserve new sub group in database for non-raid groups
                if (!IsBGGroup() &&
                    !IsBFGroup())
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_SUBGROUP);
                    stmt.AddValue(0, slots[i].group);
                    stmt.AddValue(1, slots[i].guid.GetCounter());

                    trans.Append(stmt);
                }

                Player player = Global.ObjAccessor.FindConnectedPlayer(slots[i].guid);

                if (player)
                {
                    if (player.GetGroup() == this)
                        player.GetGroupRef().SetSubGroup(slots[i].group);
                    else
                        player.GetOriginalGroupRef().SetSubGroup(slots[i].group);
                }
            }

            DB.Characters.CommitTransaction(trans);

            SendUpdate();
        }

        public void UpdateLooterGuid(WorldObject pLootedObject, bool ifneed = false)
        {
            switch (GetLootMethod())
            {
                case LootMethod.MasterLoot:
                case LootMethod.FreeForAll:
                    return;
                default:
                    // round robin style looting applies for all low
                    // quality items in each loot method except free for all and master loot
                    break;
            }

            ObjectGuid oldLooterGUID = GetLooterGuid();
            var memberSlot = _getMemberSlot(oldLooterGUID);

            if (memberSlot != null)
                if (ifneed)
                {
                    // not update if only update if need and ok
                    Player looter = Global.ObjAccessor.FindPlayer(memberSlot.guid);

                    if (looter && looter.IsAtGroupRewardDistance(pLootedObject))
                        return;
                }

            // search next after current
            Player pNewLooter = null;

            foreach (var member in _memberSlots)
            {
                if (member == memberSlot)
                    continue;

                Player player = Global.ObjAccessor.FindPlayer(member.guid);

                if (player)
                    if (player.IsAtGroupRewardDistance(pLootedObject))
                    {
                        pNewLooter = player;

                        break;
                    }
            }

            if (!pNewLooter)
                // search from start
                foreach (var member in _memberSlots)
                {
                    Player player = Global.ObjAccessor.FindPlayer(member.guid);

                    if (player)
                        if (player.IsAtGroupRewardDistance(pLootedObject))
                        {
                            pNewLooter = player;

                            break;
                        }
                }

            if (pNewLooter)
            {
                if (oldLooterGUID != pNewLooter.GetGUID())
                {
                    SetLooterGuid(pNewLooter.GetGUID());
                    SendUpdate();
                }
            }
            else
            {
                SetLooterGuid(ObjectGuid.Empty);
                SendUpdate();
            }
        }

        public GroupJoinBattlegroundResult CanJoinBattlegroundQueue(Battleground bgOrTemplate, BattlegroundQueueTypeId bgQueueTypeId, uint MinPlayerCount, uint MaxPlayerCount, bool isRated, uint arenaSlot, out ObjectGuid errorGuid)
        {
            errorGuid = new ObjectGuid();

            // check if this group is LFG group
            if (IsLFGGroup())
                return GroupJoinBattlegroundResult.LfgCantUseBattleground;

            BattlemasterListRecord bgEntry = CliDB.BattlemasterListStorage.LookupByKey(bgOrTemplate.GetTypeID());

            if (bgEntry == null)
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed; // shouldn't happen

            // check for min / max Count
            uint memberscount = GetMembersCount();

            if (memberscount > bgEntry.MaxGroupSize)     // no MinPlayerCount for Battlegrounds
                return GroupJoinBattlegroundResult.None; // ERR_GROUP_JOIN_Battleground_TOO_MANY handled on client side

            // get a player as reference, to compare other players' Stats to (arena team Id, queue Id based on level, etc.)
            Player reference = GetFirstMember().GetSource();

            // no reference found, can't join this way
            if (!reference)
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bgOrTemplate.GetMapId(), reference.GetLevel());

            if (bracketEntry == null)
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

            uint arenaTeamId = reference.GetArenaTeamId((byte)arenaSlot);
            Team team = reference.GetTeam();
            bool isMercenary = reference.HasAura(BattlegroundConst.SpellMercenaryContractHorde) || reference.HasAura(BattlegroundConst.SpellMercenaryContractAlliance);

            // check every member of the group to be able to join
            memberscount = 0;

            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next(), ++memberscount)
            {
                Player member = refe.GetSource();

                // offline member? don't let join
                if (!member)
                    return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

                // rbac permissions
                if (!member.CanJoinToBattleground(bgOrTemplate))
                    return GroupJoinBattlegroundResult.JoinTimedOut;

                // don't allow cross-faction join as group
                if (member.GetTeam() != team)
                {
                    errorGuid = member.GetGUID();

                    return GroupJoinBattlegroundResult.JoinTimedOut;
                }

                // not in the same Battleground level braket, don't let join
                PvpDifficultyRecord memberBracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bracketEntry.MapID, member.GetLevel());

                if (memberBracketEntry != bracketEntry)
                    return GroupJoinBattlegroundResult.JoinRangeIndex;

                // don't let join rated matches if the arena team Id doesn't match
                if (isRated && member.GetArenaTeamId((byte)arenaSlot) != arenaTeamId)
                    return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

                // don't let join if someone from the group is already in that bg queue
                if (member.InBattlegroundQueueForBattlegroundQueueType(bgQueueTypeId))
                    return GroupJoinBattlegroundResult.BattlegroundJoinFailed; // not blizz-like

                // don't let join if someone from the group is in bg queue random
                bool isInRandomBgQueue = member.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RB, BattlegroundQueueIdType.Battleground, false, 0)) || member.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RandomEpic, BattlegroundQueueIdType.Battleground, false, 0));

                if (bgOrTemplate.GetTypeID() != BattlegroundTypeId.AA && isInRandomBgQueue)
                    return GroupJoinBattlegroundResult.InRandomBg;

                // don't let join to bg queue random if someone from the group is already in bg queue
                if ((bgOrTemplate.GetTypeID() == BattlegroundTypeId.RB || bgOrTemplate.GetTypeID() == BattlegroundTypeId.RandomEpic) &&
                    member.InBattlegroundQueue(true) &&
                    !isInRandomBgQueue)
                    return GroupJoinBattlegroundResult.InNonRandomBg;

                // check for deserter debuff in case not arena queue
                if (bgOrTemplate.GetTypeID() != BattlegroundTypeId.AA &&
                    member.IsDeserter())
                    return GroupJoinBattlegroundResult.Deserters;

                // check if member can join any more Battleground queues
                if (!member.HasFreeBattlegroundQueueId())
                    return GroupJoinBattlegroundResult.TooManyQueues; // not blizz-like

                // check if someone in party is using dungeon system
                if (member.IsUsingLfg())
                    return GroupJoinBattlegroundResult.LfgCantUseBattleground;

                // check Freeze debuff
                if (member.HasAura(9454))
                    return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

                if (isMercenary != (member.HasAura(BattlegroundConst.SpellMercenaryContractHorde) || member.HasAura(BattlegroundConst.SpellMercenaryContractAlliance)))
                    return GroupJoinBattlegroundResult.BattlegroundJoinMercenary;
            }

            // only check for MinPlayerCount since MinPlayerCount == MaxPlayerCount for arenas...
            if (bgOrTemplate.IsArena() &&
                memberscount != MinPlayerCount)
                return GroupJoinBattlegroundResult.ArenaTeamPartySize;

            return GroupJoinBattlegroundResult.None;
        }

        public void SetDungeonDifficultyID(Difficulty difficulty)
        {
            _dungeonDifficulty = difficulty;

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_DIFFICULTY);

                stmt.AddValue(0, (byte)_dungeonDifficulty);
                stmt.AddValue(1, _dbStoreId);

                DB.Characters.Execute(stmt);
            }

            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();

                if (player.GetSession() == null)
                    continue;

                player.SetDungeonDifficultyID(difficulty);
                player.SendDungeonDifficulty();
            }
        }

        public void SetRaidDifficultyID(Difficulty difficulty)
        {
            _raidDifficulty = difficulty;

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_RAID_DIFFICULTY);

                stmt.AddValue(0, (byte)_raidDifficulty);
                stmt.AddValue(1, _dbStoreId);

                DB.Characters.Execute(stmt);
            }

            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();

                if (player.GetSession() == null)
                    continue;

                player.SetRaidDifficultyID(difficulty);
                player.SendRaidDifficulty(false);
            }
        }

        public void SetLegacyRaidDifficultyID(Difficulty difficulty)
        {
            _legacyRaidDifficulty = difficulty;

            if (!IsBGGroup() &&
                !IsBFGroup())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_LEGACY_RAID_DIFFICULTY);

                stmt.AddValue(0, (byte)_legacyRaidDifficulty);
                stmt.AddValue(1, _dbStoreId);

                DB.Characters.Execute(stmt);
            }

            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();

                if (player.GetSession() == null)
                    continue;

                player.SetLegacyRaidDifficultyID(difficulty);
                player.SendRaidDifficulty(true);
            }
        }

        public Difficulty GetDifficultyID(MapRecord mapEntry)
        {
            if (!mapEntry.IsRaid())
                return _dungeonDifficulty;

            MapDifficultyRecord defaultDifficulty = Global.DB2Mgr.GetDefaultMapDifficulty(mapEntry.Id);

            if (defaultDifficulty == null)
                return _legacyRaidDifficulty;

            DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(defaultDifficulty.DifficultyID);

            if (difficulty == null ||
                difficulty.Flags.HasAnyFlag(DifficultyFlags.Legacy))
                return _legacyRaidDifficulty;

            return _raidDifficulty;
        }

        public Difficulty GetDungeonDifficultyID()
        {
            return _dungeonDifficulty;
        }

        public Difficulty GetRaidDifficultyID()
        {
            return _raidDifficulty;
        }

        public Difficulty GetLegacyRaidDifficultyID()
        {
            return _legacyRaidDifficulty;
        }

        public void ResetInstances(InstanceResetMethod method, Player notifyPlayer)
        {
            for (GroupInstanceReference refe = _ownedInstancesMgr.GetFirst(); refe != null; refe = refe.Next())
            {
                InstanceMap map = refe.GetSource();

                switch (map.Reset(method))
                {
                    case InstanceResetResult.Success:
                        notifyPlayer.SendResetInstanceSuccess(map.GetId());
                        _recentInstances.Remove(map.GetId());

                        break;
                    case InstanceResetResult.NotEmpty:
                        if (method == InstanceResetMethod.Manual)
                            notifyPlayer.SendResetInstanceFailed(ResetFailedReason.Failed, map.GetId());
                        else if (method == InstanceResetMethod.OnChangeDifficulty)
                            _recentInstances.Remove(map.GetId()); // map might not have been reset on difficulty change but we still don't want to zone in there again

                        break;
                    case InstanceResetResult.CannotReset:
                        _recentInstances.Remove(map.GetId()); // forget the instance, allows retrying different lockout with a new leader

                        break;
                    default:
                        break;
                }
            }
        }

        public void LinkOwnedInstance(GroupInstanceReference refe)
        {
            _ownedInstancesMgr.InsertLast(refe);
        }

        private void _homebindIfInstance(Player player)
        {
            if (player &&
                !player.IsGameMaster() &&
                CliDB.MapStorage.LookupByKey(player.GetMapId()).IsDungeon())
                player.InstanceValid = false;
        }

        public void BroadcastGroupUpdate()
        {
            // FG: HACK: Force Flags update on group leave - for values update hack
            // -- not very efficient but safe
            foreach (var member in _memberSlots)
            {
                Player pp = Global.ObjAccessor.FindPlayer(member.guid);

                if (pp && pp.IsInWorld)
                {
                    pp.Values.ModifyValue(pp.UnitData).ModifyValue(pp.UnitData.PvpFlags);
                    pp.Values.ModifyValue(pp.UnitData).ModifyValue(pp.UnitData.FactionTemplate);
                    pp.ForceUpdateFieldChange();
                    Log.outDebug(LogFilter.Server, "-- Forced group value update for '{0}'", pp.GetName());
                }
            }
        }

        public void SetLootMethod(LootMethod method)
        {
            _lootMethod = method;
        }

        public void SetLooterGuid(ObjectGuid guid)
        {
            _looterGuid = guid;
        }

        public void SetMasterLooterGuid(ObjectGuid guid)
        {
            _masterLooterGuid = guid;
        }

        public void SetLootThreshold(ItemQuality threshold)
        {
            _lootThreshold = threshold;
        }

        public void SetLfgRoles(ObjectGuid guid, LfgRoles roles)
        {
            var slot = _getMemberSlot(guid);

            if (slot == null)
                return;

            slot.roles = roles;
            SendUpdate();
        }

        public LfgRoles GetLfgRoles(ObjectGuid guid)
        {
            MemberSlot slot = _getMemberSlot(guid);

            if (slot == null)
                return 0;

            return slot.roles;
        }

        private void UpdateReadyCheck(uint diff)
        {
            if (!_readyCheckStarted)
                return;

            _readyCheckTimer -= TimeSpan.FromMilliseconds(diff);

            if (_readyCheckTimer <= TimeSpan.Zero)
                EndReadyCheck();
        }

        public void StartReadyCheck(ObjectGuid starterGuid, sbyte partyIndex, TimeSpan duration)
        {
            if (_readyCheckStarted)
                return;

            MemberSlot slot = _getMemberSlot(starterGuid);

            if (slot == null)
                return;

            _readyCheckStarted = true;
            _readyCheckTimer = duration;

            SetOfflineMembersReadyChecked();

            SetMemberReadyChecked(slot);

            ReadyCheckStarted readyCheckStarted = new();
            readyCheckStarted.PartyGUID = _guid;
            readyCheckStarted.PartyIndex = partyIndex;
            readyCheckStarted.InitiatorGUID = starterGuid;
            readyCheckStarted.Duration = (uint)duration.TotalMilliseconds;
            BroadcastPacket(readyCheckStarted, false);
        }

        private void EndReadyCheck()
        {
            if (!_readyCheckStarted)
                return;

            _readyCheckStarted = false;
            _readyCheckTimer = TimeSpan.Zero;

            ResetMemberReadyChecked();

            ReadyCheckCompleted readyCheckCompleted = new();
            readyCheckCompleted.PartyIndex = 0;
            readyCheckCompleted.PartyGUID = _guid;
            BroadcastPacket(readyCheckCompleted, false);
        }

        private bool IsReadyCheckCompleted()
        {
            foreach (var member in _memberSlots)
                if (!member.readyChecked)
                    return false;

            return true;
        }

        public void SetMemberReadyCheck(ObjectGuid guid, bool ready)
        {
            if (!_readyCheckStarted)
                return;

            MemberSlot slot = _getMemberSlot(guid);

            if (slot != null)
                SetMemberReadyCheck(slot, ready);
        }

        private void SetMemberReadyCheck(MemberSlot slot, bool ready)
        {
            ReadyCheckResponse response = new();
            response.PartyGUID = _guid;
            response.Player = slot.guid;
            response.IsReady = ready;
            BroadcastPacket(response, false);

            SetMemberReadyChecked(slot);
        }

        private void SetOfflineMembersReadyChecked()
        {
            foreach (MemberSlot member in _memberSlots)
            {
                Player player = Global.ObjAccessor.FindConnectedPlayer(member.guid);

                if (!player ||
                    !player.GetSession())
                    SetMemberReadyCheck(member, false);
            }
        }

        private void SetMemberReadyChecked(MemberSlot slot)
        {
            slot.readyChecked = true;

            if (IsReadyCheckCompleted())
                EndReadyCheck();
        }

        private void ResetMemberReadyChecked()
        {
            foreach (MemberSlot member in _memberSlots)
                member.readyChecked = false;
        }

        public void AddRaidMarker(byte markerId, uint mapId, float positionX, float positionY, float positionZ, ObjectGuid transportGuid = default)
        {
            if (markerId >= MapConst.RaidMarkersCount ||
                _markers[markerId] != null)
                return;

            _activeMarkers |= (1u << markerId);
            _markers[markerId] = new RaidMarker(mapId, positionX, positionY, positionZ, transportGuid);
            SendRaidMarkersChanged();
        }

        public void DeleteRaidMarker(byte markerId)
        {
            if (markerId > MapConst.RaidMarkersCount)
                return;

            for (byte i = 0; i < MapConst.RaidMarkersCount; i++)
                if (_markers[i] != null &&
                    (markerId == i || markerId == MapConst.RaidMarkersCount))
                {
                    _markers[i] = null;
                    _activeMarkers &= ~(1u << i);
                }

            SendRaidMarkersChanged();
        }

        public void SendRaidMarkersChanged(WorldSession session = null, sbyte partyIndex = 0)
        {
            RaidMarkersChanged packet = new();

            packet.PartyIndex = partyIndex;
            packet.ActiveMarkers = _activeMarkers;

            for (byte i = 0; i < MapConst.RaidMarkersCount; i++)
                if (_markers[i] != null)
                    packet.RaidMarkers.Add(_markers[i]);

            if (session)
                session.SendPacket(packet);
            else
                BroadcastPacket(packet, false);
        }

        public bool IsFull()
        {
            return IsRaidGroup() ? (_memberSlots.Count >= MapConst.MaxRaidSize) : (_memberSlots.Count >= MapConst.MaxGroupSize);
        }

        public bool IsLFGGroup()
        {
            return _groupFlags.HasAnyFlag(GroupFlags.Lfg);
        }

        public bool IsRaidGroup()
        {
            return _groupFlags.HasAnyFlag(GroupFlags.Raid);
        }

        public bool IsBGGroup()
        {
            return _bgGroup != null;
        }

        public bool IsBFGroup()
        {
            return _bfGroup != null;
        }

        public bool IsCreated()
        {
            return GetMembersCount() > 0;
        }

        public ObjectGuid GetLeaderGUID()
        {
            return _leaderGuid;
        }

        public ObjectGuid GetGUID()
        {
            return _guid;
        }

        public ulong GetLowGUID()
        {
            return _guid.GetCounter();
        }

        private string GetLeaderName()
        {
            return _leaderName;
        }

        public LootMethod GetLootMethod()
        {
            return _lootMethod;
        }

        public ObjectGuid GetLooterGuid()
        {
            if (GetLootMethod() == LootMethod.FreeForAll)
                return ObjectGuid.Empty;

            return _looterGuid;
        }

        public ObjectGuid GetMasterLooterGuid()
        {
            return _masterLooterGuid;
        }

        public ItemQuality GetLootThreshold()
        {
            return _lootThreshold;
        }

        public bool IsMember(ObjectGuid guid)
        {
            return _getMemberSlot(guid) != null;
        }

        public bool IsLeader(ObjectGuid guid)
        {
            return GetLeaderGUID() == guid;
        }

        public bool IsAssistant(ObjectGuid guid)
        {
            return GetMemberFlags(guid).HasAnyFlag(GroupMemberFlags.Assistant);
        }

        public ObjectGuid GetMemberGUID(string name)
        {
            foreach (var member in _memberSlots)
                if (member.name == name)
                    return member.guid;

            return ObjectGuid.Empty;
        }

        public GroupMemberFlags GetMemberFlags(ObjectGuid guid)
        {
            var mslot = _getMemberSlot(guid);

            if (mslot == null)
                return 0;

            return mslot.flags;
        }

        public bool SameSubGroup(ObjectGuid guid1, ObjectGuid guid2)
        {
            var mslot2 = _getMemberSlot(guid2);

            if (mslot2 == null)
                return false;

            return SameSubGroup(guid1, mslot2);
        }

        public bool SameSubGroup(ObjectGuid guid1, MemberSlot slot2)
        {
            var mslot1 = _getMemberSlot(guid1);

            if (mslot1 == null ||
                slot2 == null)
                return false;

            return (mslot1.group == slot2.group);
        }

        public bool HasFreeSlotSubGroup(byte subgroup)
        {
            return (_subGroupsCounts != null && _subGroupsCounts[subgroup] < MapConst.MaxGroupSize);
        }

        public byte GetMemberGroup(ObjectGuid guid)
        {
            var mslot = _getMemberSlot(guid);

            if (mslot == null)
                return (byte)(MapConst.MaxRaidSubGroups + 1);

            return mslot.group;
        }

        public void SetBattlegroundGroup(Battleground bg)
        {
            _bgGroup = bg;
        }

        public void SetBattlefieldGroup(BattleField bg)
        {
            _bfGroup = bg;
        }

        public void SetGroupMemberFlag(ObjectGuid guid, bool apply, GroupMemberFlags flag)
        {
            // Assistants, main assistants and main tanks are only available in raid groups
            if (!IsRaidGroup())
                return;

            // Check if player is really in the raid
            var slot = _getMemberSlot(guid);

            if (slot == null)
                return;

            // Do flag specific actions, e.g ensure uniqueness
            switch (flag)
            {
                case GroupMemberFlags.MainAssist:
                    RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainAssist); // Remove main assist flag from current if any.

                    break;
                case GroupMemberFlags.MainTank:
                    RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainTank); // Remove main tank flag from current if any.

                    break;
                case GroupMemberFlags.Assistant:
                    break;
                default:
                    return; // This should never happen
            }

            // Switch the actual flag
            ToggleGroupMemberFlag(slot, flag, apply);

            // Preserve the new setting in the db
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_FLAG);

            stmt.AddValue(0, (byte)slot.flags);
            stmt.AddValue(1, guid.GetCounter());

            DB.Characters.Execute(stmt);

            // Broadcast the changes to the group
            SendUpdate();
        }

        public void LinkMember(GroupReference pRef)
        {
            _memberMgr.InsertFirst(pRef);
        }

        private void DelinkMember(ObjectGuid guid)
        {
            GroupReference refe = _memberMgr.GetFirst();

            while (refe != null)
            {
                GroupReference nextRef = refe.Next();

                if (refe.GetSource().GetGUID() == guid)
                {
                    refe.Unlink();

                    break;
                }

                refe = nextRef;
            }
        }

        private void _initRaidSubGroupsCounter()
        {
            // Sub group counters initialization
            if (_subGroupsCounts == null)
                _subGroupsCounts = new byte[MapConst.MaxRaidSubGroups];

            foreach (var memberSlot in _memberSlots)
                ++_subGroupsCounts[memberSlot.group];
        }

        private MemberSlot _getMemberSlot(ObjectGuid guid)
        {
            foreach (var member in _memberSlots)
                if (member.guid == guid)
                    return member;

            return null;
        }

        private void SubGroupCounterIncrease(byte subgroup)
        {
            if (_subGroupsCounts != null)
                ++_subGroupsCounts[subgroup];
        }

        private void SubGroupCounterDecrease(byte subgroup)
        {
            if (_subGroupsCounts != null)
                --_subGroupsCounts[subgroup];
        }

        public void RemoveUniqueGroupMemberFlag(GroupMemberFlags flag)
        {
            foreach (var member in _memberSlots)
                if (member.flags.HasAnyFlag(flag))
                    member.flags &= ~flag;
        }

        private void ToggleGroupMemberFlag(MemberSlot slot, GroupMemberFlags flag, bool apply)
        {
            if (apply)
                slot.flags |= flag;
            else
                slot.flags &= ~flag;
        }

        public void StartLeaderOfflineTimer()
        {
            _isLeaderOffline = true;
            _leaderOfflineTimer.Reset(2 * Time.Minute * Time.InMilliseconds);
        }

        public void StopLeaderOfflineTimer()
        {
            _isLeaderOffline = false;
        }

        public void SetEveryoneIsAssistant(bool apply)
        {
            if (apply)
                _groupFlags |= GroupFlags.EveryoneAssistant;
            else
                _groupFlags &= ~GroupFlags.EveryoneAssistant;

            foreach (MemberSlot member in _memberSlots)
                ToggleGroupMemberFlag(member, GroupMemberFlags.Assistant, apply);

            SendUpdate();
        }

        public GroupCategory GetGroupCategory()
        {
            return _groupCategory;
        }

        public uint GetDbStoreId()
        {
            return _dbStoreId;
        }

        public List<MemberSlot> GetMemberSlots()
        {
            return _memberSlots;
        }

        public GroupReference GetFirstMember()
        {
            return (GroupReference)_memberMgr.GetFirst();
        }

        public uint GetMembersCount()
        {
            return (uint)_memberSlots.Count;
        }

        public uint GetInviteeCount()
        {
            return (uint)_invitees.Count;
        }

        public GroupFlags GetGroupFlags()
        {
            return _groupFlags;
        }

        private bool IsReadyCheckStarted()
        {
            return _readyCheckStarted;
        }

        public void BroadcastWorker(Action<Player> worker)
        {
            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
                worker(refe.GetSource());
        }

        public ObjectGuid GetRecentInstanceOwner(uint mapId)
        {
            if (_recentInstances.TryGetValue(mapId, out Tuple<ObjectGuid, uint> value))
                return value.Item1;

            return _leaderGuid;
        }

        public uint GetRecentInstanceId(uint mapId)
        {
            if (_recentInstances.TryGetValue(mapId, out Tuple<ObjectGuid, uint> value))
                return value.Item2;

            return 0;
        }

        public void SetRecentInstance(uint mapId, ObjectGuid instanceOwner, uint instanceId)
        {
            _recentInstances[mapId] = Tuple.Create(instanceOwner, instanceId);
        }

        public static implicit operator bool(Group group)
        {
            return group != null;
        }
    }

    public class MemberSlot
    {
        public byte _class;
        public GroupMemberFlags flags;
        public byte group;
        public ObjectGuid guid;
        public string name;
        public Race race;
        public bool readyChecked;
        public LfgRoles roles;
    }

    public class RaidMarker
    {
        public WorldLocation Location;
        public ObjectGuid TransportGUID;

        public RaidMarker(uint mapId, float positionX, float positionY, float positionZ, ObjectGuid transportGuid = default)
        {
            Location = new WorldLocation(mapId, positionX, positionY, positionZ);
            TransportGUID = transportGuid;
        }
    }
}