﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // Demonbolt - 157695
    [SpellScript(157695)]
    public class spell_warl_demonbolt : SpellScript, IHasSpellEffects
    {
        private int _summons = 0;

        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            int damage = GetHitDamage();
            MathFunctions.AddPct(ref damage, _summons * 20);
            SetHitDamage(damage);
        }

        private void CountSummons(List<WorldObject> targets)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            foreach (WorldObject wo in targets)
            {
                if (!wo.ToCreature())
                {
                    continue;
                }
                if (wo.ToCreature().GetOwner() != caster)
                {
                    continue;
                }
                if (wo.ToCreature().GetCreatureType() != CreatureType.Demon)
                {
                    continue;
                }

                _summons++;
            }

            targets.Clear();
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountSummons, 2, Targets.UnitCasterAndSummons));
        }
    }
}