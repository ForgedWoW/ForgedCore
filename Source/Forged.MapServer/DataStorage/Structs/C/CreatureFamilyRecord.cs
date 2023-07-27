﻿using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CreatureFamilyRecord
{
    public uint Id;
    public LocalizedString Name;
    public float MinScale;
    public sbyte MinScaleLevel;
    public float MaxScale;
    public sbyte MaxScaleLevel;
    public ushort PetFoodMask;
    public sbyte PetTalentType;
    public int IconFileID;
    public short[] SkillLine = new short[2];
}