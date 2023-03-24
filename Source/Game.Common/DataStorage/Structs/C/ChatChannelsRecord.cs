﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.ClientReader;

namespace Game.Common.DataStorage.Structs.C;

public sealed class ChatChannelsRecord
{
	public uint Id;
	public LocalizedString Name;
	public string Shortcut;
	public ChannelDBCFlags Flags;
	public sbyte FactionGroup;
	public int Ruleset;
}