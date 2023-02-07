﻿using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(5221)]
public class spell_dru_shred : SpellScript, ISpellOnHit, ISpellCalcCritChance
{


	public override bool Load()
	{
		Unit caster = GetCaster();

		if (caster.HasAuraType(AuraType.ModStealth))
		{
			m_stealthed = true;
		}

		if (caster.HasAura(ShapeshiftFormSpells.SPELL_DRUID_INCARNATION_KING_OF_JUNGLE))
		{
			m_incarnation = true;
		}

		m_casterLevel = caster.GetLevelForTarget(caster);

		return true;
	}

	public void CalcCritChance(Unit victim, ref float chance)
	{
		// If caster is level >= 56, While stealthed or have Incarnation: King of the Jungle aura,
		// Double the chance to critically strike
		if ((m_casterLevel >= 56) && (m_stealthed || m_incarnation))
		{
			chance *= 2.0f;
		}
	}

	public void OnHit()
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (caster == null || target == null)
		{
			return;
		}

		int damage = GetHitDamage();

		caster.ModifyPower(PowerType.ComboPoints, 1);

		// If caster is level >= 56, While stealthed or have Incarnation: King of the Jungle aura,
		// deals 50% increased damage (get value from the spell data)
		if ((caster.HasAura(231057)) && (m_stealthed || m_incarnation))
		{
			MathFunctions.AddPct(ref damage, Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_SHRED, Difficulty.None).GetEffect(2).BasePoints);
		}

		SetHitDamage(damage);
	}

	private bool m_stealthed = false;
	private bool m_incarnation = false;
	private uint m_casterLevel;
}