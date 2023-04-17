﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 6353 - Soul Fire
[SpellScript(6353)]
public class SpellWarlockSoulFire : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
    }

    private void HandleHit(int effIndex)
    {
        if (Caster)
            Caster.ModifyPower(PowerType.SoulShards, +40);

        //TODO: Improve it later
        Caster.
            //TODO: Improve it later
            SpellHistory.ModifyCooldown(WarlockSpells.SOUL_FIRE, TimeSpan.FromSeconds(-2));
    }
}