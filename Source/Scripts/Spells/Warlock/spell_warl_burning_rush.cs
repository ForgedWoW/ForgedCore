﻿using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{

    // Burning Rush - 111400
    [SpellScript(111400)]
    public class spell_warl_burning_rush : SpellScript, ISpellCheckCast, ISpellBeforeCast, ISpellAfterHit
    {
        private bool _isRemove = false;

        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return SpellCastResult.CantDoThatRightNow;
            }

            if (caster.HealthBelowPct(5))
            {
                return SpellCastResult.CantDoThatRightNow;
            }

            return SpellCastResult.SpellCastOk;
        }

        public void BeforeCast()
        {
            Unit caster = GetCaster();
            if (caster == null)
            {
                return;
            }

            if (caster.HasAura(WarlockSpells.BURNING_RUSH))
            {
                _isRemove = true;
            }
        }

        public void AfterHit()
        {
            if (_isRemove)
            {
                GetCaster().RemoveAurasDueToSpell(WarlockSpells.BURNING_RUSH);
            }
        }
    }

}