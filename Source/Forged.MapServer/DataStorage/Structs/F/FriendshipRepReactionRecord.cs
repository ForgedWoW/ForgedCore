﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.F;

public sealed class FriendshipRepReactionRecord
{
	public uint Id;
	public LocalizedString Reaction;
	public uint FriendshipRepID;
	public ushort ReactionThreshold;
	public int OverrideColor;
}