﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GarrBuildingPlotInstRecord
{
    public byte GarrBuildingID;
    public ushort GarrSiteLevelPlotInstID;
    public uint Id;
    public Vector2 MapOffset;
    public ushort UiTextureAtlasMemberID;
}