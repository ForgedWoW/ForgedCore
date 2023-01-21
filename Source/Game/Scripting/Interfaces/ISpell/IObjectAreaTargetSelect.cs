﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;

namespace Game.Scripting.Interfaces.Spell
{
    public interface IObjectAreaTargetSelect : ITargetHookHandler
    {
        void FilterTargets(List<WorldObject> targets);
    }

    public class ObjectAreaTargetSelectHandler : TargetHookHandler, IObjectAreaTargetSelect
    {
        public delegate void SpellObjectAreaTargetSelectFnType(List<WorldObject> targets);
        SpellObjectAreaTargetSelectFnType _func;


        public ObjectAreaTargetSelectHandler(SpellObjectAreaTargetSelectFnType func, uint effectIndex, Targets targetType, SpellScriptHookType hookType = SpellScriptHookType.ObjectAreaTargetSelect) : base(effectIndex, targetType, true, hookType)
        {
            _func = func;
        }

        public void FilterTargets(List<WorldObject> targets)
        {
            _func(targets);
        }
       
    }


}