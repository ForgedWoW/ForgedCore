﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Cryptography;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Accounts;

public sealed class AccountManager
{
	private const int MaxAccountLength = 16;
	private const int MaxEmailLength = 64;
	private readonly LoginDatabase _loginDatabase;
	private readonly CharacterDatabase _characterDatabase;
	private readonly ObjectAccessor _objectAccessor;

	private readonly MultiMap<byte, uint> _defaultPermissions = new();

	public Dictionary<uint, RBACPermission> RBACPermissionList { get; } = new();

	public AccountManager(LoginDatabase loginDatabase, CharacterDatabase characterDatabase, ObjectAccessor objectAccessor)
	{
		_loginDatabase = loginDatabase;
		_characterDatabase = characterDatabase;
		_objectAccessor = objectAccessor;
	}

	public AccountOpResult CreateAccount(string username, string password, string email = "", uint bnetAccountId = 0, byte bnetIndex = 0)
	{
		if (username.Length > MaxAccountLength)
			return AccountOpResult.NameTooLong;

		if (password.Length > MaxAccountLength)
			return AccountOpResult.PassTooLong;

		if (GetId(username) != 0)
			return AccountOpResult.NameAlreadyExist;

		var (salt, verifier) = SRP6.MakeRegistrationData(username, password);

		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_ACCOUNT);
		stmt.AddValue(0, username);
		stmt.AddValue(1, salt);
		stmt.AddValue(2, verifier);
		stmt.AddValue(3, email);
		stmt.AddValue(4, email);

		if (bnetAccountId != 0 && bnetIndex != 0)
		{
			stmt.AddValue(5, bnetAccountId);
			stmt.AddValue(6, bnetIndex);
		}
		else
		{
			stmt.AddNull(5);
			stmt.AddNull(6);
		}

		_loginDatabase.DirectExecute(stmt); // Enforce saving, otherwise AddGroup can fail

		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_REALM_CHARACTERS_INIT);
		_loginDatabase.Execute(stmt);

		return AccountOpResult.Ok;
	}

	public AccountOpResult DeleteAccount(uint accountId)
	{
		// Check if accounts exists
		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BY_ID);
		stmt.AddValue(0, accountId);
		var result = _loginDatabase.Query(stmt);

		if (result.IsEmpty())
			return AccountOpResult.NameNotExist;

		// Obtain accounts characters
		stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARS_BY_ACCOUNT_ID);
		stmt.AddValue(0, accountId);
		result = _characterDatabase.Query(stmt);

		if (!result.IsEmpty())
			do
			{
				var guid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));

				// Kick if player is online
				var p = _objectAccessor.FindPlayer(guid);

				if (p)
				{
					var s = p.Session;
					s.KickPlayer("AccountMgr::DeleteAccount Deleting the account"); // mark session to remove at next session list update
					s.LogoutPlayer(false);                                          // logout player without waiting next session list update
				}

				Player.DeleteFromDB(guid, accountId, false); // no need to update realm characters
			} while (result.NextRow());

		// table realm specific but common for all characters of account for realm
		stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_TUTORIALS);
		stmt.AddValue(0, accountId);
		_characterDatabase.Execute(stmt);

		stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ACCOUNT_DATA);
		stmt.AddValue(0, accountId);
		_characterDatabase.Execute(stmt);

		stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_BAN);
		stmt.AddValue(0, accountId);
		_characterDatabase.Execute(stmt);

		SQLTransaction trans = new();

		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT);
		stmt.AddValue(0, accountId);
		trans.Append(stmt);

		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_ACCESS);
		stmt.AddValue(0, accountId);
		trans.Append(stmt);

		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_REALM_CHARACTERS);
		stmt.AddValue(0, accountId);
		trans.Append(stmt);

		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_BANNED);
		stmt.AddValue(0, accountId);
		trans.Append(stmt);

		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_MUTED);
		stmt.AddValue(0, accountId);
		trans.Append(stmt);

		_loginDatabase.CommitTransaction(trans);

		return AccountOpResult.Ok;
	}

	public AccountOpResult ChangeUsername(uint accountId, string newUsername, string newPassword)
	{
		// Check if accounts exists
		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BY_ID);
		stmt.AddValue(0, accountId);
		var result = _loginDatabase.Query(stmt);

		if (result.IsEmpty())
			return AccountOpResult.NameNotExist;

		if (newUsername.Length > MaxAccountLength)
			return AccountOpResult.NameTooLong;

		if (newPassword.Length > MaxAccountLength)
			return AccountOpResult.PassTooLong;

		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_USERNAME);
		stmt.AddValue(0, newUsername);
		stmt.AddValue(1, accountId);
		_loginDatabase.Execute(stmt);

		var (salt, verifier) = SRP6.MakeRegistrationData(newUsername, newPassword);
		stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_LOGON);
		stmt.AddValue(0, salt);
		stmt.AddValue(1, verifier);
		stmt.AddValue(2, accountId);
		_loginDatabase.Execute(stmt);

		return AccountOpResult.Ok;
	}

	public AccountOpResult ChangePassword(uint accountId, string newPassword)
	{
		if (!GetName(accountId, out var username))
			return AccountOpResult.NameNotExist; // account doesn't exist

		if (newPassword.Length > MaxAccountLength)
			return AccountOpResult.PassTooLong;

		var (salt, verifier) = SRP6.MakeRegistrationData(username, newPassword);

		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_LOGON);
		stmt.AddValue(0, salt);
		stmt.AddValue(1, verifier);
		stmt.AddValue(2, accountId);
		_loginDatabase.Execute(stmt);

		return AccountOpResult.Ok;
	}

	public AccountOpResult ChangeEmail(uint accountId, string newEmail)
	{
		if (!GetName(accountId, out _))
			return AccountOpResult.NameNotExist; // account doesn't exist

		if (newEmail.Length > MaxEmailLength)
			return AccountOpResult.EmailTooLong;

		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_EMAIL);
		stmt.AddValue(0, newEmail);
		stmt.AddValue(1, accountId);
		_loginDatabase.Execute(stmt);

		return AccountOpResult.Ok;
	}

	public AccountOpResult ChangeRegEmail(uint accountId, string newEmail)
	{
		if (!GetName(accountId, out _))
			return AccountOpResult.NameNotExist; // account doesn't exist

		if (newEmail.Length > MaxEmailLength)
			return AccountOpResult.EmailTooLong;

		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_REG_EMAIL);
		stmt.AddValue(0, newEmail);
		stmt.AddValue(1, accountId);
		_loginDatabase.Execute(stmt);

		return AccountOpResult.Ok;
	}

	public uint GetId(string username)
	{
		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.GET_ACCOUNT_ID_BY_USERNAME);
		stmt.AddValue(0, username);
		var result = _loginDatabase.Query(stmt);

		return !result.IsEmpty() ? result.Read<uint>(0) : 0;
	}

	public AccountTypes GetSecurity(uint accountId, int realmId)
	{
		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.GET_GMLEVEL_BY_REALMID);
		stmt.AddValue(0, accountId);
		stmt.AddValue(1, realmId);
		var result = _loginDatabase.Query(stmt);

		return !result.IsEmpty() ? (AccountTypes)result.Read<uint>(0) : AccountTypes.Player;
	}

	public QueryCallback GetSecurityAsync(uint accountId, int realmId, Action<uint> callback)
	{
		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.GET_GMLEVEL_BY_REALMID);
		stmt.AddValue(0, accountId);
		stmt.AddValue(1, realmId);

		return _loginDatabase.AsyncQuery(stmt).WithCallback(result => { callback(!result.IsEmpty() ? result.Read<byte>(0) : (uint)AccountTypes.Player); });
	}

	public bool GetName(uint accountId, out string name)
	{
		name = "";
		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.GET_USERNAME_BY_ID);
		stmt.AddValue(0, accountId);
		var result = _loginDatabase.Query(stmt);

		if (!result.IsEmpty())
		{
			name = result.Read<string>(0);

			return true;
		}

		return false;
	}

	public bool GetEmail(uint accountId, out string email)
	{
		email = "";
		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.GET_EMAIL_BY_ID);
		stmt.AddValue(0, accountId);
		var result = _loginDatabase.Query(stmt);

		if (!result.IsEmpty())
		{
			email = result.Read<string>(0);

			return true;
		}

		return false;
	}

	public bool CheckPassword(uint accountId, string password)
	{
		if (!GetName(accountId, out var username))
			return false;

		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_CHECK_PASSWORD);
		stmt.AddValue(0, accountId);
		var result = _loginDatabase.Query(stmt);

		if (!result.IsEmpty())
		{
			var salt = result.Read<byte[]>(0);
			var verifier = result.Read<byte[]>(1);

			if (SRP6.CheckLogin(username, password, salt, verifier))
				return true;
		}

		return false;
	}

	public bool CheckEmail(uint accountId, string newEmail)
	{
		// We simply return false for a non-existing email
		if (!GetEmail(accountId, out var oldEmail))
			return false;

		if (oldEmail == newEmail)
			return true;

		return false;
	}

	public uint GetCharactersCount(uint accountId)
	{
		// check character count
		var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_SUM_CHARS);
		stmt.AddValue(0, accountId);
		var result = _characterDatabase.Query(stmt);

		return result.IsEmpty() ? 0 : (uint)result.Read<ulong>(0);
	}

	public bool IsBannedAccount(string name)
	{
		var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BANNED_BY_USERNAME);
		stmt.AddValue(0, name);
		var result = _loginDatabase.Query(stmt);

		return !result.IsEmpty();
	}

	public bool IsPlayerAccount(AccountTypes gmlevel)
	{
		return gmlevel == AccountTypes.Player;
	}

	public bool IsAdminAccount(AccountTypes gmlevel)
	{
		return gmlevel is >= AccountTypes.Administrator and <= AccountTypes.Console;
	}

	public bool IsConsoleAccount(AccountTypes gmlevel)
	{
		return gmlevel == AccountTypes.Console;
	}

	public void LoadRBAC()
	{
		RBACPermissionList.Clear();
		_defaultPermissions.Clear();

		Log.Logger.Debug("AccountMgr:LoadRBAC");
		var oldMsTime = Time.MSTime;
		uint count1 = 0;
		uint count2 = 0;
		uint count3 = 0;

		Log.Logger.Debug("AccountMgr:LoadRBAC: Loading permissions");
		var result = _loginDatabase.Query("SELECT id, name FROM rbac_permissions");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 account permission definitions. DB table `rbac_permissions` is empty.");

			return;
		}

		do
		{
			var id = result.Read<uint>(0);
			RBACPermissionList[id] = new RBACPermission(id, result.Read<string>(1));
			++count1;
		} while (result.NextRow());

		Log.Logger.Debug("AccountMgr:LoadRBAC: Loading linked permissions");
		result = _loginDatabase.Query("SELECT id, linkedId FROM rbac_linked_permissions ORDER BY id ASC");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 linked permissions. DB table `rbac_linked_permissions` is empty.");

			return;
		}

		uint permissionId = 0;
		RBACPermission permission = null;

		do
		{
			var newId = result.Read<uint>(0);

			if (permissionId != newId)
			{
				permissionId = newId;
				permission = RBACPermissionList[newId];
			}

			var linkedPermissionId = result.Read<uint>(1);

			if (linkedPermissionId == permissionId)
			{
				Log.Logger.Error("RBAC Permission {0} has itself as linked permission. Ignored", permissionId);

				continue;
			}

			permission?.AddLinkedPermission(linkedPermissionId);

			++count2;
		} while (result.NextRow());

		Log.Logger.Debug("AccountMgr:LoadRBAC: Loading default permissions");
		result = _loginDatabase.Query("SELECT secId, permissionId FROM rbac_default_permissions ORDER BY secId ASC");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 default permission definitions. DB table `rbac_default_permissions` is empty.");

			return;
		}

		do
		{
			var secId = result.Read<uint>(0);

			_defaultPermissions.Add((byte)secId, result.Read<uint>(1));
			++count3;
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} permission definitions, {1} linked permissions and {2} default permissions in {3} ms", count1, count2, count3, Time.GetMSTimeDiffToNow(oldMsTime));
	}

	public void UpdateAccountAccess(RBACData rbac, uint accountId, byte securityLevel, int realmId)
	{
		if (rbac != null && securityLevel != rbac.GetSecurityLevel())
			rbac.SetSecurityLevel(securityLevel);

		PreparedStatement stmt;
		SQLTransaction trans = new();

		// Delete old security level from DB
		if (realmId == -1)
		{
			stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_ACCESS);
			stmt.AddValue(0, accountId);
			trans.Append(stmt);
		}
		else
		{
			stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_ACCESS_BY_REALM);
			stmt.AddValue(0, accountId);
			stmt.AddValue(1, realmId);
			trans.Append(stmt);
		}

		// Add new security level
		if (securityLevel != 0)
		{
			stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_ACCOUNT_ACCESS);
			stmt.AddValue(0, accountId);
			stmt.AddValue(1, securityLevel);
			stmt.AddValue(2, realmId);
			trans.Append(stmt);
		}

		_loginDatabase.CommitTransaction(trans);
	}

	public RBACPermission GetRBACPermission(uint permissionId)
	{
		Log.Logger.Debug("AccountMgr:GetRBACPermission: {0}", permissionId);

		return RBACPermissionList.LookupByKey(permissionId);
	}

	public bool HasPermission(uint accountId, RBACPermissions permissionId, uint realmId)
	{
		if (accountId == 0)
		{
			Log.Logger.Error("AccountMgr:HasPermission: Wrong accountId 0");

			return false;
		}

		RBACData rbac = new(accountId, "", (int)realmId, (byte)GetSecurity(accountId, (int)realmId));
		rbac.LoadFromDB();
		var hasPermission = rbac.HasPermission(permissionId);

		Log.Logger.Debug("AccountMgr:HasPermission [AccountId: {0}, PermissionId: {1}, realmId: {2}]: {3}",
						accountId,
						permissionId,
						realmId,
						hasPermission);

		return hasPermission;
	}

	public List<uint> GetRBACDefaultPermissions(byte secLevel)
	{
		return _defaultPermissions[secLevel];
	}
}