﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.BattlePet;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.BattlePetRequestJournalLock)]
	void HandleBattlePetRequestJournalLock(BattlePetRequestJournalLock battlePetRequestJournalLock)
	{
		BattlePetMgr.SendJournalLockStatus();

		if (BattlePetMgr.HasJournalLock)
			BattlePetMgr.SendJournal();
	}

	[WorldPacketHandler(ClientOpcodes.BattlePetSetBattleSlot)]
	void HandleBattlePetSetBattleSlot(BattlePetSetBattleSlot battlePetSetBattleSlot)
	{
		var pet = BattlePetMgr.GetPet(battlePetSetBattleSlot.PetGuid);

		if (pet != null)
		{
			var slot = BattlePetMgr.GetSlot((BattlePetSlots)battlePetSetBattleSlot.Slot);

			if (slot != null)
				slot.Pet = pet.PacketInfo;
		}
	}

	[WorldPacketHandler(ClientOpcodes.BattlePetModifyName)]
	void HandleBattlePetModifyName(BattlePetModifyName battlePetModifyName)
	{
		BattlePetMgr.ModifyName(battlePetModifyName.PetGuid, battlePetModifyName.Name, battlePetModifyName.DeclinedNames);
	}

	[WorldPacketHandler(ClientOpcodes.BattlePetDeletePet)]
	void HandleBattlePetDeletePet(BattlePetDeletePet battlePetDeletePet)
	{
		BattlePetMgr.RemovePet(battlePetDeletePet.PetGuid);
	}

	[WorldPacketHandler(ClientOpcodes.BattlePetSetFlags)]
	void HandleBattlePetSetFlags(BattlePetSetFlags battlePetSetFlags)
	{
		if (!BattlePetMgr.HasJournalLock)
			return;

		var pet = BattlePetMgr.GetPet(battlePetSetFlags.PetGuid);

		if (pet != null)
		{
			if (battlePetSetFlags.ControlType == FlagsControlType.Apply)
				pet.PacketInfo.Flags |= (ushort)battlePetSetFlags.Flags;
			else
				pet.PacketInfo.Flags &= (ushort)~battlePetSetFlags.Flags;

			if (pet.SaveInfo != BattlePetSaveInfo.New)
				pet.SaveInfo = BattlePetSaveInfo.Changed;
		}
	}

	[WorldPacketHandler(ClientOpcodes.BattlePetClearFanfare)]
	void HandleBattlePetClearFanfare(BattlePetClearFanfare battlePetClearFanfare)
	{
		BattlePetMgr.ClearFanfare(battlePetClearFanfare.PetGuid);
	}

	[WorldPacketHandler(ClientOpcodes.CageBattlePet)]
	void HandleCageBattlePet(CageBattlePet cageBattlePet)
	{
		BattlePetMgr.CageBattlePet(cageBattlePet.PetGuid);
	}

	[WorldPacketHandler(ClientOpcodes.BattlePetSummon, Processing = PacketProcessing.Inplace)]
	void HandleBattlePetSummon(BattlePetSummon battlePetSummon)
	{
		if (_player.SummonedBattlePetGUID != battlePetSummon.PetGuid)
			BattlePetMgr.SummonPet(battlePetSummon.PetGuid);
		else
			BattlePetMgr.DismissPet();
	}

	[WorldPacketHandler(ClientOpcodes.BattlePetUpdateNotify)]
	void HandleBattlePetUpdateNotify(BattlePetUpdateNotify battlePetUpdateNotify)
	{
		BattlePetMgr.UpdateBattlePetData(battlePetUpdateNotify.PetGuid);
	}
}