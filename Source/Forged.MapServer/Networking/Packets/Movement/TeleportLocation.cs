﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Movement;

public class TeleportLocation
{
	public Position Pos;
	public int Unused901_1 = -1;
	public int Unused901_2 = -1;

	public void Write(WorldPacket data)
	{
		data.WriteXYZO(Pos);
		data.WriteInt32(Unused901_1);
		data.WriteInt32(Unused901_2);
	}
}