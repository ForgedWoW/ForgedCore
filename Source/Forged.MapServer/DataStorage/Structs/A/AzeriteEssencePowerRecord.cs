﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AzeriteEssencePowerRecord
{
    public int AzeriteEssenceID;
    public uint Id;
    public uint MajorPowerActual;
    public uint MajorPowerDescription;
    public uint MinorPowerActual;
    public uint MinorPowerDescription;
    public string SourceAlliance;
    public string SourceHorde;
    public byte Tier;
}