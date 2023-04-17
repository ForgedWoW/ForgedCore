﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using static Scripts.EasternKingdoms.Deadmines.Bosses.BossHelixGearbreaker;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(47314)]
public class NPCStickyBomb : NullCreatureAI
{
    private readonly InstanceScript _instance;

    private uint _phase;
    private uint _uiTimer;

    public NPCStickyBomb(Creature pCreature) : base(pCreature)
    {
        _instance = pCreature.InstanceScript;
    }

    public override void Reset()
    {
        _phase = 1;
        _uiTimer = 500;

        if (!Me)
            return;

        DoCast(Me, ESpels.CHEST_BOMB);
    }

    public override void UpdateAI(uint uiDiff)
    {
        if (!Me)
            return;

        if (_uiTimer < uiDiff)
        {
            switch (_phase)
            {
                case 1:
                    DoCast(Me, ESpels.ARMING_VISUAL_YELLOW);
                    _uiTimer = 700;

                    break;

                case 2:
                    DoCast(Me, ESpels.ARMING_VISUAL_ORANGE);
                    _uiTimer = 600;

                    break;

                case 3:
                    DoCast(Me, ESpels.ARMING_VISUAL_RED);
                    _uiTimer = 500;

                    break;

                case 4:
                    DoCast(Me, ESpels.BOMB_ARMED_STATE);
                    _uiTimer = 400;

                    break;

                case 5:
                    DoCast(Me, Me.Map.IsHeroic ? ESpels.STICKY_BOMB_EXPLODE_H : ESpels.STICKY_BOMB_EXPLODE);
                    _uiTimer = 300;

                    break;

                case 6:
                    Me.DespawnOrUnsummon();

                    break;
            }

            _phase++;
        }
        else
        {
            _uiTimer -= uiDiff;
        }
    }
}