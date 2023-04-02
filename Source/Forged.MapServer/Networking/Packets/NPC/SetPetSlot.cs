﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.NPC;

internal class SetPetSlot : ClientPacket
{
    public byte DestSlot;
    public uint PetNumber;
    public ObjectGuid StableMaster;
    public SetPetSlot(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetNumber = WorldPacket.ReadUInt32();
        DestSlot = WorldPacket.ReadUInt8();
        StableMaster = WorldPacket.ReadPackedGuid();
    }
}