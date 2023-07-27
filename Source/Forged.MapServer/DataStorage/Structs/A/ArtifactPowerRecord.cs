﻿using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record ArtifactPowerRecord
{
    public Vector2 DisplayPos;
    public uint Id;
    public byte ArtifactID;
    public byte MaxPurchasableRank;
    public int Label;
    public ArtifactPowerFlag Flags;
    public byte Tier;
}