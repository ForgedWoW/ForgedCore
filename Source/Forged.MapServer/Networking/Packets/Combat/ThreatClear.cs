﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Combat;

public class ThreatClear : ServerPacket
{
	public ObjectGuid UnitGUID;
	public ThreatClear() : base(ServerOpcodes.ThreatClear) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
	}
}