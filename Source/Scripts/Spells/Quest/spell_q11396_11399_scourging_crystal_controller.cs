﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 50133 - Scourging Crystal Controller
internal class SpellQ1139611399ScourgingCrystalController : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var target = HitUnit;

        if (target)
            if (target.IsTypeId(TypeId.Unit) &&
                target.HasAura(QuestSpellIds.FORCE_SHIELD_ARCANE_PURPLE_X3))
                // Make sure nobody else is channeling the same Target
                if (!target.HasAura(QuestSpellIds.SCOURGING_CRYSTAL_CONTROLLER))
                    Caster.SpellFactory.CastSpell(target, QuestSpellIds.SCOURGING_CRYSTAL_CONTROLLER, new CastSpellExtraArgs(CastItem));
    }
}