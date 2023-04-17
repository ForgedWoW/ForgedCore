﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 51505 - Lava burst
[SpellScript(51505)]
internal class SpellShaLavaBurst : SpellScript, ISpellAfterCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterCast()
    {
        var caster = Caster;

        var lavaSurge = caster.GetAura(ShamanSpells.LavaSurge);

        if (lavaSurge != null)
            if (!Spell.AppliedMods.Contains(lavaSurge))
            {
                var chargeCategoryId = SpellInfo.ChargeCategoryId;

                // Ensure we have at least 1 usable charge after cast to allow next cast immediately
                if (!caster.SpellHistory.HasCharge(chargeCategoryId))
                    caster.SpellHistory.RestoreCharge(chargeCategoryId);
            }
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;

        if (caster)
            if (caster.HasAura(ShamanSpells.PathOfFlamesTalent))
                caster.SpellFactory.CastSpell(HitUnit, ShamanSpells.PathOfFlamesSpread, new CastSpellExtraArgs(Spell));
    }
}