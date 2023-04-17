﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(1329)]
public class SpellRogMutilateSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 2, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHitTarget));
    }


    private void HandleOnHit(int effIndex)
    {
        var caster = Caster.AsPlayer;
        var target = HitUnit;

        if (target == null || caster == null)
            return;

        if (caster.HasAura(5374) || caster.HasAura(27576))
            caster.AsPlayer.ModifyPower(PowerType.ComboPoints, 1);

        if (caster.HasAura(14190))
            caster.AsPlayer.ModifyPower(PowerType.ComboPoints, 2);

        caster.ModifyPower(PowerType.ComboPoints, -3);
    }
}