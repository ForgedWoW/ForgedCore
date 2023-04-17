﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenPreventEmotes : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, SpellConst.EffectFirstFound, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
    }
}