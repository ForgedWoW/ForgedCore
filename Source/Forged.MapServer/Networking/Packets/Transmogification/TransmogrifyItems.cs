﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Transmogification;

internal class TransmogrifyItems : ClientPacket
{
    public bool CurrentSpecOnly;
    public Array<TransmogrifyItem> Items = new(13);
    public ObjectGuid Npc;
    public TransmogrifyItems(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var itemsCount = WorldPacket.ReadUInt32();
        Npc = WorldPacket.ReadPackedGuid();

        for (var i = 0; i < itemsCount; ++i)
        {
            TransmogrifyItem item = new();
            item.Read(WorldPacket);
            Items[i] = item;
        }

        CurrentSpecOnly = WorldPacket.HasBit();
    }
}