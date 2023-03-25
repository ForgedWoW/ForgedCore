﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Movement;

class MoveTimeSkipped : ClientPacket
{
	public ObjectGuid MoverGUID;
	public uint TimeSkipped;
	public MoveTimeSkipped(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		MoverGUID = _worldPacket.ReadPackedGuid();
		TimeSkipped = _worldPacket.ReadUInt32();
	}
}