﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Server;

namespace Game.Common.Networking;

public abstract class ServerPacket
{
	protected WorldPacket _worldPacket;

	byte[] buffer;

	protected ServerPacket(ServerOpcodes opcode)
	{
		_worldPacket = new WorldPacket(opcode);
	}

    public void Clear()
	{
		_worldPacket.Clear();
		buffer = null;
	}

	public ServerOpcodes GetOpcode()
	{
		return (ServerOpcodes)_worldPacket.GetOpcode();
	}

	public byte[] GetData()
	{
		return buffer;
	}

	public void LogPacket(WorldSession session)
	{
		Log.outDebug(LogFilter.Network, "Sent ServerOpcode: {0} To: {1}", GetOpcode(), session != null ? session.GetPlayerInfo() : "");
	}

	public abstract void Write();

	public void WritePacketData()
	{
		if (buffer != null)
			return;

		Write();

		buffer = _worldPacket.GetData();
		_worldPacket.Dispose();
	}
}
