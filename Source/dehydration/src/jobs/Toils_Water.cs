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
                /*
                if (thing != actor.carrier.CarriedThing && Find.Reservations.FirstReserverOf(thing, actor.Faction) == actor)
                {
                    Find.Reservations.Release(thing, actor);
                } */
                if (wc.StoredLitres <= wantedLitres)
                {
                    if (!thing.SpawnedInWorld || !Find.Reservations.CanReserve(resWater.actor, thing, 1))
                    {
                        resWater.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
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
                bool fromThing = toil.actor.jobs.curJob.GetTarget(TargetIndex.A).HasThing;
                if (fromThing)
                {
                    toil.FailOnForbidden(TargetIndex.A);
                    if (!wornByActor)
                    {
                        toil.FailOnDespawned(TargetIndex.A);
                    }
                }

                float wantDrink = Math.Min(pawnNeed.HydrationWantedLitres, sipLitres);
                float didDrink = pawnNeed.DrinkFrom(waterContainer, wantDrink);
            };
            toil.FailOn(() => waterContainer.IsEmpty);
            
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

        public static Toil TransferWater(CompWaterContainer fromWC, CompWaterContainer toWC,
            float maxLitres = -1f, float minInToWC = 0f)
        {
            var toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 120;
            toil.initAction = delegate
            {
                if (fromWC == null || toWC == null)
                {
                    Log.Error("TransferWater has a null CompWaterContainer.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                    return;
                }

                if (maxLitres < 0)
                {
                    maxLitres = Math.Min(fromWC.StoredLitres, toWC.FreeSpaceLitres);
                }

                WaterUtility.TransferWater(fromWC, toWC, maxLitres);

                if (toWC.StoredLitres < minInToWC)
                {
                    toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
            };
            return toil;
        }

        public static Toil WaterPlant(Plant plant, CompWaterContainer fromWC, float waterLitres)
        {
            var toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 180;
            toil.initAction = delegate
            {
                if (fromWC == null || plant == null)
                {
                    Log.Error("WaterPlant has a null parameter.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                    return;
                }

                if (fromWC.StoredLitres < waterLitres)
                {
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                }

                fromWC.RemoveWater(waterLitres);
                plant.Watered();
            };
            return toil;
        }
    }
}
