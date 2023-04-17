﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 67489 - Runic Healing Injector
internal class SpellItemRunicHealingInjector : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.IsPlayer;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHeal(int effIndex)
    {
        var caster = Caster.AsPlayer;

        if (caster != null)
            if (caster.HasSkill(SkillType.Engineering))
                HitHeal = (int)(HitHeal * 1.25f);
    }
}