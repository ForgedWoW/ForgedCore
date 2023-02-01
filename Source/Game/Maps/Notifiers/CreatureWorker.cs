﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class CreatureWorker : Notifier
    {
        private readonly IDoWork<Creature> _do;
        private readonly PhaseShift _phaseShift;

        public CreatureWorker(WorldObject searcher, IDoWork<Creature> _Do)
        {
            _phaseShift = searcher.GetPhaseShift();
            _do = _Do;
        }

        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (creature.InSamePhase(_phaseShift))
                    _do.Invoke(creature);
            }
        }
    }
}