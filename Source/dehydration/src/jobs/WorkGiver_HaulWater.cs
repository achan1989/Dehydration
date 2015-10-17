﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    /// <summary>
    /// For hauling water to containers.
    /// </summary>
    public class WorkGiver_HaulWater : WorkGiver_Scanner
    {
        private static readonly string HaulDefName = "Dehydration_HaulWaterJob";
        /// <summary>
        /// Don't haul if the target needs a small amount of water.
        /// </summary>
        private static readonly float MinimumFill = 5f;

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            var wc = t.TryGetComp<CompWaterContainer>();
            if (wc == null) { return false; }

            return wc.ManuallyFillable && wc.FreeSpaceLitres >= MinimumFill;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            var toWc = t.TryGetComp<CompWaterContainer>();
            if (toWc == null)
            {
                Log.Error("Thing has no CompWaterContainer");
                return null;
            }

            IntVec3 waterNear = t.Position;

            Thing tool = WaterUtility.HaulingToolInInventory(pawn);
            if (tool == null)
            {
                tool = WaterUtility.NearestHaulingTool(pawn);
                if (tool == null)
                {
                    JobFailReason.Is("nothing to haul with");
                    return null;
                }
            }
            
            var toolWc = tool.TryGetComp<CompWaterContainer>();
            float collectLitres = toWc.FreeSpaceLitres - toolWc.StoredLitres;
            if (collectLitres > toolWc.FreeSpaceLitres)
            {
                collectLitres = toolWc.FreeSpaceLitres;
            }

            // If we already have enough water in the tool, can just haul it now.
            if (collectLitres <= 0)
            {
                return MakeHaulJob(TargetInfo.NullThing, t, tool);
            }
            // Otherwise we need to find more.
            else
            {
                // Wells, water carriers on the ground, etc.
                // Don't take water from the target Thing.
                // TODO: some priority logic to avoid transferring water between two containers in an infinite loop.
                Predicate<Thing> validator = (Thing candidate) =>
                {
                    var wc = candidate.TryGetComp<CompWaterContainer>();
                    return (wc != null && wc.StoredLitres > MinimumFill && !candidate.Equals(t));
                };
                var thingWater = WaterUtility.BestWaterSpawnedFor(pawn, collectLitres, validator, waterNear);
            
                // Rivers, lakes, other terrain-based water sources.
                var terrainWaterVec = WaterUtility.BestTerrainWaterFor(pawn, waterNear);

                // Pick the nearest of the water sources.
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
                    return MakeHaulJob(thingWater, t, tool);
                }
                else if (terrainWaterVec != null)
                {
                    return MakeHaulJob(terrainWaterVec.Value, t, tool);
                }
                else
                {
                    JobFailReason.Is("nowhere to haul from");
                    return null;
                }
            }
        }

        private Job MakeHaulJob(TargetInfo fromTarget, TargetInfo toTarget, TargetInfo toolTarget)
        {
            return new Job(DefDatabase<JobDef>.GetNamed(HaulDefName), fromTarget, toTarget, toolTarget)
            {
                locomotionUrgency = LocomotionUrgency.Walk
            };
        }
    }
}
