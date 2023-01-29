﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Threading;
using System.Timers;
using BNetServer.Networking;
using Framework.Configuration;
using Framework.Database;
using Framework.Networking;
using Timer = System.Timers.Timer;

namespace BNetServer
{
    internal class Server
    {
        private static Timer _banExpiryCheckTimer;

        private static void Main()
        {
            //Set Culture
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (!ConfigMgr.Load("BNetServer.conf"))
                ExitNow();

            // Initialize the database
            if (!StartDB())
                ExitNow();

            string bindIp = ConfigMgr.GetDefaultValue("BindIP", "0.0.0.0");

            var restSocketServer = new SocketManager<RestSession>();
            int restPort = ConfigMgr.GetDefaultValue("LoginREST.Port", 8081);

            if (restPort < 0 ||
                restPort > 0xFFFF)
            {
                Log.outError(LogFilter.Network, $"Specified login service port ({restPort}) out of allowed range (1-65535), defaulting to 8081");
                restPort = 8081;
            }

            if (!restSocketServer.StartNetwork(bindIp, restPort))
            {
                Log.outError(LogFilter.Server, "Failed to initialize Rest Socket Server");
                ExitNow();
            }

            // Get the list of realms for the server
            Global.RealmMgr.Initialize(ConfigMgr.GetDefaultValue("RealmsStateUpdateDelay", 10));
            Global.LoginServiceMgr.Initialize();

            var sessionSocketServer = new SocketManager<Session>();
            // Start the listening port (acceptor) for auth connections
            int bnPort = ConfigMgr.GetDefaultValue("BattlenetPort", 1119);

            if (bnPort < 0 ||
                bnPort > 0xFFFF)
            {
                Log.outError(LogFilter.Server, $"Specified battle.net port ({bnPort}) out of allowed range (1-65535)");
                ExitNow();
            }

            if (!sessionSocketServer.StartNetwork(bindIp, bnPort))
            {
                Log.outError(LogFilter.Network, "Failed to start BnetServer Network");
                ExitNow();
            }

            uint _banExpiryCheckInterval = ConfigMgr.GetDefaultValue("BanExpiryCheckInterval", 60u);
            _banExpiryCheckTimer = new Timer(_banExpiryCheckInterval);
            _banExpiryCheckTimer.Elapsed += BanExpiryCheckTimer_Elapsed;
            _banExpiryCheckTimer.Start();
        }

        private static bool StartDB()
        {
            DatabaseLoader loader = new(DatabaseTypeFlags.None);
            loader.AddDatabase(DB.Login, "Login");

            if (!loader.Load())
                return false;

            Log.SetRealmId(0); // Enables DB appenders when realm is set.

            return true;
        }

        private static void ExitNow()
        {
            Console.WriteLine("Halting process...");
            Thread.Sleep(10000);
            Environment.Exit(-1);
        }

        private static void BanExpiryCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.DelExpiredIpBans));
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.UpdExpiredAccountBans));
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.DelBnetExpiredAccountBanned));
        }
    }
}