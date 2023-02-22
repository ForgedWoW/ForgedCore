﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.IPlayer;
using static Game.AI.SmartAction;

namespace Scripts.Spells.Warlock
{
    [Script]
    internal class player_warl_script : ScriptObjectAutoAdd, IPlayerOnModifyPower
    {
        public Class PlayerClass { get; } = Class.Warlock;

        public player_warl_script() : base("player_warl_script")
        {
        }

        public void OnModifyPower(Player player, PowerType power, int oldValue, ref int newValue, bool regen)
        {
            if (regen || power != PowerType.SoulShards)
                return;

            var shardCost = oldValue - newValue;

            PowerOverwhelming(player, shardCost);
            RitualOfRuin(player, shardCost);
        }

        private static void PowerOverwhelming(Player player, int shardCost)
        {
            if (shardCost <= 0 || !player.HasAura(WarlockSpells.POWER_OVERWHELMING))
                return;

            var cost = shardCost / 10;

            for (int i = 0; i < cost; i++)
                player.AddAura(WarlockSpells.POWER_OVERWHELMING_AURA, player);
        }

        private static void RitualOfRuin(Player player, int shardCost)
        {
            if (shardCost <= 0 || !player.HasAura(WarlockSpells.RITUAL_OF_RUIN))
                return;

            var soulShardsSpent = player.VariableStorage.GetValue(WarlockSpells.RITUAL_OF_RUIN.ToString(), 0) + shardCost;
            var needed = (int)Global.SpellMgr.GetSpellInfo(WarlockSpells.RITUAL_OF_RUIN).GetEffect(0).BasePoints * 10; // each soul shard is 10

            if (soulShardsSpent > needed)
            {
                player.AddAura(WarlockSpells.RITUAL_OF_RUIN_FREE_CAST_AURA, player);
                soulShardsSpent -= needed;
            }

            player.VariableStorage.Set(WarlockSpells.RITUAL_OF_RUIN.ToString(), soulShardsSpent);
        }

        public void OnProc(ProcEventInfo info)
        {
            
        }
    }
}