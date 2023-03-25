﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QueryQuestInfo : ClientPacket
{
	public ObjectGuid QuestGiver;
	public uint QuestID;
	public QueryQuestInfo(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		QuestID = _worldPacket.ReadUInt32();
		QuestGiver = _worldPacket.ReadPackedGuid();
	}
}