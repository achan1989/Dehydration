using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public class Need_Water : Need
    {
        private const int TicksBetweenDehydrationDamage = 15000;
        private const float DehydrationDamAmount = 0.066666f;
        private int tickToDehydrationDamage;
        private CompWaterDrinker waterDrinker;

        public float CurLevelLitres
        {
            get { return this.CurLevel * waterDrinker.props.capacity; }
            set { this.CurLevel = UnityEngine.Mathf.Clamp01(value / waterDrinker.props.capacity); }
        }

        public float ThreshThirstyLitres
		{
			get { return waterDrinker.props.capacityWantDrink * 0.9f; }
		}

        public float ThreshThirsty
        {
            get { return ThreshThirstyLitres / waterDrinker.props.capacity; }
        }

        public float ThreshUrgentlyThirstyLitres
        {
            get { return waterDrinker.props.capacityWantDrink * 0.45f; }
        }

        public float ThreshUrgentlyThirsty
        {
            get { return ThreshUrgentlyThirstyLitres / waterDrinker.props.capacity; }
        }

        public bool Dehydrated
        {
            get { return this.CurCategory == HydratedCategory.Dehydrated; }
        }

        public HydratedCategory CurCategory
        {
            get
            {
                if (this.CurLevel < 0.01f)
                {
                    return HydratedCategory.Dehydrated;
                }
                if (CurLevelLitres < ThreshUrgentlyThirstyLitres)
                {
                    return HydratedCategory.UrgentlyThirsty;
                }
                if (CurLevelLitres < ThreshThirstyLitres)
                {
                    return HydratedCategory.Thirsty;
                }
                return HydratedCategory.Hydrated;
            }
        }

		public float WaterFallLitresPerInterval
		{
            // An interval is 150 ticks.
            // 30K ticks per 24 hours, so 200 intervals per day.
            // TODO: change based on environmental temperature, sun exposure, etc...
			get
			{
                float intervalNeed = waterDrinker.props.dailyNeed / 200;
				switch (this.CurCategory)
				{
				case HydratedCategory.Hydrated:
					return intervalNeed;
				case HydratedCategory.Thirsty:
                    return intervalNeed * 0.85f;
				case HydratedCategory.UrgentlyThirsty:
                    return intervalNeed * 0.5f;
				case HydratedCategory.Dehydrated:
                    return intervalNeed * 0.25f;
				default:
                    throw new NotImplementedException();
				}
			}
		}

		public override int GUIChangeArrow
		{
			get
			{
				return -1;
			}
		}

		public float HydrationWantedLitres
		{
			get { return waterDrinker.props.capacity - CurLevelLitres;  }
		}

		public Need_Water(Pawn pawn) : base(pawn)
		{
			if (pawn.RaceProps.Humanlike)
			{
				this.CurLevel = 0.9f;
			}
			else
			{
				this.CurLevel = Rand.Range(0.5f, 0.9f);
			}

            this.waterDrinker = pawn.GetComp<CompWaterDrinker>();

            if (this.waterDrinker == null)
            {
                Log.Warning(string.Format("Created Need_Water on pawn with no CompWaterDrinker: {0}", pawn.KindLabel));
                this.waterDrinker = new CompWaterDrinker();
                this.waterDrinker.Initialize(new CompPropertiesWaterDrinker { dailyNeed = 0f });
            }
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.LookValue<int>(ref this.tickToDehydrationDamage, "ticksToNextDehydrationDamage", 0, false);
		}

		public override void NeedInterval()
		{
            // Called every 150 ticks.
            // 30K ticks per 24 hours, so called 200 times per day.
            this.CurLevelLitres -= WaterFallLitresPerInterval;
            this.tickToDehydrationDamage -= 150;
            if (this.tickToDehydrationDamage <= 0)
            {
                // TODO: damaged based off environment, temperateure, etc...
                float damage = DehydrationDamAmount;
                HediffDef hd = DefDatabase<HediffDef>.GetNamed("Dehydration", true);
                if (this.Dehydrated)
                {
                    HealthUtility.AdjustSeverity(this.pawn, hd, damage);
                }
                else
                {
                    HealthUtility.AdjustSeverity(this.pawn, hd, -damage);
                }
                this.tickToDehydrationDamage = TicksBetweenDehydrationDamage;
            }
		}

        public float DrinkFrom(CompWaterContainer waterContainer, float litresWanted)
        {
            float litresDrunk = waterContainer.RemoveWater(litresWanted);
            CurLevelLitres += litresDrunk;
            return litresDrunk;
        }

		public override void DrawOnGUI(UnityEngine.Rect rect)
		{
			if (this.threshPercents == null)
			{
				this.threshPercents = new List<float>();
			}
			this.threshPercents.Clear();
			this.threshPercents.Add(this.ThreshThirsty);
			this.threshPercents.Add(this.ThreshUrgentlyThirsty);
			base.DrawOnGUI(rect);
		}
    }
}
