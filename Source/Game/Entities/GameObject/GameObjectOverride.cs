﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    // From `gameobject_template_addon`, `gameobject_overrides`
    public class GameObjectOverride
    {
        public uint Faction { get; set; }
        public GameObjectFlags Flags { get; set; }
    }
}