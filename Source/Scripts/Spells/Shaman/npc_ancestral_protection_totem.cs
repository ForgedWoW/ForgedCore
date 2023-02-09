﻿using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    [CreatureScript(78001)]
    //104818 - Ancestral Protection Totem
    public class npc_ancestral_protection_totem : ScriptedAI
    {
        public npc_ancestral_protection_totem(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            me.CastSpell(me, TotemSpells.SPELL_TOTEM_ANCESTRAL_PROTECTION_AT, true);
        }
    }
}
