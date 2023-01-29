// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Shazzrah
{
    internal struct SpellIds
    {
        public const uint ArcaneExplosion = 19712;
        public const uint ShazzrahCurse = 19713;
        public const uint MagicGrounding = 19714;
        public const uint Counterspell = 19715;
        public const uint ShazzrahGateDummy = 23138; // Teleports to and attacks a random Target.
        public const uint ShazzrahGate = 23139;
    }

    internal struct EventIds
    {
        public const uint ArcaneExplosion = 1;
        public const uint ArcaneExplosionTriggered = 2;
        public const uint ShazzrahCurse = 3;
        public const uint MagicGrounding = 4;
        public const uint Counterspell = 5;
        public const uint ShazzrahGate = 6;
    }

    [Script]
    internal class boss_shazzrah : BossAI
    {
        public boss_shazzrah(Creature creature) : base(creature, DataTypes.Shazzrah)
        {
        }

        public override void JustEngagedWith(Unit target)
        {
            base.JustEngagedWith(target);
            Events.ScheduleEvent(EventIds.ArcaneExplosion, TimeSpan.FromSeconds(6));
            Events.ScheduleEvent(EventIds.ShazzrahCurse, TimeSpan.FromSeconds(10));
            Events.ScheduleEvent(EventIds.MagicGrounding, TimeSpan.FromSeconds(24));
            Events.ScheduleEvent(EventIds.Counterspell, TimeSpan.FromSeconds(15));
            Events.ScheduleEvent(EventIds.ShazzrahGate, TimeSpan.FromSeconds(45));
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            Events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            Events.ExecuteEvents(eventId =>
                                  {
                                      switch (eventId)
                                      {
                                          case EventIds.ArcaneExplosion:
                                              DoCastVictim(SpellIds.ArcaneExplosion);
                                              Events.ScheduleEvent(EventIds.ArcaneExplosion, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(7));

                                              break;
                                          // Triggered subsequent to using "Gate of Shazzrah".
                                          case EventIds.ArcaneExplosionTriggered:
                                              DoCastVictim(SpellIds.ArcaneExplosion);

                                              break;
                                          case EventIds.ShazzrahCurse:
                                              Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.ShazzrahCurse);

                                              if (target)
                                                  DoCast(target, SpellIds.ShazzrahCurse);

                                              Events.ScheduleEvent(EventIds.ShazzrahCurse, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(30));

                                              break;
                                          case EventIds.MagicGrounding:
                                              DoCast(me, SpellIds.MagicGrounding);
                                              Events.ScheduleEvent(EventIds.MagicGrounding, TimeSpan.FromSeconds(35));

                                              break;
                                          case EventIds.Counterspell:
                                              DoCastVictim(SpellIds.Counterspell);
                                              Events.ScheduleEvent(EventIds.Counterspell, TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(20));

                                              break;
                                          case EventIds.ShazzrahGate:
                                              ResetThreatList();
                                              DoCastAOE(SpellIds.ShazzrahGateDummy);
                                              Events.ScheduleEvent(EventIds.ArcaneExplosionTriggered, TimeSpan.FromSeconds(2));
                                              Events.RescheduleEvent(EventIds.ArcaneExplosion, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6));
                                              Events.ScheduleEvent(EventIds.ShazzrahGate, TimeSpan.FromSeconds(45));

                                              break;
                                          default:
                                              break;
                                      }

                                      if (me.HasUnitState(UnitState.Casting))
                                          return;
                                  });


            DoMeleeAttackIfReady();
        }
    }

    [Script] // 23138 - Gate of Shazzrah
    internal class spell_shazzrah_gate_dummy : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ShazzrahGate);
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Empty())
                return;

            WorldObject target = targets.SelectRandom();
            targets.Clear();
            targets.Add(target);
        }

        private void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();

            if (target)
            {
                target.CastSpell(GetCaster(), SpellIds.ShazzrahGate, true);
                Creature creature = GetCaster().ToCreature();

                if (creature)
                    creature.GetAI().AttackStart(target); // Attack the Target which caster will teleport to.
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }
    }
}