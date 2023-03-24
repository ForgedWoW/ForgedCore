﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.RealmServer;

[Flags]
public enum ShutdownMask
{
	Restart = 1,
	Idle = 2,
	Force = 4
}