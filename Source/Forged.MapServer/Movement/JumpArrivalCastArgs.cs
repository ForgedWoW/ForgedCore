﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Movement;

public class JumpArrivalCastArgs
{
    public ObjectGuid Target;
    public uint SpellId { get; set; }
}