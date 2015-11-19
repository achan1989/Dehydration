using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace achan1989.dehydration
{
    /// <summary>
    /// Replacement for the base game's Plant.  Requires water for full growth speed.
    /// </summary>
    /// <remarks>
    /// Had to copy-paste a lot of the base game's Plant code because things were private.
    /// </remarks>
    public class Plant : RimWorld.Plant
    {
        private const float RotDamagePerTick = 0.005f;
        private const float GridPosRandomnessFactor = 0.3f;
        private const int TicksWithoutLightBeforeRot = 450000;
        private const int LeaflessMinRecoveryTicks = 60000;
        private const float MinLeaflessTemperature = -10f;
        private const float MinAnimalEatPlantsTemperature = 0f;
        // Base rate
        public const int MinTicksForDehydrated = GenDate.TicksPerDay * 7;
        public const int MaxTicksForDehydrated = GenDate.TicksPerDay * 14;

        /// <summary>
        /// Hides a private version in the base class.
        /// </summary>
        protected int unlitTicks;
        protected int waterlessTicks = 0;


        public void Sown()
        {
            this.growth = 0f;
            this.sown = true;
            // Sown plants start needing some water.
            this.waterlessTicks = (int)Mathf.Lerp((float)MinTicksForDehydrated, (float)MaxTicksForDehydrated, 0.5f);
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            
            // Wild plants can start fully watered to fully dehydrated.  If this plant is actually
            // a sown plant, this will be reset later in Sown().
            this.waterlessTicks = Rand.Range(0, MaxTicksForDehydrated);
        }
        
        protected internal bool Resting
        {
            get
            {
                return GenDate.CurrentDayPercent < 0.25f || GenDate.CurrentDayPercent > 0.8f;
            }
        }
        
        protected internal float GrowthPerTick
        {
            get
            {
                if (this.Resting || this.LifeStage != PlantLifeStage.Growing)
                {
                    return 0f;
                }
                float growthPerTick = 1f / (GenDate.TicksPerDay * this.def.plant.growDays);
                return growthPerTick * this.GrowthRate;
            }
        }

        new public float GrowthRate
        {
            get
            {
                return this.GrowthRateFactor_Fertility * this.GrowthRateFactor_Temperature * this.GrowthRateFactor_Light * this.GrowthRateFactor_Water;
            }
        }

        public float GrowthRateFactor_Water
        {
            get
            {
                float factor = Mathf.InverseLerp(MaxTicksForDehydrated, MinTicksForDehydrated, waterlessTicks);
                if (factor > 1f) { factor = 1f; }
                else if (factor < 0.1f) { factor = 0.1f; }
                return factor;
            }
        }

        public bool NeedsWatering
        {
            get { return this.waterlessTicks > MinTicksForDehydrated; }
        }

        public void Watered()
        {
            this.waterlessTicks = 0;
        }

        protected bool CurrentlyCultivated()
        {
            if (!this.def.plant.Sowable)
            {
                return false;
            }
            Zone zone = Find.ZoneManager.ZoneAt(base.Position);
            if (zone != null && zone is Zone_Growing)
            {
                return true;
            }
            Building edifice = base.Position.GetEdifice();
            return edifice != null && edifice.def.building.SupportsPlants;
        }

        protected void NewlyMatured()
        {
            if (CurrentlyCultivated())
            {
                Find.MapDrawer.MapMeshDirty(base.Position, MapMeshFlag.Things);
            }
        }

        protected string GrowthPercentString
        {
            get
            {
                return (this.growth + 0.0001f).ToStringPercent();
            }
        }

        protected internal bool HasEnoughLightToGrow
        {
            get
            {
                return this.GrowthRateFactor_Light > 0.001f;
            }
        }

        protected void CheckTemperatureMakeLeafless()
        {
            float num = 8f;
            float num2 = (float)this.HashOffset() * 0.01f % num - num + -2f;
            if (base.Position.GetTemperature() < num2)
            {
                this.MakeLeafless();
            }
        }

        // Shown on map mouseover when nothing is selected.
        public override string LabelMouseover
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(this.def.LabelCap);
                stringBuilder.Append(" (" + "PercentGrowth".Translate(new object[]
		        {
			        this.GrowthPercentString
		        }));
                if (this.Rotting)
                {
                    stringBuilder.Append(", " + "DyingLower".Translate());
                }
                if (this.NeedsWatering)
                {
                    stringBuilder.Append(", needs water");
                }
                stringBuilder.Append(")");
                return stringBuilder.ToString();
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (this.LifeStage == PlantLifeStage.Growing)
            {
                stringBuilder.AppendLine("PercentGrowth".Translate(new object[]
		        {
			        this.GrowthPercentString
		        }));
                stringBuilder.AppendLine("GrowthRate".Translate() + ": " + this.GrowthRate.ToStringPercent());
                
                if (this.Resting)
                {
                    stringBuilder.AppendLine("PlantResting".Translate());
                }
                else
                {
                    if (!this.HasEnoughLightToGrow)
                    {
                        stringBuilder.AppendLine("PlantNeedsLightLevel".Translate() + ": " + this.def.plant.growMinGlow.ToStringPercent());
                    }

                    float growthRateFactor_Temperature = this.GrowthRateFactor_Temperature;
                    if (growthRateFactor_Temperature < 0.99f)
                    {
                        if (growthRateFactor_Temperature < 0.01f)
                        {
                            stringBuilder.AppendLine("OutOfIdealTemperatureRangeNotGrowing".Translate());
                        }
                        else
                        {
                            stringBuilder.AppendLine("OutOfIdealTemperatureRange".Translate(new object[]
					        {
						        Mathf.RoundToInt(growthRateFactor_Temperature * 100f).ToString()
					        }));
                        }
                    }

                    float growthRateFactor_Water = this.GrowthRateFactor_Water;
                    if (growthRateFactor_Water < 0.99f)
                    {
                        stringBuilder.AppendFormat("Lacks water (growing at {0:P0} of normal speed)", growthRateFactor_Water).AppendLine();
                    }
                }
            }
            else if (this.LifeStage == PlantLifeStage.Mature)
            {
                if (this.def.plant.Harvestable)
                {
                    stringBuilder.AppendLine("ReadyToHarvest".Translate());
                }
                else
                {
                    stringBuilder.AppendLine("Mature".Translate());
                }
            }
            return stringBuilder.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // Use our value for the unlitTicks, not the base class value.
            Scribe_Values.LookValue<int>(ref this.unlitTicks, "unlitTicks", 0, false);
            // Stuff specific to this child class.
            Scribe_Values.LookValue<int>(ref waterlessTicks, "waterlessTicks", 0);
        }

        public override void TickLong()
        {
            CheckTemperatureMakeLeafless();
            if (GenPlant.GrowthSeasonNow(base.Position))
            {
                if (!this.HasEnoughLightToGrow)
                {
                    this.unlitTicks += GenTicks.TickLongInterval;
                }
                else
                {
                    this.unlitTicks = 0;
                }

                if (Find.WeatherManager.RainRate > 0.01f)
                {
                    Watered();
                }
                else
                {
                    this.waterlessTicks += GenTicks.TickLongInterval;
                }

                bool alreadyMature = this.LifeStage == PlantLifeStage.Mature;
                this.growth += this.GrowthPerTick * GenTicks.TickLongInterval;
                if (!alreadyMature && this.LifeStage == PlantLifeStage.Mature)
                {
                    NewlyMatured();
                }

                if (this.def.plant.LimitedLifespan)
                {
                    this.age += GenTicks.TickLongInterval;
                    if (this.Rotting)
                    {
                        base.TakeDamage(new DamageInfo(DamageDefOf.Rotting, 10));
                    }
                }

                if (!base.Destroyed && this.def.plant.shootsSeeds && this.growth >= 0.6f &&
                    Rand.MTBEventOccurs(this.def.plant.seedEmitMTBDays, GenDate.TicksPerDay, GenTicks.TickLongInterval))
                {
                    if (!GenPlant.SnowAllowsPlanting(base.Position)) { return; }
                    if (base.Position.Roofed()) { return; }
                    GenPlantReproduction.TrySpawnSeed(base.Position, this.def, SeedTargFindMode.ReproduceSeed, this);
                }
            }
        }
    }
}
