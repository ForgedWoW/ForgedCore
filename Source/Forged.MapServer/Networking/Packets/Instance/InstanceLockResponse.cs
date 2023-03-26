﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Instance;

internal class InstanceLockResponse : ClientPacket
{
	public bool AcceptLock;
	public InstanceLockResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AcceptLock = _worldPacket.HasBit();
	}
}