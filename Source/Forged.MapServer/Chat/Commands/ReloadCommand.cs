﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Achievements;
using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.AuctionHouse;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.Movement;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Skills;
using Forged.MapServer.SupportSystem;
using Forged.MapServer.Text;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Serilog;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("reload")]
internal class ReloadCommand
{
    [Command("access_requirement", RBACPermissions.CommandReloadAccessRequirement, true)]
    private static bool HandleReloadAccessRequirementCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Access Requirement definitions...");
        handler.ClassFactory.Resolve<AccessRequirementsCache>().Load();
        handler.SendGlobalGMSysMessage("DB table `access_requirement` reloaded.");

        return true;
    }

    [Command("achievement_reward", RBACPermissions.CommandReloadAchievementReward, true)]
    private static bool HandleReloadAchievementRewardCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Achievement Reward Data...");
        handler.ClassFactory.Resolve<AchievementGlobalMgr>().LoadRewards();
        handler.SendGlobalGMSysMessage("DB table `achievement_reward` reloaded.");

        return true;
    }

    [Command("achievement_reward_locale", RBACPermissions.CommandReloadAchievementRewardLocale, true)]
    private static bool HandleReloadAchievementRewardLocaleCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Achievement Reward Data Locale...");
        handler.ClassFactory.Resolve<AchievementGlobalMgr>().LoadRewardLocales();
        handler.SendGlobalGMSysMessage("DB table `achievement_reward_locale` reloaded.");

        return true;
    }

    [Command("areatrigger_tavern", RBACPermissions.CommandReloadAreatriggerTavern, true)]
    private static bool HandleReloadAreaTriggerTavernCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Tavern Area Triggers...");
        handler.ObjectManager.LoadTavernAreaTriggers();
        handler.SendGlobalGMSysMessage("DB table `areatrigger_tavern` reloaded.");

        return true;
    }

    [Command("areatrigger_teleport", RBACPermissions.CommandReloadAreatriggerTeleport, true)]
    private static bool HandleReloadAreaTriggerTeleportCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading AreaTrigger teleport definitions...");
        handler.ClassFactory.Resolve<AreaTriggerCache>().Load();
        handler.SendGlobalGMSysMessage("DB table `areatrigger_teleport` reloaded.");

        return true;
    }

    [Command("areatrigger_template", RBACPermissions.CommandReloadSceneTemplate, true)]
    private static bool HandleReloadAreaTriggerTemplateCommand(CommandHandler handler)
    {
        Log.Logger.Information("Reloading areatrigger_template table...");
        handler.ClassFactory.Resolve<AreaTriggerDataStorage>().LoadAreaTriggerTemplates();
        handler.SendGlobalGMSysMessage("AreaTrigger templates reloaded. Already spawned AT won't be affected. New scriptname need a reboot.");

        return true;
    }

    [Command("auctions", RBACPermissions.CommandReloadAuctions, true)]
    private static bool HandleReloadAuctionsCommand(CommandHandler handler)
    {
        // Reload dynamic data tables from the database
        Log.Logger.Information("Re-Loading Auctions...");
        handler.ClassFactory.Resolve<AuctionManager>().LoadAuctions();
        handler.SendGlobalGMSysMessage("Auctions reloaded.");

        return true;
    }

    [Command("autobroadcast", RBACPermissions.CommandReloadAutobroadcast, true)]
    private static bool HandleReloadAutobroadcastCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Autobroadcasts...");
        handler.WorldManager.LoadAutobroadcasts();
        handler.SendGlobalGMSysMessage("DB table `autobroadcast` reloaded.");

        return true;
    }

    [Command("battleground_template", RBACPermissions.CommandReloadBattlegroundTemplate, true)]
    private static bool HandleReloadBattlegroundTemplate(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Battleground Templates...");
        handler.ClassFactory.Resolve<BattlegroundManager>().LoadBattlegroundTemplates();
        handler.SendGlobalGMSysMessage("DB table `battleground_template` reloaded.");

        return true;
    }

    [Command("character_template", RBACPermissions.CommandReloadCharacterTemplate, true)]
    private static bool HandleReloadCharacterTemplate(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Character Templates...");
        handler.ClassFactory.Resolve<CharacterTemplateDataStorage>().LoadCharacterTemplates();
        handler.SendGlobalGMSysMessage("DB table `character_template` and `character_template_class` reloaded.");

        return true;
    }

    [Command("conditions", RBACPermissions.CommandReloadConditions, true)]
    private static bool HandleReloadConditions(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Conditions...");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);
        handler.SendGlobalGMSysMessage("Conditions reloaded.");

        return true;
    }

    [Command("config", RBACPermissions.CommandReloadConfig, true)]
    private static bool HandleReloadConfigCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading config settings...");
        handler.WorldManager.LoadConfigSettings(true);
        handler.ClassFactory.Resolve<MapManager>().InitializeVisibilityDistanceInfo();
        handler.SendGlobalGMSysMessage("World config settings reloaded.");

        return true;
    }

    [Command("conversation_template", RBACPermissions.CommandReloadConversationTemplate, true)]
    private static bool HandleReloadConversationTemplateCommand(CommandHandler handler)
    {
        Log.Logger.Information("Reloading conversation_* tables...");
        handler.ClassFactory.Resolve<ConversationDataStorage>().LoadConversationTemplates();
        handler.SendGlobalGMSysMessage("Conversation templates reloaded.");

        return true;
    }

    [Command("creature_movement_override", RBACPermissions.CommandReloadCreatureMovementOverride, true)]
    private static bool HandleReloadCreatureMovementOverrideCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Creature movement overrides...");
        handler.ObjectManager.CreatureMovementOverrideCache.Load();
        handler.SendGlobalGMSysMessage("DB table `creature_movement_override` reloaded.");

        return true;
    }

    [Command("creature_questender", RBACPermissions.CommandReloadCreatureQuestender, true)]
    private static bool HandleReloadCreatureQuestEnderCommand(CommandHandler handler)
    {
        Log.Logger.Information("Loading Quests Relations... (`creature_questender`)");
        handler.ObjectManager.LoadCreatureQuestEnders();
        handler.SendGlobalGMSysMessage("DB table `creature_questender` reloaded.");

        return true;
    }

    [Command("creature_queststarter", RBACPermissions.CommandReloadCreatureQueststarter, true)]
    private static bool HandleReloadCreatureQuestStarterCommand(CommandHandler handler)
    {
        Log.Logger.Information("Loading Quests Relations... (`creature_queststarter`)");
        handler.ObjectManager.LoadCreatureQuestStarters();
        handler.SendGlobalGMSysMessage("DB table `creature_queststarter` reloaded.");

        return true;
    }

    [Command("creature_summon_groups", RBACPermissions.CommandReloadCreatureSummonGroups, true)]
    private static bool HandleReloadCreatureSummonGroupsCommand(CommandHandler handler)
    {
        Log.Logger.Information("Reloading creature summon groups...");
        handler.ObjectManager.LoadTempSummons();
        handler.SendGlobalGMSysMessage("DB table `creature_summon_groups` reloaded.");

        return true;
    }

    [Command("creature_template", RBACPermissions.CommandReloadCreatureTemplate, true)]
    private static bool HandleReloadCreatureTemplateCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        uint entry;

        while ((entry = args.NextUInt32()) != 0)
        {
            var stmt = handler.ClassFactory.Resolve<WorldDatabase>().GetPreparedStatement(WorldStatements.SEL_CREATURE_TEMPLATE);
            stmt.AddValue(0, entry);
            stmt.AddValue(1, 0);
            var result = handler.ClassFactory.Resolve<WorldDatabase>().Query(stmt);

            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.CommandCreaturetemplateNotfound, entry);

                continue;
            }

            var cInfo = handler.ObjectManager.CreatureTemplateCache.GetCreatureTemplate(entry);

            if (cInfo == null)
            {
                handler.SendSysMessage(CypherStrings.CommandCreaturestorageNotfound, entry);

                continue;
            }

            Log.Logger.Information("Reloading creature template entry {0}", entry);

            handler.ObjectManager.CreatureTemplateCache.LoadCreatureTemplate(result.GetFields());
            handler.ObjectManager.CheckCreatureTemplate(cInfo);
        }

        handler.ObjectManager.InitializeQueriesData(QueryDataGroup.Creatures);
        handler.SendGlobalGMSysMessage("Creature template reloaded.");

        return true;
    }

    [Command("creature_template_locale", RBACPermissions.CommandReloadCreatureTemplateLocale, true)]
    private static bool HandleReloadCreatureTemplateLocaleCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Creature Template Locale...");
        handler.ObjectManager.CreatureLocaleCache.Load();
        handler.SendGlobalGMSysMessage("DB table `Creature Template Locale` reloaded.");

        return true;
    }

    [Command("creature_text", RBACPermissions.CommandReloadCreatureText, true)]
    private static bool HandleReloadCreatureText(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Creature Texts...");
        handler.ClassFactory.Resolve<CreatureTextManager>().LoadCreatureTexts();
        handler.SendGlobalGMSysMessage("Creature Texts reloaded.");

        return true;
    }

    [Command("creature_text_locale", RBACPermissions.CommandReloadCreatureTextLocale, true)]
    private static bool HandleReloadCreatureTextLocaleCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Creature Texts Locale...");
        handler.ClassFactory.Resolve<CreatureTextManager>().LoadCreatureTextLocales();
        handler.SendGlobalGMSysMessage("DB table `creature_text_locale` reloaded.");

        return true;
    }

    [Command("criteria_data", RBACPermissions.CommandReloadCriteriaData, true)]
    private static bool HandleReloadCriteriaDataCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Additional Criteria Data...");
        handler.ClassFactory.Resolve<CriteriaManager>().LoadCriteriaData();
        handler.SendGlobalGMSysMessage("DB table `criteria_data` reloaded.");

        return true;
    }

    [Command("trinity_string", RBACPermissions.CommandReloadCypherString, true)]
    private static bool HandleReloadCypherStringCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading trinity_string Table!");
        handler.ObjectManager.LoadCypherStrings();
        handler.SendGlobalGMSysMessage("DB table `trinity_string` reloaded.");

        return true;
    }

    [Command("disables", RBACPermissions.CommandReloadDisables, true)]
    private static bool HandleReloadDisablesCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading disables table...");
        handler.ClassFactory.Resolve<DisableManager>().LoadDisables();
        Log.Logger.Information("Checking quest disables...");
        handler.ClassFactory.Resolve<DisableManager>().CheckQuestDisables();
        handler.SendGlobalGMSysMessage("DB table `disables` reloaded.");

        return true;
    }

    [Command("event_scripts", RBACPermissions.CommandReloadEventScripts, true)]
    private static bool HandleReloadEventScriptsCommand(CommandHandler handler, StringArguments args)
    {
        if (handler.ClassFactory.Resolve<MapManager>().IsScriptScheduled())
        {
            handler.SendSysMessage("DB scripts used currently, please attempt reload later.");

            return false;
        }

        if (args != null)
            Log.Logger.Information("Re-Loading Scripts from `event_scripts`...");

        handler.ObjectManager.LoadEventScripts();

        if (args != null)
            handler.SendGlobalGMSysMessage("DB table `event_scripts` reloaded.");

        return true;
    }

    [Command("graveyard_zone", RBACPermissions.CommandReloadGraveyardZone, true)]
    private static bool HandleReloadGameGraveyardZoneCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Graveyard-zone links...");

        handler.ObjectManager.LoadGraveyardZones();

        handler.SendGlobalGMSysMessage("DB table `game_graveyard_zone` reloaded.");

        return true;
    }

    [Command("gameobject_template_locale", RBACPermissions.CommandReloadGameobjectTemplateLocale, true)]
    private static bool HandleReloadGameobjectTemplateLocaleCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Gameobject Template Locale... ");
        handler.ObjectManager.LoadGameObjectLocales();
        handler.SendGlobalGMSysMessage("DB table `gameobject_template_locale` reloaded.");

        return true;
    }

    [Command("game_tele", RBACPermissions.CommandReloadGameTele, true)]
    private static bool HandleReloadGameTeleCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Game Tele coordinates...");

        handler.ClassFactory.Resolve<GameTeleObjectCache>().Load();

        handler.SendGlobalGMSysMessage("DB table `game_tele` reloaded.");

        return true;
    }

    [Command("gameobject_questender", RBACPermissions.CommandReloadGameobjectQuestender, true)]
    private static bool HandleReloadGOQuestEnderCommand(CommandHandler handler)
    {
        Log.Logger.Information("Loading Quests Relations... (`gameobject_questender`)");
        handler.ObjectManager.LoadGameobjectQuestEnders();
        handler.SendGlobalGMSysMessage("DB table `gameobject_questender` reloaded.");

        return true;
    }

    [Command("gameobject_queststarter", RBACPermissions.CommandReloadGameobjectQueststarter, true)]
    private static bool HandleReloadGOQuestStarterCommand(CommandHandler handler)
    {
        Log.Logger.Information("Loading Quests Relations... (`gameobject_queststarter`)");
        handler.ObjectManager.LoadGameobjectQuestStarters();
        handler.SendGlobalGMSysMessage("DB table `gameobject_queststarter` reloaded.");

        return true;
    }

    [Command("gossip_menu", RBACPermissions.CommandReloadGossipMenu, true)]
    private static bool HandleReloadGossipMenuCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading `gossip_menu` Table!");
        handler.ObjectManager.LoadGossipMenu();
        handler.SendGlobalGMSysMessage("DB table `gossip_menu` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("gossip_menu_option", RBACPermissions.CommandReloadGossipMenuOption, true)]
    private static bool HandleReloadGossipMenuOptionCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading `gossip_menu_option` Table!");
        handler.ObjectManager.GossipMenuItemsCache.Load();
        handler.SendGlobalGMSysMessage("DB table `gossip_menu_option` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("gossip_menu_option_locale", RBACPermissions.CommandReloadGossipMenuOptionLocale, true)]
    private static bool HandleReloadGossipMenuOptionLocaleCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Gossip Menu Option Locale... ");
        handler.ObjectManager.LoadGossipMenuItemsLocales();
        handler.SendGlobalGMSysMessage("DB table `gossip_menu_option_locale` reloaded.");

        return true;
    }

    [Command("item_random_bonus_list_template", RBACPermissions.CommandReloadItemRandomBonusListTemplate, true)]
    private static bool HandleReloadItemRandomBonusListTemplatesCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Random item bonus list definitions...");
        handler.ClassFactory.Resolve<ItemEnchantmentManager>().LoadItemRandomBonusListTemplates();
        handler.SendGlobalGMSysMessage("DB table `item_random_bonus_list_template` reloaded.");

        return true;
    }

    [Command("lfg_dungeon_rewards", RBACPermissions.CommandReloadLfgDungeonRewards, true)]
    private static bool HandleReloadLfgRewardsCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading lfg dungeon rewards...");
        handler.ClassFactory.Resolve<LFGManager>().LoadRewards();
        handler.SendGlobalGMSysMessage("DB table `lfg_dungeon_rewards` reloaded.");

        return true;
    }

    [Command("creature_linked_respawn", RBACPermissions.CommandReloadCreatureLinkedRespawn, true)]
    private static bool HandleReloadLinkedRespawnCommand(CommandHandler handler)
    {
        Log.Logger.Information("Loading Linked Respawns... (`creature_linked_respawn`)");
        handler.ObjectManager.LoadLinkedRespawn();
        handler.SendGlobalGMSysMessage("DB table `creature_linked_respawn` (creature linked respawns) reloaded.");

        return true;
    }

    [Command("creature_loot_template", RBACPermissions.CommandReloadCreatureLootTemplate, true)]
    private static bool HandleReloadLootTemplatesCreatureCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`creature_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Creature();
        handler.ClassFactory.Resolve<LootStoreBox>().Creature.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `creature_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("disenchant_loot_template", RBACPermissions.CommandReloadDisenchantLootTemplate, true)]
    private static bool HandleReloadLootTemplatesDisenchantCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`disenchant_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Disenchant();
        handler.ClassFactory.Resolve<LootStoreBox>().Disenchant.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `disenchant_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("fishing_loot_template", RBACPermissions.CommandReloadFishingLootTemplate, true)]
    private static bool HandleReloadLootTemplatesFishingCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`fishing_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Fishing();
        handler.ClassFactory.Resolve<LootStoreBox>().Fishing.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `fishing_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("gameobject_loot_template", RBACPermissions.CommandReloadGameobjectQuestLootTemplate, true)]
    private static bool HandleReloadLootTemplatesGameobjectCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`gameobject_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Gameobject();
        handler.ClassFactory.Resolve<LootStoreBox>().Gameobject.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `gameobject_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("item_loot_template", RBACPermissions.CommandReloadItemLootTemplate, true)]
    private static bool HandleReloadLootTemplatesItemCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`item_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Item();
        handler.ClassFactory.Resolve<LootStoreBox>().Items.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `item_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("mail_loot_template", RBACPermissions.CommandReloadMailLootTemplate, true)]
    private static bool HandleReloadLootTemplatesMailCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`mail_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Mail();
        handler.ClassFactory.Resolve<LootStoreBox>().Mail.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `mail_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("milling_loot_template", RBACPermissions.CommandReloadMillingLootTemplate, true)]
    private static bool HandleReloadLootTemplatesMillingCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`milling_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Milling();
        handler.ClassFactory.Resolve<LootStoreBox>().Milling.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `milling_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("pickpocketing_loot_template", RBACPermissions.CommandReloadPickpocketingLootTemplate, true)]
    private static bool HandleReloadLootTemplatesPickpocketingCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`pickpocketing_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Pickpocketing();
        handler.ClassFactory.Resolve<LootStoreBox>().Pickpocketing.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `pickpocketing_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("prospecting_loot_template", RBACPermissions.CommandReloadProspectingLootTemplate, true)]
    private static bool HandleReloadLootTemplatesProspectingCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`prospecting_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Prospecting();
        handler.ClassFactory.Resolve<LootStoreBox>().Prospecting.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `prospecting_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("reference_loot_template", RBACPermissions.CommandReloadReferenceLootTemplate, true)]
    private static bool HandleReloadLootTemplatesReferenceCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`reference_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Reference();
        handler.SendGlobalGMSysMessage("DB table `reference_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("skinning_loot_template", RBACPermissions.CommandReloadSkinningLootTemplate, true)]
    private static bool HandleReloadLootTemplatesSkinningCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`skinning_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Skinning();
        handler.ClassFactory.Resolve<LootStoreBox>().Skinning.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `skinning_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("spell_loot_template", RBACPermissions.CommandReloadSpellLootTemplate, true)]
    private static bool HandleReloadLootTemplatesSpellCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Loot Tables... (`spell_loot_template`)");
        handler.ClassFactory.Resolve<LootManager>().LoadLootTemplates_Spell();
        handler.ClassFactory.Resolve<LootStoreBox>().Spell.CheckLootRefs();
        handler.SendGlobalGMSysMessage("DB table `spell_loot_template` reloaded.");
        handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

        return true;
    }

    [Command("mail_level_reward", RBACPermissions.CommandReloadMailLevelReward, true)]
    private static bool HandleReloadMailLevelRewardCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Player level dependent mail rewards...");
        handler.ObjectManager.LoadMailLevelRewards();
        handler.SendGlobalGMSysMessage("DB table `mail_level_reward` reloaded.");

        return true;
    }

    [Command("npc_vendor", RBACPermissions.CommandReloadNpcVendor, true)]
    private static bool HandleReloadNpcVendorCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading `npc_vendor` Table!");
        handler.ObjectManager.VendorItemCache.Load();
        handler.SendGlobalGMSysMessage("DB table `npc_vendor` reloaded.");

        return true;
    }

    [Command("creature_onkill_reputation", RBACPermissions.CommandReloadCreatureOnkillReputation, true)]
    private static bool HandleReloadOnKillReputationCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading creature award reputation definitions...");
        handler.ObjectManager.LoadReputationOnKill();
        handler.SendGlobalGMSysMessage("DB table `creature_onkill_reputation` reloaded.");

        return true;
    }

    [Command("page_text_locale", RBACPermissions.CommandReloadPageTextLocale, true)]
    private static bool HandleReloadPageTextLocaleCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Page Text Locale... ");
        handler.ObjectManager.LoadPageTextLocales();
        handler.SendGlobalGMSysMessage("DB table `page_text_locale` reloaded.");

        return true;
    }

    [Command("page_text", RBACPermissions.CommandReloadPageText, true)]
    private static bool HandleReloadPageTextsCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Page Text...");
        handler.ObjectManager.PageTextCache.Load();
        handler.SendGlobalGMSysMessage("DB table `page_text` reloaded.");

        return true;
    }

    [Command("points_of_interest", RBACPermissions.CommandReloadPointsOfInterest, true)]
    private static bool HandleReloadPointsOfInterestCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading `points_of_interest` Table!");
        handler.ObjectManager.PointOfInterestCache.Load();
        handler.SendGlobalGMSysMessage("DB table `points_of_interest` reloaded.");

        return true;
    }

    [Command("points_of_interest_locale", RBACPermissions.CommandReloadPointsOfInterestLocale, true)]
    private static bool HandleReloadPointsOfInterestLocaleCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Points Of Interest Locale... ");
        handler.ObjectManager.LoadPointOfInterestLocales();
        handler.SendGlobalGMSysMessage("DB table `points_of_interest_locale` reloaded.");

        return true;
    }

    [Command("areatrigger_involvedrelation", RBACPermissions.CommandReloadAreatriggerInvolvedrelation, true)]
    private static bool HandleReloadQuestAreaTriggersCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading QuestId Area Triggers...");
        handler.ObjectManager.LoadQuestAreaTriggers();
        handler.SendGlobalGMSysMessage("DB table `areatrigger_involvedrelation` (quest area triggers) reloaded.");

        return true;
    }

    [Command("quest_greeting", RBACPermissions.CommandReloadQuestGreeting, true)]
    private static bool HandleReloadQuestGreetingCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading QuestId Greeting ... ");
        handler.ObjectManager.LoadQuestGreetings();
        handler.SendGlobalGMSysMessage("DB table `quest_greeting` reloaded.");

        return true;
    }

    [Command("quest_poi", RBACPermissions.CommandReloadQuestPoi, true)]
    private static bool HandleReloadQuestPOICommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading QuestId POI ...");
        handler.ObjectManager.LoadQuestPOI();
        handler.ObjectManager.InitializeQueriesData(QueryDataGroup.POIs);
        handler.SendGlobalGMSysMessage("DB Table `quest_poi` and `quest_poi_points` reloaded.");

        return true;
    }

    [Command("quest_template", RBACPermissions.CommandReloadQuestTemplate, true)]
    private static bool HandleReloadQuestTemplateCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading QuestId Templates...");
        handler.ObjectManager.QuestTemplateCache.Load();
        handler.ObjectManager.InitializeQueriesData(QueryDataGroup.Quests);
        handler.SendGlobalGMSysMessage("DB table `quest_template` (quest definitions) reloaded.");

        // dependent also from `gameobject` but this table not reloaded anyway
        Log.Logger.Information("Re-Loading GameObjects for quests...");
        handler.ObjectManager.LoadGameObjectForQuests();
        handler.SendGlobalGMSysMessage("Data GameObjects for quests reloaded.");

        return true;
    }

    [Command("quest_locale", RBACPermissions.CommandReloadQuestTemplateLocale, true)]
    private static bool HandleReloadQuestTemplateLocaleCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading QuestId Locale... ");
        handler.ObjectManager.LoadQuestTemplateLocale();
        handler.ObjectManager.LoadQuestObjectivesLocale();
        handler.ObjectManager.LoadQuestGreetingLocales();
        handler.ObjectManager.LoadQuestOfferRewardLocale();
        handler.ObjectManager.LoadQuestRequestItemsLocale();
        handler.SendGlobalGMSysMessage("DB table `quest_template_locale` reloaded.");
        handler.SendGlobalGMSysMessage("DB table `quest_objectives_locale` reloaded.");
        handler.SendGlobalGMSysMessage("DB table `quest_greeting_locale` reloaded.");
        handler.SendGlobalGMSysMessage("DB table `quest_offer_reward_locale` reloaded.");
        handler.SendGlobalGMSysMessage("DB table `quest_request_items_locale` reloaded.");

        return true;
    }

    [Command("rbac", RBACPermissions.CommandReloadRbac, true)]
    private static bool HandleReloadRBACCommand(CommandHandler handler)
    {
        Log.Logger.Information("Reloading RBAC tables...");
        handler.AccountManager.LoadRBAC();
        handler.WorldManager.ReloadRBAC();
        handler.SendGlobalGMSysMessage("RBAC data reloaded.");

        return true;
    }

    [Command("reputation_reward_rate", RBACPermissions.CommandReloadReputationRewardRate, true)]
    private static bool HandleReloadReputationRewardRateCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading `reputation_reward_rate` Table!");
        handler.ObjectManager.LoadReputationRewardRate();
        handler.SendGlobalSysMessage("DB table `reputation_reward_rate` reloaded.");

        return true;
    }

    [Command("reputation_spillover_template", RBACPermissions.CommandReloadSpilloverTemplate, true)]
    private static bool HandleReloadReputationSpilloverTemplateCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading `reputation_spillover_template` Table!");
        handler.ObjectManager.LoadReputationSpilloverTemplate();
        handler.SendGlobalSysMessage("DB table `reputation_spillover_template` reloaded.");

        return true;
    }

    [Command("reserved_name", RBACPermissions.CommandReloadReservedName, true)]
    private static bool HandleReloadReservedNameCommand(CommandHandler handler)
    {
        Log.Logger.Information("Loading ReservedNames... (`reserved_name`)");
        handler.ObjectManager.LoadReservedPlayersNames();
        handler.SendGlobalGMSysMessage("DB table `reserved_name` (player reserved names) reloaded.");

        return true;
    }

    [Command("scene_template", RBACPermissions.CommandReloadSceneTemplate, true)]
    private static bool HandleReloadSceneTemplateCommand(CommandHandler handler)
    {
        Log.Logger.Information("Reloading scene_template table...");
        handler.ObjectManager.LoadSceneTemplates();
        handler.SendGlobalGMSysMessage("Scenes templates reloaded. New scriptname need a reboot.");

        return true;
    }

    [Command("skill_discovery_template", RBACPermissions.CommandReloadSkillDiscoveryTemplate, true)]
    private static bool HandleReloadSkillDiscoveryTemplateCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Skill Discovery Table...");
        handler.ClassFactory.Resolve<SkillDiscovery>().LoadSkillDiscoveryTable();
        handler.SendGlobalGMSysMessage("DB table `skill_discovery_template` (recipes discovered at crafting) reloaded.");

        return true;
    }

    [Command("skill_extra_item_template", RBACPermissions.CommandReloadSkillExtraItemTemplate, true)]
    private static bool HandleReloadSkillExtraItemTemplateCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Skill Extra Item Table...");
        handler.ClassFactory.Resolve<SkillExtraItems>().LoadSkillExtraItemTable();
        handler.SendGlobalGMSysMessage("DB table `skill_extra_item_template` (extra item creation when crafting) reloaded.");

        return HandleReloadSkillPerfectItemTemplateCommand(handler);
    }

    [Command("skill_fishing_base_level", RBACPermissions.CommandReloadSkillFishingBaseLevel, true)]
    private static bool HandleReloadSkillFishingBaseLevelCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Skill Fishing base level requirements...");
        handler.ObjectManager.LoadFishingBaseSkillLevel();
        handler.SendGlobalGMSysMessage("DB table `skill_fishing_base_level` (fishing base level for zone/subzone) reloaded.");

        return true;
    }

    private static bool HandleReloadSkillPerfectItemTemplateCommand(CommandHandler handler)
    {
        // latched onto HandleReloadSkillExtraItemTemplateCommand as it's part of that table group (and i don't want to chance all the command IDs)
        Log.Logger.Information("Re-Loading Skill Perfection Data Table...");
        handler.ClassFactory.Resolve<SkillPerfectItems>().LoadSkillPerfectItemTable();
        handler.SendGlobalGMSysMessage("DB table `skill_perfect_item_template` (perfect item procs when crafting) reloaded.");

        return true;
    }

    [Command("smart_scripts", RBACPermissions.CommandReloadSmartScripts, true)]
    private static bool HandleReloadSmartScripts(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Smart Scripts...");
        handler.ClassFactory.Resolve<SmartAIManager>().LoadFromDB();
        handler.SendGlobalGMSysMessage("Smart Scripts reloaded.");

        return true;
    }

    [Command("spell_area", RBACPermissions.CommandReloadSpellArea, true)]
    private static bool HandleReloadSpellAreaCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading SpellArea Data...");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellAreas();
        handler.SendGlobalGMSysMessage("DB table `spell_area` (spell dependences from area/quest/auras state) reloaded.");

        return true;
    }

    [Command("npc_spellclick_spells", RBACPermissions.CommandReloadNpcSpellclickSpells, true)]
    private static bool HandleReloadSpellClickSpellsCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading `npc_spellclick_spells` Table!");
        handler.ClassFactory.Resolve<SpellClickInfoCache>().Load();
        handler.SendGlobalGMSysMessage("DB table `npc_spellclick_spells` reloaded.");

        return true;
    }

    [Command("spell_group", RBACPermissions.CommandReloadSpellGroup, true)]
    private static bool HandleReloadSpellGroupsCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Spell Groups...");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellGroups();
        handler.SendGlobalGMSysMessage("DB table `spell_group` (spell groups) reloaded.");

        return true;
    }

    [Command("spell_group_stack_rules", RBACPermissions.CommandReloadSpellGroupStackRules, true)]
    private static bool HandleReloadSpellGroupStackRulesCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Spell Group Stack Rules...");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellGroupStackRules();
        handler.SendGlobalGMSysMessage("DB table `spell_group_stack_rules` (spell stacking definitions) reloaded.");

        return true;
    }

    [Command("spell_learn_spell", RBACPermissions.CommandReloadSpellLearnSpell, true)]
    private static bool HandleReloadSpellLearnSpellCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Spell Learn Spells...");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellLearnSpells();
        handler.SendGlobalGMSysMessage("DB table `spell_learn_spell` reloaded.");

        return true;
    }

    [Command("spell_linked_spell", RBACPermissions.CommandReloadSpellLinkedSpell, true)]
    private static bool HandleReloadSpellLinkedSpellCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Spell Linked Spells...");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellLinked();
        handler.SendGlobalGMSysMessage("DB table `spell_linked_spell` reloaded.");

        return true;
    }

    [Command("spell_pet_auras", RBACPermissions.CommandReloadSpellPetAuras, true)]
    private static bool HandleReloadSpellPetAurasCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Spell pet auras...");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellPetAuras();
        handler.SendGlobalGMSysMessage("DB table `spell_pet_auras` reloaded.");

        return true;
    }

    [Command("spell_proc", RBACPermissions.CommandReloadSpellProc, true)]
    private static bool HandleReloadSpellProcsCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Spell Proc conditions and data...");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellProcs();
        handler.SendGlobalGMSysMessage("DB table `spell_proc` (spell proc conditions and data) reloaded.");

        return true;
    }

    [Command("spell_required", RBACPermissions.CommandReloadSpellRequired, true)]
    private static bool HandleReloadSpellRequiredCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Spell Required Data... ");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellRequired();
        handler.SendGlobalGMSysMessage("DB table `spell_required` reloaded.");

        return true;
    }

    [Command("spell_script_names", RBACPermissions.CommandReloadSpellScriptNames, true)]
    private static bool HandleReloadSpellScriptNamesCommand(CommandHandler handler)
    {
        Log.Logger.Information("Reloading spell_script_names table...");
        handler.ObjectManager.LoadSpellScriptNames();
        //handler.ClassFactory.Resolve<ScriptManager>().NotifyScriptIDUpdate();
        handler.ObjectManager.ValidateSpellScripts();
        handler.SendGlobalGMSysMessage("Spell scripts reloaded.");

        return true;
    }

    [Command("spell_scripts", RBACPermissions.CommandReloadSpellScripts, true)]
    private static bool HandleReloadSpellScriptsCommand(CommandHandler handler, StringArguments args)
    {
        if (handler.ClassFactory.Resolve<MapManager>().IsScriptScheduled())
        {
            handler.SendSysMessage("DB scripts used currently, please attempt reload later.");

            return false;
        }

        if (args != null)
            Log.Logger.Information("Re-Loading Scripts from `spell_scripts`...");

        handler.ObjectManager.LoadSpellScripts();

        if (args != null)
            handler.SendGlobalGMSysMessage("DB table `spell_scripts` reloaded.");

        return true;
    }

    [Command("spell_target_position", RBACPermissions.CommandReloadSpellTargetPosition, true)]
    private static bool HandleReloadSpellTargetPositionCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Spell target coordinates...");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellTargetPositions();
        handler.SendGlobalGMSysMessage("DB table `spell_target_position` (destination coordinates for spell targets) reloaded.");

        return true;
    }

    [Command("spell_threats", RBACPermissions.CommandReloadSpellThreats, true)]
    private static bool HandleReloadSpellThreatsCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Aggro Spells Definitions...");
        handler.ClassFactory.Resolve<SpellManager>().LoadSpellThreats();
        handler.SendGlobalGMSysMessage("DB table `spell_threat` (spell aggro definitions) reloaded.");

        return true;
    }

    [Command("support", RBACPermissions.CommandReloadSupportSystem, true)]
    private static bool HandleReloadSupportSystemCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading Support System Tables...");
        handler.ClassFactory.Resolve<SupportManager>().LoadBugTickets();
        handler.ClassFactory.Resolve<SupportManager>().LoadComplaintTickets();
        handler.ClassFactory.Resolve<SupportManager>().LoadSuggestionTickets();
        handler.SendGlobalGMSysMessage("DB tables `gm_*` reloaded.");

        return true;
    }

    [Command("trainer", RBACPermissions.CommandReloadTrainer, true)]
    private static bool HandleReloadTrainerCommand(CommandHandler handler)
    {
        Log.Logger.Information("Re-Loading `trainer` Table!");
        handler.ObjectManager.TrainerCache.Load();
        handler.ObjectManager.CreatureDefaultTrainersCache.Load();
        handler.SendGlobalGMSysMessage("DB table `trainer` reloaded.");
        handler.SendGlobalGMSysMessage("DB table `trainer_locale` reloaded.");
        handler.SendGlobalGMSysMessage("DB table `trainer_spell` reloaded.");
        handler.SendGlobalGMSysMessage("DB table `creature_trainer` reloaded.");

        return true;
    }

    [Command("vehicle_accessory", RBACPermissions.CommandReloadVehicleAccesory, true)]
    private static bool HandleReloadVehicleAccessoryCommand(CommandHandler handler)
    {
        Log.Logger.Information("Reloading vehicle_accessory table...");
        handler.ClassFactory.Resolve<VehicleObjectCache>().LoadVehicleAccessories();
        handler.SendGlobalGMSysMessage("Vehicle accessories reloaded.");

        return true;
    }

    [Command("vehicle_template_accessory", RBACPermissions.CommandReloadVehicleTemplateAccessory, true)]
    private static bool HandleReloadVehicleTemplateAccessoryCommand(CommandHandler handler)
    {
        Log.Logger.Information("Reloading vehicle_template_accessory table...");
        handler.ClassFactory.Resolve<VehicleObjectCache>().LoadVehicleTemplateAccessories();
        handler.SendGlobalGMSysMessage("Vehicle template accessories reloaded.");

        return true;
    }

    [Command("vehicle_template", RBACPermissions.CommandReloadVehicleTemplate, true)]
    private static bool HandleReloadVehicleTemplateCommand(CommandHandler handler)
    {
        Log.Logger.Information("Reloading vehicle_template table...");
        handler.ClassFactory.Resolve<VehicleObjectCache>().LoadVehicleTemplate();
        handler.SendGlobalGMSysMessage("Vehicle templates reloaded.");

        return true;
    }

    [Command("waypoint_data", RBACPermissions.CommandReloadWaypointData, true)]
    private static bool HandleReloadWpCommand(CommandHandler handler, StringArguments args)
    {
        if (args != null)
            Log.Logger.Information("Re-Loading Waypoints data from 'waypoints_data'");

        handler.ClassFactory.Resolve<WaypointManager>().Load();

        if (args != null)
            handler.SendGlobalGMSysMessage("DB Table 'waypoint_data' reloaded.");

        return true;
    }

    [Command("waypoint_scripts", RBACPermissions.CommandReloadWaypointScripts, true)]
    private static bool HandleReloadWpScriptsCommand(CommandHandler handler, StringArguments args)
    {
        if (handler.ClassFactory.Resolve<MapManager>().IsScriptScheduled())
        {
            handler.SendSysMessage("DB scripts used currently, please attempt reload later.");

            return false;
        }

        if (args != null)
            Log.Logger.Information("Re-Loading Scripts from `waypoint_scripts`...");

        handler.ObjectManager.LoadWaypointScripts();

        if (args != null)
            handler.SendGlobalGMSysMessage("DB table `waypoint_scripts` reloaded.");

        return true;
    }

    [CommandGroup("all")]
    private class AllCommand
    {
        [Command("achievement", RBACPermissions.CommandReloadAllAchievement, true)]
        private static bool HandleReloadAllAchievementCommand(CommandHandler handler)
        {
            HandleReloadCriteriaDataCommand(handler);
            HandleReloadAchievementRewardCommand(handler);

            return true;
        }

        [Command("area", RBACPermissions.CommandReloadAllArea, true)]
        private static bool HandleReloadAllAreaCommand(CommandHandler handler)
        {
            HandleReloadAreaTriggerTeleportCommand(handler);
            HandleReloadAreaTriggerTavernCommand(handler);
            HandleReloadGameGraveyardZoneCommand(handler);

            return true;
        }

        [Command("", RBACPermissions.CommandReloadAll, true)]
        private static bool HandleReloadAllCommand(CommandHandler handler)
        {
            HandleReloadSkillFishingBaseLevelCommand(handler);

            HandleReloadAllAchievementCommand(handler);
            HandleReloadAllAreaCommand(handler);
            HandleReloadAllLootCommand(handler);
            HandleReloadAllNpcCommand(handler);
            HandleReloadAllQuestCommand(handler);
            HandleReloadAllSpellCommand(handler);
            HandleReloadAllItemCommand(handler);
            HandleReloadAllGossipsCommand(handler);
            HandleReloadAllLocalesCommand(handler);

            HandleReloadAccessRequirementCommand(handler);
            HandleReloadMailLevelRewardCommand(handler);
            HandleReloadReservedNameCommand(handler);
            HandleReloadCypherStringCommand(handler);
            HandleReloadGameTeleCommand(handler);

            HandleReloadCreatureMovementOverrideCommand(handler);
            HandleReloadCreatureSummonGroupsCommand(handler);

            HandleReloadVehicleAccessoryCommand(handler);
            HandleReloadVehicleTemplateAccessoryCommand(handler);

            HandleReloadAutobroadcastCommand(handler);
            HandleReloadBattlegroundTemplate(handler);
            HandleReloadCharacterTemplate(handler);

            return true;
        }

        [Command("gossips", RBACPermissions.CommandReloadAllGossip, true)]
        private static bool HandleReloadAllGossipsCommand(CommandHandler handler)
        {
            HandleReloadGossipMenuCommand(handler);
            HandleReloadGossipMenuOptionCommand(handler);
            HandleReloadPointsOfInterestCommand(handler);

            return true;
        }

        [Command("item", RBACPermissions.CommandReloadAllItem, true)]
        private static bool HandleReloadAllItemCommand(CommandHandler handler)
        {
            HandleReloadPageTextsCommand(handler);
            HandleReloadItemRandomBonusListTemplatesCommand(handler);

            return true;
        }

        [Command("locales", RBACPermissions.CommandReloadAllLocales, true)]
        private static bool HandleReloadAllLocalesCommand(CommandHandler handler)
        {
            HandleReloadAchievementRewardLocaleCommand(handler);
            HandleReloadCreatureTemplateLocaleCommand(handler);
            HandleReloadCreatureTextLocaleCommand(handler);
            HandleReloadGameobjectTemplateLocaleCommand(handler);
            HandleReloadGossipMenuOptionLocaleCommand(handler);
            HandleReloadPageTextLocaleCommand(handler);
            HandleReloadPointsOfInterestCommand(handler);
            HandleReloadQuestTemplateLocaleCommand(handler);

            return true;
        }

        [Command("loot", RBACPermissions.CommandReloadAllLoot, true)]
        private static bool HandleReloadAllLootCommand(CommandHandler handler)
        {
            Log.Logger.Information("Re-Loading Loot Tables...");
            handler.ClassFactory.Resolve<LootManager>().LoadLootTables();
            handler.SendGlobalGMSysMessage("DB tables `*_loot_template` reloaded.");
            handler.ClassFactory.Resolve<ConditionManager>().LoadConditions(true);

            return true;
        }

        [Command("npc", RBACPermissions.CommandReloadAllNpc, true)]
        private static bool HandleReloadAllNpcCommand(CommandHandler handler)
        {
            HandleReloadTrainerCommand(handler);
            HandleReloadNpcVendorCommand(handler);
            HandleReloadPointsOfInterestCommand(handler);
            HandleReloadSpellClickSpellsCommand(handler);

            return true;
        }

        [Command("quest", RBACPermissions.CommandReloadAllQuest, true)]
        private static bool HandleReloadAllQuestCommand(CommandHandler handler)
        {
            HandleReloadQuestAreaTriggersCommand(handler);
            HandleReloadQuestGreetingCommand(handler);
            HandleReloadQuestPOICommand(handler);
            HandleReloadQuestTemplateCommand(handler);

            Log.Logger.Information("Re-Loading Quests Relations...");
            handler.ObjectManager.LoadQuestStartersAndEnders();
            handler.SendGlobalGMSysMessage("DB tables `*_queststarter` and `*_questender` reloaded.");

            return true;
        }

        [Command("scripts", RBACPermissions.CommandReloadAllScripts, true)]
        private static bool HandleReloadAllScriptsCommand(CommandHandler handler)
        {
            if (handler.ClassFactory.Resolve<MapManager>().IsScriptScheduled())
            {
                handler.SendSysMessage("DB scripts used currently, please attempt reload later.");

                return false;
            }

            Log.Logger.Information("Re-Loading Scripts...");
            HandleReloadEventScriptsCommand(handler, null);
            HandleReloadSpellScriptsCommand(handler, null);
            handler.SendGlobalGMSysMessage("DB tables `*_scripts` reloaded.");
            HandleReloadWpScriptsCommand(handler, null);
            HandleReloadWpCommand(handler, null);

            return true;
        }

        [Command("spell", RBACPermissions.CommandReloadAllSpell, true)]
        private static bool HandleReloadAllSpellCommand(CommandHandler handler)
        {
            HandleReloadSkillDiscoveryTemplateCommand(handler);
            HandleReloadSkillExtraItemTemplateCommand(handler);
            HandleReloadSpellRequiredCommand(handler);
            HandleReloadSpellAreaCommand(handler);
            HandleReloadSpellGroupsCommand(handler);
            HandleReloadSpellLearnSpellCommand(handler);
            HandleReloadSpellLinkedSpellCommand(handler);
            HandleReloadSpellProcsCommand(handler);
            HandleReloadSpellTargetPositionCommand(handler);
            HandleReloadSpellThreatsCommand(handler);
            HandleReloadSpellGroupStackRulesCommand(handler);
            HandleReloadSpellPetAurasCommand(handler);

            return true;
        }
    }
}