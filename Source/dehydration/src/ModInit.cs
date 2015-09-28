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
                Object.DontDestroyOnLoad(gameobject);
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
        private readonly string defnameGetWaterDehydrated = "Dehydration_Humanlike_Patch_GetWater_Dehydrated";
        private readonly string defnamePackWater = "Dehydration_Humanlike_Patch_PackWater_NotThirsty";
        private readonly string defnameGetWater = "Dehydration_Humanlike_Patch_GetWater";

        public void Start()
        {
            enabled = false;

            Log.Message("Dehydration ModInit.Start()");
            InjectThinkTrees();
        }

        public void FixedUpdate() { }
        public void OnLevelWasLoaded() { }

        private void InjectThinkTrees()
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

                InjectGetWaterThink(humanTree.thinkRoot);
            }
            else
            {
                Log.Error("Dehydration can't find humanlike ThinkTree to inject into.");
            }
        }

        private void InjectDehydrationThink(ThinkNode colonistTree)
        {
            bool failed = true;
            var dehydrationThink = DefDatabase<ThinkTreeDef>.GetNamed(defnameGetWaterDehydrated);

            if (dehydrationThink != null)
            {
                // Inject into the colonist tree, more important than food.
                int starvingIndex = colonistTree.subNodes.FindIndex(node =>
                    node is ThinkNode_ConditionalStarving);
                if (starvingIndex != -1)
                {
                    colonistTree.subNodes.Insert(starvingIndex, dehydrationThink.thinkRoot);
                    Log.Message(string.Format("Dehydration injected {0}", defnameGetWaterDehydrated));
                    failed = false;
                }
            }

            if (failed)
            {
                Log.Error(string.Format("Dehydration can't inject {0}", defnameGetWaterDehydrated));
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

        private void InjectGetWaterThink(ThinkNode humanTree)
        {
            bool failed = true;
            var drinkThink = DefDatabase<ThinkTreeDef>.GetNamed(defnameGetWater);

            if (drinkThink != null)
            {
                var mainBehaviours = humanTree.subNodes.Find(node =>
                    node is ThinkNode_PrioritySorter &&
                    node.subNodes.Exists(subnode => subnode is JobGiver_GetFood));
                if (mainBehaviours != null)
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
                else { Log.Error("No mainBehaviours"); }
            }
            else { Log.Error("No drinkThink"); }

            if (failed)
            {
                Log.Error(string.Format("Dehydration can't inject {0}", defnameGetWater));
            }
        }
    }
}
