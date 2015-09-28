using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public static class WaterUtility
    {
        public static Apparel WaterInInventory(Pawn pawn)
        {
            if (pawn.RaceProps.ToolUser)
            {
                return pawn.apparel.WornApparel.Find(ap =>
                {
                    var wc = ap.GetComp<CompWaterContainer>();
                    if (wc != null)
                    {
                        return wc.StoredLitres > 0;
                    }
                    return false;
                });
            }

            return null;
        }
    }
}
