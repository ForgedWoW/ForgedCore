﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Spells;

namespace Game.Networking.Packets;

public class AuraDataInfo
{
	public ObjectGuid CastID;
	public int SpellID;
	public SpellCastVisual Visual;
	public AuraFlags Flags;
	public uint ActiveFlags;
	public ushort CastLevel = 1;
	public byte Applications = 1;
	public int ContentTuningID;
	public ObjectGuid? CastUnit;
	public int? Duration;
	public int? Remaining;
	public List<double> Points = new();
	public List<double> EstimatedPoints = new();
	readonly ContentTuningParams ContentTuning;
	readonly float? TimeMod;

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(CastID);
		data.WriteInt32(SpellID);

		Visual.Write(data);

		data.WriteUInt16((ushort)Flags);
		data.WriteUInt32(ActiveFlags);
		data.WriteUInt16(CastLevel);
		data.WriteUInt8(Applications);
		data.WriteInt32(ContentTuningID);
		data.WriteBit(CastUnit.HasValue);
		data.WriteBit(Duration.HasValue);
		data.WriteBit(Remaining.HasValue);
		data.WriteBit(TimeMod.HasValue);
		data.WriteBits(Points.Count, 6);
		data.WriteBits(EstimatedPoints.Count, 6);
		data.WriteBit(ContentTuning != null);

		if (ContentTuning != null)
			ContentTuning.Write(data);

		if (CastUnit.HasValue)
			data.WritePackedGuid(CastUnit.Value);

		if (Duration.HasValue)
			data.WriteInt32(Duration.Value);

		if (Remaining.HasValue)
			data.WriteInt32(Remaining.Value);

		if (TimeMod.HasValue)
			data.WriteFloat(TimeMod.Value);

		foreach (var point in Points)
			data.WriteFloat((float)point);

		foreach (var point in EstimatedPoints)
			data.WriteFloat((float)point);
	}
}