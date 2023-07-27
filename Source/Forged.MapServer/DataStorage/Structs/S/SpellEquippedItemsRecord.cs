﻿namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellEquippedItemsRecord
{
    public uint Id;
    public uint SpellID;
    public sbyte EquippedItemClass;
    public int EquippedItemInvTypes;
    public int EquippedItemSubclass;
}