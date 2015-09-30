using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class JobDriver_PackWater : JobDriver
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var fillInto = TargetThingB as Apparel;
            var fillFrom = TargetThingA;
            var fromWC = fillFrom.TryGetComp<CompWaterContainer>();
            float packLitres = fillInto.TryGetComp<CompWaterContainer>().FreeSpace;

            // Reserve and go to the source.
            yield return Toils_Water.ReserveWaterIfNeeded(fillFrom, fromWC, packLitres);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).
                    FailOnDespawnedOrForbidden(TargetIndex.A).FailOn(() => fromWC.IsEmpty);
            // Get water from the source.
            yield return Toils_Water.TransferWater(fillFrom, fillInto, packLitres);
        }
    }
}
