using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace achan1989.dehydration
{
    public class CommandGizmo_ChangeWaterFillPriority : Command
    {
        private static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/Icons/EmptyIcon");
        private CompWaterContainer parentContainer;

        public CommandGizmo_ChangeWaterFillPriority(CompWaterContainer container) : base()
        {
            parentContainer = container;
            this.defaultLabel = "fill priority";
            this.defaultDesc = "Prioritize the filling of this container.";
            this.icon = Icon;
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            if (ev.button == 0)
            {
                parentContainer.IncreasePriority();
            }
            else if (ev.button == 1)
            {
                parentContainer.DecreasePriority();
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            var result = base.GizmoOnGUI(topLeft);

            Rect all = new Rect(topLeft.x, topLeft.y, this.Width, Gizmo.Height);
            Rect middle = all.ContractedBy(5f);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(middle, parentContainer.Priority.Label());
            Text.Anchor = TextAnchor.UpperLeft;

            return result;
        }
    }
}
