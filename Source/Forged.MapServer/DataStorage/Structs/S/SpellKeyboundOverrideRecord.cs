﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellKeyboundOverrideRecord
{
    public uint Data;
    public int Flags;
    public string Function;
    public uint Id;
    public sbyte Type;
}