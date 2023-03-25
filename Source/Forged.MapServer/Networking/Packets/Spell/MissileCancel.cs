﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

class MissileCancel : ServerPacket
{
	public ObjectGuid OwnerGUID;
	public bool Reverse;
	public uint SpellID;
	public MissileCancel() : base(ServerOpcodes.MissileCancel) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(OwnerGUID);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(Reverse);
		_worldPacket.FlushBits();
	}
}