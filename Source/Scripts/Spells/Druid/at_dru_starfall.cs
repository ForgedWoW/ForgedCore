﻿using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Druid;

[Script]
public class at_dru_starfall : AreaTriggerAI
{
	public int timeInterval;

	public at_dru_starfall(AreaTrigger areatrigger) : base(areatrigger)
	{
		// How often should the action be executed
		areatrigger.SetPeriodicProcTimer(850);
	}

	public override void OnPeriodicProc()
	{
		Unit caster = at.GetCaster();
		if (caster != null)
		{
			foreach (ObjectGuid objguid in at.GetInsideUnits())
			{
				Unit unit = ObjectAccessor.Instance.GetUnit(caster, objguid);
				if (unit != null)
				{
					if (caster.IsValidAttackTarget(unit))
					{
						if (unit.IsInCombat())
						{
							caster.CastSpell(unit, StarfallSpells.SPELL_DRUID_STARFALL_DAMAGE, true);
							caster.CastSpell(unit, StarfallSpells.SPELL_DRUID_STELLAR_EMPOWERMENT, true);
						}
					}
				}
			}
		}
	}
}