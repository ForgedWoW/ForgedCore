﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Misc;

class SetPvP : ClientPacket
{
	public bool EnablePVP;
	public SetPvP(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		EnablePVP = _worldPacket.HasBit();
	}
}