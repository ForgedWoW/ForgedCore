﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharacterRenameResult : ServerPacket
{
	public string Name;
	public ResponseCodes Result;
	public ObjectGuid? Guid;
	public CharacterRenameResult() : base(ServerOpcodes.CharacterRenameResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Result);
		_worldPacket.WriteBit(Guid.HasValue);
		_worldPacket.WriteBits(Name.GetByteCount(), 6);
		_worldPacket.FlushBits();

		if (Guid.HasValue)
			_worldPacket.WritePackedGuid(Guid.Value);

		_worldPacket.WriteString(Name);
	}
}