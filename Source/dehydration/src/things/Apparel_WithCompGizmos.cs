using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class Apparel_WithCompGizmos : Apparel
    {
        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            // GetWornGizmos does nothing by default.  Return the apparel's comp gizmos.
            return base.GetGizmos();
        }
    }
}
