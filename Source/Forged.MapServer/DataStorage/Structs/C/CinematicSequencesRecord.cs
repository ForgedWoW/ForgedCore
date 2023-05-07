﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CinematicSequencesRecord
{
    public ushort[] Camera = new ushort[8];
    public uint Id;
    public uint SoundID;
}