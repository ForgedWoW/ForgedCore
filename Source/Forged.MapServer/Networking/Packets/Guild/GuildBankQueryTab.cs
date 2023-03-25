﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildBankQueryTab : ClientPacket
{
	public ObjectGuid Banker;
	public byte Tab;
	public bool FullUpdate;
	public GuildBankQueryTab(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		Tab = _worldPacket.ReadUInt8();

		FullUpdate = _worldPacket.HasBit();
	}
}