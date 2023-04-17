﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 157980 - Supernova
internal class SpellMageSupernova : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDamage(int effIndex)
    {
        if (ExplTargetUnit == HitUnit)
        {
            var damage = HitDamage;
            MathFunctions.AddPct(ref damage, GetEffectInfo(0).CalcValue());
            HitDamage = damage;
        }
    }
}