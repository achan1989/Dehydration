using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class JobGiver_ExitMapWalkRandomInterruptable : JobGiver_ExitMapWalkRandom
    {
        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            // Exit the map, but stop doing that if there's something more important to do.
            var job = base.TryGiveTerminalJob(pawn);
            job.expiryInterval = 900;
            job.checkOverrideOnExpire = true;

            return job;
        }
    }
}
