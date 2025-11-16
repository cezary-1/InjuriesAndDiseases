using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace InjuriesAndDiseases
{
    public class InjuriesAndDiseasesModule : MBSubModuleBase
    {
        private Harmony _harmony;
        public static string _module;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            InformationManager.DisplayMessage(
                new InformationMessage("[InjuriesAndDiseases] Module loaded."));

            var asm = Assembly.GetExecutingAssembly().Location;
            _module = asm;
            // Create a Harmony instance with a unique ID
            _harmony = new Harmony("InjuriesAndDiseases");
            // Tell Harmony to scan your assembly for [HarmonyPatch] classes
            _harmony.PatchAll(Assembly.GetExecutingAssembly());


        }


        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }


        protected override void OnGameStart(Game game, IGameStarter starter)
        {
            base.OnGameStart(game, starter);
            if (starter is CampaignGameStarter campaignStarter)
            {
                // Add our behavior
                campaignStarter.AddBehavior(new InjuriesAndDiseasesBehavior());
            }
        }



    }
}
