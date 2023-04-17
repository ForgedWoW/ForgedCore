﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 194384, 214206 - Atonement
internal class SpellPriAtonementTriggered : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleOnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        if (caster)
        {
            var atonement = caster.GetAura(PriestSpells.ATONEMENT);

            if (atonement != null)
            {
                var script = atonement.GetScript<SpellPriAtonement>();

                script?.AddAtonementTarget(Target.GUID);
            }
        }
    }

    private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        if (caster)
        {
            var atonement = caster.GetAura(PriestSpells.ATONEMENT);

            if (atonement != null)
            {
                var script = atonement.GetScript<SpellPriAtonement>();

                script?.RemoveAtonementTarget(Target.GUID);
            }
        }
    }
}