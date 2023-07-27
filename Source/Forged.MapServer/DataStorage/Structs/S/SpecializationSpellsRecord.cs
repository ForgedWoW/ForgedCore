﻿namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpecializationSpellsRecord
{
    public string Description;
    public uint Id;
    public ushort SpecID;
    public uint SpellID;
    public uint OverridesSpellID;
    public byte DisplayOrder;
}