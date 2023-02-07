﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{

    // War Machine 215556
    [SpellScript(215556)]
    public class aura_warr_war_machine : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.CastSpell(caster, WarriorSpells.WAR_MACHINE_AURA, true);
            }
        }

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.RemoveAurasDueToSpell(WarriorSpells.WAR_MACHINE_AURA);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectApplyHandler(OnApply, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real));
            AuraEffects.Add(new EffectApplyHandler(OnRemove, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}