using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class CompPropertiesWaterSource : CompPropertiesWaterContainer
    {
        /// <summary>
        /// If this provides an unlimited source of water.
        /// </summary>
        public bool unlimitedSource = false;
        /// <summary>
        /// How many litres will spontaneously generate per day.
        /// </summary>
        public float regenPerDay = 0f;

        public CompPropertiesWaterSource() : base()
        {
            this.compClass = typeof(CompPropertiesWaterSource);
        }

        public CompPropertiesWaterSource(Type compClass) : base(compClass)
        {
            Log.Error(string.Format("Called CompPropertiesWaterSource(Type) with {0}", compClass.FullName));
        }
    }

    public class CompWaterSource : CompWaterContainer
    {
        new public CompPropertiesWaterSource props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CompPropertiesWaterSource propsw = props as CompPropertiesWaterSource;
            if (propsw != null)
            {
                this.props = propsw;
            }

            if (this.props.unlimitedSource)
            {
                this.props.capacity = 999f;
                StoredLitres = 999f;
            }
        }

        private float RegenPerRareTick
        {
            // 200 times per day.
            get { return props.regenPerDay / 200; }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();

            if (!props.unlimitedSource)
            {
                AddWater(RegenPerRareTick);
            }
        }

        public override float RemoveWater(float litresWanted)
        {
            if (props.unlimitedSource)
            {
                return litresWanted;
            }
            return base.RemoveWater(litresWanted);
        }
    }
}
