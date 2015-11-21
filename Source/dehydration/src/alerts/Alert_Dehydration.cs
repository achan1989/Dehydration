using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class Alert_Dehydration : Alert_Medium
    {
        public static readonly string explanation = "These colonists are dehydrated:\n\n{0}\n" +
            "Get them some water. You'll need a nearby water source like a pond or well.";


        private IEnumerable<Pawn> DehydratedColonists
        {
            get
            {
                return from p in Find.ListerPawns.FreeColonistsSpawned
                where p.needs.TryGetNeed<Need_Water>() != null &&
                    p.needs.TryGetNeed<Need_Water>().Dehydrated
                select p;
            }
        }

        public override AlertReport Report
        {
            get { return AlertReport.CulpritIs(DehydratedColonists.FirstOrDefault()); }
        }

        public Alert_Dehydration()
        {
            this.baseLabel = "Dehydration";
        }

        public override string FullExplanation
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (Pawn current in DehydratedColonists)
                {
                    stringBuilder.AppendLine("    " + current.NameStringShort);
                }
                return string.Format(explanation, stringBuilder.ToString());
            }
        }
    }
}
