﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class MoveSetFlag : ServerPacket
{
	public ObjectGuid MoverGUID;
	public uint SequenceIndex; // Unit movement packet index, incremented each time
	public MoveSetFlag(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(MoverGUID);
		_worldPacket.WriteUInt32(SequenceIndex);
	}
}