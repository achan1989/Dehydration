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

        public WorkGiver_Warden() : base()
        {
            CriticalJobDefs = new List<JobDef>()
            {
                DefDatabase<JobDef>.GetNamedSilentFail("EscortPrisonerToBed"),
                DefDatabase<JobDef>.GetNamedSilentFail("TakeWoundedPrisonerToBed")
            };
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
                        // TODO: look for suitable water source, and give water to patient.
                        // return new Job(blah)
                    }
                }
                else if (!WaterAvailableInRoomTo(prisoner))
                {
                    // TODO: look for suitable water container in room.
                    // TODO: haul water to container.
                }
            }

            return null;
        }

        private bool WaterAvailableInRoomTo(Pawn pawn)
        {
            return false;
        }
    }
}
