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
        /// <summary>
        /// Whether an actor needs to be a tool user to use it.
        /// </summary>
        public bool needsToolUser = false;
        /// <summary>
        /// Whether the container can be filled manually.
        /// </summary>
        public bool manuallyFillable = true;
        /// <summary>
        /// If manually fillable, whether the container should always be filled to capacity.
        /// </summary>
        public bool alwaysFillMax = false;

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
        private readonly int fillageLevels = 5;
        private readonly int defaultFillageIndex = 4;
        private int fillageIndex = -1;

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

        public bool ManuallyFillable
        {
            get { return props.manuallyFillable; }
        }

        public float ManualFillToLitres
        {
            get { return CapacityLitres / (fillageLevels - 1) * fillageIndex; }
        }

        public int ManualFillToPercent
        {
            get { return UnityEngine.Mathf.RoundToInt(100f / (fillageLevels - 1) * fillageIndex); }
        }

        private CommandGizmo_WaterContainerStatus _GizmoWaterStatus;
        public CommandGizmo_WaterContainerStatus GizmoWaterStatus
        {
            get {
                if (_GizmoWaterStatus == null)
                {
                    _GizmoWaterStatus = new CommandGizmo_WaterContainerStatus(this);
                }
                return _GizmoWaterStatus;
            }
        }

        private CommandGizmo_ChangeWaterFillage _GizmoWaterFillage;
        public CommandGizmo_ChangeWaterFillage GizmoWaterFillage
        {
            get
            {
                if (_GizmoWaterFillage == null)
                {
                    _GizmoWaterFillage = new CommandGizmo_ChangeWaterFillage(this);
                }
                return _GizmoWaterFillage;
            }
        }

        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            yield return GizmoWaterStatus;
            if (ManuallyFillable && !props.alwaysFillMax) { yield return GizmoWaterFillage; }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CompPropertiesWaterContainer propsw = props as CompPropertiesWaterContainer;
            if (propsw != null)
            {
                this.props = propsw;
            }

            if (fillageIndex == -1) { fillageIndex = defaultFillageIndex; }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.LookValue<float>(ref _storedLitres, "storedLitres");
            Scribe_Values.LookValue<int>(ref fillageIndex, "fillageIndex", defaultFillageIndex);
        }

        public void AddWater(float litres)
        {
            StoredLitres += litres;
        }

        virtual public float RemoveWater(float litresWanted)
        {
            if (litresWanted > StoredLitres) { litresWanted = StoredLitres; }
            StoredLitres -= litresWanted;
            return litresWanted;
        }

        public void IncreaseWaterFillage()
        {
            fillageIndex = (fillageIndex + 1) % fillageLevels;
        }

        public void DecreaseWaterFillage()
        {
            fillageIndex = (fillageIndex + fillageLevels - 1) % fillageLevels;
        }
    }
}
