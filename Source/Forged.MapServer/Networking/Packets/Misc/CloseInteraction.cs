﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Misc;

class CloseInteraction : ClientPacket
{
	public ObjectGuid SourceGuid;
	public CloseInteraction(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SourceGuid = _worldPacket.ReadPackedGuid();
	}
}