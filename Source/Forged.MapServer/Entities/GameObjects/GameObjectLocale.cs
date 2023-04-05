﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Entities.GameObjects;

public class GameObjectLocale
{
    public StringArray CastBarCaption { get; set; } = new((int)Locale.Total);
    public StringArray Name { get; set; } = new((int)Locale.Total);
    public StringArray Unk1 { get; set; } = new((int)Locale.Total);
}