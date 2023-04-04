﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Forged.MapServer.Networking.Packets.Battlenet;
using Forged.MapServer.Server;
using Framework.Constants;
using Google.Protobuf;
using Serilog;

namespace Forged.MapServer.Services;

public class WorldServiceHandler
{
    private readonly Delegate _methodCaller;
    private readonly Type _requestType;
    private readonly Type _responseType;

    public WorldServiceHandler(MethodInfo info, ParameterInfo[] parameters)
    {
        _requestType = parameters[0].ParameterType;

        if (parameters.Length > 1)
            _responseType = parameters[1].ParameterType;

        if (_responseType != null)
            _methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[]
            {
                typeof(WorldSession), _requestType, _responseType, info.ReturnType
            }));
        else
            _methodCaller = info.CreateDelegate(Expression.GetDelegateType(new[]
            {
                typeof(WorldSession), _requestType, info.ReturnType
            }));
    }

    public void Invoke(WorldSession session, MethodCall methodCall, CodedInputStream stream)
    {
        var request = (IMessage)Activator.CreateInstance(_requestType);
        request?.MergeFrom(stream);

        BattlenetRpcErrorCode status;

        if (_responseType != null)
        {
            var response = (IMessage)Activator.CreateInstance(_responseType);
            status = (BattlenetRpcErrorCode)_methodCaller.DynamicInvoke(session, request, response);
            Log.Logger.Debug("{0} Client called server Method: {1}) Returned: {2} Status: {3}.", session.RemoteAddress, request, response, status);

            if (status == 0)
                SendBattlenetResponseMessage(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, response);
            else
                SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, status);
        }
        else
        {
            status = (BattlenetRpcErrorCode)_methodCaller.DynamicInvoke(session, request);
            Log.Logger.Debug("{0} Client called server Method: {1}) Status: {2}.", session.RemoteAddress, request, status);

            if (status != 0)
                SendBattlenetResponse(methodCall.GetServiceHash(), methodCall.GetMethodId(), methodCall.Token, status);
        }

        void SendBattlenetResponseMessage(uint serviceHash, uint methodId, uint token, IMessage response)
        {
            Response bnetResponse = new()
            {
                BnetStatus = BattlenetRpcErrorCode.Ok
            };

            bnetResponse.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
            bnetResponse.Method.ObjectId = 1;
            bnetResponse.Method.Token = token;

            if (response.CalculateSize() != 0)
                bnetResponse.Data.WriteBytes(response.ToByteArray());

            session.SendPacket(bnetResponse);
        }

        void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, BattlenetRpcErrorCode rpcStatus)
        {
            Response bnetResponse = new()
            {
                BnetStatus = rpcStatus
            };

            bnetResponse.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
            bnetResponse.Method.ObjectId = 1;
            bnetResponse.Method.Token = token;

            session.SendPacket(bnetResponse);
        }
    }
}