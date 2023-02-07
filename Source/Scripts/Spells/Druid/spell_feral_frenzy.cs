﻿using System;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Druid;

[SpellScript(274837)]
public class spell_feral_frenzy : SpellScript
{
	public void OnHit()
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();

		if (caster == null || target == null)
		{
			return;
		}

		_strikes = 0;
            
		int strikeDamage = 100 / 20 + caster.m_unitData.AttackPower;
            
		caster.m_Events.AddRepeatEventAtOffset(() =>
		                                       {
			                                       if (caster.GetDistance2d(target) <= 5.0f)
			                                       {
				                                       _strikes++;
				                                       if (this._strikes < 5)
				                                       {
					                                       return TimeSpan.FromMilliseconds(200);
				                                       }
				                                       else if (this._strikes == 5)
				                                       {
					                                       caster.CastSpell(target, DruidSpells.SPELL_FERAL_FRENZY_BLEED, true);
					                                       int bleedDamage = 100 / 10 + caster.m_unitData.AttackPower;
				                                       }
			                                       }
			                                       return default;
		                                       }, TimeSpan.FromMilliseconds(50));
	}



	private byte _strikes;
}