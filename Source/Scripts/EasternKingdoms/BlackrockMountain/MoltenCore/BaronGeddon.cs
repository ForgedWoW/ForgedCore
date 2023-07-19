// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.BaronGeddon;

internal struct SpellIds
{
    public const uint INFERNO = 19695;
    public const uint INFERNO_DMG = 19698;
    public const uint IGNITE_MANA = 19659;
    public const uint LIVING_BOMB = 20475;
    public const uint ARMAGEDDON = 20478;
}

internal struct TextIds
{
    public const uint EMOTE_SERVICE = 0;
}

[Script]
internal class BossBaronGeddon : BossAI
{
    public BossBaronGeddon(Creature creature) : base(creature, DataTypes.BARON_GEDDON) { }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(45),
                           task =>
                           {
                               DoCast(Me, SpellIds.INFERNO);
                               task.Repeat(TimeSpan.FromSeconds(45));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.IGNITE_MANA);

                               if (target)
                                   DoCast(target, SpellIds.IGNITE_MANA);

                               task.Repeat(TimeSpan.FromSeconds(30));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(35),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

                               if (target)
                                   DoCast(target, SpellIds.LIVING_BOMB);

                               task.Repeat(TimeSpan.FromSeconds(35));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff);

        // If we are <2% hp cast Armageddon
        if (!HealthAbovePct(2))
        {
            Me.InterruptNonMeleeSpells(true);
            DoCast(Me, SpellIds.ARMAGEDDON);
            Talk(TextIds.EMOTE_SERVICE);

            return;
        }

        if (Me.HasUnitState(UnitState.Casting))
            return;

        DoMeleeAttackIfReady();
    }
}

[Script] // 19695 - Inferno
internal class SpellBaronGeddonInferno : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        PreventDefaultAction();

        int[] damageForTick =
        {
            500, 500, 1000, 1000, 2000, 2000, 3000, 5000
        };

        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.TriggeringAura = aurEff;
        args.AddSpellMod(SpellValueMod.BasePoint0, damageForTick[aurEff.TickNumber - 1]);
        Target.SpellFactory.CastSpell((WorldObject)null, SpellIds.INFERNO_DMG, args);
    }
}