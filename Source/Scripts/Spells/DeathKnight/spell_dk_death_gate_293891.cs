﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(293891)]
public class spell_dk_death_gate_293891 : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleHit(int effIndex)
	{
		var target = HitUnit;

		var player = target.AsPlayer;

		if (player != null)
		{
			uint QUEST_SCARLET_ARMIES_APPROACH = 12757; // only time death gate teles to classic ebon hold.
			uint QUEST_LIGHT_OF_DAWN = 12801;           // lights hope chappel fight. after this death gate teles to final phase of classic ebon hold.

			/* If player is over level 45 tele to legion ebon hold */
			if (player.Level >= 45)
				player.TeleportTo(1220, -1503.367f, 1052.059f, 260.396f, player.Location.Orientation); // legion ebon hold
			/* If on quest 12757 "Scarlet enemies approach" */
			else if ((player.GetQuestStatus(QUEST_LIGHT_OF_DAWN) == QuestStatus.None) && (player.GetQuestStatus(QUEST_SCARLET_ARMIES_APPROACH) == QuestStatus.None) && (!player.IsAlliedRace()) || (player.HasQuest(QUEST_SCARLET_ARMIES_APPROACH) && (!player.IsAlliedRace())))
				player.TeleportTo(609, 2368.0444f, -5656.1748f, 382.2804f, player.Location.Orientation); // classic ebon hold
			/* If quest 12801 "Light of Dawn" is completed OR if player is alliedrace*/
			else if ((player.GetQuestStatus(QUEST_LIGHT_OF_DAWN) == QuestStatus.Rewarded) && (player.GetQuestStatus(QUEST_SCARLET_ARMIES_APPROACH) == QuestStatus.Rewarded) || (player.IsAlliedRace()))
				player.TeleportTo(0, 2368.0444f, -5656.1748f, 382.2804f, player.Location.Orientation); // final phase of classic ebon hold
		}
	}
}