using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class WorkGiver_GrowerWater : WorkGiver_Grower
    {
        private static readonly string WaterPlantDefName = "Dehydration_WaterPlantJob";


        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.Touch; }
        }

        public override Job JobOnCell(Pawn pawn, IntVec3 cell)
        {
            if (WorkGiver_Grower.wantedPlantDef == null)
            {
                base.DetermineWantedPlantDef(cell);
                if (WorkGiver_Grower.wantedPlantDef == null)
                {
                    return null;
                }
            }

            Plant plant = cell.GetPlant() as Plant;
            if (plant == null)
            {
                return null;
            }
            if (plant.def != WorkGiver_Grower.wantedPlantDef)
            {
                return null;
            }
            if (!plant.def.plant.Harvestable || plant.LifeStage != PlantLifeStage.Growing ||
                !plant.NeedsWatering)
            {
                return null;
            }

            if (!pawn.CanReserve(plant, 1))
            {
                return null;
            }

            // Got a plant we want to water. Do we have a watering tool?
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

            // If we already have enough water in the tool, can just haul it now.
            // TODO: use water-per-plant from JobDriver
            if (toolWc.StoredLitres >= JobDriver_WaterPlant.waterPerPlant)
            {
                return MakeWaterPlantJob(TargetInfo.NullThing, plant, tool);
            }
            // Otherwise we need to find more.
            else
            {
                float collectLitres = toolWc.FreeSpaceLitres;
                IntVec3 waterNear = pawn.Position;

                // Wells, water carriers on the ground, etc.
                Predicate<Thing> validator = (Thing candidate) =>
                {
                    var candidateWc = candidate.TryGetComp<CompWaterContainer>();
                    if (candidateWc == null) { return false; }

                    // Don't haul if the source has too little water.
                    return candidateWc.StoredLitres >= JobDriver_WaterPlant.waterPerPlant;
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
                    return MakeWaterPlantJob(thingWater, plant, tool);
                }
                else if (terrainWaterVec != null)
                {
                    return MakeWaterPlantJob(terrainWaterVec.Value, plant, tool);
                }
                else
                {
                    JobFailReason.Is("no water nearby");
                    return null;
                }
            }
        }

        private Job MakeWaterPlantJob(TargetInfo fromTarget, TargetInfo plantTarget, TargetInfo toolTarget)
        {
            return new Job(DefDatabase<JobDef>.GetNamed(WaterPlantDefName), fromTarget, plantTarget, toolTarget);
        }
    }
}
