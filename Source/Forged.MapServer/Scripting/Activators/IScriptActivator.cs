﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting.Interfaces;

namespace Forged.MapServer.Scripting.Activators;

public interface IScriptActivator
{
	List<string> ScriptBaseTypes { get; }
	IScriptObject Activate(Type type, string name, ScriptAttribute attribute);
}