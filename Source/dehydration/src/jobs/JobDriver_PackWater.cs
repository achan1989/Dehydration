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
            var toWC = fillInto.TryGetComp<CompWaterContainer>();
            float packLitres = toWC.FreeSpaceLitres;
            CompWaterContainer fromWC;

            if (TargetA.HasThing)
            {
                // Packing from a Thing.
                var fillFrom = TargetThingA;
                fromWC = fillFrom.TryGetComp<CompWaterContainer>();

                // Reserve and go to the source.
                yield return Toils_Water.ReserveWaterIfNeeded(fillFrom, fromWC, packLitres);
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).
                    FailOnDespawnedOrForbidden(TargetIndex.A).FailOn(() => fromWC.IsEmpty);
            }
            else
            {
                // Packing from a bit of terrain.
                // This assumes that all terrain water acts as an infinite source.
                fromWC = new CompWaterSource();
                fromWC.Initialize(new CompPropertiesWaterSource() { unlimitedSource = true });

                yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
            }
            
            // Get water from the source.
            yield return Toils_Water.TransferWater(fromWC, toWC, packLitres);
        }
    }
}
