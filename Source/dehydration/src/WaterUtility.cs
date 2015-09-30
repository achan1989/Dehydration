using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

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

        public static bool ShouldReserveWaterSource(Thing thing, float wantedLitres)
        {
            var wc = thing.TryGetComp<CompWaterContainer>();
            return (wc != null && wc.StoredLitres <= wantedLitres);
        }

        public static Thing BestWaterSpawnedFor(Pawn getter, bool allowPleasureDrug = true)
        {
            float wantedLitres = getter.needs.TryGetNeed<Need_Water>().HydrationWantedLitres;

            Predicate<Thing> baseValidator = (Thing t) =>
            {
                return (!t.IsForbidden(getter) && t.IsSociallyProper(getter) &&
                        getter.AnimalAwareOf(t) &&
                        (!ShouldReserveWaterSource(t, wantedLitres) || getter.CanReserve(t, 1)));
            };

            // Can drink from anything that is a non-empty water container.
            Predicate<Thing> toolValidator = (Thing t) =>
            {
                if (!baseValidator(t)) { return false; }
                var wc = t.TryGetComp<CompWaterContainer>();
                return (wc != null && wc.StoredLitres > 0);
            };

            // Can drink from anything that is a non-empty water container, but only if
            // it doesn't require tool use.
            Predicate<Thing> noToolValidator = (Thing t) =>
            {
                if (!baseValidator(t)) { return false; }
                var wc = t.TryGetComp<CompWaterContainer>();
                return (wc != null && wc.StoredLitres > 0 && !wc.props.needsToolUser);
            };

            // Prefer a water source that has at least as much as we want.
            // Otherwise make do with less.
            Func<Thing, int> priorityGetter = (Thing t) =>
            {
                var wc = t.TryGetComp<CompWaterContainer>();
                if (wc == null) { return -1; }
                if (wc.StoredLitres >= wantedLitres) { return 1; }
                return 0;
            };
            
            var allThings = ThingRequest.ForGroup(ThingRequestGroup.Everything);
            var traverse = TraverseParms.For(getter, Danger.Deadly, TraverseMode.ByPawn);
            Predicate<Thing> validator = toolValidator;
            if (!getter.RaceProps.ToolUser)
            {
                validator = noToolValidator;
            }
            Thing result = GenClosest.RegionwiseBFSWorker(
                getter.Position, allThings, PathEndMode.ClosestTouch, traverse, validator,
                priorityGetter, 0, 30, 9999f);
            return result;
        }
    }
}
