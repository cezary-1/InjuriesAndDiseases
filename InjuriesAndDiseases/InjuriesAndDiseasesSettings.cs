using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System.Collections.Generic;
using System;
using TaleWorlds.Localization;
using System.Linq;

namespace InjuriesAndDiseases
{
    public sealed class InjuriesAndDiseasesGlobalSettings : AttributeGlobalSettings<InjuriesAndDiseasesGlobalSettings>
    {
        public override string Id => "InjuriesAndDiseases";
        public override string DisplayName => new TextObject("{=IAD_TITLE}Injuries & Diseases").ToString();
        public override string FolderName => "InjuriesAndDiseases";
        public override string FormatType => "json";

        // ensure only MCM constructs this
        public InjuriesAndDiseasesGlobalSettings() { }

        // ── General ──────────────────────────────────────────────

        [SettingPropertyInteger(
            "{=IAD_IntervalDays}Days between checks",
            1, 81, "{VALUE}",
            Order = 0, RequireRestart = false,
            HintText = "{=IAD_IntervalDays_H}How many in‑game days between each disease sick tick. (Default: 1)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public int IntervalDays { get; set; } = 1;

        [SettingPropertyInteger(
            "{=IAD_MaxObjects}Max heroes/settlements per tick",
            1, 10000, "{VALUE}",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_MaxObjects_H}How many random heroes and settlements to check each tick. (Default: 50)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public int MaxObjects { get; set; } = 50;

        [SettingPropertyInteger(
            "{=IAD_IntervalDeathDays}Days between death checks",
            0, 81, "{VALUE}",
            Order = 2, RequireRestart = false,
            HintText = "{=IAD_IntervalDeathDays_H}How many being sick/injuried days between each disease/injury death tick. (Default: 1)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public int IntervalDeathDays { get; set; } = 1;

        [SettingPropertyFloatingInteger(
            "{=IAD_PurgeChance}AI purge chance base",
            0f, 1f, "#0.00%",
            Order = 4, RequireRestart = false,
            HintText = "{=IAD_PurgeChance_H}Base daily purge chance for diseased settlements. (Default: 0.02)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public float GetPurgeChance { get; set; } = 0.02f;

        [SettingPropertyInteger(
            "{=IAD_ReinfectCooldownDays}Immune days after recovery",
            0, 316, "{VALUE}",
            Order = 5, RequireRestart = false,
            HintText = "{=IAD_ReinfectCooldownDays_H}Days after recovery before you can be reinfected by any disease. (Default: 14)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public int ReinfectCooldownDays { get; set; } = 14;

        // ── Bodies ──────────────────────────────────────────────
        [SettingPropertyFloatingInteger(
            "{=IAD_BodiesChance}Chance for bodies event",
            0f, 1f, "#0.00%",
            Order = 0, RequireRestart = false,
            HintText = "{=IAD_BodiesChance_H}Base cleaning bodies event chance after settlement siege for player and npcs. (Default: 0.2)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_CLEAN_BODIES}Clean Bodies")]
        public float GetBodiesChance { get; set; } = 0.2f;

        [SettingPropertyInteger(
            "{=IAD_BodiesMorale}Morale Maulus",
            0, 100, "{VALUE}",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_BodiesMorale_H}Apply if hero decide to clean by themselves. (Default: 10)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_CLEAN_BODIES}Clean Bodies")]
        public int BodiesMorale { get; set; } = 10;

        [SettingPropertyInteger(
            "{=IAD_BodiesLoyalty}Settlement Loyalty Maulus",
            0, 100, "{VALUE}",
            Order = 2, RequireRestart = false,
            HintText = "{=IAD_BodiesLoyalty_H}Apply if hero decide to hire locals for cleaning. (Default: 10)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_CLEAN_BODIES}Clean Bodies")]
        public int BodiesLoyalty { get; set; } = 10;

        [SettingPropertyFloatingInteger(
            "{=IAD_BodiesGold}Hire Cost Multipler",
            0f, 5f, "{VALUE}",
            Order = 3, RequireRestart = false,
            HintText = "{=IAD_BodiesGold_H}Multipler applied to hire locals cost (which is random between 100 and settlement gold divided by 2) (Default: 0.3)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_CLEAN_BODIES}Clean Bodies")]
        public float BodiesCostMult { get; set; } = 0.3f;

        // ── Filters ──────────────────────────────────────────────

        [SettingPropertyBool(
            "{=IAD_AllowDisease}Enable disease mechanics",
            Order = 0, RequireRestart = false,
            HintText = "{=IAD_AllowDisease_H}Toggle whether diseases can occur. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FILTERS}Filters")]
        public bool AllowDisease { get; set; } = true;

        [SettingPropertyBool(
            "{=IAD_AllowInjury}Enable injury mechanics",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_AllowInjury_H}Toggle whether battle injuries can occur. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FILTERS}Filters")]
        public bool AllowInjury { get; set; } = true;

        [SettingPropertyBool(
            "{=IAD_AllowNotables}Include Notables",
            Order = 2, RequireRestart = false,
            HintText = "{=IAD_AllowNotables_H}Allow Notables to get sick or injured. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FILTERS}Filters")]
        public bool AllowNotables { get; set; } = true;

        [SettingPropertyBool(
            "{=IAD_AllowWanderers}Include Wanderers",
            Order = 3, RequireRestart = false,
            HintText = "{=IAD_AllowWanderers_H}Allow Wanderers to get sick or injured. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FILTERS}Filters")]
        public bool AllowWanderer { get; set; } = true;

        [SettingPropertyBool(
            "{=IAD_AllowChildren}Include Children",
            Order = 4, RequireRestart = false,
            HintText = "{=IAD_AllowChildren_H}Allow Children to get sick or injured. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FILTERS}Filters")]
        public bool AllowChildren { get; set; } = true;

        [SettingPropertyBool(
            "{=IAD_AllowPlayerClan}Include player clan",
            Order = 5, RequireRestart = false,
            HintText = "{=IAD_AllowPlayerClan_H}Allow members of your clan to get sick/injured. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FILTERS}Filters")]
        public bool AllowPlayerClan { get; set; } = true;

        [SettingPropertyBool(
            "{=IAD_AllowPlayer}Include player character",
            Order = 6, RequireRestart = false,
            HintText = "{=IAD_AllowPlayer_H}Allow your own character to get sick/injured. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FILTERS}Filters")]
        public bool AllowPlayer { get; set; } = true;

        // ── Buildings ──────────────────────────────────────────────
        [SettingPropertyText(
            "{=IAD_BuildingPositive}Positive Building Types",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_BuildingPositive_H}What building types should be decreasing negative effects? (Type English names) (Default: Fortifications, Aqueducts, Granary)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_BUILDINGS}Buildings")]
        public string BuildingPositive { get; set; } = "Fortifications, Aqueducts, Granary";

        [SettingPropertyText(
            "{=IAD_BuildingNegative}Negative Building Types",
            Order = 2, RequireRestart = false,
            HintText = "{=IAD_BuildingNegative_H}What building types should be increasing negative effects? (Default: Fairgrounds, Marketplace, Forum)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_BUILDINGS}Buildings")]
        public string BuildingNegative { get; set; } = "Fairgrounds, Marketplace, Forum";

        [SettingPropertyFloatingInteger(
            "{=IAD_BuildingChange}Base Change From Building",
            0f, 1f, "#0.00%",
            Order = 3, RequireRestart = false,
            HintText = "{=IAD_BuildingChange_H}How much percent of disease effect (or sick chance) should be substracted or added? (Default: 0.1)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_BUILDINGS}Buildings")]
        public float BuildingBonus { get; set; } = 0.1f;

        [SettingPropertyFloatingInteger(
            "{=IAD_LevelBuilding}Bonus From Level",
            0f, 1f,
            Order = 4, RequireRestart = false,
            HintText = "{=IAD_LevelBuilding_H}How much should be multipled change caused by building by each level? (Starting from 2nd) (Default: 0.1)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_BUILDINGS}Buildings")]
        public float LevelBuilding { get; set; } = 0.1f;


        // ── Debug / Inform ───────────────────────────────────────

        [SettingPropertyBool(
            "{=IAD_Debug}Enable debug logging",
            Order = 0, RequireRestart = false,
            HintText = "{=IAD_Debug_H}Extra Debug information. (Default: false)")]
        [SettingPropertyGroup("{=MCM_INFORM}Inform")]
        public bool Debug { get; set; } = false;

        [SettingPropertyBool(
            "{=IAD_Inform}Inform on all heroes",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_Inform_H}Notify when any hero gains or loses a status. (Default: false)")]
        [SettingPropertyGroup("{=MCM_INFORM}Inform")]
        public bool Inform { get; set; } = false;

        [SettingPropertyBool(
            "{=IAD_InformPlayerClan}Inform player clan",
            Order = 2, RequireRestart = false,
            HintText = "{=IAD_InformPlayerClan_H}Notify when your clan members change status. (Default: true)")]
        [SettingPropertyGroup("{=MCM_INFORM}Inform")]
        public bool InformPlayerClan { get; set; } = true;

        [SettingPropertyBool(
            "{=IAD_InformPlayer}Inform player character",
            Order = 3, RequireRestart = false,
            HintText = "{=IAD_InformPlayer_H}Notify when you yourself gain or lose a status. (Default: true)")]
        [SettingPropertyGroup("{=MCM_INFORM}Inform")]
        public bool InformPlayer { get; set; } = true;

        [SettingPropertyBool(
            "{=IAD_InformInfectedParty}Inform if you target infected?",
            Order = 4, RequireRestart = false,
            HintText = "{=IAD_InformInfectedParty_H}Notify when you target infected party/settlement. (Default: true)")]
        [SettingPropertyGroup("{=MCM_INFORM}Inform")]
        public bool InformInfectedParty { get; set; } = true;



        // ---Protect Heroes-------
        [SettingPropertyText(
            "{=IAD_DefendHeroes}Protect Heroes", HintText = "{=IAD_DefendHeroes_H}Write hero names who should be protected from death or unability to lead parties.(e.g Jon Snow, Harry Potter, Tynops etc)",
            Order = 0, RequireRestart = false)]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_PROTECT_HEROES}Protect Heroes")]
        public string ProtectHeroes { get; set; } = "";

        // ── Dialogue ───────────────────────────────────────
        [SettingPropertyBool(
            "{=IAD_DialogueDoctor}Dialogue 'Call Doctor' ",
            Order = 0, RequireRestart = false,
            HintText = "{=IAD_DialogueDoctor_H}Enable 'Call Doctor' dialogue (needs save reload). (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DIALOGUES}Dialogues")]
        public bool DialDoctor { get; set; } = true;

        [SettingPropertyFloatingInteger(
            "{=IAD_DialogueDoctorMult}Multipler To Cost",
            0f, 10f, "#0.00",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_DialogueDoctorMult_H}How much base cost will be multipled? (Base is amount between 100 and nearest settlement gold multipled by amount of diseases) (Default: 1)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DIALOGUES}Dialogues")]
        public float DoctorMult { get; set; } = 1f;

        [SettingPropertyFloatingInteger(
            "{=IAD_DialogueDoctorBonus}Multipler Bonus To Heal Chance",
            0f, 10f, "#0.00",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_DialogueDoctorBonus_H}Add to final heal chance bonus: final heal chance multipled by 1f + this value (Default: 0.1)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DIALOGUES}Dialogues")]
        public float DoctorBonus { get; set; } = 0.1f;

        [SettingPropertyInteger(
            "{=IAD_DialogueDoctorDays}Visit Day Cooldown",
            0, 365,
            Order = 2, RequireRestart = false,
            HintText = "{=IAD_DialogueDoctorDays_H}How much days need to wait for another visit? (Default: 7)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DIALOGUES}Dialogues")]
        public int DoctorDays { get; set; } = 7;

        // ── NPC doctors ───────────────────────────────────────
        [SettingPropertyBool(
            "{=IAD_NPCDoctor}NPCs visit doctors?",
            Order = 0, RequireRestart = false,
            HintText = "{=IAD_NPCDoctor_H}If true, npcs visit doctors. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DOCTORS}Doctors")]
        public bool NPCDoctor { get; set; } = true;

        [SettingPropertyFloatingInteger(
            "{=IAD_NPCDoctor_Chance}Base Chance To Visit",
            0f, 1f, "#0.00",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_NPCDoctor_Chance_H}Base chance for NPC to visit doctor. (Default: 0.5)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DOCTORS}Doctors")]
        public float NPCDoctorChance { get; set; } = 0.5f;

        // ── Infect Action ───────────────────────────────────────
        /*
        [SettingPropertyBool(
            "{=IAD_InfectAction}Infect Action",
            Order = 0, RequireRestart = false,
            HintText = "{=IAD_InfectAction_H}If true, player and npcs can infect other heroes with poison. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public bool InfectAction { get; set; } = true;

        [SettingPropertyInteger(
            "{=IAD_InfectActionDays}Infect Action Day Cooldown",
            0, 365,
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_InfectActionDays_H}How much days need to wait for infect action? (Default: 7)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public int InfectActionDays { get; set; } = 7;

        [SettingPropertyFloatingInteger(
            "{=IAD_InfectAction_NPC_Chance}Base Chance To Infect By AI",
            0f, 1f, "#0.00%",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_InfectAction_NPC_Chance_H}Base chance for NPC to infect other heroes. (Default: 0.5)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public float NPCInfectActionChance { get; set; } = 0.5f;

        [SettingPropertyFloatingInteger(
            "{=IAD_InfectAction_Chance}Base Success Chance To Infect",
            0f, 1f, "#0.00%",
            Order = 2, RequireRestart = false,
            HintText = "{=IAD_InfectAction_Chance_H}Base success chance for hereos to infect other heroes. It increases by having better roguery skill. (Default: 0.1)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public float InfectActionSuccess { get; set; } = 0.1f;

        [SettingPropertyFloatingInteger(
            "{=IAD_InfectAction_Discover_Chance}Base Find Out Chance",
            0f, 1f, "#0.00%",
            Order = 3, RequireRestart = false,
            HintText = "{=IAD_InfectAction_Discover_Chance_H}Base chance for finding out who tried/succesfully infected other hero. It decreases by having better roguery skill. (Default: 0.5)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public float InfectActionDiscover { get; set; } = 0.5f;

        [SettingPropertyFloatingInteger(
            "{=IAD_InfectAction_Caught_Chance}Base Getting Caught Chance",
            0f, 1f, "#0.00%",
            Order = 3, RequireRestart = false,
            HintText = "{=IAD_InfectAction_Caught_Chance_H}Base chance of getting caught if discovered. It decreases by having better roguery skill. (Default: 0.5)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public float InfectActionCaught { get; set; } = 0.5f;

        [SettingPropertyInteger(
            "{=IAD_InfectActionCost}Base Cost For Infect Action",
            0, 1000000000,
            Order = 4, RequireRestart = false,
            HintText = "{=IAD_InfectActionCost_H}Base cost for infect action. It increases by how bad, deadly disease is. (Default: 1000)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public int InfectActionCost { get; set; } = 1000;

        [SettingPropertyInteger(
            "{=IAD_InfectActionRelation}Relation Change With Victim",
            -100, 0,
            Order = 5, RequireRestart = false,
            HintText = "{=IAD_InfectActionRelation_H}Relation change between hero who poison/tried and victim. (Default: -100)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public int InfectActionRelation { get; set; } = -100;

        [SettingPropertyInteger(
            "{=IAD_InfectActionRelationClan}Relation Change With Victim's Clan",
            -100, 0,
            Order = 6, RequireRestart = false,
            HintText = "{=IAD_InfectActionRelationClan_H}Relation change between hero who poison/tried and victim's clan. (Default: -50)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public int InfectActionRelationClan { get; set; } = -50;

        [SettingPropertyInteger(
            "{=IAD_InfectActionRelationFriends}Relation Change With Victim's Friends",
            -100, 0,
            Order = 7, RequireRestart = false,
            HintText = "{=IAD_InfectActionRelationFriends_H}Relation change between hero who poison/tried and victim's friends. (Default: -25)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public int InfectActionRelationFriends { get; set; } = -25;

        [SettingPropertyInteger(
            "{=IAD_InfectActionWar}Start Of War?",
            0, 2,
            Order = 8, RequireRestart = false,
            HintText = "{=IAD_InfectActionWar_H}0 - poison doesn't mean starting war. 1 - war begins only if victim was from rulling clan. 2 - war begins always. (Default: 1)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_INFECT_ACTION}Infect Action")]
        public int InfectActionWar { get; set; } = 1;

        */

        public static List<string> ParseNameCsv(string raw)
        {
            return raw?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList()
            ?? new List<string>();
        }
    }
}
