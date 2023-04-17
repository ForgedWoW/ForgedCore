﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GLIDE)] // 358733 - Glide (Racial)
internal class SpellEvokerGlide : SpellScript, ISpellCheckCast, ISpellOnCast
{
    public SpellCastResult CheckCast()
    {
        var caster = Caster;

        if (!caster.IsFalling)
            return SpellCastResult.NotOnGround;

        if (caster.HasAura(EvokerSpells.VISAGE_AURA))
            return SpellCastResult.DontReport; // SpellCastResult.NotShapeshift;

        return SpellCastResult.SpellCastOk;
    }

    public void OnCast()
    {
        var caster = Caster.AsPlayer;

        if (caster == null)
            return;

        caster.SpellFactory.CastSpell(caster, EvokerSpells.GLIDE_KNOCKBACK, true);

        caster.SpellHistory.StartCooldown(Global.SpellMgr.GetSpellInfo(EvokerSpells.HOVER, CastDifficulty), 0, null, false, TimeSpan.FromMilliseconds(250));
        caster.SpellHistory.StartCooldown(Global.SpellMgr.GetSpellInfo(EvokerSpells.SOAR_RACIAL, CastDifficulty), 0, null, false, TimeSpan.FromMilliseconds(250));
    }
}