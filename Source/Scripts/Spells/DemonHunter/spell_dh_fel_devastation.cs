﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(212084)]
public class SpellDhFelDevastation : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (aurEff.GetTickNumber() == 1)
            return;

        caster.SpellFactory.CastSpell(caster, DemonHunterSpells.FEL_DEVASTATION_DAMAGE, true);
    }
}