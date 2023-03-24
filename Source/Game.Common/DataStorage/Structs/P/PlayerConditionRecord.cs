﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.P;

public sealed class PlayerConditionRecord
{
	public uint Id;
	public long RaceMask;
	public string FailureDescription;
	public int ClassMask;
	public uint SkillLogic;
	public int LanguageID;
	public byte MinLanguage;
	public int MaxLanguage;
	public ushort MaxFactionID;
	public byte MaxReputation;
	public uint ReputationLogic;
	public sbyte CurrentPvpFaction;
	public byte PvpMedal;
	public uint PrevQuestLogic;
	public uint CurrQuestLogic;
	public uint CurrentCompletedQuestLogic;
	public uint SpellLogic;
	public uint ItemLogic;
	public byte ItemFlags;
	public uint AuraSpellLogic;
	public ushort WorldStateExpressionID;
	public int WeatherID;
	public byte PartyStatus;
	public byte LifetimeMaxPVPRank;
	public uint AchievementLogic;
	public sbyte Gender;
	public sbyte NativeGender;
	public uint AreaLogic;
	public uint LfgLogic;
	public uint CurrencyLogic;
	public uint QuestKillID;
	public uint QuestKillLogic;
	public sbyte MinExpansionLevel;
	public sbyte MaxExpansionLevel;
	public int MinAvgItemLevel;
	public int MaxAvgItemLevel;
	public ushort MinAvgEquippedItemLevel;
	public ushort MaxAvgEquippedItemLevel;
	public byte PhaseUseFlags;
	public ushort PhaseID;
	public uint PhaseGroupID;
	public int Flags;
	public sbyte ChrSpecializationIndex;
	public sbyte ChrSpecializationRole;
	public uint ModifierTreeID;
	public sbyte PowerType;
	public byte PowerTypeComp;
	public byte PowerTypeValue;
	public int WeaponSubclassMask;
	public byte MaxGuildLevel;
	public byte MinGuildLevel;
	public sbyte MaxExpansionTier;
	public sbyte MinExpansionTier;
	public byte MinPVPRank;
	public byte MaxPVPRank;
	public uint ContentTuningID;
	public int CovenantID;
	public uint TraitNodeEntryLogic;
	public ushort[] SkillID = new ushort[4];
	public ushort[] MinSkill = new ushort[4];
	public ushort[] MaxSkill = new ushort[4];
	public uint[] MinFactionID = new uint[3];
	public byte[] MinReputation = new byte[3];
	public uint[] PrevQuestID = new uint[4];
	public uint[] CurrQuestID = new uint[4];
	public uint[] CurrentCompletedQuestID = new uint[4];
	public uint[] SpellID = new uint[4];
	public uint[] ItemID = new uint[4];
	public uint[] ItemCount = new uint[4];
	public ushort[] Explored = new ushort[2];
	public uint[] Time = new uint[2];
	public uint[] AuraSpellID = new uint[4];
	public byte[] AuraStacks = new byte[4];
	public ushort[] Achievement = new ushort[4];
	public ushort[] AreaID = new ushort[4];
	public byte[] LfgStatus = new byte[4];
	public byte[] LfgCompare = new byte[4];
	public uint[] LfgValue = new uint[4];
	public uint[] CurrencyID = new uint[4];
	public uint[] CurrencyCount = new uint[4];
	public uint[] QuestKillMonster = new uint[6];
	public int[] MovementFlags = new int[2];
	public int[] TraitNodeEntryID = new int[4];
	public ushort[] TraitNodeEntryMinRank = new ushort[4];
	public ushort[] TraitNodeEntryMaxRank = new ushort[4];
}