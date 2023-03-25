﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Battlepay;

public class Purchase
{
	public ObjectGuid TargetCharacter = new();
	public ulong DistributionId;
	public ulong PurchaseID;
	public ulong CurrentPrice;
	public uint ClientToken;
	public uint ServerToken;
	public uint ProductID;
	public ushort Status;
	public bool Lock;
}