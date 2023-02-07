﻿using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Mage;

[SpellScript(236058)]
public class spell_mage_frenetic_speed : AuraScript, IAuraCheckProc
{


	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo().Id == MageSpells.SPELL_MAGE_SCORCH;
	}


}