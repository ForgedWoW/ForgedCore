﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Achievements;

internal class GuildGetAchievementMembers : ClientPacket
{
    public uint AchievementID;
    public ObjectGuid GuildGUID;
    public ObjectGuid PlayerGUID;
    public GuildGetAchievementMembers(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PlayerGUID = WorldPacket.ReadPackedGuid();
        GuildGUID = WorldPacket.ReadPackedGuid();
        AchievementID = WorldPacket.ReadUInt32();
    }
}