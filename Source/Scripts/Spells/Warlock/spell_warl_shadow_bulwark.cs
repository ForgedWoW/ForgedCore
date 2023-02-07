﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(17767)]
    public class spell_warl_shadow_bulwark : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                amount = (int)caster.CountPctFromMaxHealth(amount);
            }
        }
        public override void Register()
        {
            AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.ModIncreaseHealthPercent));
        }
    }
}