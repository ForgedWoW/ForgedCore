﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Felstorm - 119914
[SpellScript(119914)]
public class SpellWarlFelstorm : SpellScript, ISpellAfterHit, ISpellCheckCast
{
    public void AfterHit()
    {
        var caster = Caster;

        if (caster == null)
            return;

        caster.AsPlayer.SpellHistory.ModifyCooldown(SpellInfo.Id, TimeSpan.FromSeconds(45));
    }

    public SpellCastResult CheckCast()
    {
        var caster = Caster;
        var pet = caster.GetGuardianPet();

        if (caster == null || pet == null)
            return SpellCastResult.DontReport;

        if (pet.SpellHistory.HasCooldown(WarlockSpells.FELGUARD_FELSTORM))
            return SpellCastResult.CantDoThatRightNow;

        return SpellCastResult.SpellCastOk;
    }
}