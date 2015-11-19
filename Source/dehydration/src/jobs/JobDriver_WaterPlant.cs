using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class JobDriver_WaterPlant : JobDriver
    {
        public static readonly float waterPerPlant = 0.5f;


        protected override IEnumerable<Toil> MakeNewToils()
        {
            // TargetA = from
            // TargetB = to plant
            // TargetC = tool

            Plant plant = TargetThingB as Plant;

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
                // If TargetA has been provided, the tool is empty.
                // Try to fill as much as possible.
                float collectLitres = toolWc.FreeSpaceLitres;
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
                yield return Toils_Water.TransferWater(fromWc, toolWc, collectLitres, waterPerPlant);
            }

            // Carry to plant and water it.
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrForbidden(TargetIndex.B);
            yield return Toils_General.FaceThing(TargetIndex.B);
            yield return Toils_Water.WaterPlant(plant, toolWc, waterPerPlant);
            
            // Put tool back in inventory.  Speeds up multiple hauling trips and stops other colonists
            // trying to grab the tool all the time.
            yield return Verse.AI.Toils_General.PutCarriedThingInInventory();
        }
    }
}
