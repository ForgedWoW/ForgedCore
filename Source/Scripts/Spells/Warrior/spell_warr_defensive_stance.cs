﻿using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
    // Defensive Stance - 71
    [SpellScript(71)]
    public class spell_warr_defensive_stance : AuraScript, IAuraOnProc
    {
        private uint _damageTaken = 0;

        public void OnProc(ProcEventInfo eventInfo)
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            _damageTaken = eventInfo.GetDamageInfo() != null ? eventInfo.GetDamageInfo().GetDamage() : 0;
            if (_damageTaken <= 0)
            {
                return;
            }

            int rageAmount = (int)((50.0f * (float)_damageTaken) / (float)caster.GetMaxHealth());
            caster.ModifyPower(PowerType.Rage, 10 * rageAmount);
        }
    }
}