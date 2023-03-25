﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class RequestPVPRewardsResponse : ServerPacket
{
	public uint RatedRewardPointsThisWeek;
	public uint ArenaRewardPointsThisWeek;
	public uint RatedMaxRewardPointsThisWeek;
	public uint ArenaRewardPoints;
	public uint RandomRewardPointsThisWeek;
	public uint ArenaMaxRewardPointsThisWeek;
	public uint RatedRewardPoints;
	public uint MaxRewardPointsThisWeek;
	public uint RewardPointsThisWeek;
	public uint RandomMaxRewardPointsThisWeek;
	public RequestPVPRewardsResponse() : base(ServerOpcodes.RequestPvpRewardsResponse) { }

	public override void Write()
	{
		throw new NotImplementedException();
	}
}