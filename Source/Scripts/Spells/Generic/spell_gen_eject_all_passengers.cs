﻿using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_eject_all_passengers : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var vehicle = GetHitUnit().GetVehicleKit();

		if (vehicle)
			vehicle.RemoveAllPassengers();
	}
}