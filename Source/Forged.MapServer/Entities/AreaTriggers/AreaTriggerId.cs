﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.AreaTriggers;

public struct AreaTriggerId
{
    public uint Id;
    public bool IsServerSide;

    public AreaTriggerId(uint id, bool isServerSide)
    {
        Id = id;
        IsServerSide = isServerSide;
    }

    public static bool operator !=(AreaTriggerId left, AreaTriggerId right)
    {
        return !(left == right);
    }

    public static bool operator ==(AreaTriggerId left, AreaTriggerId right)
    {
        return left.Id == right.Id && left.IsServerSide == right.IsServerSide;
    }

    public override bool Equals(object obj)
    {
        return this == (obj is AreaTriggerId id ? id : default);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode() ^ IsServerSide.GetHashCode();
    }
}