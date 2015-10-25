using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public static class Toils_General
    {
        public static Toil CarryThingFromInventory(Thing thing)
        {
            var toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                if (actor.inventory != null && actor.inventory.container.Contains(thing))
                {
                    actor.inventory.container.TransferToContainer(thing, actor.carrier.container, 1);
                }
                else
                {
                    Log.Error(string.Format("{0} can't transfer {1} from inventory to hands.", actor, thing));
                    actor.jobs.EndCurrentJob(JobCondition.Errored);
                    return;
                }
            };
            return toil;
        }

        public static Toil PickUpTool(TargetIndex toolInd)
        {
            Toil toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Thing tool = curJob.GetTarget(toolInd).Thing;
                actor.carrier.TryStartCarry(tool);
                if (tool != actor.carrier.CarriedThing && Find.Reservations.FirstReserverOf(tool, actor.Faction) == actor)
                {
                    Log.Error("Carried tool != target tool");
                    Find.Reservations.Release(tool, actor);
                }
                curJob.targetC = actor.carrier.CarriedThing;
            };
            return toil;
        }

        public static Toil FaceThing(TargetIndex thingInd)
        {
            var toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                actor.drawer.rotator.FaceCell(actor.jobs.curJob.GetTarget(thingInd).Cell);
            };
            return toil;
        }
    }
}
