﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Quest;

public class QuestStatusData
{
    public bool Explored { get; set; }
    public ushort Slot { get; set; } = SharedConst.MaxQuestLogSize;
    public QuestStatus Status { get; set; }
    public uint Timer { get; set; }
}