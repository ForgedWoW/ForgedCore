﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class MoveKnockBack : ServerPacket
{
	public ObjectGuid MoverGUID;
	public Vector2 Direction;
	public MoveKnockBackSpeeds Speeds;
	public uint SequenceIndex;
	public MoveKnockBack() : base(ServerOpcodes.MoveKnockBack, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(MoverGUID);
		_worldPacket.WriteUInt32(SequenceIndex);
		_worldPacket.WriteVector2(Direction);
		Speeds.Write(_worldPacket);
	}
}