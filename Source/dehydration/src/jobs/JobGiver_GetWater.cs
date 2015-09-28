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

            var wornWater = WaterUtility.WaterInInventory(pawn);
            if (wornWater != null)
            {
                return new Job(DefDatabase<JobDef>.GetNamed("Drink"), wornWater);
            }

            return null;
            // TODO: implement more sources.
        }
    }
}
