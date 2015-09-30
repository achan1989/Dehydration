﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public static class Toils_Water
    {
        private static readonly float sipLitres = 0.25f;
        private static readonly int sipTicks = 60;
        private static readonly string drinkSoundName = "Ingest_Beer";


        public static Toil ReserveWaterIfNeeded(Thing thing, CompWaterContainer wc, float wantedLitres)
        {
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
                if (wc.StoredLitres <= wantedLitres)
                {
                    if (!thing.SpawnedInWorld || !Find.Reservations.CanReserve(resWater.actor, thing, 1))
                    {
                        Log.Warning("ReserveWater toil can't complete.");
                        resWater.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Log.Warning(string.Format("ReserveWater toil is reserving ({0} wanted, {1} stored in {2}).", wantedLitres, wc.StoredLitres, thing.Label));
                    Find.Reservations.Reserve(resWater.actor, thing, 1);
                }
            };
            return resWater;
        }

        public static int SipsToDrink(float litres)
        {
            return (int)Math.Ceiling(litres / sipLitres);
        }

        public static Toil SipWater(Pawn drinker, Need_Water pawnNeed, CompWaterContainer waterContainer, bool wornByActor)
        {
            var toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = sipTicks;
            toil.initAction = delegate
            {
                float wantDrink = Math.Min(pawnNeed.HydrationWantedLitres, sipLitres);
                float didDrink = pawnNeed.DrinkFrom(waterContainer, wantDrink);
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
                    return DefDatabase<SoundDef>.GetNamed(drinkSoundName);
                }
                return null;
            });
            return toil;
        }
    }
}
