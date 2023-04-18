﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.S;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SpellInfoLoadHelper
{
    public SpellAuraOptionsRecord AuraOptions { get; set; }
    public SpellAuraRestrictionsRecord AuraRestrictions { get; set; }
    public SpellCastingRequirementsRecord CastingRequirements { get; set; }
    public SpellCategoriesRecord Categories { get; set; }
    public SpellClassOptionsRecord ClassOptions { get; set; }
    public SpellCooldownsRecord Cooldowns { get; set; }
    public Dictionary<int, SpellEffectRecord> Effects { get; set; } = new();
    public List<SpellEmpowerStageRecord> EmpowerStages { get; set; } = new();
    public SpellEquippedItemsRecord EquippedItems { get; set; }
    public SpellInterruptsRecord Interrupts { get; set; }
    public List<SpellLabelRecord> Labels { get; set; } = new();
    public SpellLevelsRecord Levels { get; set; }
    public SpellMiscRecord Misc { get; set; }
    public SpellPowerRecord[] Powers { get; set; } = new SpellPowerRecord[SpellConst.MaxPowersPerSpell];
    public SpellReagentsRecord Reagents { get; set; }
    public List<SpellReagentsCurrencyRecord> ReagentsCurrency { get; set; } = new();
    public SpellScalingRecord Scaling { get; set; }
    public SpellShapeshiftRecord Shapeshift { get; set; }
    public SpellTargetRestrictionsRecord TargetRestrictions { get; set; }
    public SpellTotemsRecord Totems { get; set; }
    public List<SpellXSpellVisualRecord> Visuals { get; set; } = new(); // only to group visuals when parsing sSpellXSpellVisualStore, not for loading
}