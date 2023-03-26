﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class QuestPushResultResponse : ServerPacket
{
	public ObjectGuid SenderGUID;
	public QuestPushReason Result;
	public string QuestTitle;
	public QuestPushResultResponse() : base(ServerOpcodes.QuestPushResult) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(SenderGUID);
		_worldPacket.WriteUInt8((byte)Result);

		_worldPacket.WriteBits(QuestTitle.GetByteCount(), 9);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(QuestTitle);
	}
}