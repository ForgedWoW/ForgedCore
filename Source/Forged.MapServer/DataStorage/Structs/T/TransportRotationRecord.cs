﻿namespace Forged.MapServer.DataStorage.Structs.T;

public sealed record TransportRotationRecord
{
    public uint Id;
    public float[] Rot = new float[4];
    public uint TimeIndex;
    public uint GameObjectsID;
}