﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Taxi;

internal class ActivateTaxi : ClientPacket
{
    public uint FlyingMountID;
    public uint GroundMountID;
    public uint Node;
    public ObjectGuid Vendor;
    public ActivateTaxi(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Vendor = WorldPacket.ReadPackedGuid();
        Node = WorldPacket.ReadUInt32();
        GroundMountID = WorldPacket.ReadUInt32();
        FlyingMountID = WorldPacket.ReadUInt32();
    }
}