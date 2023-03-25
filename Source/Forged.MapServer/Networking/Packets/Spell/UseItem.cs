﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Spell;

public class UseItem : ClientPacket
{
	public byte PackSlot;
	public byte Slot;
	public ObjectGuid CastItem;
	public SpellCastRequest Cast;

	public UseItem(WorldPacket packet) : base(packet)
	{
		Cast = new SpellCastRequest();
	}

	public override void Read()
	{
		PackSlot = _worldPacket.ReadUInt8();
		Slot = _worldPacket.ReadUInt8();
		CastItem = _worldPacket.ReadPackedGuid();
		Cast.Read(_worldPacket);
	}
}