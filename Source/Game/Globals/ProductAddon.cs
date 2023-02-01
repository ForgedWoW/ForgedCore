﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class ProductAddon
    {
        public uint DisplayInfoEntry { get; set; }
        public byte DisableListing { get; set; }
        public byte DisableBuy { get; set; }
        public byte NameColorIndex { get; set; }
        public string ScriptName { get; set; } = "";
        public string Comment { get; set; } = "";
    }
}