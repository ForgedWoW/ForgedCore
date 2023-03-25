﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public struct SpellAmmo
{
	public int DisplayID;
	public sbyte InventoryType;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(DisplayID);
		data.WriteInt8(InventoryType);
	}
}