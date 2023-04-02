﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Petition;

public class PetitionRenameGuildResponse : ServerPacket
{
    public string NewGuildName;
    public ObjectGuid PetitionGuid;
    public PetitionRenameGuildResponse() : base(ServerOpcodes.PetitionRenameGuildResponse) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(PetitionGuid);

        WorldPacket.WriteBits(NewGuildName.GetByteCount(), 7);
        WorldPacket.FlushBits();

        WorldPacket.WriteString(NewGuildName);
    }
}