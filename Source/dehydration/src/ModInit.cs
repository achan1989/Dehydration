using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace achan1989.dehydration
{
    public class ModInitTab : ITab
    {
        public static GameObject gameobject;

        public ModInitTab()
        {
            Log.Message("Dehydration ModInitTab()");
            if (gameobject == null)
            {
                gameobject = new GameObject("DehydrationLoader");
                gameobject.AddComponent<ModInit>();
                UnityEngine.Object.DontDestroyOnLoad(gameobject);
            }
        }

        protected override void FillTab()
        {   
        }
    }

    public class ModInit : MonoBehaviour
    {
        public readonly string GameObjectName = "Dehydration Init";

        private readonly string defnameCoreHumanlike = "Humanlike";
        private readonly string defnameCoreAnimal = "Animal";
        private readonly string defnamePackWater = "Dehydration_Patch_PackWater_NotThirsty";
        private readonly string defnameGetWater = "Dehydration_Patch_GetWater";
        private readonly string defnameExitMapThirsty = "Dehydration_Patch_ExitMapThirsty";

        public void Start()
        {
            enabled = false;

            Log.Message("Dehydration ModInit.Start()");
            InjectHumanThinkTrees();
            InjectAnimalThinkTrees();
            InjectCompWaterDrinker();
            InjectCustomPlantClass();
        }

        public void FixedUpdate() { }
        public void OnLevelWasLoaded() { }

        private void InjectHumanThinkTrees()
        {
            var humanTree = DefDatabase<ThinkTreeDef>.GetNamed(defnameCoreHumanlike);
            if (humanTree != null)
            {
                var colonistTree = humanTree.thinkRoot.subNodes.Find(node =>
                    node is ThinkNode_ConditionalColonist &&
                    node.subNodes.Exists(subnode => subnode is ThinkNode_ConditionalStarving));
                if (colonistTree != null)
                {
                    InjectDehydrationThink(colonistTree);
                    InjectPackWaterThink(colonistTree);
                }
                else
                {
                    Log.Error("Dehydration can't find colonist ThinkTree to inject into.");
                }

                var mainBehaviours = humanTree.thinkRoot.subNodes.Find(node =>
                    node is ThinkNode_PrioritySorter &&
                    node.subNodes.Exists(subnode => subnode is JobGiver_GetFood));
                if (mainBehaviours != null)
                {
                    InjectGetWaterThink(mainBehaviours);
                }
                else
                {
                    Log.Error("Dehydration can't find main behaviours ThinkTree to inject into.");
                }

                ThinkNode mainPrisonerBehaviours = null;
                try
                {
                    mainPrisonerBehaviours = humanTree.thinkRoot.subNodes.Find(
                        node => node is ThinkNode_ConditionalPrisoner).subNodes.Find(
                            node => node is ThinkNode_PrioritySorter);
                }
                catch { }
                if (mainPrisonerBehaviours != null)
                {
                    InjectGetWaterThink(mainPrisonerBehaviours);
                }
                else
                {
                    Log.Error("Dehydration can't find main prisoner behaviours ThinkTree to inject into.");
                }
                
            }
            else
            {
                Log.Error("Dehydration can't find humanlike ThinkTree to inject into.");
            }
        }

        private void InjectAnimalThinkTrees()
        {
            var animalTree = DefDatabase<ThinkTreeDef>.GetNamed(defnameCoreAnimal);
            if (animalTree != null)
            {
                var targetTree = animalTree.thinkRoot.subNodes.Find(node =>
                    node is ThinkNode_PrioritySorter &&
                    node.subNodes.Exists(subnode => subnode is JobGiver_GetFood));
                if (targetTree != null)
                {
                    InjectGetWaterThink(targetTree);
                }
                else
                {
                    Log.Error("Dehydration can't find animal subtree to inject GetWater into.");
                }

                targetTree = animalTree.thinkRoot.subNodes.Find(node =>
                    node is ThinkNode_ConditionalHasFaction &&
                    node.subNodes.Exists(subnode =>
                        subnode is ThinkNode_Subtree &&
                        ((ThinkNode_Subtree)subnode).treeDef.defName == "LeaveIfWrongSeason"));
                if (targetTree != null)
                {
                    InjectExitMapOnThirsty(targetTree);
                }
                else
                {
                    Log.Error("Dehydration can't find animal subtree to inject ExitMap into.");
                }
            }
            else
            {
                Log.Error("Dehydration can't find animal ThinkTree to inject into.");
            }
        }

        private void InjectDehydrationThink(ThinkNode colonistTree)
        {
            bool failed = true;
            var dehydrationThink = DefDatabase<ThinkTreeDef>.GetNamed(defnameGetWater);

            if (dehydrationThink != null)
            {
                // Inject into the colonist tree, more important than food.
                int starvingIndex = colonistTree.subNodes.FindIndex(node =>
                    node is ThinkNode_ConditionalStarving);
                if (starvingIndex != -1)
                {
                    colonistTree.subNodes.Insert(starvingIndex, dehydrationThink.thinkRoot);
                    Log.Message(string.Format("Dehydration injected {0}", defnameGetWater));
                    failed = false;
                }
            }

            if (failed)
            {
                Log.Error(string.Format("Dehydration can't inject {0}", defnameGetWater));
            }
        }

        private void InjectPackWaterThink(ThinkNode colonistTree)
        {
            bool failed = true;
            var packThink = DefDatabase<ThinkTreeDef>.GetNamed(defnamePackWater);

            if (packThink != null)
            {
                int packFoodIndex = colonistTree.subNodes.FindIndex(node =>
                    node is ThinkNode_ConditionalNeedAbove &&
                    node.subNodes.Exists(subnode => subnode is JobGiver_PackFood));
                if (packFoodIndex != -1)
                {
                    colonistTree.subNodes.Insert(packFoodIndex, packThink.thinkRoot);
                    Log.Message(string.Format("Dehydration injected {0}", defnamePackWater));
                    failed = false;
                }
            }

            if (failed)
            {
                Log.Error(string.Format("Dehydration can't inject {0}", defnamePackWater));
            }
        }

        private void InjectGetWaterThink(ThinkNode mainBehaviours)
        {
            bool failed = true;
            var drinkThink = DefDatabase<ThinkTreeDef>.GetNamed(defnameGetWater);

            if (drinkThink != null)
            {
                int foodIndex = mainBehaviours.subNodes.FindIndex(node =>
                    node is JobGiver_GetFood);
                if (foodIndex != -1)
                {
                    mainBehaviours.subNodes.Insert(foodIndex, drinkThink.thinkRoot.subNodes[0]);
                    Log.Message(string.Format("Dehydration injected {0}", defnameGetWater));
                    failed = false;
                }
                else { Log.Error("No foodIndex"); }
            }
            else { Log.Error("No drinkThink"); }

            if (failed)
            {
                Log.Error(string.Format("Dehydration can't inject {0}", defnameGetWater));
            }
        }

        private void InjectExitMapOnThirsty(ThinkNode parent)
        {
            bool failed = true;
            var exitThink = DefDatabase<ThinkTreeDef>.GetNamed(defnameExitMapThirsty);

            if (exitThink != null)
            {
                parent.subNodes.Insert(0, new ThinkNode_Subtree() {treeDef = exitThink});
                Log.Message(string.Format("Dehydration injected {0}", defnameExitMapThirsty));
                failed = false;
            }
            else { Log.Error("No exit map think."); }

            if (failed)
            {
                Log.Error(string.Format("Dehydration can't inject {0}", defnameExitMapThirsty));
            }
        }

        private void InjectCompWaterDrinker()
        {
            // Criteria for injecting a generic water drinker comp into a ThingDef.
            Predicate<ThingDef> heuristicDoInjectGeneric = thing =>
            {
                if (thing.category != ThingCategory.Pawn) { return false; }
                if (thing.race == null) { return false; }
                if (!thing.race.mechanoid && !thing.race.Animal) { return false; }

                // Don't inject a generic water drinker comp if it already has one.
                if (thing.CompDefFor<CompWaterDrinker>() != null) { return false; }

                return true;
            };

            // Make a generic water drinker comp from a ThingDef.
            var MakeWaterDrinkerComp = new Func<ThingDef, CompProperties>( thing =>
            {
                // Used to "remove" a water need from mechanoids (although currently it still shows up
                // in the Needs tab).
                // TODO: remove water from mechanoid needs tab.
                if (thing.race.mechanoid)
                {
                    return new CompPropertiesWaterDrinker() {capacity=1f, dailyNeed=0f, capacityWantDrink=0f};
                }

                // Make an educated guess about how much water the animal needs based on body size.
                return new CompPropertiesWaterDrinker()
                {
                    capacity = 4 * thing.race.baseBodySize,
                    dailyNeed = 3.5f * thing.race.baseBodySize,
                    capacityWantDrink = 3 * thing.race.baseBodySize
                };
            });
            
            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
	        {
                if (heuristicDoInjectGeneric(thing))
                {
                    Log.Message(string.Format("Injecting generic CompPropertiesWaterDrinker "
                                + "into {0}", thing.defName));
                    thing.comps.Add(MakeWaterDrinkerComp(thing));
                }
	        }
        }

        /// <summary>
        /// Make all plants use the dehydration Plant class.
        /// </summary>
        private void InjectCustomPlantClass()
        {
            foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
            {
                if (thing.thingClass.Equals(typeof(RimWorld.Plant)))
                {
                    Log.Message(string.Format("Converting {0} to custom Plant class.", thing.defName));
                    thing.thingClass = typeof(achan1989.dehydration.Plant);
                }
            }
        }
    }
}
