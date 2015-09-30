using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class JobGiver_PackWater : ThinkNode_JobGiver
    {
        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            // Find a non-full water carrier you are wearing.
            var packInto = WaterUtility.WaterInInventoryNotFull(pawn);
            if (packInto != null)
            {
                // Find a source of water.
                var packFrom = WaterUtility.BestWaterSpawnedFor(pawn);
                if (packFrom != null)
                {
                    return new Job(DefDatabase<JobDef>.GetNamed("Dehydration_PackWaterJob"), packFrom, packInto);
                }
            }

            return null;
        }
    }
}
