﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.PYRE,
            EvokerSpells.FIRE_STORM_DAMAGE,
            EvokerSpells.LIVING_FLAME_DAMAGE)]
public class spell_evoker_everburning_flame : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (!TryGetCaster(out Player caster) || !caster.HasSpell(EvokerSpells.EVERBURNING_FLAME))
            return;

        if (TryGetExplTargetUnit(out Unit target) && target.TryGetAura(EvokerSpells.FIRE_BREATH_CHARGED, out var aura))
            aura.ModDuration(SpellManager.Instance.GetSpellInfo(EvokerSpells.EVERBURNING_FLAME).GetEffect(0).BasePoints * 1000);
    }
}