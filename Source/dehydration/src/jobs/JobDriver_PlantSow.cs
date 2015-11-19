using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class JobDriver_PlantSow : RimWorld.JobDriver_PlantSow
    {
        protected override IEnumerable<Verse.AI.Toil> MakeNewToils()
        {
            var baseToils = base.MakeNewToils().ToList();
            
            // Change the original plant spawning toil to call the plant's Sown() function.
            baseToils[2].initAction = delegate
            {
                Plant plant = (Plant) GenSpawn.Spawn(this.CurJob.plantDefToSow, this.TargetLocA);
                plant.Sown();
                this.TargetThingA = plant;
            };

            return baseToils;
        }
    }
}
