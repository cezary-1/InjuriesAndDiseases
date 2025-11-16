using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace InjuriesAndDiseases
{
    public class DiseaseDeathMapNotification : InformationData
    {
        [SaveableProperty(1)] public Hero VictimHero { get; }
        [SaveableProperty(2)] public string DiseaseName { get; }
        [SaveableProperty(3)] public CampaignTime CreationTime { get; }

        public DiseaseDeathMapNotification(Hero victim, string disease, CampaignTime creationTime)
          : base(
              // this is the little tooltip under the icon:
              new TextObject("{=IAD_ILL_DEAD}{HERO} died from {SICK}!")
                .SetTextVariable("HERO", victim.Name)
                .SetTextVariable("SICK", disease)
            )
        {
            VictimHero = victim;
            DiseaseName = disease;
            CreationTime = creationTime;
        }

        public override TextObject TitleText => new TextObject("{=W73My5KO}Death");
        public override string SoundEventPath => "event:/ui/notification/death";
    }
}
