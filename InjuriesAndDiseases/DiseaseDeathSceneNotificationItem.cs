using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace InjuriesAndDiseases
{
    // 1) Create your own SceneNotificationData
    public class DiseaseDeathSceneNotificationItem : SceneNotificationData
    {
        public Hero Victim { get; }
        public string DiseaseName { get; }
        private readonly CampaignTime _creationTime;

        public override string SceneID => "scn_cutscene_death_old_age";
        public override TextObject TitleText
        {
            get
            {
                // You can reuse the old‐age title template, but override the NAME and SICK variables
                GameTexts.SetVariable("HERO", Victim.Name);
                GameTexts.SetVariable("SICK", DiseaseName);
                // either pull an existing string or hard‐code your own:
                return new TextObject("{=IAD_ILL_DEAD}{HERO} died from {SICK}!");
            }
        }

        public override IEnumerable<Banner> GetBanners()
            => new[] { Victim.ClanBanner };

        public override IEnumerable<SceneNotificationData.SceneNotificationCharacter> GetSceneNotificationCharacters()
        {
            // exactly the same logic the old‐age cutscene uses
            var list = new List<SceneNotificationData.SceneNotificationCharacter>();
            var eq = Victim.CivilianEquipment.Clone(false);
            CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref eq, false, false);
            list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(
                Victim, eq, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
            foreach (var hero in CampaignSceneNotificationHelper
                         .GetMilitaryAudienceForHero(Victim, true, false)
                         .Take(5))
            {
                var eq2 = hero.CivilianEquipment.Clone(false);
                CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref eq2, false, false);
                list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(
                    hero, eq2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
            }
            return list;
        }

        public DiseaseDeathSceneNotificationItem(Hero victim, string diseaseName)
        {
            Victim = victim;
            DiseaseName = diseaseName;
            _creationTime = CampaignTime.Now;
        }
    }
}
