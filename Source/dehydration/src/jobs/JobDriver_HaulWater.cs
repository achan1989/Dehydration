using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class JobDriver_HaulWater : JobDriver
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // TargetA = from
            // TargetB = to
            // TargetC = tool

            Thing haulTo = TargetThingB;
            var toWc = haulTo.TryGetComp<CompWaterContainer>();

            var tool = TargetC.Thing;
            var toolWc = tool.TryGetComp<CompWaterContainer>();

            // Grab the water carrying tool if it's not in our inventory.
            if (!this.pawn.inventory.container.Contains(tool))
            {
                yield return Toils_Reserve.Reserve(TargetIndex.C);
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.ClosestTouch).FailOnDespawnedOrForbidden(TargetIndex.C);
                yield return PickUpTool(TargetIndex.C);
            }
            // If the tool is in our inventory, carry it.
            else
            {
                yield return Toils_General.CarryThingFromInventory(tool);
            }

            // Fill it if necessary.
            if (TargetA.IsValid)
            {
                float collectLitres = toWc.FreeSpaceLitres - toolWc.StoredLitres;
                if (collectLitres > toolWc.FreeSpaceLitres)
                {
                    collectLitres = toolWc.FreeSpaceLitres;
                }

                if (collectLitres > 0)
                {
                    CompWaterContainer fromWc;

                    // Go to Thing water source.
                    if (TargetA.HasThing)
                    {
                        fromWc = TargetThingA.TryGetComp<CompWaterContainer>();
                        yield return Toils_Water.ReserveWaterIfNeeded(TargetThingA, fromWc, collectLitres);
                        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).
                            FailOnDespawnedOrForbidden(TargetIndex.A).FailOn(() => fromWc.IsEmpty);
                    }
                    // Go to Terrain water source.
                    else
                    {
                        // This assumes that all terrain water acts as an infinite source.
                        fromWc = new CompWaterSource();
                        fromWc.Initialize(new CompPropertiesWaterSource() { unlimitedSource = true });

                        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
                    }

                    // (Partially) fill water carrying tool.
                    yield return Toils_Water.TransferWater(fromWc, toolWc, collectLitres);
                }
            }
            
            // Carry to destination and empty.
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrForbidden(TargetIndex.B);
            yield return Toils_General.FaceThing(TargetIndex.B);
            yield return Toils_Water.TransferWater(toolWc, haulTo.TryGetComp<CompWaterContainer>());

            // Put tool back in inventory.  Speeds up multiple hauling trips and stops other colonists
            // trying to grab the tool all the time.
            yield return Verse.AI.Toils_General.PutCarriedThingInInventory();
        }

        private Toil DropToolIfNeeded(Thing tool)
        {
            var toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Thing droppedTool;
                if (actor.inventory != null && actor.inventory.container.Contains(tool))
                {
                    if (!actor.inventory.container.TryDrop(tool, actor.Position, ThingPlaceMode.Near, 1, out droppedTool))
                    {
                        Log.Error(actor + " could not drop a water carrying tool.");
                        actor.jobs.EndCurrentJob(JobCondition.Errored);
                        return;
                    }
                    if (droppedTool != tool)
                    {
                        Log.Error("Dropped tool != target tool");
                        actor.CurJob.SetTarget(TargetIndex.C, droppedTool);
                    }
                }
            };
            return toil;
        }

        private Toil PickUpTool(TargetIndex toolInd)
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
    }
}
