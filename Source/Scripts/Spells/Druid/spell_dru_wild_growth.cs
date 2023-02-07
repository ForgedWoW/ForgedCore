﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 48438 - Wild Growth
    internal class spell_dru_wild_growth : SpellScript, IHasSpellEffects
    {
        private List<WorldObject> _targets;
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            if (spellInfo.GetEffects().Count <= 2 ||
                spellInfo.GetEffect(2).IsEffect() ||
                spellInfo.GetEffect(2).CalcValue() <= 0)
                return false;

            return true;
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(SetTargets, 1, Targets.UnitDestAreaAlly));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
                              {
                                  Unit target = obj.ToUnit();

                                  if (target)
                                      return !GetCaster().IsInRaidWith(target);

                                  return true;
                              });

            int maxTargets = GetEffectInfo(2).CalcValue(GetCaster());

            if (targets.Count > maxTargets)
            {
                targets.Sort(new HealthPctOrderPred());
                targets.RemoveRange(maxTargets, targets.Count - maxTargets);
            }

            _targets = targets;
        }

        private void SetTargets(List<WorldObject> targets)
        {
            targets.Clear();
            targets.AddRange(_targets);
        }
    }
}