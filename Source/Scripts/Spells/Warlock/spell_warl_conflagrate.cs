﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{

    // 17962 - Conflagrate
    [SpellScript(17962)]
    public class spell_warl_conflagrate : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            return ValidateSpellInfo(WarlockSpells.IMMOLATE);
        }

        private void HandleHit(uint UnnamedParameter)
        {
            Unit caster = GetCaster();
            Unit target = GetHitUnit();
            if (caster == null || target == null)
            {
                return;
            }

            if (caster.HasAura(WarlockSpells.BACKDRAFT_AURA))
            {
                caster.CastSpell(caster, WarlockSpells.BACKDRAFT, true);
            }

            caster.ModifyPower(PowerType.SoulShards, 75);

            if (caster.HasAura(WarlockSpells.ROARING_BLAZE))
            {
                Aura aur = target.GetAura(WarlockSpells.IMMOLATE_DOT, caster.GetGUID());
                if (aur != null)
                {
                    AuraEffect aurEff = aur.GetEffect(0);
                    if (aurEff != null)
                    {
                        int damage = aurEff.GetAmount();
                        aurEff.SetAmount(MathFunctions.AddPct(ref damage, 25));
                        aur.SetNeedClientUpdateForTargets();
                    }
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        }
    }
}