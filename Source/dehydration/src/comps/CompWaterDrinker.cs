using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class CompPropertiesWaterDrinker : CompProperties
    {
        /// <summary>
        /// Water capacity, in liters.
        /// </summary>
        public float capacity = 1f;
        /// <summary>
        /// Daily need for water, in liters.
        /// </summary>
        public float dailyNeed = 1f;
        /// <summary>
        /// First capacity at which the pawn wants to drink, in liters.
        /// </summary>
        public float capacityWantDrink = 0.75f;


        public CompPropertiesWaterDrinker() : base()
		{
            this.compClass = typeof(CompPropertiesWaterDrinker);
		}

        public CompPropertiesWaterDrinker(Type compClass)
            : base(compClass)
		{
            Log.Error(string.Format("Called CompPropertiesWaterDrinker(Type) with {0}", compClass.FullName));
		}
    }

    public class CompWaterDrinker : ThingComp
    {
        new public CompPropertiesWaterDrinker props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CompPropertiesWaterDrinker propsw = props as CompPropertiesWaterDrinker;
            if (propsw != null)
            {
                this.props = propsw;
            }
        }
    }
}
