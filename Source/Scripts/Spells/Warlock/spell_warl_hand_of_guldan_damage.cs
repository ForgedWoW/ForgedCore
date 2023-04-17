﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Hand of Guldan damage - 86040
[SpellScript(86040)]
internal class SpellWarlHandOfGuldanDamage : SpellScript, IHasSpellEffects
{
    private int _soulshards = 1;

    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        _soulshards += Caster.GetPower(PowerType.SoulShards);

        if (_soulshards > 4)
        {
            Caster.SetPower(PowerType.SoulShards, 1);
            _soulshards = 4;
        }
        else
        {
            Caster.SetPower(PowerType.SoulShards, 0);
        }

        return true;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHit(int effIndex)
    {
        var caster = Caster;

        if (caster != null)
        {
            var target = HitUnit;

            if (target != null)
            {
                var dmg = HitDamage;
                HitDamage = dmg * _soulshards;

                if (caster.HasAura(WarlockSpells.HAND_OF_DOOM))
                    caster.SpellFactory.CastSpell(target, WarlockSpells.DOOM, true);
            }
        }
    }
}