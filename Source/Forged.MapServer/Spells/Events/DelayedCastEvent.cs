﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Framework.Dynamic;

namespace Forged.MapServer.Spells.Events;

public class DelayedCastEvent : BasicEvent
{
    public DelayedCastEvent(Unit trigger, Unit target, uint spellId, CastSpellExtraArgs args)
    {
        Trigger = trigger;
        Target = target;
        SpellId = spellId;
        CastFlags = args;
    }

    public CastSpellExtraArgs CastFlags { get; set; }
    public uint SpellId { get; set; }
    public Unit Target { get; set; }
    public Unit Trigger { get; set; }

    public override bool Execute(ulong etime, uint pTime)
    {
        Trigger.SpellFactory.CastSpell(Target, SpellId, CastFlags);

        return true;
    }
}