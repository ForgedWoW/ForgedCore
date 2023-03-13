﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IUnit;

namespace Scripts.Spells.Evoker;

[Script]
internal class player_evoker_script : ScriptObjectAutoAdd, IUnitOnHeal
{
    public Class PlayerClass { get; } = Class.Evoker;

    public player_evoker_script() : base("player_evoker_script") { }

    public void OnHeal(HealInfo healInfo, ref uint gain)
    {
        EmeraldCommunion(healInfo, gain);
    }

    private void EmeraldCommunion(HealInfo healInfo, uint gain)
    {
        if (healInfo.SpellInfo.Id == EvokerSpells.EMERALD_COMMUNION &&
            healInfo.Healer == healInfo.Target && gain < healInfo.Heal)
        {
            var healer = healInfo.Healer;
            // get targets
            var targetList = new List<Unit>();
            healer.GetAlliesWithinRange(targetList, 100);
            targetList.RemoveIf(a => a.IsFullHealth);

            if (targetList.Count == 0)
                return;

            // reduce targetList to the number allowed
            targetList.RandomResize(1);

            // cast on targets
            HealInfo info = new(healer, targetList[0], healInfo.Heal - gain,
                healInfo.SpellInfo, healInfo.SchoolMask);

            Unit.DealHeal(info);
        }
    }
}