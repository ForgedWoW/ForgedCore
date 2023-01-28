﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class PlayerCurrency
	{
		public byte Flags { get; set; }
        public uint Quantity { get; set; }
        public PlayerCurrencyState State;
		public uint TrackedQuantity { get; set; }
        public uint WeeklyQuantity { get; set; }
    }
}