using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class WorkGiver_Warden : RimWorld.WorkGiver_Warden
    {
        private readonly List<JobDef> CriticalJobDefs;
        private readonly WorkGiverDef HaulWaterWorkGiverDef;
        private readonly WorkGiverDef WaterPatientWorkGiverDef;

        public WorkGiver_Warden() : base()
        {
            CriticalJobDefs = new List<JobDef>()
            {
                DefDatabase<JobDef>.GetNamedSilentFail("EscortPrisonerToBed"),
                DefDatabase<JobDef>.GetNamedSilentFail("TakeWoundedPrisonerToBed")
            };

            HaulWaterWorkGiverDef = DefDatabase<WorkGiverDef>.GetNamed("Dehydration_HaulWater");
            WaterPatientWorkGiverDef = DefDatabase<WorkGiverDef>.GetNamed("Dehydration_DoctorWater");
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Job baseJob = base.JobOnThing(pawn, t);
            
            if (baseJob == null || !CriticalJobDefs.Contains(baseJob.def))
            {
                Job waterPrisonerJob = MakeWaterPrisonerJob(pawn, t);
                if (waterPrisonerJob != null)
                {
                    return waterPrisonerJob;
                }
            }

            // There is no water-prisoner job, return the base job.
            return baseJob;
        }

        private Job MakeWaterPrisonerJob(Pawn warden, Thing t)
        {
            Pawn prisoner = t as Pawn;
            if (prisoner == null || !prisoner.IsPrisonerOfColony || !prisoner.guest.PrisonerIsSecure ||
                prisoner.holder != null || (prisoner.Broken && prisoner.BrokenStateDef.isAggro) ||
                !warden.CanReserveAndReach(prisoner, PathEndMode.OnCell, warden.NormalMaxDanger(), 1))
            {
                return null;
            }

            var waterNeed = prisoner.needs.TryGetNeed<Need_Water>();
            if (waterNeed != null && prisoner.guest.ShouldBeBroughtFood && waterNeed.NeedWaterSoon)
            {
                if (prisoner.Downed || (prisoner.InBed() && prisoner.CurrentBed().Medical))
                {
                    if (warden.CanReserve(prisoner, 1))
                    {
                        var waterPatientWorkGiver = WaterPatientWorkGiverDef.Worker as WorkGiver_WaterPatient;
                        if (waterPatientWorkGiver != null)
                        {
                            // Use the existing WorkGiver for doctors giving water to patients.
                            return waterPatientWorkGiver.JobOnThing(warden, prisoner);
                        }
                    }
                }
                else if (!WaterAvailableInRoomTo(prisoner))
                {
                    Thing haulTo = BestWaterContainerInPrison(prisoner);
                    if (haulTo != null)
                    {
                        var haulWorkGiver = HaulWaterWorkGiverDef.Worker as WorkGiver_HaulWater;
                        if (haulWorkGiver != null)
                        {
                            // Use the existing WorkGiver for water hauling.
                            // Note that the normal check for social properness happens in
                            // HasJobOnThing(), so this warden bypasses the normal inability to
                            // haul to containers inside a prison.
                            return haulWorkGiver.JobOnThing(warden, haulTo);
                        }
                    }
                }
            }

            return null;
        }

        private bool WaterAvailableInRoomTo(Pawn prisoner)
        {
            if (WaterUtility.WaterInInventory(prisoner) != null)
            {
                return true;
            }

            float wantedLitres = 0f;
            float availableLitres = 0f;

            Room room = RoomQuery.RoomAt(prisoner.Position);
            if (room == null)
            {
                return false;
            }

            foreach (Region region in room.Regions)
            {
                // Check how much water is available here.
                foreach (Thing thing in region.ListerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
                {
                    var wc = thing.TryGetComp<CompWaterContainer>();
                    if (wc != null)
                    {
                        availableLitres += wc.StoredLitres;
                    }
                }

                // How much do prisoners want?
                foreach (Pawn pawn in region.ListerThings.ThingsInGroup(ThingRequestGroup.Pawn))
                {
                    var waterNeed = pawn.needs.TryGetNeed<Need_Water>();
                    if (pawn.IsPrisonerOfColony && waterNeed.NeedWaterSoon && (WaterUtility.WaterInInventory(pawn) == null))
                    {
                        wantedLitres += waterNeed.HydrationWantedLitres;
                    }
                }
            }

            return availableLitres >= wantedLitres;
        }

        private Thing BestWaterContainerInPrison(Pawn prisoner)
        {
            var prisonContainers = WaterUtility.WaterContainersInRoom(prisoner.Position);
            return prisonContainers.OrderByDescending(container =>
                container.TryGetComp<CompWaterContainer>().FreeSpaceLitres
            ).FirstOrDefault();
        }
    }
}
