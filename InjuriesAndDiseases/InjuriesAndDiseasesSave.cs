using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.PerSave;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace InjuriesAndDiseases
{
    public sealed class InjuriesAndDiseasesSave : AttributePerSaveSettings<InjuriesAndDiseasesSave>
    {
        public override string Id => "InjuriesAndDiseasesSave";
        public override string DisplayName => new TextObject("{=IAD_SAVE_TITLE}Injuries & Diseases Save").ToString();
        public override string FolderName => "InjuriesAndDiseasesSave";

        [SettingPropertyButton(
           "{=IAD_CLEAR_SAVE}Clear All Data",
           Content = "{=IAD_CLEAR_BTN}Clear",
           Order = 1, RequireRestart = false,
           HintText = "{=IAD_CLEAR_SAVE_H}Wipe out every saved status.")]
        [SettingPropertyGroup("{=MCM_SAVE}Save")]
        public Action ClearSave
        {
            get;
            set;
        } = () =>
        {
            InformationManager.DisplayMessage(
                new InformationMessage("All Injuries & Diseases data cleared.", Colors.Green)
            );
            var bh = InjuriesAndDiseasesBehavior.Instance;
            if(bh != null)
            {
                bh._heroStatusesSave = new List<HeroStatusContainer>();
                bh._heroStatuses.Clear();
                bh._settlementStatusesSave = new List<SettlementStatusContainer>();
                bh._settlementStatuses.Clear();
            }

        };

    }
}
