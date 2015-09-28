using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class CompPropertiesWaterContainer : CompProperties
    {
        /// <summary>
        /// The capacity of the container in litres.
        /// </summary>
        public float capacity = 1f;

        public CompPropertiesWaterContainer() : base()
        {
            this.compClass = typeof(CompPropertiesWaterContainer);
        }

        public CompPropertiesWaterContainer(Type compClass) : base(compClass)
        {
            Log.Error(string.Format("Called CompPropertiesWaterContainer(Type) with {0}", compClass.FullName));
        }
    }

    public class CompWaterContainer : ThingComp
    {
        new public CompPropertiesWaterContainer props;
        
        private float _storedLitres;
        public float StoredLitres
        {
            get { return _storedLitres; }
            protected set { _storedLitres = UnityEngine.Mathf.Clamp(value, 0, props.capacity); }
        }

        public float CapacityLitres
        {
            get { return props.capacity; }
        }

        public float FreeSpace
        {
            get { return props.capacity - StoredLitres; }
        }

        public bool IsEmpty
        {
            get { return StoredLitres < 0.025; }
        }

        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            yield return new CommandGizmo_WaterContainerStatus(this);
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CompPropertiesWaterContainer propsw = props as CompPropertiesWaterContainer;
            if (propsw != null)
            {
                this.props = propsw;
            }

            // TODO: return to zero after testing!
            StoredLitres = propsw.capacity;
        }

        public void AddWater(float litres)
        {
            StoredLitres += litres;
        }

        public float RemoveWater(float litresWanted)
        {
            if (litresWanted > StoredLitres) { litresWanted = StoredLitres; }
            StoredLitres -= litresWanted;
            return litresWanted;
        }
    }
}
