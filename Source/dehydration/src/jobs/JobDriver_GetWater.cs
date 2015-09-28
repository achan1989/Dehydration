﻿using System;
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
        private static readonly float sipLitres = 0.25f;
        private static readonly int sipTicks = 60;
        private static readonly SoundDef drinkSound = DefDatabase<SoundDef>.GetNamed("Ingest_Beer");

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var need = pawn.needs.TryGetNeed<Need_Water>();
            var wc = TargetThingA.TryGetComp<CompWaterContainer>();

            var wearable = TargetThingA as Apparel;
            bool worn = (wearable != null) && (pawn.apparel.WornApparel.Contains(wearable));
            if (!worn)
            {
                Log.Message("Actor not wearing drinkable.");
                // Actor not wearing a drinkable thing.
                // Reserve it and move to the thing first.
                yield return ReserveWaterIfNeeded(TargetThingA, wc, need.HydrationWantedLitres);
                Log.Message("Generating GotoThing toil.");
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).
                    FailOnDespawnedOrForbidden(TargetIndex.A).FailOn(() => wc.IsEmpty);
            }

            // Now drink in little sips :P
            int sips = (int) Math.Ceiling(need.HydrationWantedLitres / sipLitres);
            for (int i = 0; i < sips; i++)
            {
                yield return SipWater(pawn, need, wc, worn);
            }
            Log.Message(string.Format("Made {0} SipWater toils.", sips));
        }

        private static Toil ReserveWaterIfNeeded(Thing thing, CompWaterContainer wc, float wantedLitres)
        {
            Log.Message("Generating ReserveWater toil.");
            Toil resWater = new Toil();
            resWater.defaultCompleteMode = ToilCompleteMode.Instant;
            resWater.initAction = delegate
            {
                // Reserve the thing if we'll empty it of water.
                // TODO: when do we unreserve??
                /*
                if (thing != actor.carrier.CarriedThing && Find.Reservations.FirstReserverOf(thing, actor.Faction) == actor)
                {
                    Find.Reservations.Release(thing, actor);
                } */
                Log.Message("Running ReserveWater toil.");
                if (wc.StoredLitres <= wantedLitres)
                {
                    if (!thing.SpawnedInWorld || !Find.Reservations.CanReserve(resWater.actor, thing, 1))
                    {
                        Log.Message("ReserveWater toil can't reserve.");
                        resWater.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Log.Message("ReserveWater toil is reserving.");
                    Find.Reservations.Reserve(resWater.actor, thing, 1);
                }
            };
            return resWater;
        }

        private static Toil SipWater(Pawn drinker, Need_Water pawnNeed, CompWaterContainer waterContainer, bool wornByActor)
        {
            Log.Message("Generating SipWater toil");
            var toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 60;
            toil.initAction = delegate
            {
                Log.Message("Running SipWater toil.");
                float wantDrink = Math.Min(pawnNeed.HydrationWantedLitres, sipLitres);
                float didDrink = pawnNeed.DrinkFrom(waterContainer, wantDrink);
                Log.Message(string.Format("Drank {0}L.", didDrink));
            };
            toil.tickAction = delegate
            {
                Log.Message("SipWater tick");
            };
            toil.FailOnForbidden(TargetIndex.A);
            toil.FailOn(() => waterContainer.IsEmpty);
            if (!wornByActor)
            {
                toil.FailOnDespawned(TargetIndex.A);
            }
            // TODO: toil.WithEffect();
            toil.WithSustainer(delegate
            {
                if (drinker.RaceProps.Humanlike)
                {
                    return drinkSound;
                }
                return null;
            });
            Log.Message("Generated SipWater toil");
            return toil;
        }
    }
}
