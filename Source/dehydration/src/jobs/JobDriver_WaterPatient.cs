using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class JobDriver_WaterPatient : JobDriver
    {
        // TODO: very similar to JobDriver_HaulWater, could split out a common subset.
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // TargetA = from
            // TargetB = to
            // TargetC = tool

            Pawn patient = TargetThingB as Pawn;
            var patientNeed = patient.needs.TryGetNeed<Need_Water>();

            var tool = TargetC.Thing;
            var toolWc = tool.TryGetComp<CompWaterContainer>();

            // Grab the water carrying tool if it's not in our inventory.
            if (!this.pawn.inventory.container.Contains(tool))
            {
                yield return Toils_Reserve.Reserve(TargetIndex.C);
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.ClosestTouch).FailOnDespawnedOrForbidden(TargetIndex.C);
                yield return Toils_General.PickUpTool(TargetIndex.C);
            }
            // If the tool is in our inventory, carry it.
            else
            {
                yield return Toils_General.CarryThingFromInventory(tool);
            }

            // Fill it if necessary.
            if (TargetA.IsValid)
            {
                float collectLitres = patientNeed.HydrationWantedLitres - toolWc.StoredLitres;
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

            // Carry to patient and give.
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrForbidden(TargetIndex.B);
            yield return Toils_General.FaceThing(TargetIndex.B);
            int sips = Toils_Water.SipsToDrink(patientNeed.HydrationWantedLitres);
            for (int i = 0; i < sips; i++)
            {
                yield return Toils_Water.SipWater(patient, patientNeed, toolWc, false);
            }

            // Put tool back in inventory.  Speeds up multiple hauling trips and stops other colonists
            // trying to grab the tool all the time.
            yield return Verse.AI.Toils_General.PutCarriedThingInInventory();
        }
    }
}
