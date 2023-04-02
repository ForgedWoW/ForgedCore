﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Trait;

internal class TraitsCommitConfig : ClientPacket
{
    public TraitConfigPacket Config = new();
    public int SavedConfigID;
    public int SavedLocalIdentifier;

    public TraitsCommitConfig(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Config.Read(WorldPacket);
        SavedConfigID = WorldPacket.ReadInt32();
        SavedLocalIdentifier = WorldPacket.ReadInt32();
    }
}