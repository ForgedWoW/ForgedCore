﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(2120)]
public class spell_mage_flamestrike : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleOnHit(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		if (caster.HasAura(MageSpells.SPELL_MAGE_HOT_STREAK))
		{
			caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HOT_STREAK);

			if (caster.HasAura(MageSpells.SPELL_MAGE_PYROMANIAC))
			{
				AuraEffect pyromaniacEff0 = caster.GetAuraEffect(MageSpells.SPELL_MAGE_PYROMANIAC, 0);
				if (pyromaniacEff0 != null)
				{
					if (RandomHelper.randChance(pyromaniacEff0.GetAmount()))
					{
						if (caster.HasAura(MageSpells.SPELL_MAGE_HEATING_UP))
						{
							caster.RemoveAurasDueToSpell(MageSpells.SPELL_MAGE_HEATING_UP);
						}

						caster.CastSpell(caster, MageSpells.SPELL_MAGE_HOT_STREAK, true);
					}
				}
			}
		}
	}

	public void AfterCast()
	{
		Unit          caster = GetCaster();
		WorldLocation dest   = GetExplTargetDest();
		if (caster == null || dest == null)
		{
			return;
		}

		if (caster.HasAura(MageSpells.SPELL_MAGE_FLAME_PATCH))
		{
			caster.CastSpell(dest.GetPosition(), MageSpells.SPELL_MAGE_FLAME_PATCH_TRIGGER, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
	}
}