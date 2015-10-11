using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class JobGiver_GetWater : ThinkNode_JobGiver
    {
        private static readonly string DrinkJobDefName = "Dehydration_DrinkJob";

        public override float GetPriority(Pawn pawn)
        {
            var need = pawn.needs.TryGetNeed<Need_Water>();
            if (need == null)
            {
                return 0f;
            }
            if (need.CurLevel < need.ThreshThirsty + 0.02f)
            {
                return 10f;
            }
            return 0f;
        }

        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            var waterNeed = pawn.needs.TryGetNeed<Need_Water>();
            if (waterNeed == null)
            {
                Log.Error(string.Format("JobGiver_GetWater for pawn without Need_Water: {0}", pawn.Label));
                return null;
            }

            // Drink from water carriers you are wearing.
            var wornWater = WaterUtility.WaterInInventory(pawn);
            if (wornWater != null)
            {
                return MakeGetWaterJob(wornWater);
            }

            // Look for other water sources.
            // Wells, water carriers on the ground, etc.
            var thingWater = WaterUtility.BestWaterSpawnedFor(pawn);
            // Rivers, lakes, other terrain-based water sources.
            var terrainWaterVec = WaterUtility.BestTerrainWaterFor(pawn);

            // Pick the nearest of the other water sources.
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
                return MakeGetWaterJob(thingWater);
            }
            else if (terrainWaterVec != null)
            {
                return MakeGetWaterJob(terrainWaterVec.Value);
            }

            return null;
        }

        private Job MakeGetWaterJob(TargetInfo target)
        {
            return new Job(DefDatabase<JobDef>.GetNamed(DrinkJobDefName), target)
            {
                locomotionUrgency = LocomotionUrgency.Walk
            };
        }
    }
}
