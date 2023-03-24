﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Game.Entities;
using Game.Common.Entities.Players;

namespace Forged.RealmServer.Chat.Commands;

[CommandGroup("learn")]
class LearnCommands
{
	[Command("", CypherStrings.CommandLearnHelp, RBACPermissions.CommandLearn)]
	static bool HandleLearnCommand(CommandHandler handler, uint spellId, string allRanksStr)
	{
		var targetPlayer = handler.SelectedPlayerOrSelf;

		if (!targetPlayer)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		if (!Global.SpellMgr.IsSpellValid(spellId, handler.Session.Player))
		{
			handler.SendSysMessage(CypherStrings.CommandSpellBroken, spellId);

			return false;
		}

		var allRanks = !allRanksStr.IsEmpty() && allRanksStr.Equals("all", StringComparison.OrdinalIgnoreCase);

		if (!allRanks && targetPlayer.HasSpell(spellId))
		{
			if (targetPlayer == handler.Player)
				handler.SendSysMessage(CypherStrings.YouKnownSpell);
			else
				handler.SendSysMessage(CypherStrings.TargetKnownSpell, handler.GetNameLink(targetPlayer));

			return false;
		}

		targetPlayer.LearnSpell(spellId, false);

		if (allRanks)
			while ((spellId = Global.SpellMgr.GetNextSpellInChain(spellId)) != 0)
				targetPlayer.LearnSpell(spellId, false);

		return true;
	}

	[CommandNonGroup("unlearn", CypherStrings.CommandUnlearnHelp, RBACPermissions.CommandUnlearn)]
	static bool HandleUnLearnCommand(CommandHandler handler, uint spellId, string allRanksStr)
	{
		var target = handler.SelectedPlayer;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		var allRanks = !allRanksStr.IsEmpty() && allRanksStr.Equals("all", StringComparison.OrdinalIgnoreCase);

		if (allRanks)
			spellId = Global.SpellMgr.GetFirstSpellInChain(spellId);

		if (target.HasSpell(spellId))
			target.RemoveSpell(spellId, false, !allRanks);
		else
			handler.SendSysMessage(CypherStrings.ForgetSpell);

		return true;
	}

	[CommandGroup("all")]
	class LearnAllCommands
	{
		[Command("blizzard", CypherStrings.CommandLearnAllBlizzardHelp, RBACPermissions.CommandLearnAllGm)]
		static bool HandleLearnAllGMCommand(CommandHandler handler)
		{
			foreach (var skillSpell in Global.SpellMgr.GetSkillLineAbilityMapBounds((uint)SkillType.Internal))
			{
				var spellInfo = Global.SpellMgr.GetSpellInfo(skillSpell.Spell, Difficulty.None);

				if (spellInfo == null || !Global.SpellMgr.IsSpellValid(spellInfo, handler.Session.Player, false))
					continue;

				handler.Session.Player.LearnSpell(skillSpell.Spell, false);
			}

			handler.SendSysMessage(CypherStrings.LearningGmSkills);

			return true;
		}

		[Command("debug", CypherStrings.CommandLearnAllDebugHelp, RBACPermissions.CommandLearn)]
		static bool HandleLearnDebugSpellsCommand(CommandHandler handler)
		{
			var player = handler.Player;
			player.LearnSpell(63364, false); /* 63364 - Saronite Barrier (reduces damage taken by 99%) */
			player.LearnSpell(1908, false);  /*  1908 - Uber Heal Over Time (heals target to full constantly) */
			player.LearnSpell(27680, false); /* 27680 - Berserk (+500% damage, +150% speed, 10m duration) */
			player.LearnSpell(62555, false); /* 62555 - Berserk (+500% damage, +150% melee haste, 10m duration) */
			player.LearnSpell(64238, false); /* 64238 - Berserk (+900% damage, +150% melee haste, 30m duration) */
			player.LearnSpell(72525, false); /* 72525 - Berserk (+240% damage, +160% haste, infinite duration) */
			player.LearnSpell(66776, false); /* 66776 - Rage (+300% damage, -95% damage taken, +100% speed, infinite duration) */

			return true;
		}

		[Command("crafts", CypherStrings.CommandLearnAllCraftsHelp, RBACPermissions.CommandLearnAllCrafts)]
		static bool HandleLearnAllCraftsCommand(CommandHandler handler, PlayerIdentifier player)
		{
			if (player == null)
				player = PlayerIdentifier.FromTargetOrSelf(handler);

			if (player == null || !player.IsConnected())
				return false;

			var target = player.GetConnectedPlayer();

			foreach (var (_, skillInfo) in CliDB.SkillLineStorage)
				if ((skillInfo.CategoryID == SkillCategory.Profession || skillInfo.CategoryID == SkillCategory.Secondary) && skillInfo.CanLink != 0) // only prof. with recipes have
					HandleLearnSkillRecipesHelper(target, skillInfo.Id);

			handler.SendSysMessage(CypherStrings.CommandLearnAllCraft);

			return true;
		}

		[Command("default", CypherStrings.CommandLearnAllDefaultHelp, RBACPermissions.CommandLearnAllDefault)]
		static bool HandleLearnAllDefaultCommand(CommandHandler handler, PlayerIdentifier player)
		{
			if (player == null)
				player = PlayerIdentifier.FromTargetOrSelf(handler);

			if (player == null || !player.IsConnected())
				return false;

			var target = player.GetConnectedPlayer();
			target.LearnDefaultSkills();
			target.LearnCustomSpells();
			target.LearnQuestRewardedSpells();

			handler.SendSysMessage(CypherStrings.CommandLearnAllDefaultAndQuest, handler.GetNameLink(target));

			return true;
		}

		[Command("languages", CypherStrings.CommandLearnAllLanguagesHelp, RBACPermissions.CommandLearnAllLang)]
		static bool HandleLearnAllLangCommand(CommandHandler handler)
		{
			Global.LanguageMgr.ForEachLanguage((_, languageDesc) =>
			{
				if (languageDesc.SpellId != 0)
					handler.Session.Player.LearnSpell(languageDesc.SpellId, false);

				return true;
			});

			handler.SendSysMessage(CypherStrings.CommandLearnAllLang);

			return true;
		}

		[Command("recipes", CypherStrings.CommandLearnAllRecipesHelp, RBACPermissions.CommandLearnAllRecipes)]
		static bool HandleLearnAllRecipesCommand(CommandHandler handler, Tail namePart)
		{
			//  Learns all recipes of specified profession and sets skill to max
			//  Example: .learn all_recipes enchanting

			var target = handler.SelectedPlayer;

			if (!target)
			{
				handler.SendSysMessage(CypherStrings.PlayerNotFound);

				return false;
			}

			if (namePart.IsEmpty())
				return false;

			var name = "";
			uint skillId = 0;

			foreach (var (_, skillInfo) in CliDB.SkillLineStorage)
			{
				if ((skillInfo.CategoryID != SkillCategory.Profession &&
					skillInfo.CategoryID != SkillCategory.Secondary) ||
					skillInfo.CanLink == 0) // only prof with recipes have set
					continue;

				var locale = handler.SessionDbcLocale;
				name = skillInfo.DisplayName[locale];

				if (string.IsNullOrEmpty(name))
					continue;

				if (!name.Like(namePart))
				{
					locale = 0;

					for (; locale < Locale.Total; ++locale)
					{
						name = skillInfo.DisplayName[locale];

						if (name.IsEmpty())
							continue;

						if (name.Like(namePart))
							break;
					}
				}

				if (locale < Locale.Total)
				{
					skillId = skillInfo.Id;

					break;
				}
			}

			if (!(name.IsEmpty() && skillId != 0))
				return false;

			HandleLearnSkillRecipesHelper(target, skillId);

			var maxLevel = target.GetPureMaxSkillValue((SkillType)skillId);
			target.SetSkill(skillId, target.GetSkillStep((SkillType)skillId), maxLevel, maxLevel);
			handler.SendSysMessage(CypherStrings.CommandLearnAllRecipes, name);

			return true;
		}

		[Command("talents", CypherStrings.CommandLearnAllTalentsHelp, RBACPermissions.CommandLearnAllTalents)]
		static bool HandleLearnAllTalentsCommand(CommandHandler handler)
		{
			var player = handler.Session.Player;
			var playerClass = (uint)player.Class;

			foreach (var (_, talentInfo) in CliDB.TalentStorage)
			{
				if (playerClass != talentInfo.ClassID)
					continue;

				if (talentInfo.SpecID != 0 && player.GetPrimarySpecialization() != talentInfo.SpecID)
					continue;

				var spellInfo = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);

				if (spellInfo == null || !Global.SpellMgr.IsSpellValid(spellInfo, handler.Session.Player, false))
					continue;

				// learn highest rank of talent and learn all non-talent spell ranks (recursive by tree)
				player.AddTalent(talentInfo, player.GetActiveTalentGroup(), true);
				player.LearnSpell(talentInfo.SpellID, false);
			}

			player.SendTalentsInfoData();

			handler.SendSysMessage(CypherStrings.CommandLearnClassTalents);

			return true;
		}

		[Command("pettalents", CypherStrings.CommandLearnAllPettalentHelp, RBACPermissions.CommandLearnMyPetTalents)]
		static bool HandleLearnAllPetTalentsCommand(CommandHandler handler)
		{
			return true;
		}

		static void HandleLearnSkillRecipesHelper(Player player, uint skillId)
		{
			var classmask = player.ClassMask;

			var skillLineAbilities = Global.DB2Mgr.GetSkillLineAbilitiesBySkill(skillId);

			if (skillLineAbilities == null)
				return;

			foreach (var skillLine in skillLineAbilities)
			{
				// not high rank
				if (skillLine.SupercedesSpell != 0)
					continue;

				// skip racial skills
				if (skillLine.RaceMask != 0)
					continue;

				// skip wrong class skills
				if (skillLine.ClassMask != 0 && (skillLine.ClassMask & classmask) == 0)
					continue;

				var spellInfo = Global.SpellMgr.GetSpellInfo(skillLine.Spell, Difficulty.None);

				if (spellInfo == null || !Global.SpellMgr.IsSpellValid(spellInfo, player, false))
					continue;

				player.LearnSpell(skillLine.Spell, false);
			}
		}
	}

	[CommandGroup("my")]
	class LearnAllMyCommands
	{
		[Command("quests", CypherStrings.CommandLearnMyQuestsHelp, RBACPermissions.CommandLearnAllMySpells)]
		static bool HandleLearnMyQuestsCommand(CommandHandler handler)
		{
			var player = handler.Player;

			foreach (var (_, quest) in Global.ObjectMgr.GetQuestTemplates())
				if (quest.AllowableClasses != 0 && player.SatisfyQuestClass(quest, false))
					player.LearnQuestRewardedSpells(quest);

			return true;
		}

		[Command("trainer", CypherStrings.CommandLearnMyTrainerHelp, RBACPermissions.CommandLearnAllMySpells)]
		static bool HandleLearnMySpellsCommand(CommandHandler handler)
		{
			var classEntry = CliDB.ChrClassesStorage.LookupByKey(handler.Player.Class);

			if (classEntry == null)
				return true;

			uint family = classEntry.SpellClassSet;

			foreach (var (_, entry) in CliDB.SkillLineAbilityStorage)
			{
				var spellInfo = Global.SpellMgr.GetSpellInfo(entry.Spell, Difficulty.None);

				if (spellInfo == null)
					continue;

				// skip server-side/triggered spells
				if (spellInfo.SpellLevel == 0)
					continue;

				// skip wrong class/race skills
				if (!handler.Session.Player.IsSpellFitByClassAndRace(spellInfo.Id))
					continue;

				// skip other spell families
				if ((uint)spellInfo.SpellFamilyName != family)
					continue;

				// skip broken spells
				if (!Global.SpellMgr.IsSpellValid(spellInfo, handler.Session.Player, false))
					continue;

				handler.Session.Player.LearnSpell(spellInfo.Id, false);
			}

			handler.SendSysMessage(CypherStrings.CommandLearnClassSpells);

			return true;
		}
	}
}