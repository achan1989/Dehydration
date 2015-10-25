using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class IncidentWorker_WaterLeak : IncidentWorker
    {
        private static readonly float MinForLeak = 50f;
        private static readonly float MinLeak = 30f;
        
        protected override bool StorytellerCanUseNowSub()
        {
            return BulkWaterStorageNotEmpty().Any();
        }

        private IEnumerable<Building_BulkWaterStore> BulkWaterStorageNotEmpty()
        {
            return from Building_BulkWaterStore store in Find.ListerBuildings.AllBuildingsColonistOfClass<Building_BulkWaterStore>()
            where store.GetComp<CompWaterContainer>().StoredLitres > MinForLeak
            select store;
        }

        public override bool TryExecute(IncidentParms parms)
        {
            var candidates = BulkWaterStorageNotEmpty();
            if (!candidates.Any()) { return false; }

            var target = candidates.RandomElement();
            var wc = target.GetComp<CompWaterContainer>();
            if (wc == null) { return false; }

            float severity = UnityEngine.Random.Range(0.2f, 0.8f);
            float toLeak = Math.Max(MinLeak, severity * wc.StoredLitres);
            wc.RemoveWater(toLeak);

            target.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, (int) (target.HitPoints * severity)));
            Find.LetterStack.ReceiveLetter(this.def.letterLabel, this.def.letterText, this.def.letterType,
                target.Position, null);
            return true;
        }
    }
}
