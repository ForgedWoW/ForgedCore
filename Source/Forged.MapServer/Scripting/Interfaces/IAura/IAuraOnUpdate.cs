﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Scripting.Interfaces.IAura;

public interface IAuraOnUpdate : IAuraScript
{
	void AuraOnUpdate(uint diff);
}