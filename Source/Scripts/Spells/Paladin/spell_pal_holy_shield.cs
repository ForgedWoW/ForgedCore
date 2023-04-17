﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Paladin;

// Holy Shield - 152261
[SpellScript(152261)]
public class SpellPalHolyShield : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return (eventInfo.HitMask & ProcFlagsHit.Block) != 0;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(HandleCalcAmount, 2, AuraType.SchoolAbsorb));
    }

    private void HandleCalcAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        amount.Value = 0;
    }
}