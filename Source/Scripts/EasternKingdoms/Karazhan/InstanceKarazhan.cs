// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.IMap;

namespace Scripts.EasternKingdoms.Karazhan;

internal struct DataTypes
{
	public const uint Attumen = 0;
	public const uint Moroes = 1;
	public const uint MaidenOfVirtue = 2;
	public const uint OptionalBoss = 3;
	public const uint OperaPerformance = 4;
	public const uint Curator = 5;
	public const uint Aran = 6;
	public const uint Terestian = 7;
	public const uint Netherspite = 8;
	public const uint Chess = 9;
	public const uint Malchezzar = 10;
	public const uint Nightbane = 11;

	public const uint OperaOzDeathcount = 14;

	public const uint Kilrek = 15;
	public const uint GoCurtains = 18;
	public const uint GoStagedoorleft = 19;
	public const uint GoStagedoorright = 20;
	public const uint GoLibraryDoor = 21;
	public const uint GoMassiveDoor = 22;
	public const uint GoNetherDoor = 23;
	public const uint GoGameDoor = 24;
	public const uint GoGameExitDoor = 25;

	public const uint ImageOfMedivh = 26;
	public const uint MastersTerraceDoor1 = 27;
	public const uint MastersTerraceDoor2 = 28;
	public const uint GoSideEntranceDoor = 29;
	public const uint GoBlackenedUrn = 30;
}

internal struct CreatureIds
{
	public const uint HyakissTheLurker = 16179;
	public const uint RokadTheRavager = 16181;
	public const uint ShadikithTheGlider = 16180;
	public const uint TerestianIllhoof = 15688;
	public const uint Moroes = 15687;
	public const uint Nightbane = 17225;
	public const uint AttumenUnmounted = 15550;
	public const uint AttumenMounted = 16152;
	public const uint Midnight = 16151;

	// Trash
	public const uint ColdmistWidow = 16171;
	public const uint ColdmistStalker = 16170;
	public const uint Shadowbat = 16173;
	public const uint VampiricShadowbat = 16175;
	public const uint GreaterShadowbat = 16174;
	public const uint PhaseHound = 16178;
	public const uint Dreadbeast = 16177;
	public const uint Shadowbeast = 16176;
	public const uint Kilrek = 17229;
}

internal struct GameObjectIds
{
	public const uint StageCurtain = 183932;
	public const uint StageDoorLeft = 184278;
	public const uint StageDoorRight = 184279;
	public const uint PrivateLibraryDoor = 184517;
	public const uint MassiveDoor = 185521;
	public const uint GamesmanHallDoor = 184276;
	public const uint GamesmanHallExitDoor = 184277;
	public const uint NetherspaceDoor = 185134;
	public const uint MastersTerraceDoor = 184274;
	public const uint MastersTerraceDoor2 = 184280;
	public const uint SideEntranceDoor = 184275;
	public const uint DustCoveredChest = 185119;
	public const uint BlackenedUrn = 194092;
}

internal enum KZMisc
{
	OptionalBossRequiredDeathCount = 50
}

[Script]
internal class instance_karazhan : InstanceMapScript, IInstanceMapGetInstanceScript
{
	public static Position[] OptionalSpawn =
	{
		new(-10960.981445f, -1940.138428f, 46.178097f, 4.12f),  // Hyakiss the Lurker
		new(-10945.769531f, -2040.153320f, 49.474438f, 0.077f), // Shadikith the Glider
		new(-10899.903320f, -2085.573730f, 49.474449f, 1.38f)   // Rokad the Ravager
	};

	private static readonly DungeonEncounterData[] encounters =
	{
		new(DataTypes.Attumen, 652), new(DataTypes.Moroes, 653), new(DataTypes.MaidenOfVirtue, 654), new(DataTypes.OperaPerformance, 655), new(DataTypes.Curator, 656), new(DataTypes.Aran, 658), new(DataTypes.Terestian, 657), new(DataTypes.Netherspite, 659), new(DataTypes.Chess, 660), new(DataTypes.Malchezzar, 661), new(DataTypes.Nightbane, 662)
	};

	public instance_karazhan() : base(nameof(instance_karazhan), 532) { }

	public InstanceScript GetInstanceScript(InstanceMap map)
	{
		return new instance_karazhan_InstanceMapScript(map);
	}

	private class instance_karazhan_InstanceMapScript : InstanceScript
	{
		private readonly ObjectGuid[] MastersTerraceDoor = new ObjectGuid[2];
		private readonly uint OperaEvent;
		private ObjectGuid BlackenedUrnGUID;
		private ObjectGuid CurtainGUID;
		private ObjectGuid DustCoveredChest;
		private ObjectGuid GamesmansDoor;     // Door before Chess
		private ObjectGuid GamesmansExitDoor; // Door after Chess
		private ObjectGuid ImageGUID;
		private ObjectGuid KilrekGUID;
		private ObjectGuid LibraryDoor; // Door at Shade of Aran
		private ObjectGuid MassiveDoor; // Door at Netherspite
		private ObjectGuid MoroesGUID;
		private ObjectGuid NetherspaceDoor; // Door at Malchezaar
		private ObjectGuid NightbaneGUID;
		private uint OptionalBossCount;
		private uint OzDeathCount;
		private ObjectGuid SideEntranceDoor; // Side Entrance
		private ObjectGuid StageDoorLeftGUID;
		private ObjectGuid StageDoorRightGUID;
		private ObjectGuid TerestianGUID;

		public instance_karazhan_InstanceMapScript(InstanceMap map) : base(map)
		{
			SetHeaders("KZ");
			SetBossNumber(12);
			LoadDungeonEncounterData(encounters);

			// 1 - Oz, 2 - Hood, 3 - Raj, this never gets altered.
			OperaEvent = RandomHelper.URand(1, 3);
			OzDeathCount = 0;
			OptionalBossCount = 0;
		}

		public override void OnCreatureCreate(Creature creature)
		{
			switch (creature.Entry)
			{
				case CreatureIds.Kilrek:
					KilrekGUID = creature.GUID;

					break;
				case CreatureIds.TerestianIllhoof:
					TerestianGUID = creature.GUID;

					break;
				case CreatureIds.Moroes:
					MoroesGUID = creature.GUID;

					break;
				case CreatureIds.Nightbane:
					NightbaneGUID = creature.GUID;

					break;
				default:
					break;
			}
		}

		public override void OnUnitDeath(Unit unit)
		{
			var creature = unit.AsCreature;

			if (!creature)
				return;

			switch (creature.Entry)
			{
				case CreatureIds.ColdmistWidow:
				case CreatureIds.ColdmistStalker:
				case CreatureIds.Shadowbat:
				case CreatureIds.VampiricShadowbat:
				case CreatureIds.GreaterShadowbat:
				case CreatureIds.PhaseHound:
				case CreatureIds.Dreadbeast:
				case CreatureIds.Shadowbeast:
					if (GetBossState(DataTypes.OptionalBoss) == EncounterState.ToBeDecided)
					{
						++OptionalBossCount;

						if (OptionalBossCount == (uint)KZMisc.OptionalBossRequiredDeathCount)
							switch (RandomHelper.URand(CreatureIds.HyakissTheLurker, CreatureIds.RokadTheRavager))
							{
								case CreatureIds.HyakissTheLurker:
									Instance.SummonCreature(CreatureIds.HyakissTheLurker, OptionalSpawn[0]);

									break;
								case CreatureIds.ShadikithTheGlider:
									Instance.SummonCreature(CreatureIds.ShadikithTheGlider, OptionalSpawn[1]);

									break;
								case CreatureIds.RokadTheRavager:
									Instance.SummonCreature(CreatureIds.RokadTheRavager, OptionalSpawn[2]);

									break;
							}
					}

					break;
				default:
					break;
			}
		}

		public override void SetData(uint type, uint data)
		{
			switch (type)
			{
				case DataTypes.OperaOzDeathcount:
					if (data == (uint)EncounterState.Special)
						++OzDeathCount;
					else if (data == (uint)EncounterState.InProgress)
						OzDeathCount = 0;

					break;
			}
		}

		public override bool SetBossState(uint type, EncounterState state)
		{
			if (!base.SetBossState(type, state))
				return false;

			switch (type)
			{
				case DataTypes.OperaPerformance:
					if (state == EncounterState.Done)
					{
						HandleGameObject(StageDoorLeftGUID, true);
						HandleGameObject(StageDoorRightGUID, true);
						var sideEntrance = Instance.GetGameObject(SideEntranceDoor);

						if (sideEntrance)
							sideEntrance.RemoveFlag(GameObjectFlags.Locked);

						UpdateEncounterStateForKilledCreature(16812, null);
					}

					break;
				case DataTypes.Chess:
					if (state == EncounterState.Done)
						DoRespawnGameObject(DustCoveredChest, TimeSpan.FromHours(24));

					break;
				default:
					break;
			}

			return true;
		}

		public override void SetGuidData(uint type, ObjectGuid data)
		{
			if (type == DataTypes.ImageOfMedivh)
				ImageGUID = data;
		}

		public override void OnGameObjectCreate(GameObject go)
		{
			switch (go.Entry)
			{
				case GameObjectIds.StageCurtain:
					CurtainGUID = go.GUID;

					break;
				case GameObjectIds.StageDoorLeft:
					StageDoorLeftGUID = go.GUID;

					if (GetBossState(DataTypes.OperaPerformance) == EncounterState.Done)
						go.SetGoState(GameObjectState.Active);

					break;
				case GameObjectIds.StageDoorRight:
					StageDoorRightGUID = go.GUID;

					if (GetBossState(DataTypes.OperaPerformance) == EncounterState.Done)
						go.SetGoState(GameObjectState.Active);

					break;
				case GameObjectIds.PrivateLibraryDoor:
					LibraryDoor = go.GUID;

					break;
				case GameObjectIds.MassiveDoor:
					MassiveDoor = go.GUID;

					break;
				case GameObjectIds.GamesmanHallDoor:
					GamesmansDoor = go.GUID;

					break;
				case GameObjectIds.GamesmanHallExitDoor:
					GamesmansExitDoor = go.GUID;

					break;
				case GameObjectIds.NetherspaceDoor:
					NetherspaceDoor = go.GUID;

					break;
				case GameObjectIds.MastersTerraceDoor:
					MastersTerraceDoor[0] = go.GUID;

					break;
				case GameObjectIds.MastersTerraceDoor2:
					MastersTerraceDoor[1] = go.GUID;

					break;
				case GameObjectIds.SideEntranceDoor:
					SideEntranceDoor = go.GUID;

					if (GetBossState(DataTypes.OperaPerformance) == EncounterState.Done)
						go.SetFlag(GameObjectFlags.Locked);
					else
						go.RemoveFlag(GameObjectFlags.Locked);

					break;
				case GameObjectIds.DustCoveredChest:
					DustCoveredChest = go.GUID;

					break;
				case GameObjectIds.BlackenedUrn:
					BlackenedUrnGUID = go.GUID;

					break;
			}

			switch (OperaEvent)
			{
				/// @todo Set Object visibilities for Opera based on performance
				case 1:
					break;

				case 2:
					break;

				case 3:
					break;
			}
		}

		public override uint GetData(uint type)
		{
			switch (type)
			{
				case DataTypes.OperaPerformance:
					return OperaEvent;
				case DataTypes.OperaOzDeathcount:
					return OzDeathCount;
			}

			return 0;
		}

		public override ObjectGuid GetGuidData(uint type)
		{
			switch (type)
			{
				case DataTypes.Kilrek:
					return KilrekGUID;
				case DataTypes.Terestian:
					return TerestianGUID;
				case DataTypes.Moroes:
					return MoroesGUID;
				case DataTypes.Nightbane:
					return NightbaneGUID;
				case DataTypes.GoStagedoorleft:
					return StageDoorLeftGUID;
				case DataTypes.GoStagedoorright:
					return StageDoorRightGUID;
				case DataTypes.GoCurtains:
					return CurtainGUID;
				case DataTypes.GoLibraryDoor:
					return LibraryDoor;
				case DataTypes.GoMassiveDoor:
					return MassiveDoor;
				case DataTypes.GoSideEntranceDoor:
					return SideEntranceDoor;
				case DataTypes.GoGameDoor:
					return GamesmansDoor;
				case DataTypes.GoGameExitDoor:
					return GamesmansExitDoor;
				case DataTypes.GoNetherDoor:
					return NetherspaceDoor;
				case DataTypes.MastersTerraceDoor1:
					return MastersTerraceDoor[0];
				case DataTypes.MastersTerraceDoor2:
					return MastersTerraceDoor[1];
				case DataTypes.ImageOfMedivh:
					return ImageGUID;
				case DataTypes.GoBlackenedUrn:
					return BlackenedUrnGUID;
			}

			return ObjectGuid.Empty;
		}
	}
}