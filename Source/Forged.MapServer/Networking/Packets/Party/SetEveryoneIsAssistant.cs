﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Party;

class SetEveryoneIsAssistant : ClientPacket
{
	public byte PartyIndex;
	public bool EveryoneIsAssistant;
	public SetEveryoneIsAssistant(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();
		EveryoneIsAssistant = _worldPacket.HasBit();
	}
}