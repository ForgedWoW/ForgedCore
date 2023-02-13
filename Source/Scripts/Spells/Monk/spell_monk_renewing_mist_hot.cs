﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(119611)]
public class spell_monk_renewing_mist_hot : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_RENEWING_MIST_JUMP, MonkSpells.SPELL_MONK_RENEWING_MIST);
	}

	private void HandlePeriodicHeal(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (GetTarget().IsFullHealth())
			caster.CastSpell(GetTarget(), MonkSpells.SPELL_MONK_RENEWING_MIST_JUMP, true);
	}

	private void CalcAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		var caster         = GetCaster();
		var counteractAura = caster.GetAura(MonkSpells.SPELL_MONK_COUNTERACT_MAGIC);

		if (counteractAura != null)
		{
			var appliedAuras = GetUnitOwner().GetAppliedAuras();

			foreach (var kvp in appliedAuras.KeyValueList)
			{
				var baseAura = kvp.Value.GetBase();

				if (baseAura.GetSpellInfo().IsPositive())
					continue;

				if ((baseAura.GetSpellInfo().GetSchoolMask() & SpellSchoolMask.Shadow) == 0)
					continue;

				if ((baseAura.GetSpellInfo().GetDispelMask() & (1 << (int)DispelType.Magic)) == 0)
					continue;

				if (baseAura.HasEffectType(AuraType.PeriodicDamage) || baseAura.HasEffectType(AuraType.PeriodicDamagePercent))
				{
					var effInfo = counteractAura.GetEffect(0);

					if (effInfo != null)
						MathFunctions.AddPct(ref amount, effInfo.GetAmount());

					{
					}
				}
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicHeal, 0, AuraType.PeriodicHeal));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.PeriodicHeal));
	}
}