﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class AdventureMapStartQuest : ClientPacket
{
	public uint QuestID;
	public AdventureMapStartQuest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		QuestID = _worldPacket.ReadUInt32();
	}
}