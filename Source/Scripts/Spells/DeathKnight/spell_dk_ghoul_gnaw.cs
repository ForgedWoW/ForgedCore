﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47481)]
public class SpellDkGhoulGnaw : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (caster == null || target == null)
            return;

        Unit owner = caster.OwnerUnit.AsPlayer;

        if (owner != null)
            caster.SpellFactory.CastSpell(target, caster.HasAura(DeathKnightSpells.DARK_TRANSFORMATION) ? DeathKnightSpells.DT_GHOUL_GNAW : DeathKnightSpells.GHOUL_GNAW, true);
    }
}