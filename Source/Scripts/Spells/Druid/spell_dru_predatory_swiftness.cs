﻿using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(16974)]
public class spell_dru_predatory_swiftness : SpellScript, ISpellCheckCast, ISpellOnHit
{
	private int _cp;

	public override bool Load()
	{
		_cp = GetCaster().GetPower(PowerType.ComboPoints);
		return true;
	}

	public SpellCastResult CheckCast()
	{
		if (GetCaster())
		{
			if (GetCaster().GetTypeId() != TypeId.Player)
			{
				return SpellCastResult.DontReport;
			}

			if (GetCaster().ToPlayer().GetPower(PowerType.ComboPoints) != 0)
			{
				return SpellCastResult.NoComboPoints;
			}
		}
		else
		{
			return SpellCastResult.DontReport;
		}

		return SpellCastResult.SpellCastOk;
	}

	public void OnHit()
	{
		Player player = GetCaster().ToPlayer();
		if (player != null)
		{
			if (player.HasAura(PredatorySwiftnessSpells.SPELL_DRUID_PREDATORY_SWIFTNESS) && RandomHelper.randChance(20 * _cp))
			{
				player.CastSpell(player, PredatorySwiftnessSpells.SPELL_DRUID_PREDATORY_SWIFTNESS_AURA, true);
			}
		}
	}
}