﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class RoleChangedInform : ServerPacket
{
	public sbyte PartyIndex;
	public ObjectGuid From;
	public ObjectGuid ChangedUnit;
	public int OldRole;
	public int NewRole;
	public RoleChangedInform() : base(ServerOpcodes.RoleChangedInform) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WritePackedGuid(From);
		_worldPacket.WritePackedGuid(ChangedUnit);
		_worldPacket.WriteInt32(OldRole);
		_worldPacket.WriteInt32(NewRole);
	}
}