using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;

namespace InjuriesAndDiseases
{
    // we’ll need this to override the private on‑inspect action:
    public static class MapVM_Extensions
    {
        public static void SetOnInspectAction(this MapNotificationItemBaseVM vm, Action a)
        {
            var f = typeof(MapNotificationItemBaseVM)
                        .GetField("_onInspect",
                                  System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            f.SetValue(vm, a);
        }
    }

    public class DiseaseDeathNotificationItemVM : MapNotificationItemBaseVM
    {
        public DiseaseDeathNotificationItemVM(DiseaseDeathMapNotification data) : base(data)
        {
            NotificationIdentifier = "death";
            // when clicked, fire your custom scene:
            this.SetOnInspectAction(() =>
                MBInformationManager.ShowSceneNotification(
                    new DiseaseDeathSceneNotificationItem(data.VictimHero, data.DiseaseName)
                )
            );
        }
    }
}
