﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

[AreaTriggerScript(8382)] //  8382 - AreaTriggerId
internal class AreatriggerShaEarthquake : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate, IAreaTriggerScriptValues
{
    private TimeSpan _period = TimeSpan.Zero;
    private TimeSpan _refreshTimer = TimeSpan.FromSeconds(1);

    public Dictionary<string, object> ScriptValues { get; } = new();

    public void OnCreate()
    {
        var caster = At.GetCaster();

        if (caster != null)
        {
            var earthquake = caster.GetAuraEffect(ShamanSpells.Earthquake, 1);

            if (earthquake != null)
                _period = TimeSpan.FromMilliseconds(earthquake.Period);
        }
    }

    public void OnUpdate(uint diff)
    {
        _refreshTimer -= TimeSpan.FromMilliseconds(diff);

        while (_refreshTimer <= TimeSpan.Zero)
        {
            var caster = At.GetCaster();

            caster?.SpellFactory.CastSpell(At.Location,
                              ShamanSpells.EarthquakeTick,
                              new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                  .SetOriginalCaster(At.GUID));

            _refreshTimer += _period;
        }
    }
}