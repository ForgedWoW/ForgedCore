﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[Script] // 109132 - Roll
internal class SpellMonkRoll : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public SpellCastResult CheckCast()
    {
        if (Caster.HasUnitState(UnitState.Root))
            return SpellCastResult.Rooted;

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        Caster
            .SpellFactory.CastSpell(Caster,
                       Caster.HasUnitMovementFlag(MovementFlag.Backward) ? MonkSpells.RollBackward : MonkSpells.ROLL_FORWARD,
                       new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));

        Caster.SpellFactory.CastSpell(Caster, MonkSpells.NO_FEATHER_FALL, true);
    }
}