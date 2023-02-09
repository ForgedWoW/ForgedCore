﻿using System;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraEffectAbsorbHeal : IAuraEffectHandler
    {
        void HandleAbsorb(AuraEffect aura, HealInfo healInfo, ref uint absorbAmount);
    }

    public class AuraEffectAbsorbHealHandler : AuraEffectHandler, IAuraEffectAbsorbHeal
    {
        public delegate void AuraEffectAbsorbHealDelegate(AuraEffect aura, HealInfo healInfo, ref uint absorbAmount);

        private readonly AuraEffectAbsorbHealDelegate _fn;

        public AuraEffectAbsorbHealHandler(AuraEffectAbsorbHealDelegate fn, uint effectIndex, AuraType auraType, AuraScriptHookType hookType) : base(effectIndex, auraType, hookType)
        {
            _fn = fn;

            if (hookType != AuraScriptHookType.EffectAbsorbHeal &&
                hookType != AuraScriptHookType.EffectAfterAbsorbHeal &&
                hookType != AuraScriptHookType.EffectManaShield &&
                hookType != AuraScriptHookType.EffectAfterManaShield)
                throw new Exception($"Hook Type {hookType} is not valid for {nameof(AuraEffectAbsorbHealHandler)}. Use {AuraScriptHookType.EffectAbsorbHeal}, {AuraScriptHookType.EffectAfterManaShield}, {AuraScriptHookType.EffectManaShield} or {AuraScriptHookType.EffectAfterAbsorbHeal}");
        }

        public void HandleAbsorb(AuraEffect aura, HealInfo healInfo, ref uint absorbAmount)
        {
            _fn(aura, healInfo, ref absorbAmount);
        }
    }
}