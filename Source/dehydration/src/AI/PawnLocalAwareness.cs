using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public static class PawnLocalAwareness
    {
        /// <summary>
        /// Better version of base AnimalAwareOf that works for Things with passability XML
        /// set to Impassable. 
        /// </summary>
        public static bool AnimalAwareOfEx(this Pawn p, Thing t)
        {
            if (p.RaceProps.ToolUser || p.Faction != null)
            {
                return true;
            }

            var pRoom = p.GetRoom();
            List<Room> thingRooms = new List<Room>();
            if (t.def.passability != Traversability.Impassable)
            {
                thingRooms.Add(t.GetRoom());
            }
            else
            {
                // Find the rooms that are adjacent to the impassable Thing.
                foreach (IntVec3 pos in GenAdj.CellsAdjacent8Way(t))
                {
                    var room = pos.GetRoom();
                    if (room == null) continue;
                    thingRooms.Add(room);
                }
            }

            return ((p.Position - t.Position).LengthHorizontalSquared <= 900f &&
                thingRooms.Any(tRoom => tRoom == pRoom) &&
                GenSight.LineOfSight(p.Position, t.Position, false));
        }
    }
}
