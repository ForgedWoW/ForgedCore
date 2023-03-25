﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public struct EarnedAchievement
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Id);
		data.WritePackedTime(Date);
		data.WritePackedGuid(Owner);
		data.WriteUInt32(VirtualRealmAddress);
		data.WriteUInt32(NativeRealmAddress);
	}

	public uint Id;
	public long Date;
	public ObjectGuid Owner;
	public uint VirtualRealmAddress;
	public uint NativeRealmAddress;
}