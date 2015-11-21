using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class WorkGiver_WaterPatient : WorkGiver_Scanner
    {
        private static readonly string WaterPatientDefName = "Dehydration_WaterPatientJob";
        private static readonly float MinimumHaul = 0.1f;


        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForGroup(ThingRequestGroup.Pawn); }
        }

        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.Touch; }
        }

        public override bool HasJobOnThing(Pawn carer, Thing t)
        {
            Pawn patient = t as Pawn;
            if (patient == null || patient == carer)
            {
                return false;
            }
            if (patient.GetPosture() == PawnPosture.Standing)
            {
                return false;
            }
            if (patient.RaceProps.Humanlike)
            {
                if (patient.Faction != Faction.OfColony && patient.HostFaction != Faction.OfColony)
                {
                    return false;
                }
            }
            else
            {
                Building_Bed building_Bed = patient.CurrentBed();
                if (building_Bed == null || building_Bed.Faction != patient.Faction)
                {
                    return false;
                }
            }

            var waterNeed = patient.needs.TryGetNeed<Need_Water>();
            if (waterNeed == null || !waterNeed.NeedWaterSoon)
            {
                return false;
            }

            if (patient.HostFaction != null)
            {
                if (patient.HostFaction != patient.Faction)
                {
                    return false;
                }
                if (patient.guest != null && !patient.guest.ShouldBeBroughtFood)
                {
                    return false;
                }
            }

            return carer.CanReserve(patient, 1);
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Pawn patient = t as Pawn;
            var waterNeed = patient.needs.TryGetNeed<Need_Water>();

            IntVec3 waterNear = patient.Position;

            // Prefer to haul with a tool we're holding.
            Thing tool = WaterUtility.HaulingToolInInventory(pawn);
            if (tool == null)
            {
                // Otherwise another tool that we have to go and get.
                tool = WaterUtility.NearestHaulingTool(pawn);
                if (tool == null)
                {
                    ConceptDecider.TeachOpportunity(DefDatabase<ConceptDef>.GetNamedSilentFail("Dehydration_HaulWaterTool"),
                                                    OpportunityType.Important);
                    JobFailReason.Is("nothing to haul with");
                    return null;
                }
            }

            var toolWc = tool.TryGetComp<CompWaterContainer>();
            float collectLitres = waterNeed.HydrationWantedLitres - toolWc.StoredLitres;
            if (collectLitres > toolWc.FreeSpaceLitres)
            {
                collectLitres = toolWc.FreeSpaceLitres;
            }

            // If we already have enough water in the tool, can just haul it now.
            if (collectLitres <= 0)
            {
                return MakeWaterJob(TargetInfo.NullThing, t, tool);
            }
            // Otherwise we need to find more.
            else
            {
                // Wells, water carriers on the ground, etc.
                Predicate<Thing> validator = (Thing candidate) =>
                {
                    var candidateWc = candidate.TryGetComp<CompWaterContainer>();
                    if (candidateWc == null) { return false; }

                    // Don't haul if the source has too little water.
                    return candidateWc.StoredLitres > MinimumHaul;
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
                    return MakeWaterJob(thingWater, t, tool);
                }
                else if (terrainWaterVec != null)
                {
                    return MakeWaterJob(terrainWaterVec.Value, t, tool);
                }
                else
                {
                    JobFailReason.Is("nowhere to haul from");
                    return null;
                }
            }
        }

        private Job MakeWaterJob(TargetInfo fromTarget, TargetInfo toTarget, TargetInfo toolTarget)
        {
            return new Job(DefDatabase<JobDef>.GetNamed(WaterPatientDefName), fromTarget, toTarget, toolTarget);
        }
    }
}
