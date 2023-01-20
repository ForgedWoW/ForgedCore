﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.Interfaces.Spell
{
    public interface ICalculateCastTime : ISpellScript
    {
        public int CalcCastTime(int castTime);
    }
}