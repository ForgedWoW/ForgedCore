﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.RealmServer.Quest;

public class QuestConditionalText
{
	public int PlayerConditionId;
	public int QuestgiverCreatureId;
	public StringArray Text = new((int)Locale.Total);
}