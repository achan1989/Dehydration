using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class ThinkNode_ConditionalDehydrated : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            var need = pawn.needs.TryGetNeed<Need_Water>();
            if (need != null)
            {
                return need.Dehydrated;
            }

            return false;
        }
    }
}
