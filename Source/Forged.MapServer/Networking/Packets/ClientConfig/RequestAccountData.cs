﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.ClientConfig;

public class RequestAccountData : ClientPacket
{
	public ObjectGuid PlayerGuid;
	public AccountDataTypes DataType = 0;
	public RequestAccountData(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PlayerGuid = _worldPacket.ReadPackedGuid();
		DataType = (AccountDataTypes)_worldPacket.ReadBits<uint>(4);
	}
}