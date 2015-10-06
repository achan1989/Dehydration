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
            CompWaterContainer wc;
            bool worn = false;

            if (TargetA.HasThing)
            {
                wc = TargetThingA.TryGetComp<CompWaterContainer>();
                var wearable = TargetThingA as Apparel;
                worn = (wearable != null) && (pawn.apparel.WornApparel.Contains(wearable));
                if (!worn)
                {
                    // Actor not wearing a drinkable Thing.
                    // Reserve it if necessary and move to the Thing.
                    yield return Toils_Water.ReserveWaterIfNeeded(TargetThingA, wc, need.HydrationWantedLitres);
                    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).
                        FailOnDespawnedOrForbidden(TargetIndex.A).FailOn(() => wc.IsEmpty);
                }
            }
            else
            {
                // We're targeting a piece of terrain that has some water.
                // This assumes that all terrain water acts as an infinite source.
                wc = new CompWaterSource();
                wc.Initialize(new CompPropertiesWaterSource() {unlimitedSource=true});

                yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
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
