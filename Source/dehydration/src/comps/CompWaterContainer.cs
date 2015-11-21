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
        /// <summary>
        /// How fast this collects rainwater. Base rate is one per tile. Set to zero to disable.
        /// </summary>
        public float rainWaterCollectionRate = 0f;

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
        private StoragePriority priorityInt = StoragePriority.Normal;
        /// <summary>
        /// The litres of rainwater collected in one tick by one cell collecting at the max rate.
        /// </summary>
        private static readonly float baseRainwaterCollectionPerTick = 1.4f; // One cell collects about 7L an hour.

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

        public float FreeSpaceLitres
        {
            get
            {
                if (!ManuallyFillable || props.alwaysFillMax)
                {
                    return props.capacity - StoredLitres;
                }
                else
                {
                    float fillTo = ManualFillToLitres;
                    if (StoredLitres >= fillTo) { return 0f; }
                    return fillTo - StoredLitres;
                }
            }
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

        public StoragePriority Priority
        {
            get { return priorityInt; }
        }

        private CommandGizmo_WaterContainerStatus _GizmoWaterStatus;
        public CommandGizmo_WaterContainerStatus GizmoWaterStatus
        {
            get
            {
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

        private CommandGizmo_ChangeWaterFillPriority _GizmoFillPriority;
        public CommandGizmo_ChangeWaterFillPriority GizmoFillPriority
        {
            get
            {
                if (_GizmoFillPriority == null)
                {
                    _GizmoFillPriority = new CommandGizmo_ChangeWaterFillPriority(this);
                }
                return _GizmoFillPriority;
            }
        }

        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            yield return GizmoWaterStatus;
            if (ManuallyFillable && !props.alwaysFillMax)
            {
                yield return GizmoWaterFillage;
                yield return GizmoFillPriority;
            }
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

            // Slightly hacky and non-accurate way of determining when the player has made a bucket.
            if (this.parent.def.defName.Equals("Dehydration_Bucket"))
            {
                ConceptDatabase.KnowledgeDemonstrated(DefDatabase<ConceptDef>.GetNamedSilentFail("Dehydration_HaulWaterTool"),
                                                      KnowledgeAmount.Total);
            }
            // Ditto for waterskins and canteens.
            if (this.parent.def.defName.StartsWith("Apparel_Waterskin_") ||
                this.parent.def.defName.StartsWith("Apparel_Canteen_"))
            {
                ConceptDatabase.KnowledgeDemonstrated(DefDatabase<ConceptDef>.GetNamedSilentFail("Dehydration_WaterskinsCanteens"),
                                                      KnowledgeAmount.Total);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.LookValue<float>(ref _storedLitres, "storedLitres");
            Scribe_Values.LookValue<int>(ref fillageIndex, "fillageIndex", defaultFillageIndex);
            Scribe_Values.LookValue<StoragePriority>(ref priorityInt, "priority", StoragePriority.Normal);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();

            if (props.rainWaterCollectionRate > 0 && Find.WeatherManager.RainRate > 0.01f)
            {
                // Assume that each cell of a building is equally responsible for collecting water.
                // Calculate water collection rate based on how many cells of the building are
                // exposed to the sky.
                var thingCells = GenAdj.CellsOccupiedBy(this.parent);
                int numCells = thingCells.Count();
                int numCollecting = thingCells.Count(pos => !pos.Roofed());
                float collectionRate = props.rainWaterCollectionRate / numCells * numCollecting;

                float collectedLitres = Find.WeatherManager.RainRate * collectionRate * baseRainwaterCollectionPerTick;
                if (collectedLitres + StoredLitres > CapacityLitres) { collectedLitres = props.capacity - StoredLitres; }
                AddWater(collectedLitres);
            }
        }

        public void AddWater(float litres)
        {
            StoredLitres += litres;
            if (StoredLitres > CapacityLitres)
            {
                if (StoredLitres > CapacityLitres + 0.001f)
                {
                    Log.Error(string.Format("WaterContainer + {0} litres = {1}, capacity {2}", litres,
                        StoredLitres, CapacityLitres));
                }
                StoredLitres = CapacityLitres;
            }
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

        public void IncreasePriority()
        {
            priorityInt = priorityInt.HigherPriority();
            // TODO: possible to notify others that this has changed?  Pawns should fail current
            // hauing job if this goes out of range.
        }

        public void DecreasePriority()
        {
            priorityInt = priorityInt.LowerPriority();
        }

        public override string ToString()
        {
            return string.Format("WaterContainer on {0}: {1}/{2}L", parent.ToString(), StoredLitres, CapacityLitres);
        }
    }
}
