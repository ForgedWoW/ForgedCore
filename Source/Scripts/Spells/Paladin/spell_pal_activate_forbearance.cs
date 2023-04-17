﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// Activate Forbearance
// Called by Blessing of Protection - 1022, Lay on Hands - 633, Blessing of Spellwarding - 204018
[SpellScript(new uint[]
{
    1022, 633, 204018
})]
public class SpellPalActivateForbearance : SpellScript, ISpellOnHit, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var target = ExplTargetUnit;

        if (target != null)
            if (target.HasAura(PaladinSpells.FORBEARANCE))
                return SpellCastResult.TargetAurastate;

        return SpellCastResult.SpellCastOk;
    }


    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
                player.SpellFactory.CastSpell(target, PaladinSpells.FORBEARANCE, true);
        }
    }
}