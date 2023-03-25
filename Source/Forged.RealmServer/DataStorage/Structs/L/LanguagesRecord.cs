﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class LanguagesRecord
{
	public uint Id;
	public LocalizedString Name;
	public int Flags;
	public int UiTextureKitID;
	public int UiTextureKitElementCount;
}