using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace InjuriesAndDiseases
{
    public enum InfectWay
    {
        None,
        Contact,   // town‐visit, conversation, etc.
        Blood      // wounds, blood contact (battle, sex)
    }

    public class DiseaseConfig
    {
        public string Name { get; set; } = "";

        public float GetSickChance { get; set; } = 0f;

        /// <summary> Minus hitpoints. </summary>
        public int MinusHealthHitPointsFlat { get; set; } = 0;
        public float MinusHealthHitPointsMultiplier { get; set; } = 0f;

        /// <summary> Minus health regen. </summary>
        public float MinusHealthRegenFlat { get; set; } = 0f;
        public float MinusHealthRegenMultiplier { get; set; } = 0f;

        /// <summary> Fractional speed reduction . </summary>
        public float MinusSpeedFlat { get; set; } = 0f;
        public float MinusSpeedMultiplier { get; set; } = 0f;

        /// <summary> Fractional damage absorbed reduction . </summary>
        public float MinusDamageAbsorbFlat { get; set; } = 0f;
        public float MinusDamageAbsorbMultiplier { get; set; } = 0f;

        /// <summary> Fractional weapon damage reduction . </summary>
        public float MinusDamageFlat { get; set; } = 0f;
        public float MinusDamageMultiplier { get; set; } = 0f;
        /// <summary> Fractional weapon damage reduction . </summary>
        public float MinusPregnancyChanceFlat { get; set; } = 0f;
        public float MinusPregnancyChanceMultiplier { get; set; } = 0f;

        /// <summary> Fractional survival after battle reduction . </summary>
        public float MinusBattleSurvivalBonusMultipler { get; set; } = 0f;
        public float MinusBattleSurvivalBonusFlat { get; set; } = 0f;

        /// <summary> Fractional food consumption increase(e.g 0.2 would mean they eat more while -0.2 less). </summary>
        public float MinusFoodConsumptionMultipler { get; set; } = 0f;
        public float MinusFoodConsumptionFlat { get; set; } = 0f;

        /// <summary> Mobile Party troops penalty. </summary>
        public float MinusPartyTroopsMultipler { get; set; } = 0f;
        public int MinusPartyTroopsFlat { get; set; } = 0;

        /// <summary> Additional % chance of dying each day while afflicted. </summary>
        public float DeathChance { get; set; } = 0f;

        /// <summary> Chance [0.0–1.0] per contact to infect another hero. </summary>
        public float InfectChance { get; set; } = 0f;

        /// <summary> Chance [0.0–1.0] per day to recover. </summary>
        public float HealChance { get; set; } = 0f;

        /// <summary> Multipler for heal chance day after day. </summary>
        public float DayHealMultiplier { get; set; } = 0f;

        /// <summary> Minimum duration in days (0 = no minimum). </summary>
        public int MinDays { get; set; } = 0;

        /// <summary> Maximum duration in days. </summary>
        public int MaxDays { get; set; } = 0;

        /// <summary> Minuses for settlements </summary>
        public int MinusProsperityFlat { get; set; } = 0;
        public int MinusLoyaltyFlat { get; set; } = 0;
        public int MinusFoodFlat { get; set; } = 0;
        public int MinusGarrisonFlat { get; set; } = 0;
        public int MinusMilitiaFlat { get; set; } = 0;
        public int MinusSecurityFlat { get; set; } = 0;
        public int MinusHearthFlat { get; set; } = 0;
        public float MinusProsperityMultiplier { get; set; } = 0f;
        public float MinusLoyaltyMultiplier { get; set; } = 0f;
        public float MinusFoodMultiplier { get; set; } = 0f;
        public float MinusGarrisonMultiplier { get; set; } = 0f;
        public float MinusMilitiaMultiplier { get; set; } = 0f;
        public float MinusSecurityMultiplier { get; set; } = 0f;
        public float MinusHearthMultiplier { get; set; } = 0f;

        /// <summary> Which routes this disease can use to spread. </summary>
        public int InfectWays { get; set; } = (int)InfectWay.Contact;

        public int Serious { get; set; } = 0;

        //weather
        public float RainChance { get; set; } = 1f;
        public float SnowChance { get; set; } = 1f;
        public float DesertChance { get; set; } = 1f;

        public override string ToString()
            => new TextObject("{=IAD_" + Name + "}" + Name)
            .ToString();
    }

    public class InjuryConfig
    {
        public string Name { get; set; } = "";

        public float GetInjuryChance { get; set; } = 0f;

        /// <summary> Minus hitpoints. </summary>
        public int MinusHealthHitPointsFlat { get; set; } = 0;
        public float MinusHealthHitPointsMultiplier { get; set; } = 0f;

        /// <summary> Minus health regen. </summary>
        public float MinusHealthRegenFlat { get; set; } = 0f;
        public float MinusHealthRegenMultiplier { get; set; } = 0f;

        /// <summary> Fractional speed reduction . </summary>
        public float MinusSpeedFlat { get; set; } = 0f;
        public float MinusSpeedMultiplier { get; set; } = 0f;

        /// <summary> Fractional damage absorbed reduction . </summary>
        public float MinusDamageAbsorbFlat { get; set; } = 0f;
        public float MinusDamageAbsorbMultiplier { get; set; } = 0f;

        /// <summary> Fractional weapon damage reduction . </summary>
        public float MinusDamageFlat { get; set; } = 0f;
        public float MinusDamageMultiplier { get; set; } = 0f;

        /// <summary> Fractional survival after battle reduction . </summary>
        public float MinusBattleSurvivalBonusMultipler { get; set; } = 0f;
        public float MinusBattleSurvivalBonusFlat { get; set; } = 0f;

        /// <summary> Additional % chance of dying each day while afflicted. </summary>
        public float DeathChance { get; set; } = 0f;

        /// <summary> Chance [0.0–1.0] per day to recover. </summary>
        public float HealChance { get; set; } = 0f;

        /// <summary> Multipler for heal chance day after day. </summary>
        public float DayHealMultiplier { get; set; } = 0f;

        /// <summary> Minimum duration in days. </summary>
        public int MinDays { get; set; } = 0;

        /// <summary> Maximum duration in days. </summary>
        public int MaxDays { get; set; } = 0;

        public int Serious { get; set; } = 0;

        public override string ToString()
            => new TextObject("{=IAD_" + Name + "}" + Name)
            .ToString();
    }

    // ── Data container for JSON ─────────────────────────────────────
    public class EditorData
    {
        public List<DiseaseConfig> Diseases { get; set; } = new List<DiseaseConfig>();
        public List<InjuryConfig> Injuries { get; set; } = new List<InjuryConfig>();
    }

    // ── The MCM settings class ──────────────────────────────────────
    public sealed class InjuriesAndDiseasesEditor : AttributeGlobalSettings<InjuriesAndDiseasesEditor>
    {
        public override string Id => "InjuriesAndDiseasesEditor";
        public override string DisplayName => new TextObject("{=IAD_EDITOR_TITLE}Injuries & Diseases Editor").ToString();
        public override string FolderName => "InjuriesAndDiseasesEditor";
        public override string FormatType => "json";

        private const string FILE_NAME = "editor_config.json";

        private readonly string _editorFolder;
        private readonly string _filePath;

        // In‑memory lists of configs
        public List<DiseaseConfig> DiseasesList { get; private set; }
        public List<InjuryConfig> InjuriesList { get; private set; }

        // ── Constructor: load or create JSON ──────────────────────────
        public InjuriesAndDiseasesEditor()
        {
            var asm = InjuriesAndDiseasesModule._module;
            _editorFolder = Path.Combine(Path.GetDirectoryName(asm), "editor");
            Directory.CreateDirectory(_editorFolder);
            _filePath = Path.Combine(_editorFolder, FILE_NAME);

            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonConvert.DeserializeObject<EditorData>(json)
                        ?? new EditorData();
                DiseasesList = data.Diseases;
                InjuriesList = data.Injuries;
            }
            else
            {
                // ── FIRST RUN: populate your defaults here ────────────────
                DiseasesList = new List<DiseaseConfig>
                {
            new DiseaseConfig
            {
                Name = "Common Cold",
                GetSickChance = 0.2f,
                RainChance = 1.05f,
                DesertChance = 0.8f,
                SnowChance = 1.2f,
                MinusHealthHitPointsFlat = 5,
                MinusHealthHitPointsMultiplier = 0,
                MinusHealthRegenFlat = 0,
                MinusHealthRegenMultiplier = 0.1f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.05f,
                DeathChance = 0.0f,
                InfectChance = 0.2f,
                HealChance = 0.05f,
                DayHealMultiplier = 1.1f,
                MinDays = 3,
                MaxDays = 7,
                MinusFoodConsumptionFlat = 1,
                MinusFoodConsumptionMultipler = 0.1f,
                MinusProsperityFlat = 0,
                MinusLoyaltyFlat = 0,
                MinusFoodFlat = 0,
                MinusGarrisonFlat = 0,
                MinusProsperityMultiplier = 0,
                MinusLoyaltyMultiplier = 0,
                InfectWays = (int)InfectWay.Contact,
                Serious = 0
            },
            new DiseaseConfig
            {
                Name = "Influenza",
                GetSickChance = 0.08f,
                RainChance = 1.1f,
                DesertChance = 0.85f,
                SnowChance = 1.3f,
                MinusHealthHitPointsFlat = 10,
                MinusHealthHitPointsMultiplier = 0,
                MinusHealthRegenFlat = 1,
                MinusHealthRegenMultiplier = 0.2f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.1f,
                MinusDamageFlat = 0,
                MinusDamageMultiplier = 0.1f,
                DeathChance = 0.02f,
                InfectChance = 0.3f,
                HealChance = 0.04f,
                DayHealMultiplier = 1.05f,
                MinDays = 5,
                MaxDays = 14,
                MinusHearthFlat = 1,
                MinusFoodConsumptionFlat = 2,
                MinusFoodConsumptionMultipler = 0.15f,
                MinusProsperityFlat = 1,
                MinusProsperityMultiplier = 0.02f,
                InfectWays = (int)InfectWay.Contact,
                Serious = 1
            },
            new DiseaseConfig
            {
                Name = "Cholera",
                GetSickChance = 0.04f,
                RainChance = 1.8f,
                DesertChance = 0.6f,
                SnowChance = 0.5f,
                MinusHealthHitPointsFlat = 20,
                MinusHealthHitPointsMultiplier = 0,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.15f,
                MinusBattleSurvivalBonusFlat = 0.05f,
                DeathChance = 0.1f,
                InfectChance = 0.25f,
                HealChance = 0.02f,
                DayHealMultiplier = 1.0f,
                MinDays = 2,
                MaxDays = 10,
                MinusFoodFlat = 2,
                MinusFoodMultiplier = 0.1f,
                MinusSecurityFlat = 1,
                MinusSecurityMultiplier = 0.05f,
                InfectWays = (int) InfectWay.Contact,
                Serious = 1
            },
            new DiseaseConfig
            {
                Name = "Plague",
                GetSickChance = 0.02f,
                RainChance = 1.1f,
                DesertChance = 0.9f,
                SnowChance = 0.95f,
                MinusHealthHitPointsFlat = 30,
                MinusHealthHitPointsMultiplier = 0,
                MinusHealthRegenFlat = 2,
                MinusHealthRegenMultiplier = 0.3f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.2f,
                MinusDamageAbsorbFlat = 0.05f,
                MinusDamageAbsorbMultiplier = 0,
                DeathChance = 0.2f,
                InfectChance = 0.5f,
                HealChance = 0.01f,
                DayHealMultiplier = 1.0f,
                MinDays = 5,
                MaxDays = 15,
                MinusHearthFlat = 3,
                MinusProsperityFlat = 5,
                MinusProsperityMultiplier = 0.1f,
                MinusLoyaltyFlat = 3,
                MinusLoyaltyMultiplier = 0.05f,
                InfectWays = (int) InfectWay.Contact,
                Serious = 1
            },
            new DiseaseConfig
            {
                Name = "Typhoid",
                GetSickChance = 0.03f,
                RainChance = 1.6f,
                DesertChance = 0.6f,
                SnowChance = 0.5f,
                MinusHealthHitPointsFlat = 15,
                MinusHealthHitPointsMultiplier = 0,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.1f,
                MinusDamageFlat = 0,
                MinusDamageMultiplier = 0.05f,
                DeathChance = 0.05f,
                InfectChance = 0.15f,
                HealChance = 0.03f,
                DayHealMultiplier = 1.05f,
                MinDays = 7,
                MaxDays = 21,
                MinusFoodFlat = 3,
                MinusFoodMultiplier = 0.2f,
                InfectWays = (int) InfectWay.Contact,
                Serious = 1
            },
            new DiseaseConfig
            {
                Name = "Tuberculosis",
                GetSickChance = 0.01f,
                RainChance = 1.05f,
                DesertChance = 0.9f,
                SnowChance = 1.1f,
                MinusHealthHitPointsFlat = 5,
                MinusHealthHitPointsMultiplier = 0.05f,
                MinusHealthRegenFlat = 1,
                MinusHealthRegenMultiplier = 0.2f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.1f,
                DeathChance = 0.15f,
                InfectChance = 0.1f,
                HealChance = 0.02f,
                DayHealMultiplier = 1.02f,
                MinDays = 30,
                MaxDays = 120,
                MinusHearthFlat = 1,
                MinusProsperityFlat = 2,
                MinusProsperityMultiplier = 0.05f,
                InfectWays = (int) InfectWay.Contact,
                Serious = 1
            },
            new DiseaseConfig
            {
                Name = "Malaria",
                GetSickChance = 0.06f,
                RainChance = 2.5f,
                DesertChance = 0.1f,
                SnowChance = 0.05f,
                MinusHealthHitPointsFlat = 10,
                MinusHealthRegenFlat = 1,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.1f,
                DeathChance = 0.08f,
                InfectChance = 0.2f,
                HealChance = 0.03f,
                DayHealMultiplier = 1.03f,
                MinDays = 10,
                MaxDays = 30,
                MinusPartyTroopsFlat = 10,
                MinusPartyTroopsMultipler = 0.05f,
                MinusMilitiaFlat = 10,
                MinusGarrisonFlat = 10,
                MinusGarrisonMultiplier = 0.05f,
                InfectWays = (int) InfectWay.Blood,
                Serious = 1
            },
            new DiseaseConfig
            {
                Name = "Smallpox",
                GetSickChance = 0.05f,
                RainChance = 1.05f,
                DesertChance = 0.85f,
                SnowChance = 1.1f,
                MinusHealthHitPointsFlat = 20,
                MinusHealthRegenFlat = 2,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.15f,
                DeathChance = 0.12f,
                InfectChance = 0.3f,
                HealChance = 0.02f,
                DayHealMultiplier = 1.02f,
                MinDays = 15,
                MaxDays = 40,
                MinusProsperityFlat = 1,
                MinusHearthFlat = 2,
                MinusHearthMultiplier = 0.05f,
                InfectWays = (int) InfectWay.Contact,
                Serious = 1
            },
            new DiseaseConfig
            {
                Name = "AIDS",
                GetSickChance = 0.001f,
                RainChance = 1f,
                DesertChance = 1f,
                SnowChance = 1f,
                MinusHealthHitPointsFlat = 0,
                MinusHealthHitPointsMultiplier = 0.2f,
                MinusHealthRegenFlat = 2,
                MinusHealthRegenMultiplier = 0.3f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.2f,
                MinusPregnancyChanceFlat = 0.5f,
                MinusPregnancyChanceMultiplier = 0,
                DeathChance = 0.25f,
                InfectChance = 0.05f,
                HealChance = 0.0f,
                DayHealMultiplier = 1.0f,
                MinDays = 0,
                MaxDays = 0,   // no auto‑heal
                MinusFoodFlat = 1,
                MinusHearthFlat = 1,
                MinusFoodMultiplier = 0.1f,
                MinusProsperityFlat = 1,
                MinusSecurityFlat = 1,
                InfectWays = (int) InfectWay.Blood,
                Serious = 1
            },
            new DiseaseConfig
            {
                Name = "Measles",
                GetSickChance = 0.1f,
                RainChance = 1.2f,
                DesertChance = 0.85f,
                SnowChance = 1.15f,
                MinusHealthHitPointsFlat = 8,
                MinusHealthHitPointsMultiplier = 0,
                MinusHealthRegenFlat = 0,
                MinusHealthRegenMultiplier = 0.1f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.05f,
                DeathChance = 0.01f,
                InfectChance = 0.6f,
                HealChance = 0.03f,
                DayHealMultiplier = 1.02f,
                MinDays = 7,
                MaxDays = 21,
                MinusHearthFlat = 1,
                MinusProsperityFlat = 1,
                InfectWays = (int)InfectWay.Contact,
                Serious = 0
            },

            new DiseaseConfig
            {
                Name = "Hepatitis",
                GetSickChance = 0.015f,
                RainChance = 1.3f,
                DesertChance = 0.8f,
                SnowChance = 0.8f,
                MinusHealthHitPointsFlat = 12,
                MinusHealthHitPointsMultiplier = 0.1f,
                MinusHealthRegenFlat = 1,
                MinusHealthRegenMultiplier = 0.15f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.08f,
                DeathChance = 0.08f,
                InfectChance = 0.08f,
                HealChance = 0.01f,
                DayHealMultiplier = 1.01f,
                MinDays = 30,
                MaxDays = 180,
                MinusProsperityFlat = 2,
                MinusHearthFlat = 1,
                InfectWays = (int)InfectWay.Blood,
                Serious = 1
            },

            new DiseaseConfig
            {
                Name = "Rabies",
                GetSickChance = 0.001f,
                RainChance = 1f,
                DesertChance = 0.7f,
                SnowChance = 0.9f,
                MinusHealthHitPointsFlat = 40,
                MinusHealthHitPointsMultiplier = 0,
                MinusHealthRegenFlat = 0,
                MinusHealthRegenMultiplier = 0.0f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.4f,
                DeathChance = 0.95f,
                InfectChance = 0.5f,
                HealChance = 0.0f,
                DayHealMultiplier = 1.0f,
                MinDays = 1,
                MaxDays = 14,
                MinusSecurityFlat = 2,
                InfectWays = (int)InfectWay.Blood,
                Serious = 1
            },

            new DiseaseConfig
            {
                Name = "Hemorrhagic Fever",
                GetSickChance = 0.005f,
                RainChance = 1.2f,
                DesertChance = 0.6f,
                SnowChance = 0.8f,
                MinusHealthHitPointsFlat = 50,
                MinusHealthHitPointsMultiplier = 0,
                MinusHealthRegenFlat = 3,
                MinusHealthRegenMultiplier = 0.3f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.5f,
                DeathChance = 0.50f,
                InfectChance = 0.35f,
                HealChance = 0.01f,
                DayHealMultiplier = 1.00f,
                MinDays = 5,
                MaxDays = 25,
                MinusProsperityFlat = 5,
                MinusLoyaltyFlat = 4,
                InfectWays = (int)InfectWay.Blood,
                Serious = 1
            },

            new DiseaseConfig
            {
                Name = "Dengue",
                GetSickChance = 0.03f,
                RainChance = 2.5f,
                DesertChance = 0.1f,
                SnowChance = 0.05f,
                MinusHealthHitPointsFlat = 12,
                MinusHealthHitPointsMultiplier = 0,
                MinusHealthRegenFlat = 1,
                MinusHealthRegenMultiplier = 0.05f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.1f,
                DeathChance = 0.02f,
                InfectChance = 0.15f,
                HealChance = 0.04f,
                DayHealMultiplier = 1.02f,
                MinDays = 7,
                MaxDays = 21,
                MinusPartyTroopsFlat = 5,
                MinusMilitiaFlat = 5,
                MinusGarrisonFlat = 5,
                MinusProsperityFlat = 1,
                InfectWays = (int)InfectWay.Blood,
                Serious = 0
            },

            new DiseaseConfig
            {
                Name = "Leprosy",
                GetSickChance = 0.002f,
                RainChance = 1f,
                DesertChance = 0.9f,
                SnowChance = 1f,
                MinusHealthHitPointsFlat = 0,
                MinusHealthHitPointsMultiplier = 0.05f,
                MinusHealthRegenFlat = 0,
                MinusHealthRegenMultiplier = 0.05f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.05f,
                DeathChance = 0.01f,
                InfectChance = 0.02f,
                HealChance = 0.005f,
                DayHealMultiplier = 1.005f,
                MinDays = 40,
                MaxDays = 365,
                MinusProsperityFlat = 1,
                MinusLoyaltyFlat = 2,
                InfectWays = (int)InfectWay.Contact,
                Serious = 0
            }
                };

                InjuriesList = new List<InjuryConfig>
                {
            new InjuryConfig
            {
                Name = "Broken Arm",
                GetInjuryChance = 0.3f,
                MinusHealthHitPointsFlat = 20,
                MinusHealthHitPointsMultiplier = 0,
                MinusHealthRegenFlat = 1,
                MinusHealthRegenMultiplier = 0.1f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.1f,
                DeathChance = 0.0f,
                HealChance = 0.05f,
                DayHealMultiplier = 1.2f,
                MinDays = 7,
                MaxDays = 30,
                Serious = 0
            },
            new InjuryConfig
            {
                Name = "Deep Cut",
                GetInjuryChance = 0.8f,
                MinusHealthHitPointsFlat = 15,
                MinusHealthRegenFlat = 2,
                MinusHealthRegenMultiplier = 0,
                MinusDamageFlat = 0,
                MinusDamageMultiplier = 0.1f,
                DeathChance = 0.0f,
                HealChance = 0.08f,
                DayHealMultiplier = 1.1f,
                MinDays = 3,
                MaxDays = 14,
                Serious = 0
            },
            new InjuryConfig
            {
                Name = "Sprained Ankle",
                GetInjuryChance = 0.5f,
                MinusHealthHitPointsFlat = 5,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.2f,
                DeathChance = 0.0f,
                HealChance = 0.15f,
                DayHealMultiplier = 1.3f,
                MinDays = 2,
                MaxDays = 10,
                Serious = 0
            },
            new InjuryConfig
            {
                Name = "Concussion",
                GetInjuryChance = 0.2f,
                MinusHealthHitPointsFlat = 10,
                MinusHealthRegenFlat = -1,
                MinusHealthRegenMultiplier = 0.2f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.1f,
                DeathChance = 0.0f,
                HealChance = 0.04f,
                DayHealMultiplier = 1.1f,
                MinDays = 5,
                MaxDays = 20,
                Serious = 1
            },
            new InjuryConfig
            {
                Name = "Broken Leg",
                GetInjuryChance = 0.15f,
                MinusHealthHitPointsFlat = 25,
                MinusHealthRegenFlat = 2,
                MinusHealthRegenMultiplier = 0.1f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.3f,
                DeathChance = 0.0f,
                HealChance = 0.03f,
                DayHealMultiplier = 1.2f,
                MinDays = 14,
                MaxDays = 60,
                Serious = 1
            },
            new InjuryConfig
            {
                Name = "Severe Burn",
                GetInjuryChance = 0.1f,
                MinusHealthHitPointsFlat = 20,
                MinusHealthRegenFlat = 3,
                MinusHealthRegenMultiplier = 0,
                MinusDamageAbsorbFlat = 0.1f,
                MinusDamageAbsorbMultiplier = 0,
                DeathChance = 0.0f,
                HealChance = 0.06f,
                DayHealMultiplier = 1.15f,
                MinDays = 7,
                MaxDays = 30,
                Serious = 0
            },
            new InjuryConfig
            {
                Name = "Fractured Skull",
                GetInjuryChance = 0.05f,
                MinusHealthHitPointsFlat = 30,
                MinusHealthRegenFlat = 3,
                MinusHealthRegenMultiplier = 0.2f,
                MinusSpeedFlat = 0,
                MinusSpeedMultiplier = 0.2f,
                DeathChance = 0.0f,
                HealChance = 0.02f,
                DayHealMultiplier = 1.05f,
                MinDays = 30,
                MaxDays = 90,
                Serious = 1
            }
                };
                SaveToFile();
            }

            RefreshDiseaseDropdown();
            RefreshInjuryDropdown();
        }

        private void SaveToFile()
        {
            var data = new EditorData
            {
                Diseases = DiseasesList,
                Injuries = InjuriesList
            };
            File.WriteAllText(_filePath,
                JsonConvert.SerializeObject(data, Formatting.Indented)
            );
        }

        private void RefreshDiseaseDropdown()
        {
            // make a fresh copy of all DiseaseConfig objects
            var list = DiseasesList.ToList();
            // find the old selection index (or 0 if new)
            var old = _diseaseSelector?.SelectedValue;
            var newIndex = list.IndexOf(old ?? list.FirstOrDefault());
            if (newIndex < 0) newIndex = 0;

            // create a Dropdown<DiseaseConfig> backed by the *objects* themselves
            _diseaseSelector = new Dropdown<DiseaseConfig>(list, newIndex);

            // notify MCM/UI that your public selector has changed
            OnPropertyChanged(nameof(DiseaseSelector));
        }

        private void RefreshInjuryDropdown()
        {
            var list = InjuriesList.ToList();
            var old = _injurySelector?.SelectedValue;
            var newIndex = list.IndexOf(old ?? list.FirstOrDefault());
            if (newIndex < 0) newIndex = 0;

            // create a Dropdown<DiseaseConfig> backed by the *objects* themselves
            _injurySelector = new Dropdown<InjuryConfig>(list, newIndex);

            // notify MCM/UI that your public selector has changed
            OnPropertyChanged(nameof(InjurySelector));
        }

        // ── backing fields ────────────────────────────────────────────────
        private Dropdown<DiseaseConfig> _diseaseSelector;
        private DiseaseConfig _lastSelectedDisease;

        // convenience:
        private DiseaseConfig CurrentDisease =>
            DiseasesList.ElementAtOrDefault(_diseaseSelector.SelectedIndex)
            ?? new DiseaseConfig();

        private Dropdown<InjuryConfig> _injurySelector;
        private InjuryConfig _lastSelectedInjury;

        private InjuryConfig CurrentInjury =>
            InjuriesList.ElementAtOrDefault(_injurySelector.SelectedIndex)
            ?? new InjuryConfig();

        // ── DISEASES TAB ─────────────────────────────────────────────

        [SettingPropertyDropdown(
            "{=IAD_SelectDisease}Select Disease",
            Order = 0, RequireRestart = false,
            HintText = "{=IAD_SelectDisease_H}Pick which disease to edit")]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases")]
        public Dropdown<DiseaseConfig> DiseaseSelector
        {
            get
            {
                // MCM has updated SelectedValue/Index under the hood?
                var current = _diseaseSelector.SelectedValue;
                if (current != _lastSelectedDisease)
                {
                    _lastSelectedDisease = current;
                    OnPropertyChanged(nameof(CurrentDisease));
                }
                return _diseaseSelector;
            }
        }

        [SettingPropertyButton(
            "{=IAD_AddDisease}Add New Disease",
            Content = "{=IAD_Add}Add",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_AddDisease_H}Append a blank new disease")]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases")]
        public Action AddDiseaseButton
        {
            get;
            set;
        } = () =>
        {
            Instance.DiseasesList.Add(new DiseaseConfig { Name = "New Disease" });
            Instance.RefreshDiseaseDropdown();
            Instance.SaveToFile();
        };

        [SettingPropertyButton(
            "{=IAD_DeleteDisease}Delete Disease",
            Content = "{=IAD_Delete}Delete",
            Order = 2, RequireRestart = false,
            HintText = "{=IAD_DeleteDisease_H}Remove the selected disease")]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases")]
        public Action DeleteDiseaseButton
        {
            get;
            set;
        } = () =>
        {
            if (Instance.DiseaseSelector.SelectedIndex.InRange(0, Instance.DiseasesList.Count - 1))
            {
                Instance.DiseasesList.RemoveAt(Instance.DiseaseSelector.SelectedIndex);
                Instance.RefreshDiseaseDropdown();
                Instance.SaveToFile();
            }
        };

        [SettingPropertyButton(
            "{=IAD_ClearDiseases}Clear All Diseases",
            Content = "{=IAD_Clear}Clear",
            Order = 3, RequireRestart = false,
            HintText = "{=IAD_ClearDiseases_H}Remove all diseases except the first one.")]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases")]
        public Action ClearDiseasesButton
        {
            get;
            set;
        } = () =>
        {
            // keep index 0, remove everything else
            if (Instance.DiseasesList.Count > 1)
            {
                Instance.DiseasesList.RemoveRange(1, Instance.DiseasesList.Count - 1);
                Instance.RefreshDiseaseDropdown();
                Instance.SaveToFile();
                InformationManager.DisplayMessage(
                    new InformationMessage("[IDD] Cleared all diseases (except first)", Colors.Green)
                );
            }
        };

        [SettingPropertyText(
            "{=IAD_DiseaseName}Name",
            Order = 10, RequireRestart = false,
            HintText = "{=IAD_DiseaseName_H}Unique ID for this disease"
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public string DiseaseName
        {
            get => CurrentDisease.Name;
            set { CurrentDisease.Name = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_SickChance}Daily sick chance",
            0f, 1f, "#0.00%",
            Order = 11, RequireRestart = false,
            HintText = "{=IAD_SickChance_H}Chance per check that a healthy hero/settlement gets sick from disease.")]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseGetSickChance
        {
            get => CurrentDisease.GetSickChance;
            set { CurrentDisease.GetSickChance = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_RainChance}Rain Chance Modifier",
            0f, 5f, "#0.00",
            Order = 11, RequireRestart = false,
            HintText = "{=IAD_RainChance_H}Multiply get sick chance and infect chance by this value if raining or near water. (1 = no change to chance)")]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseRainChance
        {
            get => CurrentDisease.RainChance;
            set { CurrentDisease.RainChance = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_SnowChance}Snow Chance Modifier",
            0f, 5f, "#0.00",
            Order = 11, RequireRestart = false,
            HintText = "{=IAD_SnowChance_H}Multiply get sick chance and infect chance by this value if near snow. (1 = no change to chance)")]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseSnowChance
        {
            get => CurrentDisease.SnowChance;
            set { CurrentDisease.SnowChance = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DesertChance}Desert Chance Modifier",
            0f, 5f, "#0.00",
            Order = 11, RequireRestart = false,
            HintText = "{=IAD_DesertChance_H}Multiply get sick chance and infect chance by this value if near desert. (1 = no change to chance)")]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseDesertChance
        {
            get => CurrentDisease.DesertChance;
            set { CurrentDisease.DesertChance = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_MinDays}Min Days", 0, 365, HintText = "{=IAD_MinDays_H}Disease/Injury will last at least this many days",
            Order = 12, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public int DiseaseMinDays
        {
            get => CurrentDisease.MinDays;
            set { CurrentDisease.MinDays = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_MaxDays}Max Days", 0, 3650, HintText = "{=IAD_MaxDays_H}Disease/Injury will auto‐resolve after at most this many days (if 0 no auto-resolve)",
            Order = 12, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public int DiseaseMaxDays
        {
            get => CurrentDisease.MaxDays;
            set { CurrentDisease.MaxDays = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealthHPFlat}–HP Flat", -100, 100, "{VALUE}", HintText = "{=IAD_HealthHPFlat_H}Substract this value from MaxHP",
            Order = 13, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusHealthHitPointsFlat
        {
            get => CurrentDisease.MinusHealthHitPointsFlat;
            set { CurrentDisease.MinusHealthHitPointsFlat = (int)value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealthHPMult}–HP Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_HealthHPMult_H}Multiply max HP by (1 - this value)",
            Order = 14, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusHealthHitPointsMultiplier
        {
            get => (float)CurrentDisease.MinusHealthHitPointsMultiplier;
            set { CurrentDisease.MinusHealthHitPointsMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealthRegenFlat}–Regen Flat", -10, 10, "{VALUE}", HintText = "{=IAD_HealthRegenFlat_H}Substract this value from Health Regen",
            Order = 15, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusHealthRegenFlat
        {
            get => (float)CurrentDisease.MinusHealthRegenFlat;
            set { CurrentDisease.MinusHealthRegenFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealthRegenMult}–Regen Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_HealthRegenMult_H}Multiply Health Regen by (1 - this value)",
            Order = 16, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusHealthRegenMultiplier
        {
            get => (float)CurrentDisease.MinusHealthRegenMultiplier;
            set { CurrentDisease.MinusHealthRegenMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_SpeedFlat}–Speed Flat", -1f, 1f, "{VALUE}", HintText = "{=IAD_SpeedFlat_H}Substract this value from Speed movement",
            Order = 17, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusSpeedFlat
        {
            get => (float)CurrentDisease.MinusSpeedFlat;
            set { CurrentDisease.MinusSpeedFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_SpeedMult}–Speed Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_SpeedMult_H}Multiply Speed movement by (1 - this value)",
            Order = 18, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusSpeedMultiplier
        {
            get => (float)CurrentDisease.MinusSpeedMultiplier;
            set { CurrentDisease.MinusSpeedMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DmgAbsorbFlat}–Damage Absorb Flat", -1f, 1f, "{VALUE}", HintText = "{=IAD_DmgAbsorbFlat_H}Substract this value from Damage Absorb",
            Order = 19, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusDamageAbsorbFlat
        {
            get => (float)CurrentDisease.MinusDamageAbsorbFlat;
            set { CurrentDisease.MinusDamageAbsorbFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DmgAbsorbMult}–Damage Absorb Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_DmgAbsorbMult_H}Multiply Damage Absorb by (1 - this value)",
            Order = 20, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusDamageAbsorbMultiplier
        {
            get => (float)CurrentDisease.MinusDamageAbsorbMultiplier;
            set { CurrentDisease.MinusDamageAbsorbMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DmgFlat}–Weapon Damage Flat", -100, 100, "{VALUE}", HintText = "{=IAD_DmgFlat_H}Substract this value from Weapon Damage",
            Order = 21, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusDamageFlat
        {
            get => (float)CurrentDisease.MinusDamageFlat;
            set { CurrentDisease.MinusDamageFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DmgMult}–Weapon Damage Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_DmgMult_H}Multiply Weapon Damage by (1 - this value)",
            Order = 22, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusDamageMultiplier
        {
            get => (float)CurrentDisease.MinusDamageMultiplier;
            set { CurrentDisease.MinusDamageMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_PregFlat}–Pregnancy Chance Flat", -1f, 1f, "{VALUE}", HintText = "{=IAD_PregFlat_H}Substract this value from Pregnancy Chance",
            Order = 23, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusPregnancyChanceFlat
        {
            get => (float)CurrentDisease.MinusPregnancyChanceFlat;
            set { CurrentDisease.MinusPregnancyChanceFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_PregMult}–Pregnancy Chance Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_PregMult_H}Multiply Pregnancy Chance by (1 - this value)",
            Order = 24, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusPregnancyChanceMultiplier
        {
            get => (float)CurrentDisease.MinusPregnancyChanceMultiplier;
            set { CurrentDisease.MinusPregnancyChanceMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_BattleSurvFlat}–Battle Survival Flat", -1f, 1f, "{VALUE}", HintText = "{=IAD_BattleSurvFlat_H}Substract this value from Battle Survival(chance of not dying)",
            Order = 25, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusBattleSurvivalBonusFlat
        {
            get => (float)CurrentDisease.MinusBattleSurvivalBonusFlat;
            set { CurrentDisease.MinusBattleSurvivalBonusFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_BattleSurvMult}–Battle Survival Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_BattleSurvMult_H}Multiply Battle Survival(chance of not dying after battle) by (1 - this value)",
            Order = 26, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusBattleSurvivalBonusMultiplier
        {
            get => (float)CurrentDisease.MinusBattleSurvivalBonusMultipler;
            set { CurrentDisease.MinusBattleSurvivalBonusMultipler = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_FoodFlat}–Food Consumption Flat", -10f, 10f, "{VALUE}", HintText = "{=IAD_FoodFlat_H}Substract this value from Food Consumption",
            Order = 27, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusFoodConsumptionFlat
        {
            get => (float)CurrentDisease.MinusFoodConsumptionFlat;
            set { CurrentDisease.MinusFoodConsumptionFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_FoodMult}–Food Consumption Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_FoodMult_H}Multiply Food Consumption by (1 - this value)",
            Order = 28, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusFoodConsumptionMultiplier
        {
            get => (float)CurrentDisease.MinusFoodConsumptionMultipler;
            set { CurrentDisease.MinusFoodConsumptionMultipler = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_PartyTroopsFlat}–Party Troops Flat", -10, 10, "{VALUE}", HintText = "{=IAD_PartyTroopsFlat_H}Infect (wound) such amount of troops in infected party and then kill if death roll says so.",
            Order = 28, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public int DiseaseMinusPartyTroopsFlat
        {
            get => CurrentDisease.MinusPartyTroopsFlat;
            set { CurrentDisease.MinusPartyTroopsFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_PartyTroopsMult}–Party Troops Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_PartyTroopsMult_H}Infect (wound) total troops in infected party multipled (1 - this value) and then kill if death roll says so.",
            Order = 28, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseMinusPartyTroopsMultipler
        {
            get => (float)CurrentDisease.MinusPartyTroopsMultipler;
            set { CurrentDisease.MinusPartyTroopsMultipler = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DeathChance}Death Chance", 0f, 1f, "#0.00%", HintText = "{=IAD_DeathChance_H}Base death chance",
            Order = 29, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseDeathChance
        {
            get => (float)CurrentDisease.DeathChance;
            set { CurrentDisease.DeathChance = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_InfectChance}Infect Chance", 0f, 1f, "#0.00%", HintText = "{=IAD_InfectChance_H}Base infect chance",
            Order = 30, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseInfectChance
        {
            get => (float)CurrentDisease.InfectChance;
            set { CurrentDisease.InfectChance = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealChance}Heal Chance", 0f, 1f, "#0.00%", HintText = "{=IAD_HealChance_H}Base healing chance",
            Order = 31, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseHealChance
        {
            get => (float)CurrentDisease.HealChance;
            set { CurrentDisease.HealChance = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DayHealMult}Heal Day Multiplier", 0f, 2f, "#0.00", HintText = "{=IAD_DayHealMult_H}Multiply Heal Chance by (this value raised to the power of sick days)",
            Order = 32, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public float DiseaseDayHealMultiplier
        {
            get => (float)CurrentDisease.DayHealMultiplier;
            set { CurrentDisease.DayHealMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_InfectWays}Infect Ways", 0, 2, "{VALUE}",
            Order = 33, RequireRestart = false,
            HintText = "{=IAD_InfectWays_H}0=None,1=Contact,2=Blood"
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public int DiseaseInfectWays
        {
            get => CurrentDisease.InfectWays;
            set { CurrentDisease.InfectWays = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_Serious}Is Serious?", 0, 1, "{VALUE}",
            Order = 34, RequireRestart = false,
            HintText = "{=IAD_Serious_H}If 1, hero will try to not lead a party."
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit")]
        public int DiseaseSerious
        {
            get => CurrentDisease.Serious;
            set { CurrentDisease.Serious = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_ProsperFlat}–Prosperity Flat", -50, 50, "{VALUE}", HintText = "{=IAD_ProsperFlat_H}Substract this value from Prosperity",
            Order = 34, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public int DiseaseMinusProsperityFlat
        {
            get => CurrentDisease.MinusProsperityFlat;
            set { CurrentDisease.MinusProsperityFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_ProsperMult}–Prosperity Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_ProsperMult_H}Multiply Prosperity by (1 - this value)",
            Order = 35, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public float DiseaseMinusProsperityMultiplier
        {
            get => (float)CurrentDisease.MinusProsperityMultiplier;
            set { CurrentDisease.MinusProsperityMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_LoyalFlat}–Loyalty Flat", -50, 50, "{VALUE}", HintText = "{=IAD_LoyalFlat_H}Substract this value from Loyalty",
            Order = 36, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public int DiseaseMinusLoyaltyFlat
        {
            get => CurrentDisease.MinusLoyaltyFlat;
            set { CurrentDisease.MinusLoyaltyFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_LoyalMult}–Loyalty Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_LoyalMult_H}Multiply Loyalty by (1 - this value)",
            Order = 37, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public float DiseaseMinusLoyaltyMultiplier
        {
            get => (float)CurrentDisease.MinusLoyaltyMultiplier;
            set { CurrentDisease.MinusLoyaltyMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_FoodFlat2}–Food Flat", -50, 50, "{VALUE}", HintText = "{=IAD_FoodFlat2_H}Substract this value from Foodstocks",
            Order = 38, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public int DiseaseMinusFoodFlat
        {
            get => CurrentDisease.MinusFoodFlat;
            set { CurrentDisease.MinusFoodFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_FoodMult2}–Food Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_FoodMult2_H}Multiply Foodstocks by (1 - this value)",
            Order = 39, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public float DiseaseMinusFoodMultiplier
        {
            get => (float)CurrentDisease.MinusFoodMultiplier;
            set { CurrentDisease.MinusFoodMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_GarrisonFlat}–Garrison Flat", 0, 100, "{VALUE}", HintText = "{=IAD_GarrisonFlat_H}Substract this value from Garrison",
            Order = 40, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public int DiseaseMinusGarrisonFlat
        {
            get => CurrentDisease.MinusGarrisonFlat;
            set { CurrentDisease.MinusGarrisonFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_GarrisonMult}–Garrison Multiplier", 0f, 1f, "#0.0", HintText = "{=IAD_GarrisonMult_H}Multiply Garrison by (1 - this value)",
            Order = 41, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public float DiseaseMinusGarrisonMultiplier
        {
            get => (float)CurrentDisease.MinusGarrisonMultiplier;
            set { CurrentDisease.MinusGarrisonMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_MilitiaFlat}–Militia Flat", 0, 100, "{VALUE}", HintText = "{=IAD_MilitiaFlat_H}Substract this value from Militia",
            Order = 42, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public int DiseaseMinusMilitiaFlat
        {
            get => CurrentDisease.MinusMilitiaFlat;
            set { CurrentDisease.MinusMilitiaFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_MilitiaMult}–Militia Multiplier", 0f, 1f, "#0.0", HintText = "{=IAD_MilitiaMult_H}Multiply Militia by (1 - this value)",
            Order = 43, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public float DiseaseMinusMilitiaMultiplier
        {
            get => (float)CurrentDisease.MinusMilitiaMultiplier;
            set { CurrentDisease.MinusMilitiaMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_SecurityFlat}–Security Flat", -100, 100, "{VALUE}", HintText = "{=IAD_SecurityFlat_H}Substract this value from Security",
            Order = 44, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public int DiseaseMinusSecurityFlat
        {
            get => CurrentDisease.MinusSecurityFlat;
            set { CurrentDisease.MinusSecurityFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_SecurityMult}–Security Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_SecurityMult_H}Multiply Security by (1 - this value)",
            Order = 45, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public float DiseaseMinusSecurityMultiplier
        {
            get => (float)CurrentDisease.MinusSecurityMultiplier;
            set { CurrentDisease.MinusSecurityMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_HearthFlat}–Hearth Flat", -100, 100, "{VALUE}", HintText = "{=IAD_HearthFlat_H}Substract this value from Hearth",
            Order = 46, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public int DiseaseMinusHearthFlat
        {
            get => CurrentDisease.MinusHearthFlat;
            set { CurrentDisease.MinusHearthFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HearthMult}–Hearth Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_HearthMult_H}Multiply Hearth by (1 - this value)",
            Order = 47, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases/{=MCM_EDIT}Edit/{=MCM_SETTLEMENT}Settlement")]
        public float DiseaseMinusHearthMultiplier
        {
            get => (float)CurrentDisease.MinusHearthMultiplier;
            set { CurrentDisease.MinusHearthMultiplier = value; SaveToFile(); }
        }

        // … continue the same pattern for MinusGarrisonMultiplier, MinusMilitiaFlat/Multiplier, MinusSecurityFlat/Multiplier, MinusHearthFlat/Multiplier …



        // … repeat for each field of DiseaseConfig you want editable …


        [SettingPropertyButton(
            "{=IAD_SaveDisease}Save Disease",
            Content = "{=IAD_Save}Save", HintText = "{=IAD_SaveDisease_H}Save Diseases to file (click only if your diseases are not being saved to file)",
            Order = 100, RequireRestart = false)]
        [SettingPropertyGroup("{=MCM_DISEASES}Diseases")]
        public Action SaveDiseaseButton
        {
            get;
            set;
        } = () =>
        {
            Instance.SaveToFile();
            InformationManager.DisplayMessage(
                new InformationMessage("[IDD] Diseases saved", Colors.Green)
            );
        };


        // ── INJURIES TAB ────────────────────────────────────────────

        [SettingPropertyDropdown(
            "{=IAD_SelectInjury}Select Injury",
            Order = 0, RequireRestart = false,
            HintText = "{=IAD_SelectInjury_H}Pick which injury to edit")]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries")]
        public Dropdown<InjuryConfig> InjurySelector
        {
            get
            {
                // MCM has updated SelectedValue/Index under the hood?
                var current = _injurySelector.SelectedValue;
                if (current != _lastSelectedInjury)
                {
                    _lastSelectedInjury = current;
                    OnPropertyChanged(nameof(CurrentInjury));
                }
                return _injurySelector;
            }
        }

        [SettingPropertyButton(
            "{=IAD_AddInjury}Add New Injury",
            Content = "{=IAD_Add}Add",
            Order = 1, RequireRestart = false,
            HintText = "{=IAD_AddInjury_H}Append a blank new injury")]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries")]
        public Action AddInjuryButton
        {
            get;
            set;
        } = () =>
        {
            Instance.InjuriesList.Add(new InjuryConfig { Name = "New Injury" });
            Instance.RefreshInjuryDropdown();
            Instance.SaveToFile();
        };

        [SettingPropertyButton(
            "{=IAD_DeleteInjury}Delete Injury",
            Content = "{=IAD_Delete}Delete",
            Order = 2, RequireRestart = false,
            HintText = "{=IAD_DeleteInjury_H}Remove the selected injury")]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries")]
        public Action DeleteInjuryButton
        {
            get;
            set;
        } = () =>
        {
            if (Instance.InjurySelector.SelectedIndex.InRange(0, Instance.InjuriesList.Count - 1))
            {
                Instance.InjuriesList.RemoveAt(Instance.InjurySelector.SelectedIndex);
                Instance.RefreshInjuryDropdown();
                Instance.SaveToFile();
            }
        };


        [SettingPropertyButton(
            "{=IAD_ClearInjuries}Clear All Injuries",
            Content = "{=IAD_Clear}Clear",
            Order = 3, RequireRestart = false,
            HintText = "{=IAD_ClearInjuries_H}Remove all injuries except the first one.")]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries")]
        public Action ClearInjuriesButton
        {
            get;
            set;
        } = () =>
        {
            // keep index 0, remove everything else
            if (Instance.InjuriesList.Count > 1)
            {
                Instance.InjuriesList.RemoveRange(1, Instance.InjuriesList.Count - 1);
                Instance.RefreshInjuryDropdown();
                Instance.SaveToFile();
                InformationManager.DisplayMessage(
                    new InformationMessage("[IDD] Cleared all injuries (except first)", Colors.Green)
                );
            }
        };

        [SettingPropertyText(
            "{=IAD_InjuryName}Name",
            Order = 10, RequireRestart = false,
            HintText = "{=IAD_InjuryName_H}Unique ID for this injury"
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public string InjuryName
        {
            get => CurrentInjury.Name;
            set { CurrentInjury.Name = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_InjuryChance}Injury on battle chance",
            0f, 1f, "#0.00%",
            Order = 11, RequireRestart = false,
            HintText = "{=IAD_InjuryChance_H}Chance that a wounded hero gains an injury after battle.")]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryGetInjuryChance
        {
            get => CurrentInjury.GetInjuryChance;
            set { CurrentInjury.GetInjuryChance = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_MinDays}Min Days", 0, 365, HintText = "{=IAD_MinDays_H}Disease/Injury will last at least this many days",
            Order = 11, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public int InjuryMinDays
        {
            get => CurrentInjury.MinDays;
            set { CurrentInjury.MinDays = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_MaxDays}Max Days", 0, 3650, HintText = "{=IAD_MaxDays_H}Disease/Injury will auto‐resolve after at most this many days",
            Order = 12, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public int InjuryMaxDays
        {
            get => CurrentInjury.MaxDays;
            set { CurrentInjury.MaxDays = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealthHPFlat}–HP Flat", -100, 100, "{VALUE}", HintText = "{=IAD_HealthHPFlat_H}Substract this value from MaxHP",
            Order = 13, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusHealthHitPointsFlat
        {
            get => CurrentInjury.MinusHealthHitPointsFlat;
            set { CurrentInjury.MinusHealthHitPointsFlat = (int)value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealthHPMult}–HP Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_HealthHPMult_H}Multiply max HP by (1 - this value)",
            Order = 14, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusHealthHitPointsMultiplier
        {
            get => (float)CurrentInjury.MinusHealthHitPointsMultiplier;
            set { CurrentInjury.MinusHealthHitPointsMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealthRegenFlat}–Regen Flat", -10, 10, "{VALUE}", HintText = "{=IAD_HealthRegenFlat_H}Substract this value from Health Regen",
            Order = 15, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusHealthRegenFlat
        {
            get => (float)CurrentInjury.MinusHealthRegenFlat;
            set { CurrentInjury.MinusHealthRegenFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealthRegenMult}–Regen Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_HealthRegenMult_H}Multiply Health Regen by (1 - this value)",
            Order = 16, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusHealthRegenMultiplier
        {
            get => (float)CurrentInjury.MinusHealthRegenMultiplier;
            set { CurrentInjury.MinusHealthRegenMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_SpeedFlat}–Speed Flat", -1f, 1f, "{VALUE}", HintText = "{=IAD_SpeedFlat_H}Substract this value from Speed movement",
            Order = 17, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusSpeedFlat
        {
            get => (float)CurrentInjury.MinusSpeedFlat;
            set { CurrentInjury.MinusSpeedFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_SpeedMult}–Speed Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_SpeedMult_H}Multiply Speed movement by (1 - this value)",
            Order = 18, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusSpeedMultiplier
        {
            get => (float)CurrentInjury.MinusSpeedMultiplier;
            set { CurrentInjury.MinusSpeedMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DmgAbsorbFlat}–Damage Absorb Flat", -1f, 1f, "{VALUE}", HintText = "{=IAD_DmgAbsorbFlat_H}Substract this value from Damage Absorb",
            Order = 19, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusDamageAbsorbFlat
        {
            get => (float)CurrentInjury.MinusDamageAbsorbFlat;
            set { CurrentInjury.MinusDamageAbsorbFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DmgAbsorbMult}–Damage Absorb Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_DmgAbsorbMult_H}Multiply Damage Absorb by (1 - this value)",
            Order = 20, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusDamageAbsorbMultiplier
        {
            get => (float)CurrentInjury.MinusDamageAbsorbMultiplier;
            set { CurrentInjury.MinusDamageAbsorbMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DmgFlat}–Weapon Damage Flat", -100, 100, "{VALUE}", HintText = "{=IAD_DmgFlat_H}Substract this value from Weapon Damage",
            Order = 21, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusDamageFlat
        {
            get => (float)CurrentInjury.MinusDamageFlat;
            set { CurrentInjury.MinusDamageFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DmgMult}–Weapon Damage Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_DmgMult_H}Multiply Weapon Damage by (1 - this value)",
            Order = 22, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusDamageMultiplier
        {
            get => (float)CurrentInjury.MinusDamageMultiplier;
            set { CurrentInjury.MinusDamageMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_BattleSurvFlat}–Battle Survival Flat", -1f, 1f, "{VALUE}", HintText = "{=IAD_BattleSurvFlat_H}Substract this value from Battle Survival(chance of not dying)",
            Order = 23, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusBattleSurvivalBonusFlat
        {
            get => (float)CurrentInjury.MinusBattleSurvivalBonusFlat;
            set { CurrentInjury.MinusBattleSurvivalBonusFlat = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_BattleSurvMult}–Battle Survival Multiplier", -1f, 1f, "#0.0", HintText = "{=IAD_BattleSurvMult_H}Multiply Battle Survival(chance of not dying after battle) by (1 - this value)",
            Order = 24, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryMinusBattleSurvivalBonusMultiplier
        {
            get => (float)CurrentInjury.MinusBattleSurvivalBonusMultipler;
            set { CurrentInjury.MinusBattleSurvivalBonusMultipler = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DeathChance}Death Chance", 0f, 1f, "#0.00%", HintText = "{=IAD_DeathChance_H}Base death chance",
            Order = 25, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryDeathChance
        {
            get => (float)CurrentInjury.DeathChance;
            set { CurrentInjury.DeathChance = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_HealChance}Heal Chance", 0f, 1f, "#0.00%", HintText = "{=IAD_HealChance_H}Base healing chance",
            Order = 25, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryHealChance
        {
            get => (float)CurrentInjury.HealChance;
            set { CurrentInjury.HealChance = value; SaveToFile(); }
        }

        [SettingPropertyFloatingInteger(
            "{=IAD_DayHealMult}Heal Multiplier", 0f, 2f, "#0.00", HintText = "{=IAD_DayHealMult_H}Multiply Heal Chance by (this value raised to the power of sick days)",
            Order = 26, RequireRestart = false
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public float InjuryDayHealMultiplier
        {
            get => (float)CurrentInjury.DayHealMultiplier;
            set { CurrentInjury.DayHealMultiplier = value; SaveToFile(); }
        }

        [SettingPropertyInteger(
            "{=IAD_Serious}Is Serious?", 0, 1, "{VALUE}",
            Order = 27, RequireRestart = false,
            HintText = "{=IAD_Serious_H}If 1, hero will try to not lead a party."
        )]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries/{=MCM_EDIT}Edit")]
        public int InjurySerious
        {
            get => CurrentInjury.Serious;
            set { CurrentInjury.Serious = value; SaveToFile(); }
        }

        // … repeat for each field of InjuryConfig …


        [SettingPropertyButton(
            "{=IAD_SaveInjuries}Save Injuries",
            Content = "{=IAD_Save}Save", HintText = "{=IAD_SaveInjuries_H}Save Injuries to file",
            Order = 100, RequireRestart = false)]
        [SettingPropertyGroup("{=MCM_INJURIES}Injuries")]
        public Action SaveInjuriesButton
        {
            get;
            set;
        } = () =>
        {
            Instance.SaveToFile();
            InformationManager.DisplayMessage(
                new InformationMessage("[IDD] Injuries saved", Colors.Green)
            );
        };
    }

    // ── Helpers ───────────────────────────────────────────────────
    public static class IntExtensions
    {
        public static bool InRange(this int i, int min, int max)
            => i >= min && i <= max;
    }


}
