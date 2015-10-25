using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace achan1989.dehydration
{
    public class Building_BulkWaterStore : Building
    {
        private static readonly Vector2 BarSize = new Vector2(1.3f, 0.4f);
        private static readonly Material BarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.69f, 0.9f));
        private static readonly Material BarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));

        public override void Draw()
        {
            base.Draw();

            var wc = GetComp<CompWaterContainer>();
            if (wc == null) { return; }
            
            GenDraw.FillableBarRequest bar = default(GenDraw.FillableBarRequest);
            bar.center = this.DrawPos + Vector3.up * 0.1f;
            bar.size = Building_BulkWaterStore.BarSize;
            bar.fillPercent = wc.StoredLitres / wc.CapacityLitres;
            bar.filledMat = Building_BulkWaterStore.BarFilledMat;
            bar.unfilledMat = Building_BulkWaterStore.BarUnfilledMat;
            bar.margin = 0.15f;
            bar.rotation = base.Rotation;
            GenDraw.DrawFillableBar(bar);
        }
    }
}
