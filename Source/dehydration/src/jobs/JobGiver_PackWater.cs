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
        private static readonly string PackJobDefName = "Dehydration_PackWaterJob";

        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            // Find a non-full water carrier you are wearing.
            var packInto = WaterUtility.WaterInInventoryNotFull(pawn);
            if (packInto != null)
            {
                // Find a source of water.
                // Wells, water carriers on the ground, etc.
                var thingWater = WaterUtility.BestWaterSpawnedFor(pawn);
                // Rivers, lakes, other terrain-based water sources.
                var terrainWaterVec = WaterUtility.BestTerrainWaterFor(pawn);

                // Pick the nearest of the two types.
                if (thingWater != null && terrainWaterVec != null)
                {
                    float thingDist = (pawn.Position - thingWater.Position).LengthHorizontalSquared;
                    float terrainDist = (pawn.Position - terrainWaterVec.Value).LengthHorizontalSquared;
                    if (thingDist <= terrainDist)
                    {
                        terrainWaterVec = null;
                    }
                    else
                    {
                        thingWater = null;
                    }
                }

                if (thingWater != null)
                {
                    return new Job(DefDatabase<JobDef>.GetNamed(PackJobDefName), thingWater, packInto);
                }
                else if (terrainWaterVec != null)
                {
                    return new Job(DefDatabase<JobDef>.GetNamed(PackJobDefName), terrainWaterVec.Value, packInto);
                }
            }

            return null;
        }
    }
}
