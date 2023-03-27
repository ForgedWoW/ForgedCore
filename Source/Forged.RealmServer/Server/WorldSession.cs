﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Forged.RealmServer.Accounts;
using Forged.RealmServer.BattlePets;
using Forged.RealmServer.Chat;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Battlepay;
using Forged.RealmServer.Networking;
using Serilog;
using Microsoft.Extensions.Configuration;
using Framework.Util;
using Forged.RealmServer.Networking.Packets;
using Bgs.Protocol.GameUtilities.V1;
using Framework.Serialization;
using Framework.Web;
using Google.Protobuf;
using Forged.RealmServer.Services;
using Forged.RealmServer.Globals;
using Forged.RealmServer.DataStorage;
using static Forged.RealmServer.Networking.Packets.ConnectTo;

namespace Forged.RealmServer;

public class WorldSession : IDisposable
{
	public long MuteTime;

	readonly List<ObjectGuid> _legitCharacters = new();
	readonly WorldSocket[] _socket = new WorldSocket[(int)ConnectionType.Max];
	readonly string _address;
	readonly uint _accountId;
	readonly string _accountName;
	readonly uint _battlenetAccountId;
	readonly Expansion _accountExpansion;
	readonly Expansion _expansion;
	readonly Expansion _configuredExpansion;
	readonly string _os;

	readonly DosProtection _antiDos;
	readonly Locale _sessionDbcLocale;
	readonly Locale _sessionDbLocaleIndex;
	readonly AccountData[] _accountData = new AccountData[(int)AccountDataTypes.Max];
	readonly uint[] _tutorials = new uint[SharedConst.MaxAccountTutorialValues];
	readonly Dictionary<uint /*realmAddress*/, byte> _realmCharacterCounts = new();

	readonly uint _recruiterId;
	readonly bool _isRecruiter;
    private readonly GameTime _gameTime;
    private readonly WorldConfig _worldConfig;
    private readonly IConfiguration _configuration;
    private readonly LoginDatabase _loginDatabase;
    private readonly CharacterDatabase _characterDatabase;
    private readonly RealmManager _realmManager;
    private readonly WorldManager _worldManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _gameObjectManager;
    private readonly AccountManager _accountManager;
    private readonly DB2Manager _dB2Manager;
    private readonly CharacterTemplateDataStorage _characterTemplateDataStorage;
    private readonly ActionBlock<WorldPacket> _recvQueue;

	readonly ConcurrentQueue<WorldPacket> _threadUnsafe = new();
	readonly ConcurrentQueue<WorldPacket> _inPlaceQueue = new();
	readonly ConcurrentQueue<WorldPacket> _threadSafeQueue = new();

	readonly CircularBuffer<Tuple<long, uint>> _timeSyncClockDeltaQueue = new(6); // first member: clockDelta. Second member: latency of the packet exchange that was used to compute that clockDelta.

	readonly Dictionary<uint, uint> _pendingTimeSyncRequests = new(); // key: counter. value: server time when packet with that counter was sent.

	readonly CollectionMgr _collectionMgr;

	readonly BattlePetMgr _battlePetMgr;
	private readonly BattlepayManager _battlePayMgr;

	readonly AsyncCallbackProcessor<QueryCallback> _queryProcessor = new();
	readonly AsyncCallbackProcessor<TransactionCallback> _transactionCallbacks = new();
	readonly AsyncCallbackProcessor<ISqlCallback> _queryHolderProcessor = new();

	readonly CancellationTokenSource _cancellationToken = new();
	readonly AutoResetEvent _asyncMessageQueueSemaphore = new(false);
	ulong _guidLow;
	Player _player;

	AccountTypes _security;

	uint _expireTime;
	bool _forceExit;
	Warden _warden; // Remains NULL if Warden system is not enabled by config

	long _logoutTime;
	bool _inQueue;
	ObjectGuid _playerLoading; // code processed in LoginPlayer
	bool _playerLogout;        // code processed in LogoutPlayer
	bool _playerRecentlyLogout;
	bool _playerSave;
	uint _latency;
	TutorialsFlag _tutorialsChanged;

	Array<byte> _realmListSecret = new(32);
	long _timeOutTime;

	RBACData _rbacData;
	long _timeSyncClockDelta;
	uint _timeSyncNextCounter;
	uint _timeSyncTimer;

	ConnectToKey _instanceConnectKey;

	// Packets cooldown
	long _calendarEventCreationCooldown;

    public bool CanSpeak => MuteTime <= _gameTime.GetGameTime;

	public string PlayerName => _player != null ? _player.GetName() : "Unknown";

	public ObjectGuid PlayerLoading
	{
		get { return _playerLoading; }
		set { _playerLoading = value; }
	}

	public HashSet<ObjectGuid> LegitCharacters { get; set; } = new();

	public bool PlayerLogout => _playerLogout;
	public bool PlayerLogoutWithSave => _playerLogout && _playerSave;
	public bool PlayerRecentlyLoggedOut => _playerRecentlyLogout;

	public bool PlayerDisconnected => !(_socket[(int)ConnectionType.Realm] != null &&
										_socket[(int)ConnectionType.Realm].IsOpen() &&
										_socket[(int)ConnectionType.Instance] != null &&
										_socket[(int)ConnectionType.Instance].IsOpen());

	public AccountTypes Security
	{
		get => _security;
		private set => _security = value;
	}

	public uint AccountId => _accountId;
	public ObjectGuid AccountGUID => ObjectGuid.Create(HighGuid.WowAccount, AccountId);
	public string AccountName => _accountName;
	public uint BattlenetAccountId => _battlenetAccountId;
	public ObjectGuid BattlenetAccountGUID => ObjectGuid.Create(HighGuid.BNetAccount, BattlenetAccountId);

	public Player Player
	{
		get => _player;
		set
		{
			_player = value;

			if (_player)
				_guidLow = _player.GUID.Counter;
		}
	}

	public string RemoteAddress => _address;
	public Expansion AccountExpansion => _accountExpansion;
	public Expansion Expansion => _expansion;
	public string OS => _os;

	public bool IsLogingOut => _logoutTime != 0 || _playerLogout;
	public ulong ConnectToInstanceKey => _instanceConnectKey.Raw;
	public AsyncCallbackProcessor<QueryCallback> QueryProcessor => _queryProcessor;

	public RBACData RBACData => _rbacData;

	public Locale SessionDbcLocale => _sessionDbcLocale;
	public Locale SessionDbLocaleIndex => _sessionDbLocaleIndex;

	public uint Latency
	{
		get => _latency;
		set => _latency = value;
	}

	bool IsConnectionIdle => _timeOutTime < _gameTime.GetGameTime && !_inQueue;

	public uint RecruiterId => _recruiterId;

	public bool IsARecruiter => _isRecruiter;

	// Packets cooldown
	public long CalendarEventCreationCooldown
	{
		get => _calendarEventCreationCooldown;
		set => _calendarEventCreationCooldown = value;
	}

	// Battle Pets
	public BattlePetMgr BattlePetMgr => _battlePetMgr;

	public CollectionMgr CollectionMgr => _collectionMgr;

	// Battlenet
	public Array<byte> RealmListSecret
	{
		get => _realmListSecret;
		set => _realmListSecret = value;
	}

	public Dictionary<uint, byte> RealmCharacterCounts => _realmCharacterCounts;

	public CommandHandler CommandHandler { get; private set; }

	public BattlepayManager BattlePayMgr => _battlePayMgr;

    public List<string> RegisteredAddonPrefixes { get; } = new();
	public bool FilterAddonMessages { get; set; } = false;

    public WorldSession(uint id, string name, uint battlenetAccountId, WorldSocket sock, AccountTypes sec, Expansion expansion, 
		long mute_time, string os, Locale locale, uint recruiter, bool isARecruiter, GameTime gameTime, WorldConfig worldConfig,
		IConfiguration configuration, LoginDatabase loginDatabase, CharacterDatabase characterDatabase, RealmManager realmManager,
		WorldManager worldManager, ObjectAccessor objectAccessor, GameObjectManager gameObjectManager, AccountManager accountManager,
		DB2Manager dB2Manager, CharacterTemplateDataStorage characterTemplateDataStorage)
	{
		MuteTime = mute_time;
		_antiDos = new DosProtection(this);
		_socket[(int)ConnectionType.Realm] = sock;
		_security = sec;
		_accountId = id;
		_accountName = name;
		_battlenetAccountId = battlenetAccountId;
        _gameTime = gameTime;
        _worldConfig = worldConfig;
        _configuration = configuration;
        _loginDatabase = loginDatabase;
        _characterDatabase = characterDatabase;
        _realmManager = realmManager;
        _worldManager = worldManager;
        _objectAccessor = objectAccessor;
        _gameObjectManager = gameObjectManager;
        _accountManager = accountManager;
        _dB2Manager = dB2Manager;
        _characterTemplateDataStorage = characterTemplateDataStorage;

        _configuredExpansion = _configuration.GetDefaultValue<int>("Player.OverrideExpansion", -1) == -1 ? Expansion.LevelCurrent : (Expansion)_configuration.GetDefaultValue<int>("Player.OverrideExpansion", -1);
		_accountExpansion = Expansion.LevelCurrent == _configuredExpansion ? expansion : _configuredExpansion;
		_expansion = (Expansion)Math.Min((byte)expansion, _worldConfig.GetIntValue(WorldCfg.Expansion));
		_os = os;
		_sessionDbcLocale = _worldManager.GetAvailableDbcLocale(locale);
		_sessionDbLocaleIndex = locale;
		_recruiterId = recruiter;
		_isRecruiter = isARecruiter;

        _expireTime = 60000; // 1 min after socket loss, session is deleted
		_battlePetMgr = new BattlePetMgr(this);
		_collectionMgr = new CollectionMgr(this);
		_battlePayMgr = new BattlepayManager(this);
		CommandHandler = new CommandHandler(this);

        _recvQueue = new(ProcessQueue, new ExecutionDataflowBlockOptions()
        {
            MaxDegreeOfParallelism = 10,
            EnsureOrdered = true,
			CancellationToken = _cancellationToken.Token
        });

        Task.Run(ProcessInPlace, _cancellationToken.Token);

		_address = sock.GetRemoteIpAddress().Address.ToString();
		ResetTimeOutTime(false);
		_loginDatabase.Execute("UPDATE account SET online = 1 WHERE id = {0};", AccountId); // One-time query

	}

	public void Dispose()
	{
		_cancellationToken.Cancel();

		// unload player if not unloaded
		if (_player)
			LogoutPlayer(true);

		// - If have unclosed socket, close it
		for (byte i = 0; i < 2; ++i)
			if (_socket[i] != null)
			{
				_socket[i].CloseSocket();
				_socket[i] = null;
			}

		// empty incoming packet queue
		_recvQueue.Complete();

		_loginDatabase.Execute("UPDATE account SET online = 0 WHERE id = {0};", AccountId); // One-time query
	}

	public void LogoutPlayer(bool save)
	{
        // Send to map server
    }
	
    public bool UpdateMap(uint diff)
	{
		DrainQueue(_threadSafeQueue);

		// Send time sync packet every 10s.
		if (_timeSyncTimer > 0)
		{
			if (diff >= _timeSyncTimer)
				SendTimeSync();
			else
				_timeSyncTimer -= diff;
		}

		ProcessQueryCallbacks();

		return true;
	}

	public bool UpdateWorld(uint diff)
	{
		var currentTime = DrainQueue(_threadUnsafe);

		ProcessQueryCallbacks();


		if (_socket[(int)ConnectionType.Realm] != null && _socket[(int)ConnectionType.Realm].IsOpen() && _warden != null)
			_warden.Update(diff);

		// If necessary, log the player out
		if (ShouldLogOut(currentTime) && _playerLoading.IsEmpty)
			LogoutPlayer(true);

		//- Cleanup socket if need
		if ((_socket[(int)ConnectionType.Realm] != null && !_socket[(int)ConnectionType.Realm].IsOpen()) ||
			(_socket[(int)ConnectionType.Instance] != null && !_socket[(int)ConnectionType.Instance].IsOpen()))
		{
			if (Player != null && _warden != null)
				_warden.Update(diff);

			_expireTime -= _expireTime > diff ? diff : _expireTime;

			if (_expireTime < diff || _forceExit || !Player)
			{
				if (_socket[(int)ConnectionType.Realm] != null)
				{
					_socket[(int)ConnectionType.Realm].CloseSocket();
					_socket[(int)ConnectionType.Realm] = null;
				}

				if (_socket[(int)ConnectionType.Instance] != null)
				{
					_socket[(int)ConnectionType.Instance].CloseSocket();
					_socket[(int)ConnectionType.Instance] = null;
				}
			}
		}

		if (_socket[(int)ConnectionType.Realm] == null)
			return false; //Will remove this session from the world session map


		return true;
	}

	public void QueuePacket(WorldPacket packet)
	{
		_recvQueue.Post(packet);
	}

	public void SendPacket(ServerPacket packet)
	{
		if (packet == null)
			return;

		if (packet.GetOpcode() == ServerOpcodes.Unknown || packet.GetOpcode() == ServerOpcodes.Max)
		{
			Log.Logger.Error("Prevented sending of UnknownOpcode to {0}", GetPlayerInfo());

			return;
		}

		var conIdx = packet.GetConnection();

		if (conIdx != ConnectionType.Instance && PacketManager.IsInstanceOnlyOpcode(packet.GetOpcode()))
		{
			Log.Logger.Error("Prevented sending of instance only opcode {0} with connection type {1} to {2}", packet.GetOpcode(), packet.GetConnection(), GetPlayerInfo());

			return;
		}

		if (_socket[(int)conIdx] == null)
		{
			Log.Logger.Verbose("Prevented sending of {0} to non existent socket {1} to {2}", packet.GetOpcode(), conIdx, GetPlayerInfo());

			return;
		}

		_socket[(int)conIdx].SendPacket(packet);
	}

	public void AddInstanceConnection(WorldSocket sock)
	{
		_socket[(int)ConnectionType.Instance] = sock;
	}

	public void KickPlayer(string reason)
	{
		Log.Logger.Information($"Account: {AccountId} Character: '{(_player ? _player.GetName() : "<none>")}' {(_player ? _player.GUID : "")} kicked with reason: {reason}");

		for (byte i = 0; i < 2; ++i)
			if (_socket[i] != null)
			{
				_socket[i].CloseSocket();
				_forceExit = true;
			}
	}

	public bool IsAddonRegistered(string prefix)
	{
		if (!FilterAddonMessages) // if we have hit the softcap (64) nothing should be filtered
			return true;

		if (RegisteredAddonPrefixes.Empty())
			return false;

		return RegisteredAddonPrefixes.Contains(prefix);
	}

	public void SendAccountDataTimes(ObjectGuid playerGuid, AccountDataTypes mask)
	{
		AccountDataTimes accountDataTimes = new();
		accountDataTimes.PlayerGuid = playerGuid;
		accountDataTimes.ServerTime = _gameTime.GetGameTime;

		for (var i = 0; i < (int)AccountDataTypes.Max; ++i)
			if (((int)mask & (1 << i)) != 0)
				accountDataTimes.AccountTimes[i] = GetAccountData((AccountDataTypes)i).Time;

		SendPacket(accountDataTimes);
	}

	public void LoadTutorialsData(SQLResult result)
	{
		if (!result.IsEmpty())
		{
			for (var i = 0; i < SharedConst.MaxAccountTutorialValues; i++)
				_tutorials[i] = result.Read<uint>(i);

			_tutorialsChanged |= TutorialsFlag.LoadedFromDB;
		}

		_tutorialsChanged &= ~TutorialsFlag.Changed;
	}

	public void SaveTutorialsData(SQLTransaction trans)
	{
		if (!_tutorialsChanged.HasAnyFlag(TutorialsFlag.Changed))
			return;

		var hasTutorialsInDB = _tutorialsChanged.HasAnyFlag(TutorialsFlag.LoadedFromDB);
		var stmt = _characterDatabase.GetPreparedStatement(hasTutorialsInDB ? CharStatements.UPD_TUTORIALS : CharStatements.INS_TUTORIALS);

		for (var i = 0; i < SharedConst.MaxAccountTutorialValues; ++i)
			stmt.AddValue(i, _tutorials[i]);

		stmt.AddValue(SharedConst.MaxAccountTutorialValues, AccountId);
		trans.Append(stmt);

		// now has, set flag so next save uses update query
		if (!hasTutorialsInDB)
			_tutorialsChanged |= TutorialsFlag.LoadedFromDB;

		_tutorialsChanged &= ~TutorialsFlag.Changed;
	}

	public void SendConnectToInstance(ConnectToSerial serial)
	{
		var instanceAddress = _worldManager.Realm.GetAddressForClient(System.Net.IPAddress.Parse(RemoteAddress));

		_instanceConnectKey.AccountId = AccountId;
		_instanceConnectKey.connectionType = ConnectionType.Instance;
		_instanceConnectKey.Key = RandomHelper.URand(0, 0x7FFFFFFF);

		ConnectTo connectTo = new();
		connectTo.Key = _instanceConnectKey.Raw;
		connectTo.Serial = serial;
		connectTo.Payload.Port = (ushort)_worldConfig.GetIntValue(WorldCfg.PortInstance);
		connectTo.Con = (byte)ConnectionType.Instance;

		if (instanceAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
		{
			connectTo.Payload.Where.IPv4 = instanceAddress.Address.GetAddressBytes();
			connectTo.Payload.Where.Type = AddressType.IPv4;
		}
		else
		{
			connectTo.Payload.Where.IPv6 = instanceAddress.Address.GetAddressBytes();
			connectTo.Payload.Where.Type = AddressType.IPv6;
		}

		SendPacket(connectTo);
	}

	public void SendTutorialsData()
	{
		TutorialFlags packet = new();
		Array.Copy(_tutorials, packet.TutorialData, SharedConst.MaxAccountTutorialValues);
		SendPacket(packet);
	}

	public bool DisallowHyperlinksAndMaybeKick(string str)
	{
		if (!str.Contains('|'))
			return true;

		Log.Logger.Error($"Player {Player.GetName()} ({Player.GUID}) sent a message which illegally contained a hyperlink:\n{str}");

		if (_worldConfig.GetIntValue(WorldCfg.ChatStrictLinkCheckingKick) != 0)
			KickPlayer("WorldSession::DisallowHyperlinksAndMaybeKick Illegal chat link");

		return false;
	}

	public void SendNotification(CypherStrings str, params object[] args)
	{
		SendNotification(_gameObjectManager.GetCypherString(str), args);
	}

	public void SendNotification(string str, params object[] args)
	{
		var message = string.Format(str, args);

		if (!string.IsNullOrEmpty(message))
			SendPacket(new PrintNotification(message));
	}

	public string GetPlayerInfo()
	{
		StringBuilder ss = new();
		ss.Append("[Player: ");

		if (!_playerLoading.IsEmpty)
			ss.AppendFormat("Logging in: {0}, ", _playerLoading.ToString());
		else if (_player)
			ss.AppendFormat("{0} {1}, ", _player.GetName(), _player.GUID.ToString());

		ss.AppendFormat("Account: {0}]", AccountId);

		return ss.ToString();
	}

	public void SetInQueue(bool state)
	{
		_inQueue = state;
	}

	public SQLQueryHolderCallback<R> AddQueryHolderCallback<R>(SQLQueryHolderCallback<R> callback)
	{
		return (SQLQueryHolderCallback<R>)_queryHolderProcessor.AddCallback(callback);
	}

	public bool CanAccessAlliedRaces()
	{
		if (_configuration.GetDefaultValue("CharacterCreating.DisableAlliedRaceAchievementRequirement", false))
			return true;
		else
			return AccountExpansion >= Expansion.BattleForAzeroth;
	}

	public void LoadPermissions()
	{
		var id = AccountId;
		var secLevel = Security;

		Log.Logger.Debug(
					"WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
					id,
					_accountName,
					_worldManager.Realm.Id.Index,
					secLevel);

		_rbacData = new RBACData(id, _accountName, (int)_worldManager.Realm.Id.Index, _accountManager, _loginDatabase, (byte)secLevel);
		_rbacData.LoadFromDB();
	}

	public QueryCallback LoadPermissionsAsync()
	{
		var id = AccountId;
		var secLevel = Security;

		Log.Logger.Debug(
					"WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
					id,
					_accountName,
					_worldManager.Realm.Id.Index,
					secLevel);

		_rbacData = new RBACData(id, _accountName, (int)_worldManager.Realm.Id.Index, _accountManager, _loginDatabase, (byte)secLevel);

		return _rbacData.LoadFromDBAsync();
	}

	public void InitializeSession()
	{
		AccountInfoQueryHolderPerRealm realmHolder = new();
		realmHolder.Initialize(AccountId, BattlenetAccountId);

		AccountInfoQueryHolder holder = new();
		holder.Initialize(AccountId, BattlenetAccountId);

		AccountInfoQueryHolderPerRealm characterHolder = null;
		AccountInfoQueryHolder loginHolder = null;

		AddQueryHolderCallback(_characterDatabase.DelayQueryHolder(realmHolder))
			.AfterComplete(result =>
			{
				characterHolder = (AccountInfoQueryHolderPerRealm)result;

				if (loginHolder != null && characterHolder != null)
					InitializeSessionCallback(loginHolder, characterHolder);
			});

		AddQueryHolderCallback(_loginDatabase.DelayQueryHolder(holder))
			.AfterComplete(result =>
			{
				loginHolder = (AccountInfoQueryHolder)result;

				if (loginHolder != null && characterHolder != null)
					InitializeSessionCallback(loginHolder, characterHolder);
			});
	}

	public bool HasPermission(RBACPermissions permission)
	{
		if (_rbacData == null)
			LoadPermissions();

		var hasPermission = _rbacData.HasPermission(permission);

		Log.Logger.Debug(
					"WorldSession:HasPermission [AccountId: {0}, Name: {1}, realmId: {2}]",
					_rbacData.Id,
					_rbacData.Name,
                    _worldManager.Realm.Id.Index);

		return hasPermission;
	}

	public void InvalidateRBACData()
	{
		Log.Logger.Debug(
					"WorldSession:Invalidaterbac:RBACData [AccountId: {0}, Name: {1}, realmId: {2}]",
					_rbacData.Id,
					_rbacData.Name,
					_worldManager.Realm.Id.Index);

		_rbacData = null;
	}

	public void ResetTimeSync()
	{
		_timeSyncNextCounter = 0;
		_pendingTimeSyncRequests.Clear();
	}

	public void SendTimeSync()
	{
		TimeSyncRequest timeSyncRequest = new();
		timeSyncRequest.SequenceIndex = _timeSyncNextCounter;
		SendPacket(timeSyncRequest);

		_pendingTimeSyncRequests[_timeSyncNextCounter] = Time.MSTime;

		// Schedule next sync in 10 sec (except for the 2 first packets, which are spaced by only 5s)
		_timeSyncTimer = _timeSyncNextCounter == 0 ? 5000 : 10000u;
		_timeSyncNextCounter++;
	}

	public void ResetTimeOutTime(bool onlyActive)
	{
		if (Player)
			_timeOutTime = _gameTime.GetGameTime + _worldConfig.GetIntValue(WorldCfg.SocketTimeoutTimeActive);
		else if (!onlyActive)
			_timeOutTime = _gameTime.GetGameTime + _worldConfig.GetIntValue(WorldCfg.SocketTimeoutTime);
	}

	public static implicit operator bool(WorldSession session)
	{
		return session != null;
	}

	void ProcessQueue(WorldPacket packet)
	{
		var handler = PacketManager.GetHandler((ClientOpcodes)packet.GetOpcode());

		if (handler != null)
		{
			if (handler.ProcessingPlace == PacketProcessing.Inplace)
			{
				_inPlaceQueue.Enqueue(packet);
				_asyncMessageQueueSemaphore.Set();
			}
			else if (handler.ProcessingPlace == PacketProcessing.ThreadSafe)
			{
				_threadSafeQueue.Enqueue(packet);
			}
			else
			{
				_threadUnsafe.Enqueue(packet);
			}
		}
				
	}

	void ProcessInPlace()
	{
		while (!_cancellationToken.IsCancellationRequested)
		{
			_asyncMessageQueueSemaphore.WaitOne(500);
			DrainQueue(_inPlaceQueue);
		}
	}

	private long DrainQueue(ConcurrentQueue<WorldPacket> _queue)
	{
		// Before we process anything:
		/// If necessary, kick the player because the client didn't send anything for too long
		/// (or they've been idling in character select)
		if (IsConnectionIdle && !HasPermission(RBACPermissions.IgnoreIdleConnection))
			_socket[(int)ConnectionType.Realm]?.CloseSocket();

		WorldPacket firstDelayedPacket = null;
		uint processedPackets = 0;
		var currentTime = _gameTime.GetGameTime;

		//Check for any packets they was not recived yet.
		while (_socket[(int)ConnectionType.Realm] != null && !_queue.IsEmpty && (_queue.TryPeek(out var packet) && packet != firstDelayedPacket) && _queue.TryDequeue(out packet))
		{
			try
			{
				var handler = PacketManager.GetHandler((ClientOpcodes)packet.GetOpcode());

				switch (handler.sessionStatus)
				{
					case SessionStatus.Loggedin:
						if (!_player)
						{
							if (!_playerRecentlyLogout)
							{
								if (firstDelayedPacket == null)
									firstDelayedPacket = packet;

								QueuePacket(packet);
								Log.Logger.Debug("Re-enqueueing packet with opcode {0} with with status OpcodeStatus.Loggedin. Player is currently not in world yet.", (ClientOpcodes)packet.GetOpcode());
							}

							break;
						}
						else if (_player.IsInWorld && _antiDos.EvaluateOpcode(packet, currentTime))
						{
							handler.Invoke(this, packet);
						}

						break;
					case SessionStatus.LoggedinOrRecentlyLogout:
						if (!_player && !_playerRecentlyLogout && !_playerLogout)
							LogUnexpectedOpcode(packet, handler.sessionStatus, "the player has not logged in yet and not recently logout");
						else if (_antiDos.EvaluateOpcode(packet, currentTime))
							handler.Invoke(this, packet);

						break;
					case SessionStatus.Transfer:
						if (!_player)
							LogUnexpectedOpcode(packet, handler.sessionStatus, "the player has not logged in yet");
						else if (_player.IsInWorld)
							LogUnexpectedOpcode(packet, handler.sessionStatus, "the player is still in world");
						else if (_antiDos.EvaluateOpcode(packet, currentTime))
							handler.Invoke(this, packet);

						break;
					case SessionStatus.Authed:
						// prevent cheating with skip queue wait
						if (_inQueue)
						{
							LogUnexpectedOpcode(packet, handler.sessionStatus, "the player not pass queue yet");

							break;
						}

						if ((ClientOpcodes)packet.GetOpcode() == ClientOpcodes.EnumCharacters)
							_playerRecentlyLogout = false;

						if (_antiDos.EvaluateOpcode(packet, currentTime))
							handler.Invoke(this, packet);

						break;
					default:
						Log.Logger.Error("Received not handled opcode {0} from {1}", (ClientOpcodes)packet.GetOpcode(), GetPlayerInfo());

						break;
				}
			}
			catch (InternalBufferOverflowException ex)
			{
				Log.Logger.Error("InternalBufferOverflowException: {0} while parsing {1} from {2}.", ex.Message, (ClientOpcodes)packet.GetOpcode(), GetPlayerInfo());
			}
			catch (EndOfStreamException)
			{
				Log.Logger.Error(
							"WorldSession:Update EndOfStreamException occured while parsing a packet (opcode: {0}) from client {1}, accountid={2}. Skipped packet.",
							(ClientOpcodes)packet.GetOpcode(),
							RemoteAddress,
							AccountId);
			}

			processedPackets++;

			if (processedPackets > 100)
				break;
		}

		return currentTime;
	}

	void LogUnexpectedOpcode(WorldPacket packet, SessionStatus status, string reason)
	{
		Log.Logger.Error("Received unexpected opcode {0} Status: {1} Reason: {2} from {3}", (ClientOpcodes)packet.GetOpcode(), status, reason, GetPlayerInfo());
	}

	public void LoadAccountData(SQLResult result, AccountDataTypes mask)
	{
		for (var i = 0; i < (int)AccountDataTypes.Max; ++i)
			if (Convert.ToBoolean((int)mask & (1 << i)))
				_accountData[i] = new AccountData();

		if (result.IsEmpty())
			return;

		do
		{
			int type = result.Read<byte>(0);

			if (type >= (int)AccountDataTypes.Max)
			{
				Log.Logger.Error(
							"Table `{0}` have invalid account data type ({1}), ignore.",
							mask == AccountDataTypes.GlobalCacheMask ? "account_data" : "character_account_data",
							type);

				continue;
			}

			if (((int)mask & (1 << type)) == 0)
			{
				Log.Logger.Error(
							"Table `{0}` have non appropriate for table  account data type ({1}), ignore.",
							mask == AccountDataTypes.GlobalCacheMask ? "account_data" : "character_account_data",
							type);

				continue;
			}

			_accountData[type].Time = result.Read<long>(1);
			var bytes = result.Read<byte[]>(2);
			var line = Encoding.Default.GetString(bytes);
			_accountData[type].Data = line;
		} while (result.NextRow());
	}

	internal void SetAccountData(AccountDataTypes type, long time, string data)
	{
		if (Convert.ToBoolean((1 << (int)type) & (int)AccountDataTypes.GlobalCacheMask))
		{
			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.REP_ACCOUNT_DATA);
			stmt.AddValue(0, AccountId);
			stmt.AddValue(1, (byte)type);
			stmt.AddValue(2, time);
			stmt.AddValue(3, data);
			_characterDatabase.Execute(stmt);
		}
		else
		{
			// _player can be NULL and packet received after logout but m_GUID still store correct guid
			if (_guidLow == 0)
				return;

			var stmt = _characterDatabase.GetPreparedStatement(CharStatements.REP_PLAYER_ACCOUNT_DATA);
			stmt.AddValue(0, _guidLow);
			stmt.AddValue(1, (byte)type);
			stmt.AddValue(2, time);
			stmt.AddValue(3, data);
			_characterDatabase.Execute(stmt);
		}

		_accountData[(int)type].Time = time;
		_accountData[(int)type].Data = data;
	}

	bool ValidateHyperlinksAndMaybeKick(string str)
	{
		if (Hyperlink.CheckAllLinks(str))
			return true;

		Log.Logger.Error($"Player {Player.GetName()} {Player.GUID} sent a message with an invalid link:\n{str}");

		if (_worldConfig.GetIntValue(WorldCfg.ChatStrictLinkCheckingKick) != 0)
			KickPlayer("WorldSession::ValidateHyperlinksAndMaybeKick Invalid chat link");

		return false;
	}

	void HandleWardenData(WardenData packet)
	{
		if (_warden == null || packet.Data.GetSize() == 0)
			return;

		_warden.HandleData(packet.Data);
	}

	void SetLogoutStartTime(long requestTime)
	{
		_logoutTime = requestTime;
	}

	bool ShouldLogOut(long currTime)
	{
		return (_logoutTime > 0 && currTime >= _logoutTime + 20);
	}

	void ProcessQueryCallbacks()
	{
		_queryProcessor.ProcessReadyCallbacks();
		_transactionCallbacks.ProcessReadyCallbacks();
		_queryHolderProcessor.ProcessReadyCallbacks();
	}

	internal TransactionCallback AddTransactionCallback(TransactionCallback callback)
	{
		return _transactionCallbacks.AddCallback(callback);
	}

	void InitWarden(BigInteger k)
	{
		if (_os == "Win")
		{
			_warden = new WardenWin();
			_warden.Init(this, k);
		}
		else if (_os == "Wn64")
		{
			// Not implemented
		}
		else if (_os == "Mc64")
		{
			// Not implemented
		}
	}

	void InitializeSessionCallback(SQLQueryHolder<AccountInfoQueryLoad> holder, SQLQueryHolder<AccountInfoQueryLoad> realmHolder)
	{
		LoadAccountData(realmHolder.GetResult(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm), AccountDataTypes.GlobalCacheMask);
		LoadTutorialsData(realmHolder.GetResult(AccountInfoQueryLoad.TutorialsIndexPerRealm));
		_collectionMgr.LoadAccountToys(holder.GetResult(AccountInfoQueryLoad.GlobalAccountToys));
		_collectionMgr.LoadAccountHeirlooms(holder.GetResult(AccountInfoQueryLoad.GlobalAccountHeirlooms));
		_collectionMgr.LoadAccountMounts(holder.GetResult(AccountInfoQueryLoad.Mounts));
		_collectionMgr.LoadAccountItemAppearances(holder.GetResult(AccountInfoQueryLoad.ItemAppearances), holder.GetResult(AccountInfoQueryLoad.ItemFavoriteAppearances));
		_collectionMgr.LoadAccountTransmogIllusions(holder.GetResult(AccountInfoQueryLoad.TransmogIllusions));

		if (!_inQueue)
			SendAuthResponse(BattlenetRpcErrorCode.Ok, false);
		else
			SendAuthWaitQueue(0);

		SetInQueue(false);
		ResetTimeOutTime(false);

		SendSetTimeZoneInformation();
		SendFeatureSystemStatusGlueScreen();
		SendClientCacheVersion(_worldConfig.GetUIntValue(WorldCfg.ClientCacheVersion));
		SendAvailableHotfixes();
		SendAccountDataTimes(ObjectGuid.Empty, AccountDataTypes.GlobalCacheMask);
		SendTutorialsData();

		var result = holder.GetResult(AccountInfoQueryLoad.GlobalRealmCharacterCounts);

		if (!result.IsEmpty())
			do
			{
				_realmCharacterCounts[new RealmId(result.Read<byte>(3), result.Read<byte>(4), result.Read<uint>(2)).GetAddress()] = result.Read<byte>(1);
			} while (result.NextRow());

		ConnectionStatus bnetConnected = new();
		bnetConnected.State = 1;
		SendPacket(bnetConnected);

		_battlePetMgr.LoadFromDB(holder.GetResult(AccountInfoQueryLoad.BattlePets), holder.GetResult(AccountInfoQueryLoad.BattlePetSlot));
	}

	AccountData GetAccountData(AccountDataTypes type)
	{
		return _accountData[(int)type];
	}

	internal uint GetTutorialInt(byte index)
	{
		return _tutorials[index];
	}

	internal void SetTutorialInt(byte index, uint value)
	{
		if (_tutorials[index] != value)
		{
			_tutorials[index] = value;
			_tutorialsChanged |= TutorialsFlag.Changed;
		}
	}

	uint AdjustClientMovementTime(uint time)
	{
		var movementTime = (long)time + _timeSyncClockDelta;

		if (_timeSyncClockDelta == 0 || movementTime < 0 || movementTime > 0xFFFFFFFF)
		{
			Log.Logger.Warning("The computed movement time using clockDelta is erronous. Using fallback instead");

			return _gameTime.GetGameTimeMS;
		}
		else
		{
			return (uint)movementTime;
		}
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

    [Service(OriginalHash.GameUtilitiesService, 1)]
    BattlenetRpcErrorCode HandleProcessClientRequest(ClientRequest request, ClientResponse response)
    {
        Bgs.Protocol.Attribute command = null;
        Dictionary<string, Bgs.Protocol.Variant> Params = new();

        string removeSuffix(string str)
        {
            var pos = str.IndexOf('_');

            if (pos != -1)
                return str.Substring(0, pos);

            return str;
        }

        for (var i = 0; i < request.Attribute.Count; ++i)
        {
            var attr = request.Attribute[i];

            if (attr.Name.Contains("Command_"))
            {
                command = attr;
                Params[removeSuffix(attr.Name)] = attr.Value;
            }
            else
            {
                Params[attr.Name] = attr.Value;
            }
        }

        if (command == null)
        {
            Log.Logger.Error("{0} sent ClientRequest with no command.", GetPlayerInfo());

            return BattlenetRpcErrorCode.RpcMalformedRequest;
        }

        return removeSuffix(command.Name) switch
        {
            "Command_RealmListRequest_v1" => HandleRealmListRequest(Params, response),
            "Command_RealmJoinRequest_v1" => HandleRealmJoinRequest(Params, response),
            _ => BattlenetRpcErrorCode.RpcNotImplemented
        };
    }

    [Service(OriginalHash.GameUtilitiesService, 10)]
    BattlenetRpcErrorCode HandleGetAllValuesForAttribute(GetAllValuesForAttributeRequest request, GetAllValuesForAttributeResponse response)
    {
        if (!request.AttributeKey.Contains("Command_RealmListRequest_v1"))
        {
            _realmManager.WriteSubRegions(response);

            return BattlenetRpcErrorCode.Ok;
        }

        return BattlenetRpcErrorCode.RpcNotImplemented;
    }

    BattlenetRpcErrorCode HandleRealmListRequest(Dictionary<string, Bgs.Protocol.Variant> Params, ClientResponse response)
    {
        var subRegionId = "";
        var subRegion = Params.LookupByKey("Command_RealmListRequest_v1");

        if (subRegion != null)
            subRegionId = subRegion.StringValue;

        var compressed = _realmManager.GetRealmList(_worldManager.Realm.Build, subRegionId);

        if (compressed.Empty())
            return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

        Bgs.Protocol.Attribute attribute = new();
        attribute.Name = "Param_RealmList";
        attribute.Value = new Bgs.Protocol.Variant();
        attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
        response.Attribute.Add(attribute);

        var realmCharacterCounts = new RealmCharacterCountList();

        foreach (var characterCount in RealmCharacterCounts)
        {
            RealmCharacterCountEntry countEntry = new();
            countEntry.WowRealmAddress = (int)characterCount.Key;
            countEntry.Count = characterCount.Value;
            realmCharacterCounts.Counts.Add(countEntry);
        }

        compressed = Json.Deflate("JSONRealmCharacterCountList", realmCharacterCounts);

        attribute = new Bgs.Protocol.Attribute();
        attribute.Name = "Param_CharacterCountList";
        attribute.Value = new Bgs.Protocol.Variant();
        attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
        response.Attribute.Add(attribute);

        return BattlenetRpcErrorCode.Ok;
    }

    BattlenetRpcErrorCode HandleRealmJoinRequest(Dictionary<string, Bgs.Protocol.Variant> Params, ClientResponse response)
    {
        var realmAddress = Params.LookupByKey("Param_RealmAddress");

        if (realmAddress != null)
            return _realmManager.JoinRealm((uint)realmAddress.UintValue,
                                            _worldManager.Realm.Build,
                                            System.Net.IPAddress.Parse(RemoteAddress),
                                            RealmListSecret,
                                            SessionDbcLocale,
                                            OS,
                                            AccountName,
                                            response);

        return BattlenetRpcErrorCode.Ok;
    }

    public void BuildNameQueryData(ObjectGuid guid, out NameCacheLookupResult lookupData)
    {
        lookupData = new NameCacheLookupResult();

        var player = _objectAccessor.FindPlayer(guid);

        lookupData.Player = guid;

        lookupData.Data = new PlayerGuidLookupData();

        if (lookupData.Data.Initialize(guid, player))
            lookupData.Result = (byte)ResponseCodes.Success;
        else
            lookupData.Result = (byte)ResponseCodes.Failure; // name unknown
    }
    public void SendAuthResponse(BattlenetRpcErrorCode code, bool queued, uint queuePos = 0)
    {
        AuthResponse response = new();
        response.Result = code;

        if (code == BattlenetRpcErrorCode.Ok)
        {
            response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
            var forceRaceAndClass = _configuration.GetDefaultValue("character.EnforceRaceAndClassExpansions", true);

            response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
            response.SuccessInfo.ActiveExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)Expansion;
            response.SuccessInfo.AccountExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)AccountExpansion;
            response.SuccessInfo.VirtualRealmAddress = _worldManager.VirtualRealmAddress;
            response.SuccessInfo.Time = (uint)_gameTime.GetGameTime;

            var realm = _worldManager.Realm;

            // Send current home realm. Also there is no need to send it later in realm queries.
            response.SuccessInfo.VirtualRealms.Add(new VirtualRealmInfo(realm.Id.GetAddress(), true, false, realm.Name, realm.NormalizedName));

            if (HasPermission(RBACPermissions.UseCharacterTemplates))
                foreach (var templ in _characterTemplateDataStorage.GetCharacterTemplates().Values)
                    response.SuccessInfo.Templates.Add(templ);

            response.SuccessInfo.AvailableClasses = _gameObjectManager.GetClassExpansionRequirements();
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

    public void SendFeatureSystemStatusGlueScreen()
    {
        FeatureSystemStatusGlueScreen features = new();
        features.BpayStoreAvailable = _worldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
        features.BpayStoreDisabledByParentalControls = false;
        features.CharUndeleteEnabled = _worldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled);
        features.BpayStoreEnabled = _worldConfig.GetBoolValue(WorldCfg.FeatureSystemBpayStoreEnabled);
        features.MaxCharactersPerRealm = _worldConfig.GetIntValue(WorldCfg.CharactersPerRealm);
        features.MinimumExpansionLevel = (int)Expansion.Classic;
        features.MaximumExpansionLevel = _worldConfig.GetIntValue(WorldCfg.Expansion);

        var europaTicketConfig = new EuropaTicketConfig();
        europaTicketConfig.ThrottleState.MaxTries = 10;
        europaTicketConfig.ThrottleState.PerMilliseconds = 60000;
        europaTicketConfig.ThrottleState.TryCount = 1;
        europaTicketConfig.ThrottleState.LastResetTimeBeforeNow = 111111;
        europaTicketConfig.TicketsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled);
        europaTicketConfig.BugsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled);
        europaTicketConfig.ComplaintsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled);
        europaTicketConfig.SuggestionsEnabled = _worldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled);

        features.EuropaTicketSystemStatus = europaTicketConfig;

        SendPacket(features);
    }

    public void SendClientCacheVersion(uint version)
    {
        ClientCacheVersion cache = new();
        cache.CacheVersion = version;
        SendPacket(cache); //enabled it
    }

    private void SendAvailableHotfixes()
    {
        SendPacket(new AvailableHotfixes(_worldManager.RealmId.GetAddress(), _dB2Manager.GetHotfixData()));
    }
}