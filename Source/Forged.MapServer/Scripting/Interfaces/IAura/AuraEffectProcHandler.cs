﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IAura;

public class AuraEffectProcHandler : AuraEffectHandler, IAuraEffectProcHandler
{
    private readonly Action<AuraEffect, ProcEventInfo> _fn;

    public AuraEffectProcHandler(Action<AuraEffect, ProcEventInfo> fn, int effectIndex, AuraType auraType, AuraScriptHookType hookType) : base(effectIndex, auraType, hookType)
    {
        _fn = fn;

        if (hookType != AuraScriptHookType.EffectProc &&
            hookType != AuraScriptHookType.EffectAfterProc)
            throw new Exception($"Hook Type {hookType} is not valid for {nameof(AuraEffectProcHandler)}. Use {AuraScriptHookType.EffectProc} or {AuraScriptHookType.EffectAfterProc}");
    }

    public void HandleProc(AuraEffect aura, ProcEventInfo info)
    {
        _fn(aura, info);
    }
}