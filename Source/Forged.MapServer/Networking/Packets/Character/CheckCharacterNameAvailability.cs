﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Character;

internal class CheckCharacterNameAvailability : ClientPacket
{
    public string Name;
    public uint SequenceIndex;
    public CheckCharacterNameAvailability(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        SequenceIndex = WorldPacket.ReadUInt32();
        Name = WorldPacket.ReadString(WorldPacket.ReadBits<uint>(6));
    }
}