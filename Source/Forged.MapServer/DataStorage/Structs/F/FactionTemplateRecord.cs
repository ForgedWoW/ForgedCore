﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.F;

public sealed record FactionTemplateRecord
{
    public ushort[] Enemies = new ushort[MAX_FACTION_RELATIONS];
    public byte EnemyGroup;
    public ushort Faction;
    public byte FactionGroup;
    public ushort Flags;
    public ushort[] Friend = new ushort[MAX_FACTION_RELATIONS];
    public byte FriendGroup;
    public uint Id;
    private static readonly int MAX_FACTION_RELATIONS = 8;

    public bool IsContestedGuardFaction()
    {
        return (Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0;
    }

    // helpers
    public bool IsFriendlyTo(FactionTemplateRecord entry)
    {
        if (this == entry)
            return true;

        if (entry.Faction != 0)
        {
            for (var i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Enemies[i] == entry.Faction)
                    return false;

            for (var i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Friend[i] == entry.Faction)
                    return true;
        }

        return (FriendGroup & entry.FactionGroup) != 0 || (FactionGroup & entry.FriendGroup) != 0;
    }

    public bool IsHostileTo(FactionTemplateRecord entry)
    {
        if (this == entry)
            return false;

        if (entry.Faction != 0)
        {
            for (var i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Enemies[i] == entry.Faction)
                    return true;

            for (var i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Friend[i] == entry.Faction)
                    return false;
        }

        return (EnemyGroup & entry.FactionGroup) != 0;
    }

    public bool IsHostileToPlayers()
    {
        return (EnemyGroup & (byte)FactionMasks.Player) != 0;
    }

    public bool IsNeutralToAll()
    {
        for (var i = 0; i < MAX_FACTION_RELATIONS; ++i)
            if (Enemies[i] != 0)
                return false;

        return EnemyGroup == 0 && FriendGroup == 0;
    }
}