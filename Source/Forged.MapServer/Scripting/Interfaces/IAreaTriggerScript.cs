﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.AreaTriggers;

namespace Forged.MapServer.Scripting.Interfaces;

public interface IAreaTriggerScript
{
	AreaTrigger At { get; }
	byte CurrentScriptState { get; set; }
	string ScriptName { get; set; }
	uint ScriptAreaTriggerId { get; set; }

	bool Load();
	void Register();
	void Unload();
	string _GetScriptName();
	void _Init(string scriptname, uint sreaTriggerId);
	void _Register();
	void _Unload();
}