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

        public static Apparel WaterInInventoryNotFull(Pawn pawn)
        {
            if (pawn.RaceProps.ToolUser)
            {
                return pawn.apparel.WornApparel.Find(ap =>
                {
                    var wc = ap.GetComp<CompWaterContainer>();
                    if (wc != null)
                    {
                        return wc.FreeSpaceLitres > 0.01;
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

        public static Thing BestWaterSpawnedFor(Pawn getter, float wantedLitres, Predicate<Thing> validator = null,
            IntVec3? near = null, bool allowPleasureDrug = true)
        {
            if (!near.HasValue)
            {
                near = getter.Position;
            }

            Predicate<Thing> baseValidator;
            if (validator == null)
            {
                baseValidator = (Thing t) =>
                {
                    return (!t.IsForbidden(getter) && t.IsSociallyProper(getter) &&
                            getter.AnimalAwareOfEx(t) &&
                            (!ShouldReserveWaterSource(t, wantedLitres) || getter.CanReserve(t, 1)));
                };
            }
            else
            {
                baseValidator = (Thing t) =>
                {
                    return (!t.IsForbidden(getter) && t.IsSociallyProper(getter) &&
                            getter.AnimalAwareOfEx(t) &&
                            (!ShouldReserveWaterSource(t, wantedLitres) || getter.CanReserve(t, 1)) &&
                            validator(t));
                };
            }

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
            // Otherwise prefer the one with the most.
            Func<Thing, int> priorityGetter = (Thing t) =>
            {
                var wc = t.TryGetComp<CompWaterContainer>();
                if (wc == null) { return -1; }
                if (wc.StoredLitres >= wantedLitres) { return int.MaxValue; }
                return (int) Math.Round(wc.StoredLitres, 0, MidpointRounding.AwayFromZero);
            };
            
            // TODO: may be able to narrow down the ThingRequestGroup.
            var allThings = ThingRequest.ForGroup(ThingRequestGroup.Everything);
            var traverse = TraverseParms.For(getter, Danger.Deadly, TraverseMode.ByPawn);
            Predicate<Thing> searchValidator = toolValidator;
            if (!getter.RaceProps.ToolUser)
            {
                searchValidator = noToolValidator;
            }
            Thing result = GenClosest.RegionwiseBFSWorker(
                near.Value, allThings, PathEndMode.ClosestTouch, traverse, searchValidator,
                priorityGetter, 9, 30, 9999f);
            return result;
        }

        public static IntVec3? BestTerrainWaterFor(Pawn getter, IntVec3? near = null)
        {
            var terrainFinder = Find.Map.GetComponent<MapCompTerrainFinder>();
            if (terrainFinder == null)
            {
                Log.Error("No MapCompTerrainFinder for terrain water search.");
                return null;
            }

            if (!near.HasValue)
            {
                near = getter.Position;
            }

            var traverse = TraverseParms.For(getter, Danger.Deadly, TraverseMode.ByPawn);
            Func<TerrainDef, bool> terrainPred = td =>
                td.defName.Equals("WaterDeep") || td.defName.Equals("WaterShallow");
            return terrainFinder.NearestTerrainOfType(near.Value, terrainPred, traverse);
        }

        public static Thing HaulingToolInInventory(Pawn pawn)
        {
            if (pawn.inventory == null) { return null; }
            return pawn.inventory.container.FirstOrDefault(thing =>
                thing.TryGetComp<CompWaterContainer>() != null);
        }

        public static Thing NearestHaulingTool(Pawn getter)
        {
            var searchThings = ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);
            var traverse = TraverseParms.For(getter, Danger.Deadly, TraverseMode.ByPawn);
            
            Predicate<Thing> validator = (Thing t) =>
                t.TryGetComp<CompWaterContainer>() != null &&
                Find.Reservations.CanReserve(getter, t, 1);
            
            Thing result = GenClosest.RegionwiseBFSWorker(
                getter.Position, searchThings, PathEndMode.ClosestTouch, traverse, validator,
                null, 0, 40, 9999f);
            return result;
        }

        public static void TransferWater(CompWaterContainer fromWc, CompWaterContainer toWc, float litres)
        {
            if (litres > toWc.FreeSpaceLitres)
            {
                Log.Error(string.Format("TransferWater() trying to put {0} litres into {1} with "
                    + "{2} litres free space.", litres, toWc.parent.Label, toWc.FreeSpaceLitres));
                litres = toWc.FreeSpaceLitres;
            }

            float transfer = fromWc.RemoveWater(litres);
            toWc.AddWater(transfer);
        }

        public static List<Thing> WaterContainersInRoom(IntVec3 position)
        {
            var containers = new List<Thing>();

            Room room = RoomQuery.RoomAt(position);
            if (room == null)
            {
                return containers;
            }

            foreach (Region region in room.Regions)
            {
                // Check how much water is available here.
                foreach (Thing thing in region.ListerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
                {
                    var wc = thing.TryGetComp<CompWaterContainer>();
                    if (wc != null)
                    {
                        containers.Add(thing);
                    }
                }
            }

            return containers;
        }
    }
}
