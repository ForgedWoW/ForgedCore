﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.EasternKingdoms.Deadmines.Spells;

[SpellScript(89268, 89740, 90561, 90562, 90563, 90564, 90565, 90582, 90583, 90584, 90585, 90586)]
public class SpellCaptainCookieThrowFoodTargeting : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        if (!Caster || !HitUnit)
            return;

        uint spellId = 0;

        var spellInfo = SpellInfo;

        if (spellInfo != null)
            spellId = (uint)spellInfo.GetEffect(0).BasePoints;

        if (Global.SpellMgr.GetSpellInfo(spellId, CastDifficulty) != null)
            return;

        Caster.SpellFactory.CastSpell(HitUnit, spellId);
    }
}