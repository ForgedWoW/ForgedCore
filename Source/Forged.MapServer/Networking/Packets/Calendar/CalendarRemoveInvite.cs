﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarRemoveInvite : ClientPacket
{
    public ulong EventID;
    public ObjectGuid Guid;
    public ulong InviteID;
    public ulong ModeratorID;
    public CalendarRemoveInvite(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Guid = WorldPacket.ReadPackedGuid();
        InviteID = WorldPacket.ReadUInt64();
        ModeratorID = WorldPacket.ReadUInt64();
        EventID = WorldPacket.ReadUInt64();
    }
}