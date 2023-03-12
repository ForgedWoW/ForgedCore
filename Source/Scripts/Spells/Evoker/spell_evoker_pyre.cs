﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.PYRE_MISSILE)]
internal class spell_evoker_pyre : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        Caster.CastSpell(ExplTargetUnit, EvokerSpells.PYRE_DAMAGE, true);
	}
}