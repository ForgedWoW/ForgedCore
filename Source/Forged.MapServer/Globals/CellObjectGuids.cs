﻿using System.Collections.Generic;
using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Globals;

public class CellObjectGuids
{
    public SortedSet<ulong> Creatures = new();
    public SortedSet<ulong> Gameobjects = new();

    public void AddSpawn(SpawnData data)
    {
        switch (data.Type)
        {
            case SpawnObjectType.Creature:
                Creatures.Add(data.SpawnId);

                break;
            case SpawnObjectType.GameObject:
                Gameobjects.Add(data.SpawnId);

                break;
        }
    }

    public void RemoveSpawn(SpawnData data)
    {
        switch (data.Type)
        {
            case SpawnObjectType.Creature:
                Creatures.Remove(data.SpawnId);

                break;
            case SpawnObjectType.GameObject:
                Gameobjects.Remove(data.SpawnId);

                break;
        }
    }
}