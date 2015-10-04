using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace achan1989.dehydration
{
    public class SectionTerrain
    {
        public CellRect Rect { get; private set; }
        public HashSet<TerrainDef> Terrains { get; private set; }

        public SectionTerrain(CellRect rect)
        {
            Rect = rect;
            Terrains = new HashSet<TerrainDef>();
        }

        public void TerrainChanged()
        {
            // Maintain a collection of the unique terrain types in this section.
            Terrains.Clear();
            var tg = Find.TerrainGrid;
            foreach (var cellVect in Rect.Cells)
	        {
                Terrains.Add(tg.TerrainAt(cellVect));
	        }
        }
    }


    public class MapCompTerrainFinder : MapComponent
    {
        // CellRect from core, gives all cells within a rectangular area?
        // Use of Section inside core?  By MapDrawer
        // region.extentsClose is the minimum bounding box around all region cells.
        // region.extentsLimit is basically just the whole 12x12 region.
        // Use CellFinder.TryFindClosestRegionWith()

        // At the moment this class just maintains a bit list of all sections, and
        // search involves iterating through all of them.  Could go to a full quadtree
        // data structure if performance becomes an issue.

        private Dictionary<CellRect, SectionTerrain> SectionTerrains;

        public MapCompTerrainFinder()
        {
            SectionTerrains = new Dictionary<CellRect, SectionTerrain>();
        }

        public void TerrainChanged(CellRect rect)
        {
            SectionTerrain st;
            if (!SectionTerrains.TryGetValue(rect, out st))
            {
                st = new SectionTerrain(rect);
                SectionTerrains.Add(rect, st);
            }

            st.TerrainChanged();
        }

        /// <summary>
        /// Check if a Region may contain terrain matching a given predicate.
        /// </summary>
        /// <remarks>
        /// "May contain" because the terrain type cache does not line up neatly with any Regions.
        /// </remarks>
        /// <param name="region">The Region to test.</param>
        /// <param name="predicate">Specifies the terrain of interest.</param>
        /// <returns>False if the Region definitely does not contain the terrain, else true.</returns>
        public bool RegionMayContain(Region region, Func<TerrainDef, bool> predicate)
        {
            // extentsClose is the minimum bounding box around all region cells.
            var regRect = region.extentsClose;
            // Sections don't align nicely with regions; a region could overlap up to
            // four sections. Return true if any of these sections contains the TerrainDef
            // we're interested in.
            var candidates = SectionTerrains.Values.Where(st => st.Rect.Overlaps(regRect));
            return candidates.Any(st => st.Terrains.Any(predicate));
        }

        public IntVec3? NearestTerrainOfType(IntVec3 root, Func<TerrainDef, bool> terrainPred,
            TraverseParms traverseParams, int minRegions=9, int maxRegions=0, float maxDistance=9999f)
        {
            IntVec3? nearest = null;

            Region validRegionAt = Find.RegionGrid.GetValidRegionAt(root);
            if (validRegionAt == null)
            {
                return nearest;
            }

            maxRegions = (maxRegions == 0 ? 50 : maxRegions);
            
            float maxDistSquared = maxDistance * maxDistance;
            float closestDistSquared = 99999f;
            int regionsSeen = 0;
            RegionEntryPredicate entryCondition = r =>
                r.Allows(traverseParams, false) &&
                (maxDistance > 5000f || r.extentsClose.ClosestDistSquaredTo(root) < maxDistSquared);

            RegionProcessor regionProc = delegate(Region r)
            {
                if (!r.Allows(traverseParams, true)) { return false; }
                // If a region definitely doesn't have the correct terrain, skip it.
                if (!RegionMayContain(r, terrainPred)) { return false; }

                var tg = Find.TerrainGrid;
                foreach (var cellVec in r.Cells)
                {
                    float cellDistSquared = (cellVec - root).LengthHorizontalSquared;
                    var cellTerrain = tg.TerrainAt(cellVec);
                    if ((cellDistSquared < closestDistSquared && cellDistSquared < maxDistSquared)
                        && (terrainPred == null || terrainPred(cellTerrain)))
                    {
                        nearest = cellVec;
                        closestDistSquared = cellDistSquared;
                    }
                }

                regionsSeen++;
                return regionsSeen > minRegions && nearest != null;
            };

            RegionTraverser.BreadthFirstTraverse(validRegionAt, entryCondition, regionProc, maxRegions);
            return nearest;
        }
    }

    /// <summary>
    /// Hooks into the SectionLayer system to receive notifications when
    /// terrain is modified.
    /// </summary>
    public class SectionLayer_TerrainFinder : SectionLayer
    {
        public SectionLayer_TerrainFinder(Section section) : base(section)
        {
            this.relevantChangeTypes = MapMeshFlag.Terrain;
        }

        /// <summary>
        /// Called when the terrain in a section is changed.
        /// </summary>
        public override void Regenerate()
        {
            // Pass the notification to the MapCompTerrainFinder.
            var tf = Find.Map.GetComponent<MapCompTerrainFinder>();
            if (tf == null)
            {
                return;
            }

            tf.TerrainChanged(section.CellRect);
        }
    }

    public static class CellRectExtensions
    {
        public static bool Overlaps(this CellRect rect, CellRect other)
        {
            if (rect.maxX <= other.minX) return false;
            if (rect.minX >= other.maxX) return false;
            if (rect.maxZ <= other.minZ) return false;
            if (rect.minZ >= other.maxZ) return false;
            return true;
        }
    }
}
