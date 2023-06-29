﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Scene;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class SceneHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public SceneHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.ScenePlaybackCanceled)]
    private void HandleScenePlaybackCanceled(ScenePlaybackCanceled scenePlaybackCanceled)
    {
        Log.Logger.Debug("HandleScenePlaybackCanceled: SceneInstanceID: {0}", scenePlaybackCanceled.SceneInstanceID);

        _session.Player.SceneMgr.OnSceneCancel(scenePlaybackCanceled.SceneInstanceID);
    }

    [WorldPacketHandler(ClientOpcodes.ScenePlaybackComplete)]
    private void HandleScenePlaybackComplete(ScenePlaybackComplete scenePlaybackComplete)
    {
        Log.Logger.Debug("HandleScenePlaybackComplete: SceneInstanceID: {0}", scenePlaybackComplete.SceneInstanceID);

        _session.Player.SceneMgr.OnSceneComplete(scenePlaybackComplete.SceneInstanceID);
    }

    [WorldPacketHandler(ClientOpcodes.SceneTriggerEvent)]
    private void HandleSceneTriggerEvent(SceneTriggerEvent sceneTriggerEvent)
    {
        Log.Logger.Debug("HandleSceneTriggerEvent: SceneInstanceID: {0} Event: {1}", sceneTriggerEvent.SceneInstanceID, sceneTriggerEvent._Event);

        _session.Player.SceneMgr.OnSceneTrigger(sceneTriggerEvent.SceneInstanceID, sceneTriggerEvent._Event);
    }
}