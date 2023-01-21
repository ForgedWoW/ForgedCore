﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Spells;
using static Game.ScriptInfo;

namespace Game.Scripting.Interfaces.Aura
{
    public interface IEffectAbsorb : IAuraEffectHandler
    {
        void HandleAbsorb(AuraEffect aura, DamageInfo damageInfo, ref uint absorbAmount);
    }

    public class EffectAbsorbHandler : AuraEffectHandler, IEffectAbsorb
    {
        public delegate void AuraEffectAbsorbDelegate(AuraEffect aura, DamageInfo damageInfo, ref uint absorbAmount);
        AuraEffectAbsorbDelegate _fn;

        public EffectAbsorbHandler(AuraEffectAbsorbDelegate fn, uint effectIndex, bool overkill, AuraScriptHookType hookType) : base(effectIndex, overkill ? AuraType.SchoolAbsorbOverkill : AuraType.SchoolAbsorb, hookType)
        {
            _fn = fn;

            if (hookType != AuraScriptHookType.EffectAbsorb && hookType != AuraScriptHookType.EffectAfterAbsorb) 
                throw new Exception($"Hook Type {hookType} is not valid for {nameof(EffectAbsorbHandler)}. Use {AuraScriptHookType.EffectAbsorb} or {AuraScriptHookType.EffectAfterAbsorb}");
        }

        public void HandleAbsorb(AuraEffect aura, DamageInfo damageInfo, ref uint absorbAmount)
        {
            _fn(aura, damageInfo, ref absorbAmount);
        }
    }
}