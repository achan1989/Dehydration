using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class ThoughtWorker_Water : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var need = p.needs.TryGetNeed<Need_Water>();
            if (need == null)
            {
                return ThoughtState.Inactive;
            }

            switch (need.CurCategory)
            {
                case HydratedCategory.Hydrated:
                    return ThoughtState.Inactive;
                case HydratedCategory.Thirsty:
                    return ThoughtState.ActiveAtStage(0);
                case HydratedCategory.UrgentlyThirsty:
                    return ThoughtState.ActiveAtStage(1);
                case HydratedCategory.Dehydrated:
                    return ThoughtState.ActiveAtStage(2);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
