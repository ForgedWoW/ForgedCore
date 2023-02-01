﻿using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Game.Spells;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [Script]
        public class npc_warl_demonic_gateway : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public class npc_warl_demonic_gatewayAI : CreatureAI
            {
                public EventMap events = new();
                public bool firstTick = true;

                public npc_warl_demonic_gatewayAI(Creature creature) : base(creature)
                {
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                    if (firstTick)
                    {
                        me.CastSpell(me, SpellIds.DEMONIC_GATEWAY_VISUAL, true);

                        //todo me->SetInteractSpellId(SPELL_WARLOCK_DEMONIC_GATEWAY_ACTIVATE);
                        me.SetUnitFlag(UnitFlags.NonAttackable);
                        me.SetNpcFlag(NPCFlags.SpellClick);
                        me.SetReactState(ReactStates.Passive);
                        me.SetControlled(true, UnitState.Root);

                        firstTick = false;
                    }
                }

                public override void OnSpellClick(Unit clicker, ref bool spellClickHandled)
                {
                    if (clicker.IsPlayer())
                    {
                        // don't allow using the gateway while having specific Auras
                        uint[] aurasToCheck =
                        {
                            121164, 121175, 121176, 121177
                        }; // Orbs of Power @ Temple of Kotmogu

                        foreach (var auraToCheck in aurasToCheck)
                            if (clicker.HasAura(auraToCheck))
                                return;

                        TeleportTarget(clicker, true);
                    }

                    return;
                }

                public void TeleportTarget(Unit target, bool allowAnywhere)
                {
                    Unit owner = me.GetOwner();

                    if (owner == null)
                        return;

                    // only if Target stepped through the portal
                    if (!allowAnywhere &&
                        me.GetDistance2d(target) > 1.0f)
                        return;

                    // check if Target wasn't recently teleported
                    if (target.HasAura(SpellIds.DEMONIC_GATEWAY_DEBUFF))
                        return;

                    // only if in same party
                    if (!target.IsInRaidWith(owner))
                        return;

                    // not allowed while CC'ed
                    if (!target.CanFreeMove())
                        return;

                    uint otherGateway = me.GetEntry() == SpellIds.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN ? SpellIds.NPC_WARLOCK_DEMONIC_GATEWAY_PURPLE : SpellIds.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN;
                    uint teleportSpell = me.GetEntry() == SpellIds.NPC_WARLOCK_DEMONIC_GATEWAY_GREEN ? SpellIds.DEMONIC_GATEWAY_JUMP_GREEN : SpellIds.DEMONIC_GATEWAY_JUMP_PURPLE;

                    var gateways = me.GetCreatureListWithEntryInGrid(otherGateway, 100.0f);

                    foreach (var gateway in gateways)
                    {
                        if (gateway.GetOwnerGUID() != me.GetOwnerGUID())
                            continue;

                        target.CastSpell(gateway, teleportSpell, true);

                        if (target.HasAura(SpellIds.PLANESWALKER))
                            target.CastSpell(target, SpellIds.PLANESWALKER_BUFF, true);

                        // Item - Warlock PvP Set 4P Bonus: "Your allies can use your Demonic Gateway again 15 sec sooner"
                        int amount = owner.GetAuraEffect(SpellIds.PVP_4P_BONUS, 0).GetAmount();

                        if (amount > 0)
                        {
                            Aura aura = target.GetAura(SpellIds.DEMONIC_GATEWAY_DEBUFF);

                            aura?.SetDuration(aura.GetDuration() - amount * Time.InMilliseconds);
                        }

                        break;
                    }
                }
            }

            public npc_warl_demonic_gateway() : base("npc_warl_demonic_gateway")
            {
            }

            //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
            //ORIGINAL LINE: CreatureAI* GetAI(Creature* creature) const
            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warl_demonic_gatewayAI(creature);
            }
        }
    }
}