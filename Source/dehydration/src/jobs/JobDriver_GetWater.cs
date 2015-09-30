using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class JobDriver_GetWater : JobDriver
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            var need = pawn.needs.TryGetNeed<Need_Water>();
            var wc = TargetThingA.TryGetComp<CompWaterContainer>();

            var wearable = TargetThingA as Apparel;
            bool worn = (wearable != null) && (pawn.apparel.WornApparel.Contains(wearable));
            if (!worn)
            {
                // Actor not wearing a drinkable thing.
                // Reserve it and move to the thing first.
                yield return Toils_Water.ReserveWaterIfNeeded(TargetThingA, wc, need.HydrationWantedLitres);
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).
                    FailOnDespawnedOrForbidden(TargetIndex.A).FailOn(() => wc.IsEmpty);
            }

            // Now drink in little sips :P
            int sips = Toils_Water.SipsToDrink(need.HydrationWantedLitres);
            for (int i = 0; i < sips; i++)
            {
                yield return Toils_Water.SipWater(pawn, need, wc, worn);
            }
        }
    }
}
