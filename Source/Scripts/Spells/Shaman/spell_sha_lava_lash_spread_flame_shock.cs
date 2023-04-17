﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 105792 - Lava Lash
[SpellScript(105792)]
public class SpellShaLavaLashSpreadFlameShock : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.TypeId == TypeId.Player;
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveIf(new UnitAuraCheck<WorldObject>(true, ShamanSpells.FLAME_SHOCK, Caster.GUID));
    }

    private void HandleScript(int effIndex)
    {
        var mainTarget = ExplTargetUnit;

        if (mainTarget != null)
        {
            var flameShock = mainTarget.GetAura(ShamanSpells.FLAME_SHOCK, Caster.GUID);

            if (flameShock != null)
            {
                var newAura = Caster.AddAura(ShamanSpells.FLAME_SHOCK, HitUnit);

                if (newAura != null)
                {
                    newAura.SetDuration(flameShock.Duration);
                    newAura.SetMaxDuration(flameShock.Duration);
                }
            }
        }
    }
}