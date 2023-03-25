﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

class VoidItemSwapResponse : ServerPacket
{
	public ObjectGuid VoidItemA;
	public ObjectGuid VoidItemB;
	public uint VoidItemSlotA;
	public uint VoidItemSlotB;
	public VoidItemSwapResponse() : base(ServerOpcodes.VoidItemSwapResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(VoidItemA);
		_worldPacket.WriteUInt32(VoidItemSlotA);
		_worldPacket.WritePackedGuid(VoidItemB);
		_worldPacket.WriteUInt32(VoidItemSlotB);
	}
}