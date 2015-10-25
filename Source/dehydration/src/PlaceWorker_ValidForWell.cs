using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class PlaceWorker_ValidForWell : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot)
        {
            foreach (IntVec3 current in GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size))
            {
                var terrain = Find.TerrainGrid.TerrainAt(current);
                if (terrain.defName.Equals("Ice")) { return "Cannot be placed on ice."; }
            }
            return true;
        }
    }
}
