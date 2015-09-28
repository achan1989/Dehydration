using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class Toils_Drink
    {
        public static Toil MagicDrink(Pawn drinker)
        {
            Toil toil = new Toil();
            // TODO: duration based on how much is being drunk.
            toil.defaultDuration = 500;
            toil.tickAction = delegate
            {
                toil.actor.GainComfortFromCellIfPossible();
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            // TODO: drinking animation/effect.
            // toil.WithEffect(() => toil.actor.jobs.curJob.targetA.Thing.def.ingestible.eatEffect, TargetIndex.A);
            // TODO: drinking sound.
            /*
            toil.WithSustainer(delegate
            {
                if (!chewer.RaceProps.Humanlike)
                {
                    return null;
                }
                return toil.actor.CurJob.targetA.Thing.def.ingestible.soundEat;
            });
            */
            return toil;
        }

        public static Toil FinalizeDrink(Pawn drinker, TargetIndex drinkableInd)
        {
            Toil toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Thing thing = actor.jobs.curJob.GetTarget(drinkableInd).Thing;

                var need = drinker.needs.TryGetNeed<Need_Water>();
                if (need == null)
                {
                    throw new InvalidOperationException("Drinker drinking, but has no Need_Water.");
                }

                var drinkable = thing.TryGetComp<CompWaterContainer>();
                if (drinkable == null)
                {
                    throw new InvalidOperationException("Drinker drinking, but target thing has no CompDrinkable.");
                }

                // TODO: drinkable.DrunkBy(drinker, need.HydrationWantedLiters);
            };
            return toil;
        }
    }
}
