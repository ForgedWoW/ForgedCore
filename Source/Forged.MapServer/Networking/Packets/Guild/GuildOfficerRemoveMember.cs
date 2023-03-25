﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildOfficerRemoveMember : ClientPacket
{
	public ObjectGuid Removee;
	public GuildOfficerRemoveMember(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Removee = _worldPacket.ReadPackedGuid();
	}
}