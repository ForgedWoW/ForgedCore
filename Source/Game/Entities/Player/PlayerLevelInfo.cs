﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class PlayerLevelInfo
    {
        public int[] Stats { get; set; } = new int[(int)Framework.Constants.Stats.Max];
    }
}