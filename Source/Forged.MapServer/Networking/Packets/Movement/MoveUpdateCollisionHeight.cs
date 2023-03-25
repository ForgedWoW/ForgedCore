﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveUpdateCollisionHeight : ServerPacket
{
	public MovementInfo Status;
	public float Scale = 1.0f;
	public float Height = 1.0f;
	public MoveUpdateCollisionHeight() : base(ServerOpcodes.MoveUpdateCollisionHeight) { }

	public override void Write()
	{
		MovementExtensions.WriteMovementInfo(_worldPacket, Status);
		_worldPacket.WriteFloat(Height);
		_worldPacket.WriteFloat(Scale);
	}
}