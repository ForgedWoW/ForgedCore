﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Forged.MapServer.Server;
using Forged.MapServer.Services;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.Networking;

public class PacketManager
{
    private readonly IContainer _container;
    private readonly ConcurrentDictionary<ClientOpcodes, PacketHandler> _clientPacketTable = new();

    public PacketManager(IContainer container)
    {
        _container = container;
    }

    public bool ContainsHandler(ClientOpcodes opcode)
    {
        return _clientPacketTable.ContainsKey(opcode);
    }

    public PacketHandler GetHandler(ClientOpcodes opcode)
    {
        return _clientPacketTable.LookupByKey(opcode);
    }

    public void Initialize(WorldSession session)
    {
        var impl = _container.Resolve<IEnumerable<IWorldSocketHandler>>(new PositionalParameter(0, session));

        foreach (var worldSocketHandler in impl)
        {
            foreach (var methodInfo in worldSocketHandler.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                foreach (var msgAttr in methodInfo.GetCustomAttributes<WorldPacketHandlerAttribute>())
                {
                    if (msgAttr.Opcode == ClientOpcodes.Unknown)
                    {
                        Log.Logger.Error("Opcode {0} does not have a value", msgAttr.Opcode);

                        continue;
                    }

                    if (_clientPacketTable.ContainsKey(msgAttr.Opcode))
                    {
                        Log.Logger.Error("Tried to override OpcodeHandler of {0} with {1} (Opcode {2})", _clientPacketTable[msgAttr.Opcode].ToString(), methodInfo.Name, msgAttr.Opcode);

                        continue;
                    }

                    var parameters = methodInfo.GetParameters();

                    if (parameters.Length == 0)
                    {
                        Log.Logger.Error("Method: {0} Has no paramters", methodInfo.Name);

                        continue;
                    }

                    if (parameters[0].ParameterType.BaseType != typeof(ClientPacket))
                    {
                        Log.Logger.Error("Method: {0} has wrong BaseType", methodInfo.Name);

                        continue;
                    }

                    _clientPacketTable[msgAttr.Opcode] = new PacketHandler(worldSocketHandler, session, methodInfo, msgAttr.Status, msgAttr.Processing, parameters[0].ParameterType);
                }
            }
        }
    }
}