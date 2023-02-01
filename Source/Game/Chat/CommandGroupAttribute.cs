﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.Chat
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupAttribute : CommandAttribute
    {
        public CommandGroupAttribute(string command) : base(command)
        {
        }
    }
}