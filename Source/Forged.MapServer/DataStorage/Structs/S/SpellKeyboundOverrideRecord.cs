﻿namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellKeyboundOverrideRecord
{
    public uint Id;
    public string Function;
    public sbyte Type;
    public uint Data;
    public int Flags;
}