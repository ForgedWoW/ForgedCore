﻿namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class ConditionalContentTuningRecord
{
    public uint Id;
    public int OrderIndex;
    public int RedirectContentTuningID;
    public int RedirectFlag;
    public uint ParentContentTuningID;
}