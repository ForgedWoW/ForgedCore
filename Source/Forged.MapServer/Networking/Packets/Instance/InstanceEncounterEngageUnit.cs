﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Instance;

class InstanceEncounterEngageUnit : ServerPacket
{
	public ObjectGuid Unit;
	public byte TargetFramePriority; // used to set the initial position of the frame if multiple frames are sent
	public InstanceEncounterEngageUnit() : base(ServerOpcodes.InstanceEncounterEngageUnit, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteUInt8(TargetFramePriority);
	}
}