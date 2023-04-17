﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206491)]
public class SpellDhNemesis : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 0, AuraType.ModSchoolMaskDamageFromCaster, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleAfterRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        if (TargetApplication == null)
            return;

        if (TargetApplication.RemoveMode != AuraRemoveMode.Death)
            return;

        var target = TargetApplication.Target;
        var type = target.CreatureType;
        var dur = TargetApplication.Base.Duration;
        var caster = Aura.Caster;

        if (caster == null || target == null)
            return;

        uint spellId = 0;

        switch (type)
        {
            case CreatureType.Aberration:
                spellId = NemesisSpells.NEMESIS_ABERRATION;

                break;
            case CreatureType.Beast:
                spellId = NemesisSpells.NEMESIS_BEASTS;

                break;
            case CreatureType.Critter:
                spellId = NemesisSpells.NEMESIS_CRITTERS;

                break;
            case CreatureType.Demon:
                spellId = NemesisSpells.NEMESIS_DEMONS;

                break;
            case CreatureType.Dragonkin:
                spellId = NemesisSpells.NEMESIS_DRAGONKIN;

                break;
            case CreatureType.Elemental:
                spellId = NemesisSpells.NEMESIS_ELEMENTAL;

                break;
            case CreatureType.Giant:
                spellId = NemesisSpells.NEMESIS_GIANTS;

                break;
            case CreatureType.Humanoid:
                spellId = NemesisSpells.NEMESIS_HUMANOID;

                break;
            case CreatureType.Mechanical:
                spellId = NemesisSpells.NEMESIS_MECHANICAL;

                break;
            case CreatureType.Undead:
                spellId = NemesisSpells.NEMESIS_UNDEAD;

                break;
        }

        if (spellId != 0)
        {
            var aur = caster.AddAura(spellId, caster);

            if (aur != null)
                aur.SetDuration(dur);
        }
    }
}