﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Net.Sockets;
using Framework.Networking;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Networking;

public class WorldSocketManager : SocketManager<WorldSocket>
{
    private readonly IConfiguration _configuration;
    private AsyncAcceptor _instanceAcceptor;
    private int _socketSendBufferSize;
    private bool _tcpNoDelay;

    public WorldSocketManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void OnSocketOpen(Socket sock)
    {
        // set some options here
        try
        {
            if (_socketSendBufferSize >= 0)
                sock.SendBufferSize = _socketSendBufferSize;

            // Set TCP_NODELAY.
            sock.NoDelay = _tcpNoDelay;
        }
        catch (SocketException ex)
        {
            Log.Logger.Error(ex);

            return;
        }

        base.OnSocketOpen(sock);
    }

    public override bool StartNetwork(string bindIp, int port, int threadCount = 1)
    {
        _tcpNoDelay = _configuration.GetDefaultValue("Network:TcpNodelay", true);

        Log.Logger.Debug("Max allowed socket connections {0}", ushort.MaxValue);

        // -1 means use default
        _socketSendBufferSize = _configuration.GetDefaultValue("Network:OutKBuff", -1);

        if (!base.StartNetwork(bindIp, port, threadCount))
            return false;

        _instanceAcceptor = new AsyncAcceptor();

        if (!_instanceAcceptor.Start(bindIp, _configuration.GetDefaultValue("InstanceServerPort", 8086)))
        {
            Log.Logger.Error("StartNetwork failed to start instance AsyncAcceptor");

            return false;
        }

        _instanceAcceptor.AsyncAcceptSocket(OnSocketOpen);

        return true;
    }

    public override void StopNetwork()
    {
        _instanceAcceptor.Close();
        base.StopNetwork();

        _instanceAcceptor = null;
    }
}