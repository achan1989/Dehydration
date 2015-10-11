using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace achan1989.dehydration
{
    public class CommandGizmo_ChangeWaterFillage : Command
    {
        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Icons/EmptyIcon");
        private readonly string descriptionFragment = "Fill this container to {0}% capacity.";
        private CompWaterContainer parentContainer;

        public override SoundDef CurActivateSound
        {
            get { return SoundDefOf.Click; }
        }

        public CommandGizmo_ChangeWaterFillage(CompWaterContainer container) : base()
        {
            parentContainer = container;
            this.defaultLabel = "keep filled";
            this.icon = Icon;
            UpdateDescription();
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            if (ev.button == 0)
            {
                parentContainer.IncreaseWaterFillage();
                UpdateDescription();
            }
            else if (ev.button == 1)
            {
                parentContainer.DecreaseWaterFillage();
                UpdateDescription();
            }
        }

        private void UpdateDescription()
        {
            this.defaultDesc = string.Format(descriptionFragment, parentContainer.ManualFillToPercent);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            var result = base.GizmoOnGUI(topLeft);

            Rect all = new Rect(topLeft.x, topLeft.y, this.Width, Gizmo.Height);
            Rect middle = all.ContractedBy(5f);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            Widgets.Label(middle, string.Format("{0}%", parentContainer.ManualFillToPercent));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            return result;
        }
    }
}
