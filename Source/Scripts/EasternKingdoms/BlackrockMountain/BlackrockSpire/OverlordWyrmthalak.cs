// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.OverlordWyrmthalak;

internal struct SpellIds
{
    public const uint Blastwave = 11130;
    public const uint Shout = 23511;
    public const uint Cleave = 20691;
    public const uint Knockaway = 20686;
}

internal struct MiscConst
{
    public const uint NpcSpirestoneWarlord = 9216;
    public const uint NpcSmolderthornBerserker = 9268;

    public static Position SummonLocation = new(-39.355f, -513.456f, 88.472f, 4.679f);
    public static Position SummonLocation2 = new(-49.875f, -511.896f, 88.195f, 4.613f);
}

[Script]
internal class boss_overlord_wyrmthalak : BossAI
{
    private bool Summoned;

    public boss_overlord_wyrmthalak(Creature creature) : base(creature, DataTypes.OverlordWyrmthalak)
    {
        Initialize();
    }

    public override void Reset()
    {
        _Reset();
        Initialize();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.Blastwave);
                               task.Repeat(TimeSpan.FromSeconds(20));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCastVictim(SpellIds.Shout);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCastVictim(SpellIds.Cleave);
                               task.Repeat(TimeSpan.FromSeconds(7));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               DoCastVictim(SpellIds.Knockaway);
                               task.Repeat(TimeSpan.FromSeconds(14));
                           });
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        if (!Summoned &&
            HealthBelowPct(51))
        {
            var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

            if (target)
            {
                Creature warlord = Me.SummonCreature(MiscConst.NpcSpirestoneWarlord, MiscConst.SummonLocation, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));

                if (warlord)
                    warlord.AI.AttackStart(target);

                Creature berserker = Me.SummonCreature(MiscConst.NpcSmolderthornBerserker, MiscConst.SummonLocation2, TempSummonType.TimedDespawn, TimeSpan.FromMinutes(5));

                if (berserker)
                    berserker.AI.AttackStart(target);

                Summoned = true;
            }
        }

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void Initialize()
    {
        Summoned = false;
    }
}