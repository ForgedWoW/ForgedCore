﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Spell;

class TradeSkillSetFavorite : ClientPacket
{
	public uint RecipeID;
	public bool IsFavorite;

	public TradeSkillSetFavorite(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		RecipeID = _worldPacket.ReadUInt32();
		IsFavorite = _worldPacket.HasBit();
	}
}