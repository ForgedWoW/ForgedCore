﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Forged.MapServer.Arenas.Zones;

internal class DalaranSewersArena : Arena
{
    public DalaranSewersArena(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
    {
        _events = new EventMap();
    }

    public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        if (GetStatus() != BattlegroundStatus.InProgress)
            return;

        switch (trigger)
        {
            case 5347:
            case 5348:
                // Remove effects of Demonic Circle Summon
                player.RemoveAura(DalaranSewersSpells.DEMONIC_CIRCLE);

                // Someone has get back into the pipes and the knockback has already been performed,
                // so we reset the knockback count for kicking the player again into the arena.
                _events.ScheduleEvent(DalaranSewersEvents.PIPE_KNOCKBACK, DalaranSewersData.PipeKnockbackDelay);

                break;

            default:
                base.HandleAreaTrigger(player, trigger, entered);

                break;
        }
    }

    public override void PostUpdateImpl(uint diff)
    {
        if (GetStatus() != BattlegroundStatus.InProgress)
            return;

        _events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case DalaranSewersEvents.WATERFALL_WARNING:
                    // Add the water
                    DoorClose(DalaranSewersObjectTypes.WATER2);
                    _events.ScheduleEvent(DalaranSewersEvents.WATERFALL_ON, DalaranSewersData.WaterWarningDuration);

                    break;

                case DalaranSewersEvents.WATERFALL_ON:
                    // Active collision and start knockback timer
                    DoorClose(DalaranSewersObjectTypes.WATER1);
                    _events.ScheduleEvent(DalaranSewersEvents.WATERFALL_OFF, DalaranSewersData.WaterfallDuration);
                    _events.ScheduleEvent(DalaranSewersEvents.WATERFALL_KNOCKBACK, DalaranSewersData.WaterfallKnockbackTimer);

                    break;

                case DalaranSewersEvents.WATERFALL_OFF:
                    // Remove collision and water
                    DoorOpen(DalaranSewersObjectTypes.WATER1);
                    DoorOpen(DalaranSewersObjectTypes.WATER2);
                    _events.CancelEvent(DalaranSewersEvents.WATERFALL_KNOCKBACK);
                    _events.ScheduleEvent(DalaranSewersEvents.WATERFALL_WARNING, DalaranSewersData.WaterfallTimerMin, DalaranSewersData.WaterfallTimerMax);

                    break;

                case DalaranSewersEvents.WATERFALL_KNOCKBACK:
                {
                    // Repeat knockback while the waterfall still active
                    var waterSpout = GetBGCreature(DalaranSewersCreatureTypes.WATERFALL_KNOCKBACK);

                    if (waterSpout)
                        waterSpout.CastSpell(waterSpout, DalaranSewersSpells.WATER_SPOUT, true);

                    _events.ScheduleEvent(eventId, DalaranSewersData.WaterfallKnockbackTimer);
                }

                break;

                case DalaranSewersEvents.PIPE_KNOCKBACK:
                {
                    for (var i = DalaranSewersCreatureTypes.PIPE_KNOCKBACK1; i <= DalaranSewersCreatureTypes.PIPE_KNOCKBACK2; ++i)
                    {
                        var waterSpout = GetBGCreature(i);

                        if (waterSpout)
                            waterSpout.CastSpell(waterSpout, DalaranSewersSpells.FLUSH, true);
                    }
                }

                break;
            }
        });
    }

    public override bool SetupBattleground()
    {
        var result = true;
        result &= AddObject(DalaranSewersObjectTypes.DOOR1, DalaranSewersGameObjects.DOOR1, 1350.95f, 817.2f, 20.8096f, 3.15f, 0, 0, 0.99627f, 0.0862864f);
        result &= AddObject(DalaranSewersObjectTypes.DOOR2, DalaranSewersGameObjects.DOOR2, 1232.65f, 764.913f, 20.0729f, 6.3f, 0, 0, 0.0310211f, -0.999519f);

        if (!result)
        {
            Log.Logger.Error("DalaranSewersArena: Failed to spawn door object!");

            return false;
        }

        // buffs
        result &= AddObject(DalaranSewersObjectTypes.BUFF1, DalaranSewersGameObjects.BUFF1, 1291.7f, 813.424f, 7.11472f, 4.64562f, 0, 0, 0.730314f, -0.683111f, 120);
        result &= AddObject(DalaranSewersObjectTypes.BUFF2, DalaranSewersGameObjects.BUFF2, 1291.7f, 768.911f, 7.11472f, 1.55194f, 0, 0, 0.700409f, 0.713742f, 120);

        if (!result)
        {
            Log.Logger.Error("DalaranSewersArena: Failed to spawn buff object!");

            return false;
        }

        result &= AddObject(DalaranSewersObjectTypes.WATER1, DalaranSewersGameObjects.WATER1, 1291.56f, 790.837f, 7.1f, 3.14238f, 0, 0, 0.694215f, -0.719768f, 120);
        result &= AddObject(DalaranSewersObjectTypes.WATER2, DalaranSewersGameObjects.WATER2, 1291.56f, 790.837f, 7.1f, 3.14238f, 0, 0, 0.694215f, -0.719768f, 120);
        result &= AddCreature(DalaranSewersData.NPC_WATER_SPOUT, DalaranSewersCreatureTypes.WATERFALL_KNOCKBACK, 1292.587f, 790.2205f, 7.19796f, 3.054326f);
        result &= AddCreature(DalaranSewersData.NPC_WATER_SPOUT, DalaranSewersCreatureTypes.PIPE_KNOCKBACK1, 1369.977f, 817.2882f, 16.08718f, 3.106686f);
        result &= AddCreature(DalaranSewersData.NPC_WATER_SPOUT, DalaranSewersCreatureTypes.PIPE_KNOCKBACK2, 1212.833f, 765.3871f, 16.09484f, 0.0f);

        if (!result)
        {
            Log.Logger.Error("DalaranSewersArena: Failed to spawn collision object!");

            return false;
        }

        return true;
    }

    public override void StartingEventCloseDoors()
    {
        for (var i = DalaranSewersObjectTypes.DOOR1; i <= DalaranSewersObjectTypes.DOOR2; ++i)
            SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
    }

    public override void StartingEventOpenDoors()
    {
        for (var i = DalaranSewersObjectTypes.DOOR1; i <= DalaranSewersObjectTypes.DOOR2; ++i)
            DoorOpen(i);

        for (var i = DalaranSewersObjectTypes.BUFF1; i <= DalaranSewersObjectTypes.BUFF2; ++i)
            SpawnBGObject(i, 60);

        _events.ScheduleEvent(DalaranSewersEvents.WATERFALL_WARNING, DalaranSewersData.WaterfallTimerMin, DalaranSewersData.WaterfallTimerMax);
        _events.ScheduleEvent(DalaranSewersEvents.PIPE_KNOCKBACK, DalaranSewersData.PipeKnockbackFirstDelay);

        SpawnBGObject(DalaranSewersObjectTypes.WATER2, BattlegroundConst.RespawnImmediately);

        DoorOpen(DalaranSewersObjectTypes.WATER1); // Turn off collision
        DoorOpen(DalaranSewersObjectTypes.WATER2);

        // Remove effects of Demonic Circle Summon
        foreach (var pair in GetPlayers())
        {
            var player = _GetPlayer(pair, "BattlegroundDS::StartingEventOpenDoors");

            if (player)
                player.RemoveAura(DalaranSewersSpells.DEMONIC_CIRCLE);
        }
    }
}