﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class LootReleaseResponse : ServerPacket
{
    public ObjectGuid LootObj;
    public ObjectGuid Owner;
    public LootReleaseResponse() : base(ServerOpcodes.LootRelease) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(LootObj);
        WorldPacket.WritePackedGuid(Owner);
    }
}