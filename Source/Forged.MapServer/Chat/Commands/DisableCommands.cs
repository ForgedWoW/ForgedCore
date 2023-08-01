﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Achievements;
using Forged.MapServer.Conditions;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("disable")]
internal class DisableCommands
{
    [CommandGroup("add")]
    private class DisableAddCommands
    {
        [Command("Battleground", RBACPermissions.CommandDisableAddBattleground, true)]
        private static bool HandleAddDisableBattlegroundCommand(CommandHandler handler, uint entry, uint flags, string disableComment)
        {
            return HandleAddDisables(entry, flags, disableComment, handler, DisableType.Battleground);
        }

        [Command("criteria", RBACPermissions.CommandDisableAddCriteria, true)]
        private static bool HandleAddDisableCriteriaCommand(CommandHandler handler, uint entry, uint flags, string disableComment)
        {
            return HandleAddDisables(entry, flags, disableComment, handler, DisableType.Criteria);
        }

        [Command("map", RBACPermissions.CommandDisableAddMap, true)]
        private static bool HandleAddDisableMapCommand(CommandHandler handler, uint entry, uint flags, string disableComment)
        {
            return HandleAddDisables(entry, flags, disableComment, handler, DisableType.Map);
        }

        [Command("mmap", RBACPermissions.CommandDisableAddMmap, true)]
        private static bool HandleAddDisableMMapCommand(CommandHandler handler, uint entry, uint flags, string disableComment)
        {
            return HandleAddDisables(entry, flags, disableComment, handler, DisableType.MMAP);
        }

        [Command("outdoorpvp", RBACPermissions.CommandDisableAddOutdoorpvp, true)]
        private static bool HandleAddDisableOutdoorPvPCommand(CommandHandler handler, uint entry, uint flags, string disableComment)
        {
            return HandleAddDisables(entry, flags, disableComment, handler, DisableType.OutdoorPVP);
        }

        [Command("quest", RBACPermissions.CommandDisableAddQuest, true)]
        private static bool HandleAddDisableQuestCommand(CommandHandler handler, uint entry, uint flags, string disableComment)
        {
            return HandleAddDisables(entry, flags, disableComment, handler, DisableType.Quest);
        }

        private static bool HandleAddDisables(uint entry, uint flags, string disableComment, CommandHandler handler, DisableType disableType)
        {
            if (entry == 0)
                return false;

            if (disableComment.IsEmpty())
                return false;

            switch (disableType)
            {
                case DisableType.Spell:
                {
                    if (!handler.ClassFactory.Resolve<SpellManager>().HasSpellInfo(entry))
                    {
                        handler.SendSysMessage(CypherStrings.CommandNospellfound);

                        return false;
                    }

                    break;
                }
                case DisableType.Quest:
                {
                    if (handler.ObjectManager.QuestTemplateCache.GetQuestTemplate(entry) == null)
                    {
                        handler.SendSysMessage(CypherStrings.CommandNoquestfound, entry);

                        return false;
                    }

                    break;
                }
                case DisableType.Map:
                {
                    if (!handler.CliDB.MapStorage.ContainsKey(entry))
                    {
                        handler.SendSysMessage(CypherStrings.CommandNomapfound);

                        return false;
                    }

                    break;
                }
                case DisableType.Battleground:
                {
                    if (!handler.CliDB.BattlemasterListStorage.ContainsKey(entry))
                    {
                        handler.SendSysMessage(CypherStrings.CommandNoBattlegroundFound);

                        return false;
                    }

                    break;
                }
                case DisableType.Criteria:
                {
                    if (handler.ClassFactory.Resolve<CriteriaManager>().GetCriteria(entry) == null)
                    {
                        handler.SendSysMessage(CypherStrings.CommandNoAchievementCriteriaFound);

                        return false;
                    }

                    break;
                }
                case DisableType.OutdoorPVP:
                {
                    if (entry > (int)OutdoorPvPTypes.Max)
                    {
                        handler.SendSysMessage(CypherStrings.CommandNoOutdoorPvpForund);

                        return false;
                    }

                    break;
                }
                case DisableType.VMAP:
                {
                    if (!handler.CliDB.MapStorage.ContainsKey(entry))
                    {
                        handler.SendSysMessage(CypherStrings.CommandNomapfound);

                        return false;
                    }

                    break;
                }
                case DisableType.MMAP:
                {
                    if (!handler.CliDB.MapStorage.ContainsKey(entry))
                    {
                        handler.SendSysMessage(CypherStrings.CommandNomapfound);

                        return false;
                    }

                    break;
                }
            }

            var worldDB = handler.ClassFactory.Resolve<WorldDatabase>();
            var stmt = worldDB.GetPreparedStatement(WorldStatements.SEL_DISABLES);
            stmt.AddValue(0, entry);
            stmt.AddValue(1, (byte)disableType);
            var result = worldDB.Query(stmt);

            if (!result.IsEmpty())
            {
                handler.SendSysMessage($"This {disableType} (Id: {entry}) is already disabled.");

                return false;
            }

            stmt = worldDB.GetPreparedStatement(WorldStatements.INS_DISABLES);
            stmt.AddValue(0, entry);
            stmt.AddValue(1, (byte)disableType);
            stmt.AddValue(2, flags);
            stmt.AddValue(3, disableComment);
            worldDB.Execute(stmt);

            handler.SendSysMessage($"Add Disabled {disableType} (Id: {entry}) for reason {disableComment}");

            return true;
        }

        [Command("spell", RBACPermissions.CommandDisableAddSpell, true)]
        private static bool HandleAddDisableSpellCommand(CommandHandler handler, uint entry, uint flags, string disableComment)
        {
            return HandleAddDisables(entry, flags, disableComment, handler, DisableType.Spell);
        }

        [Command("vmap", RBACPermissions.CommandDisableAddVmap, true)]
        private static bool HandleAddDisableVmapCommand(CommandHandler handler, uint entry, uint flags, string disableComment)
        {
            return HandleAddDisables(entry, flags, disableComment, handler, DisableType.VMAP);
        }
    }

    [CommandGroup("remove")]
    private class DisableRemoveCommands
    {
        [Command("Battleground", RBACPermissions.CommandDisableRemoveBattleground, true)]
        private static bool HandleRemoveDisableBattlegroundCommand(CommandHandler handler, uint entry)
        {
            return HandleRemoveDisables(entry, handler, DisableType.Battleground);
        }

        [Command("criteria", RBACPermissions.CommandDisableRemoveCriteria, true)]
        private static bool HandleRemoveDisableCriteriaCommand(CommandHandler handler, uint entry)
        {
            return HandleRemoveDisables(entry, handler, DisableType.Criteria);
        }

        [Command("map", RBACPermissions.CommandDisableRemoveMap, true)]
        private static bool HandleRemoveDisableMapCommand(CommandHandler handler, uint entry)
        {
            return HandleRemoveDisables(entry, handler, DisableType.Map);
        }

        [Command("mmap", RBACPermissions.CommandDisableRemoveMmap, true)]
        private static bool HandleRemoveDisableMMapCommand(CommandHandler handler, uint entry)
        {
            return HandleRemoveDisables(entry, handler, DisableType.MMAP);
        }

        [Command("outdoorpvp", RBACPermissions.CommandDisableRemoveOutdoorpvp, true)]
        private static bool HandleRemoveDisableOutdoorPvPCommand(CommandHandler handler, uint entry)
        {
            return HandleRemoveDisables(entry, handler, DisableType.OutdoorPVP);
        }

        [Command("quest", RBACPermissions.CommandDisableRemoveQuest, true)]
        private static bool HandleRemoveDisableQuestCommand(CommandHandler handler, uint entry)
        {
            return HandleRemoveDisables(entry, handler, DisableType.Quest);
        }

        private static bool HandleRemoveDisables(uint entry, CommandHandler handler, DisableType disableType)
        {
            if (entry == 0)
                return false;

            var worldDB = handler.ClassFactory.Resolve<WorldDatabase>();
            var stmt = worldDB.GetPreparedStatement(WorldStatements.SEL_DISABLES);
            stmt.AddValue(0, entry);
            stmt.AddValue(1, (byte)disableType);
            var result = worldDB.Query(stmt);

            if (result.IsEmpty())
            {
                handler.SendSysMessage($"This {disableType} (Id: {entry}) is not disabled.");

                return false;
            }

            stmt = worldDB.GetPreparedStatement(WorldStatements.DEL_DISABLES);
            stmt.AddValue(0, entry);
            stmt.AddValue(1, (byte)disableType);
            worldDB.Execute(stmt);

            handler.SendSysMessage($"Remove Disabled {disableType} (Id: {entry})");

            return true;
        }

        [Command("spell", RBACPermissions.CommandDisableRemoveSpell, true)]
        private static bool HandleRemoveDisableSpellCommand(CommandHandler handler, uint entry)
        {
            return HandleRemoveDisables(entry, handler, DisableType.Spell);
        }

        [Command("vmap", RBACPermissions.CommandDisableRemoveVmap, true)]
        private static bool HandleRemoveDisableVmapCommand(CommandHandler handler, uint entry)
        {
            return HandleRemoveDisables(entry, handler, DisableType.VMAP);
        }
    }
}