﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals.Caching;
using Framework.Constants;

namespace Forged.MapServer.LootManagement;

public class LootItem
{
    private readonly ConditionManager _conditionManager;

    // quest drop
    private readonly ItemTemplateCache _itemTemplateCache;

    public LootItem(ItemTemplateCache itemTemplateCache, ConditionManager conditionManager)
    {
        _itemTemplateCache = itemTemplateCache;
        _conditionManager = conditionManager;
    }

    public LootItem(ItemTemplateCache itemTemplateCache, ConditionManager conditionManager, LootStoreItem li, ItemEnchantmentManager itemEnchantmentManager) : this(itemTemplateCache, conditionManager)
    {
        Itemid = li.Itemid;
        Conditions = li.Conditions;

        var proto = itemTemplateCache.GetItemTemplate(Itemid);
        Freeforall = proto != null && proto.HasFlag(ItemFlags.MultiDrop);
        FollowLootRules = !li.NeedsQuest || (proto != null && proto.FlagsCu.HasAnyFlag(ItemFlagsCustom.FollowLootRules));

        NeedsQuest = li.NeedsQuest;

        RandomBonusListId = itemEnchantmentManager.GenerateItemRandomBonusListId(Itemid);
    }

    public List<ObjectGuid> AllowedGuiDs { get; set; } = new();
    public List<uint> BonusListIDs { get; set; } = new();
    public List<Condition> Conditions { get; set; } = new();
    public ItemContext Context { get; set; }
    public byte Count { get; set; }
    public bool FollowLootRules { get; set; }
    public bool Freeforall { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsCounted { get; set; }

    public bool IsLooted { get; set; }

    // free for all
    public bool IsUnderthreshold { get; set; }

    public uint Itemid { get; set; }
    public uint LootListId { get; set; }
    public bool NeedsQuest { get; set; }

    public uint RandomBonusListId { get; set; }

    // additional loot condition
    public ObjectGuid RollWinnerGuid { get; set; } // Stores the guid of person who won loot, if his bags are full only he can see the item in loot list!

    public static bool AllowedForPlayer(Player player, Loot loot, uint itemid, bool needsQuest, bool followLootRules, bool strictUsabilityCheck, List<Condition> conditions, ItemTemplateCache itemTemplateCache, ConditionManager conditionManager)
    {
        // DB conditions check
        if (!conditionManager.IsObjectMeetToConditions(player, conditions))
            return false;

        var pProto = itemTemplateCache.GetItemTemplate(itemid);

        if (pProto == null)
            return false;

        // not show loot for not own team
        if (pProto.HasFlag(ItemFlags2.FactionHorde) && player.Team != TeamFaction.Horde)
            return false;

        if (pProto.HasFlag(ItemFlags2.FactionAlliance) && player.Team != TeamFaction.Alliance)
            return false;

        // Master looter can see all items even if the character can't loot them
        if (loot != null && loot.LootMethod == LootMethod.MasterLoot && followLootRules && loot.LootMasterGuid == player.GUID)
            return true;

        // Don't allow loot for players without profession or those who already know the recipe
        if (pProto.HasFlag(ItemFlags.HideUnusableRecipe))
        {
            if (!player.HasSkill((SkillType)pProto.RequiredSkill))
                return false;

            foreach (var itemEffect in pProto.Effects)
            {
                if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
                    continue;

                if (player.HasSpell((uint)itemEffect.SpellID))
                    return false;
            }
        }

        // check quest requirements
        if (!pProto.FlagsCu.HasAnyFlag(ItemFlagsCustom.IgnoreQuestStatus) && (needsQuest || (pProto.StartQuest != 0 && player.GetQuestStatus(pProto.StartQuest) != QuestStatus.None)) && !player.HasQuestForItem(itemid))
            return false;

        if (!strictUsabilityCheck)
            return true;

        if ((pProto.IsWeapon || pProto.IsArmor) && !pProto.IsUsableByLootSpecialization(player, true))
            return false;

        return player.CanRollNeedForItem(pProto, null, false) == InventoryResult.Ok;
    }

    public void AddAllowedLooter(Player player)
    {
        AllowedGuiDs.Add(player.GUID);
    }

    /// <summary>
    ///     Basic checks for player/item compatibility - if false no chance to see the item in the loot - used only for loot generation
    /// </summary>
    /// <param name="player"> </param>
    /// <param name="loot"> </param>
    /// <returns> </returns>
    public bool AllowedForPlayer(Player player, Loot loot)
    {
        return AllowedForPlayer(player, loot, Itemid, NeedsQuest, FollowLootRules, false, Conditions, _itemTemplateCache, _conditionManager);
    }

    public List<ObjectGuid> GetAllowedLooters()
    {
        return AllowedGuiDs;
    }

    public LootSlotType? GetUiTypeForPlayer(Player player, Loot loot)
    {
        if (IsLooted)
            return null;

        if (!AllowedGuiDs.Contains(player.GUID))
            return null;

        if (Freeforall)
        {
            var ffaItems = loot.PlayerFFAItems.LookupByKey(player.GUID);

            var ffaItemItr = ffaItems?.Find(ffaItem => ffaItem.LootListId == LootListId);

            if (ffaItemItr is { IsLooted: false })
                return loot.LootMethod == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;

            return null;
        }

        if (NeedsQuest && !FollowLootRules)
            return loot.LootMethod == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;

        switch (loot.LootMethod)
        {
            case LootMethod.FreeForAll:
                return LootSlotType.Owner;
            case LootMethod.RoundRobin:
                if (!loot.RoundRobinPlayer.IsEmpty && loot.RoundRobinPlayer != player.GUID)
                    return null;

                return LootSlotType.AllowLoot;
            case LootMethod.MasterLoot:
                if (!IsUnderthreshold)
                    return loot.LootMasterGuid == player.GUID ? LootSlotType.Master : LootSlotType.Locked;

                if (!loot.RoundRobinPlayer.IsEmpty && loot.RoundRobinPlayer != player.GUID)
                    return null;

                return LootSlotType.AllowLoot;

            case LootMethod.GroupLoot:
            case LootMethod.NeedBeforeGreed:
                if (IsUnderthreshold)
                    if (!loot.RoundRobinPlayer.IsEmpty && loot.RoundRobinPlayer != player.GUID)
                        return null;

                if (IsBlocked)
                    return LootSlotType.RollOngoing;

                if (RollWinnerGuid.IsEmpty) // all passed
                    return LootSlotType.AllowLoot;

                if (RollWinnerGuid == player.GUID)
                    return LootSlotType.Owner;

                return null;
            case LootMethod.PersonalLoot:
                return LootSlotType.Owner;
        }

        return null;
    }

    public bool HasAllowedLooter(ObjectGuid looter)
    {
        return AllowedGuiDs.Contains(looter);
    }
}