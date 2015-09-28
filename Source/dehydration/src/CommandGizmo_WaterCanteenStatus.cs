using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace achan1989.dehydration
{
    /// <summary>
    /// Gizmo to show the water level in a CompWaterContainer.
    /// Is technically a Command but uses none of it's functionality, necessary to attach to a ThingComp.
    /// </summary>
    public class CommandGizmo_WaterContainerStatus : Command
    {
        private static readonly int windowID = "CommandGizmo_WaterCanteenStatus".GetHashCode();
        private static readonly Texture2D FullTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.06f, 0.63f, 0.95f));
        private static readonly Texture2D EmptyTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        protected CompWaterContainer container;

        public override float Width
        {
            get { return 140f; }
        }

        public CommandGizmo_WaterContainerStatus(CompWaterContainer container)
        {
            this.container = container;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            Rect overRect = new Rect(topLeft.x, topLeft.y, Width, Height);
            Find.WindowStack.ImmediateWindow(windowID, overRect, WindowLayer.GameUI, delegate
            {
                Rect rect = overRect.AtZero().ContractedBy(6f);
                Rect rect2 = rect;
                rect2.height = overRect.height / 2f;
                Text.Font = GameFont.Tiny;
                Widgets.Label(rect2, "Water");
                Rect rect3 = rect;
                rect3.yMin = overRect.height / 2f;
                float fillPercent = container.StoredLitres / container.CapacityLitres;
                Widgets.FillableBar(rect3, fillPercent, FullTex, EmptyTex, false);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect3, container.StoredLitres.ToString("F1") + " / " + container.CapacityLitres.ToString("F1") + "L");
                Text.Anchor = TextAnchor.UpperLeft;
            });
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
