using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class ThinkNode_ConditionalUrgentlyThirsty : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            var need = pawn.needs.TryGetNeed<Need_Water>();
            if (need != null)
            {
                var curCategory = need.CurCategory;
                return curCategory == HydratedCategory.UrgentlyThirsty ||
                       curCategory == HydratedCategory.Dehydrated;
            }

            return false;
        }
    }
}
