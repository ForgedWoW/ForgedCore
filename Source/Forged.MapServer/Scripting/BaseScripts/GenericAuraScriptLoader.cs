﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Scripting.Interfaces;

namespace Forged.MapServer.Scripting.BaseScripts;

public class GenericAuraScriptLoader<A> : AuraScriptLoader, IScriptAutoAdd where A : AuraScript
{
    private readonly object[] _args;

    public GenericAuraScriptLoader(string name, object[] args) : base(name)
    {
        _args = args;
    }

    public override AuraScript GetAuraScript()
    {
        return (A)Activator.CreateInstance(typeof(A), _args);
    }
}