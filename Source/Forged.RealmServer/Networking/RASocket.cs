﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Framework.Configuration;
using Framework.Constants;
using Framework.Cryptography;
using Framework.Database;
using Framework.Networking;
using Forged.RealmServer.Chat;

namespace Forged.RealmServer.Networking;

public class RASocket : ISocket
{
	readonly Socket _socket;
	readonly IPAddress _remoteAddress;
	readonly byte[] _receiveBuffer;

	public RASocket(Socket socket)
	{
		_socket = socket;
		_remoteAddress = ((IPEndPoint)_socket.RemoteEndPoint).Address;
		_receiveBuffer = new byte[1024];
	}

	public void Accept()
	{
		// wait 1 second for active connections to send negotiation request
		for (var counter = 0; counter < 10 && _socket.Available == 0; counter++)
			Thread.Sleep(100);

		if (_socket.Available > 0)
		{
			// Handle subnegotiation
			_socket.Receive(_receiveBuffer);

			// Send the end-of-negotiation packet
			byte[] reply =
			{
				0xFF, 0xF0
			};

			_socket.Send(reply);
		}

		Send("Authentication Required\r\n");
		Send("Email: ");
		var userName = ReadString();

		if (userName.IsEmpty())
		{
			CloseSocket();

			return;
		}

		Log.outInfo(LogFilter.CommandsRA, $"Accepting RA connection from user {userName} (IP: {_remoteAddress})");

		Send("Password: ");
		var password = ReadString();

		if (password.IsEmpty())
		{
			CloseSocket();

			return;
		}

		if (!CheckAccessLevelAndPassword(userName, password))
		{
			Send("Authentication failed\r\n");
			CloseSocket();

			return;
		}

		Log.outInfo(LogFilter.CommandsRA, $"User {userName} (IP: {_remoteAddress}) authenticated correctly to RA");

		// Authentication successful, send the motd
		foreach (var line in Global.WorldMgr.Motd)
			Send(line);

		Send("\r\n");

		// Read commands
		for (;;)
		{
			Send("\r\nCypher>");
			var command = ReadString();

			if (!ProcessCommand(command))
				break;
		}

		CloseSocket();
	}

	public bool Update()
	{
		return IsOpen();
	}

	public bool IsOpen()
	{
		return _socket.Connected;
	}

	public void CloseSocket()
	{
		if (_socket == null)
			return;

		try
		{
			_socket.Shutdown(SocketShutdown.Both);
			_socket.Close();
		}
		catch (Exception ex)
		{
			Log.outDebug(LogFilter.Network, "WorldSocket.CloseSocket: {0} errored when shutting down socket: {1}", _remoteAddress.ToString(), ex.Message);
		}
	}

	void Send(string str)
	{
		if (!IsOpen())
			return;

		_socket.Send(Encoding.UTF8.GetBytes(str));
	}

	string ReadString()
	{
		try
		{
			var str = "";

			do
			{
				var bytes = _socket.Receive(_receiveBuffer);

				if (bytes == 0)
					return "";

				str = string.Concat(str, Encoding.UTF8.GetString(_receiveBuffer, 0, bytes));
			} while (!str.Contains("\n"));

			return str.TrimEnd('\r', '\n');
		}
		catch (Exception ex)
		{
			Log.outException(ex);

			return "";
		}
	}

	bool CheckAccessLevelAndPassword(string email, string password)
	{
		//"SELECT a.id, a.username FROM account a LEFT JOIN battlenet_accounts ba ON a.battlenet_account = ba.id WHERE ba.email = ?"
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_GAME_ACCOUNT_LIST);
		stmt.AddValue(0, email);
		var result = DB.Login.Query(stmt);

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.CommandsRA, $"User {email} does not exist in database");

			return false;
		}

		var accountId = result.Read<uint>(0);
		var username = result.Read<string>(1);

		stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_ACCESS_BY_ID);
		stmt.AddValue(0, accountId);
		result = DB.Login.Query(stmt);

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.CommandsRA, $"User {email} has no privilege to login");

			return false;
		}

		//"SELECT SecurityLevel, RealmID FROM account_access WHERE AccountID = ? and (RealmID = ? OR RealmID = -1) ORDER BY SecurityLevel desc");
		if (result.Read<byte>(0) < ConfigMgr.GetDefaultValue("Ra.MinLevel", (byte)AccountTypes.Administrator))
		{
			Log.outInfo(LogFilter.CommandsRA, $"User {email} has no privilege to login");

			return false;
		}
		else if (result.Read<int>(1) != -1)
		{
			Log.outInfo(LogFilter.CommandsRA, $"User {email} has to be assigned on all realms (with RealmID = '-1')");

			return false;
		}

		stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_CHECK_PASSWORD);
		stmt.AddValue(0, accountId);
		result = DB.Login.Query(stmt);

		if (!result.IsEmpty())
		{
			var salt = result.Read<byte[]>(0);
			var verifier = result.Read<byte[]>(1);

			if (SRP6.CheckLogin(username, password, salt, verifier))
				return true;
		}

		Log.outInfo(LogFilter.CommandsRA, $"Wrong password for user: {email}");

		return false;
	}

	bool ProcessCommand(string command)
	{
		if (command.Length == 0)
			return false;

		Log.outInfo(LogFilter.CommandsRA, $"Received command: {command}");

		// handle quit, exit and logout commands to terminate connection
		if (command == "quit" || command == "exit" || command == "logout")
		{
			Send("Closing\r\n");

			return false;
		}

		RemoteAccessHandler cmd = new(CommandPrint);
		cmd.ParseCommands(command);

		return true;
	}

	void CommandPrint(string text)
	{
		if (text.IsEmpty())
			return;

		Send(text);
	}
}