﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Configuration;
using Framework.Constants;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	public void SendAuthResponse(BattlenetRpcErrorCode code, bool queued, uint queuePos = 0)
	{
		AuthResponse response = new();
		response.Result = code;

		if (code == BattlenetRpcErrorCode.Ok)
		{
			response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
			var forceRaceAndClass = ConfigMgr.GetDefaultValue("character.EnforceRaceAndClassExpansions", true);

			response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
			response.SuccessInfo.ActiveExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)Expansion;
			response.SuccessInfo.AccountExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)AccountExpansion;
			response.SuccessInfo.VirtualRealmAddress = Global.WorldMgr.VirtualRealmAddress;
			response.SuccessInfo.Time = (uint)GameTime.GetGameTime();

			var realm = Global.WorldMgr.Realm;

			// Send current home realm. Also there is no need to send it later in realm queries.
			response.SuccessInfo.VirtualRealms.Add(new VirtualRealmInfo(realm.Id.GetAddress(), true, false, realm.Name, realm.NormalizedName));

			if (HasPermission(RBACPermissions.UseCharacterTemplates))
				foreach (var templ in Global.CharacterTemplateDataStorage.GetCharacterTemplates().Values)
					response.SuccessInfo.Templates.Add(templ);

			response.SuccessInfo.AvailableClasses = Global.ObjectMgr.GetClassExpansionRequirements();
		}

		if (queued)
		{
			AuthWaitInfo waitInfo = new();
			waitInfo.WaitCount = queuePos;
			response.WaitInfo = waitInfo;
		}

		SendPacket(response);
	}

	public void SendAuthWaitQueue(uint position)
	{
		if (position != 0)
		{
			WaitQueueUpdate waitQueueUpdate = new();
			waitQueueUpdate.WaitInfo.WaitCount = position;
			waitQueueUpdate.WaitInfo.WaitTime = 0;
			waitQueueUpdate.WaitInfo.HasFCM = false;
			SendPacket(waitQueueUpdate);
		}
		else
		{
			SendPacket(new WaitQueueFinish());
		}
	}

	public void SendClientCacheVersion(uint version)
	{
		ClientCacheVersion cache = new();
		cache.CacheVersion = version;
		SendPacket(cache); //enabled it
	}

	public void SendSetTimeZoneInformation()
	{
		// @todo: replace dummy values
		SetTimeZoneInformation packet = new();
		packet.ServerTimeTZ = "Europe/Paris";
		packet.GameTimeTZ = "Europe/Paris";
		packet.ServerRegionalTZ = "Europe/Paris";

		SendPacket(packet); //enabled it
	}

	public void SendFeatureSystemStatusGlueScreen()
	{
		FeatureSystemStatusGlueScreen features = new();
		features.BpayStoreAvailable = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
		features.BpayStoreDisabledByParentalControls = false;
		features.CharUndeleteEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled);
		features.BpayStoreEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
		features.MaxCharactersPerRealm = WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm);
		features.MinimumExpansionLevel = (int)Expansion.Classic;
		features.MaximumExpansionLevel = WorldConfig.GetIntValue(WorldCfg.Expansion);

		var europaTicketConfig = new EuropaTicketConfig();
		europaTicketConfig.ThrottleState.MaxTries = 10;
		europaTicketConfig.ThrottleState.PerMilliseconds = 60000;
		europaTicketConfig.ThrottleState.TryCount = 1;
		europaTicketConfig.ThrottleState.LastResetTimeBeforeNow = 111111;
		europaTicketConfig.TicketsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled);
		europaTicketConfig.BugsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled);
		europaTicketConfig.ComplaintsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled);
		europaTicketConfig.SuggestionsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled);

		features.EuropaTicketSystemStatus = europaTicketConfig;

		SendPacket(features);
	}
}