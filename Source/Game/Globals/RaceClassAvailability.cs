﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game
{
    public class RaceClassAvailability
    {
        public List<ClassAvailability> Classes { get; set; } = new();
        public byte RaceID { get; set; }
    }
}