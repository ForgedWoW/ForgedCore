﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.SPIRITBLOOM, EvokerSpells.SPIRITBLOOM_2)]
internal class spell_evoker_empath : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (TryGetCaster(out Player player) && player.HasSpell(EvokerSpells.EMPATH))
            player.AddAura(EvokerSpells.EMPATH_AURA);
    }
}