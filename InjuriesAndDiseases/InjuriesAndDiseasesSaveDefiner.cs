using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace InjuriesAndDiseases
{

    public class DiseaseEntry
    {
        [SaveableProperty(1)]
        public string ConfigId { get; set; }

        [SaveableProperty(2)]
        public int Days { get; set; }

        public DiseaseEntry() { }
        public DiseaseEntry(string configId, int days)
        {
            ConfigId = configId;
            Days = days;
        }
    }

    public class InjuryEntry
    {
        [SaveableProperty(1)]
        public string ConfigId { get; set; }

        [SaveableProperty(2)]
        public int Days { get; set; }

        public InjuryEntry() { }
        public InjuryEntry(string configId, int days)
        {
            ConfigId = configId;
            Days = days;
        }
    }


    public class HeroStatusContainer
    {
        [SaveableProperty(1)]
        public string HeroId { get; set; }

        [SaveableProperty(2)]
        public List<DiseaseEntry> Diseases { get; set; } = new List<DiseaseEntry>();

        [SaveableProperty(3)]
        public List<InjuryEntry> Injuries { get; set; } = new List<InjuryEntry>();

        [SaveableProperty(4)]
        public int LastRecoveryDay { get; set; } = 0;

        public HeroStatusContainer() { }

        public HeroStatusContainer(string heroId, List<DiseaseEntry> diseases, List<InjuryEntry> injuries, int lastRecovery)
        {
            HeroId = heroId;
            Diseases = diseases ?? new List<DiseaseEntry>();
            Injuries = injuries ?? new List<InjuryEntry>();
            LastRecoveryDay = lastRecovery;
        }
    }

    public class SettlementStatusContainer
    {
        // 1 = unique ID within our mod; bump if you add more props
        [SaveableProperty(1)]
        public string SettlementId { get; set; }
        [SaveableProperty(2)]
        public List<DiseaseEntry> Diseases { get; set; } = new List<DiseaseEntry>();
        [SaveableProperty(3)]
        public int LastRecoveryDay { get; set; } = 0;

        public SettlementStatusContainer() { }

        public SettlementStatusContainer(string settlement, List<DiseaseEntry> diseases, int lastRecovery)
        {
            SettlementId = settlement;
            Diseases = diseases ?? new List<DiseaseEntry>();
            LastRecoveryDay = lastRecovery;
        }
    }

    internal sealed class IADSaveDefiner : SaveableTypeDefiner
    {
        // Pick a seed that won't collide with other mods
        public IADSaveDefiner() : base(98765432) { }

        protected override void DefineClassTypes()
        {
            // class ID 1 = KingdomHistoryEntry
            AddClassDefinition(typeof(HeroStatusContainer), 101);
            AddClassDefinition(typeof(SettlementStatusContainer), 102);
            AddClassDefinition(typeof(DiseaseEntry), 103);
            AddClassDefinition(typeof(InjuryEntry), 104);
        }

        protected override void DefineContainerDefinitions()
        {
            // container ID 1 = List<KingdomHistoryEntry>
            ConstructContainerDefinition(typeof(List<HeroStatusContainer>));
            ConstructContainerDefinition(typeof(List<SettlementStatusContainer>));
            ConstructContainerDefinition(typeof(List<DiseaseEntry>));
            ConstructContainerDefinition(typeof(List<InjuryEntry>));
        }

    }
}
