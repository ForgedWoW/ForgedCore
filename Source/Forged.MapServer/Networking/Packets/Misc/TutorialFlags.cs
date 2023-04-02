﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class TutorialFlags : ServerPacket
{
    public uint[] TutorialData = new uint[SharedConst.MaxAccountTutorialValues];
    public TutorialFlags() : base(ServerOpcodes.TutorialFlags) { }

    public override void Write()
    {
        for (byte i = 0; i < (int)Tutorials.Max; ++i)
            WorldPacket.WriteUInt32(TutorialData[i]);
    }
}