﻿using System;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraEffectAbsorb : IAuraEffectHandler
    {
        void HandleAbsorb(AuraEffect aura, DamageInfo damageInfo, ref uint absorbAmount);
    }

    public class AuraEffectAbsorbHandler : AuraEffectHandler, IAuraEffectAbsorb
    {
        public delegate void AuraEffectAbsorbDelegate(AuraEffect aura, DamageInfo damageInfo, ref uint absorbAmount);

        private readonly AuraEffectAbsorbDelegate _fn;

        public AuraEffectAbsorbHandler(AuraEffectAbsorbDelegate fn, uint effectIndex, bool overkill = false, AuraScriptHookType hookType = AuraScriptHookType.EffectAbsorb) : base(effectIndex, overkill ? AuraType.SchoolAbsorbOverkill : AuraType.SchoolAbsorb, hookType)
        {
            _fn = fn;

            if (hookType != AuraScriptHookType.EffectAbsorb &&
                hookType != AuraScriptHookType.EffectAfterAbsorb)
                throw new Exception($"Hook Type {hookType} is not valid for {nameof(AuraEffectAbsorbHandler)}. Use {AuraScriptHookType.EffectAbsorb} or {AuraScriptHookType.EffectAfterAbsorb}");
        }

        public void HandleAbsorb(AuraEffect aura, DamageInfo damageInfo, ref uint absorbAmount)
        {
            _fn(aura, damageInfo, ref absorbAmount);
        }
    }
}