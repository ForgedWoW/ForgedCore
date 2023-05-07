﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemArmorTotalRecord
{
    public float Cloth;
    public uint Id;
    public short ItemLevel;
    public float Leather;
    public float Mail;
    public float Plate;
}