﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ECHO)]
public class SpellEvokerResonatingSphere : SpellScript, ISpellCalculateBonusCoefficient
{
    public double CalcBonusCoefficient(double bonusCoefficient)
    {
        var aura = Spell.TriggeredByAuraSpell;

        if (aura != null && aura.Id == EvokerSpells.RESONATING_SPHERE)
            bonusCoefficient *= aura.GetEffect(0).BasePoints * 0.01;

        return bonusCoefficient;
    }
}