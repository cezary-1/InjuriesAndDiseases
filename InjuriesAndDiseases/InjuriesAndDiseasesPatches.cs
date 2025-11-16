using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace InjuriesAndDiseases.Patches
{

    [HarmonyPatch]
    public static class SettlementPatches
    {
        // 1) LOYALTY
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultSettlementLoyaltyModel), nameof(DefaultSettlementLoyaltyModel.CalculateLoyaltyChange))]
        public static void Postfix_Loyalty(
            Town town,
            bool includeDescriptions,
            ref ExplainedNumber __result)
        {
            if (!includeDescriptions) return;
            var behavior = InjuriesAndDiseasesBehavior.Instance;
            if (behavior == null) return;

            var settlement = town.Settlement;
            if (settlement == null) return;
            var st = behavior.GetSettlementStatus(settlement);
            if (st == null) return;


            foreach (var ad in st.Diseases.ToList())
            {
                var cfg = ad.Config;
                float vanilla = town.Loyalty;
                var minus = 0;
                // Flat
                if (cfg.MinusLoyaltyFlat != 0)
                {
                    minus += cfg.MinusLoyaltyFlat;
                }
                // Multiplier
                if (cfg.MinusLoyaltyMultiplier != 0)
                {
                    float perc = (float)(vanilla * cfg.MinusLoyaltyMultiplier);
                    minus += (int)perc;
                }

                if(minus != 0)
                {
                    //building
                    var bonusBuilding = 0f;
                    if (settlement.IsFortification && InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus != 0)
                    {
                        var buildings = settlement.Town.Buildings.ToList();
                        var positive = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(InjuriesAndDiseasesGlobalSettings.Instance.BuildingPositive);
                        var negative = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(InjuriesAndDiseasesGlobalSettings.Instance.BuildingNegative);
                        foreach (var b in buildings)
                        {
                            if (b.CurrentLevel != 0)
                            {
                                var raw = InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus;
                                if (InjuriesAndDiseasesGlobalSettings.Instance.LevelBuilding != 0 && b.CurrentLevel > 1) raw += raw * InjuriesAndDiseasesGlobalSettings.Instance.LevelBuilding * (b.CurrentLevel - 1);
                                foreach (var p in positive)
                                {
                                    if (b.Name.Contains(p))
                                    {
                                        bonusBuilding += minus * raw;
                                    }
                                }
                                foreach (var n in negative)
                                {
                                    if (b.Name.Contains(n))
                                    {
                                        bonusBuilding -= minus * raw;
                                    }
                                }

                            }

                        }

                        minus -= (int)bonusBuilding;


                    }
                    minus = Math.Min(minus, (int)town.Loyalty);
                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    __result.Add(-minus, line, null);
                }
            }
        }

        // 2) FOOD STOCKS (Towns)
        [HarmonyPostfix]
        [HarmonyPatch(
          typeof(DefaultSettlementFoodModel),
          nameof(DefaultSettlementFoodModel.CalculateTownFoodStocksChange))]
        public static void Postfix_FoodStocksChange(
            Town town,
            bool includeMarketStocks,
            bool includeDescriptions,
            ref ExplainedNumber __result)
        {
            var behavior = InjuriesAndDiseasesBehavior.Instance;
            if (behavior == null) return;
            var settlement = town.Settlement;
            if(settlement == null) return;
            var st = behavior.GetSettlementStatus(settlement);
            if (st == null) return;
            foreach (var ad in st.Diseases.ToList())
            {
                var cfg = ad.Config;
                float vanilla = town.FoodStocks;
                var minus = 0;
                if (cfg.MinusFoodFlat != 0)
                {
                    minus += cfg.MinusFoodFlat;
                }
                if (cfg.MinusFoodMultiplier != 0)
                {
                    float perc = (float)(vanilla * cfg.MinusFoodMultiplier);
                    minus += (int)perc;
                }

                if (minus != 0)
                {
                    //building
                    var bonusBuilding = 0f;
                    if (settlement.IsFortification && InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus != 0)
                    {
                        var buildings = settlement.Town.Buildings.ToList();
                        var positive = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(InjuriesAndDiseasesGlobalSettings.Instance.BuildingPositive);
                        var negative = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(InjuriesAndDiseasesGlobalSettings.Instance.BuildingNegative);
                        foreach (var b in buildings)
                        {
                            if (b.CurrentLevel != 0)
                            {
                                var raw = InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus;
                                if (InjuriesAndDiseasesGlobalSettings.Instance.LevelBuilding != 0 && b.CurrentLevel > 1) raw += raw * InjuriesAndDiseasesGlobalSettings.Instance.LevelBuilding * (b.CurrentLevel - 1);
                                foreach (var p in positive)
                                {
                                    if (b.Name.Contains(p))
                                    {
                                        bonusBuilding += minus * raw;
                                    }
                                }
                                foreach (var n in negative)
                                {
                                    if (b.Name.Contains(n))
                                    {
                                        bonusBuilding -= minus * raw;
                                    }
                                }

                            }

                        }
                        minus -= (int)bonusBuilding;
                    }


                    minus = Math.Min(minus, (int)town.FoodStocks);

                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    __result.Add(-minus, line, null);
                }
            }
        }

        // 5) PROSPERITY (Towns)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultSettlementProsperityModel), nameof(DefaultSettlementProsperityModel.CalculateProsperityChange))]
        public static void Postfix_Prosperity(
            Town fortification,
            bool includeDescriptions,
            ref ExplainedNumber __result)
        {
            if (!includeDescriptions) return;
            var behavior = InjuriesAndDiseasesBehavior.Instance;
            if (behavior == null) return;
            var settlement = fortification.Settlement;
            if (settlement == null) return;
            var st = behavior.GetSettlementStatus(settlement);
            if (st == null) return;
            foreach (var ad in st.Diseases.ToList())
            {
                var cfg = ad.Config;
                float vanilla = fortification.Prosperity;
                var minus = 0;
                if (cfg.MinusProsperityFlat != 0)
                {
                    minus += cfg.MinusProsperityFlat;
                }
                if (cfg.MinusProsperityMultiplier != 0)
                {
                    float perc = (float)(vanilla * cfg.MinusProsperityMultiplier);
                    minus += (int)perc;
                }
                if (minus != 0)
                {
                    //building
                    var bonusBuilding = 0f;
                    if (settlement.IsFortification && InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus != 0)
                    {
                        var buildings = settlement.Town.Buildings.ToList();
                        var positive = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(InjuriesAndDiseasesGlobalSettings.Instance.BuildingPositive);
                        var negative = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(InjuriesAndDiseasesGlobalSettings.Instance.BuildingNegative);
                        foreach (var b in buildings)
                        {
                            if (b.CurrentLevel != 0)
                            {
                                var raw = InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus;
                                if (InjuriesAndDiseasesGlobalSettings.Instance.LevelBuilding != 0 && b.CurrentLevel > 1) raw += raw * InjuriesAndDiseasesGlobalSettings.Instance.LevelBuilding * (b.CurrentLevel - 1);
                                foreach (var p in positive)
                                {
                                    if (b.Name.Contains(p))
                                    {
                                        bonusBuilding += minus * raw;
                                    }
                                }
                                foreach (var n in negative)
                                {
                                    if (b.Name.Contains(n))
                                    {
                                        bonusBuilding -= minus * raw;
                                    }
                                }

                            }

                        }

                        minus -= (int)bonusBuilding;

                    }
                    minus = Math.Min(minus, (int)fortification.Prosperity);
                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    __result.Add(-minus, line, null);
                }

            }
        }

        // 6) HEARTH (Villages)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultSettlementProsperityModel), nameof(DefaultSettlementProsperityModel.CalculateHearthChange))]
        public static void Postfix_Hearth(
            Village village,
            bool includeDescriptions,
            ref ExplainedNumber __result)
        {
            if (!includeDescriptions) return;
            var behavior = InjuriesAndDiseasesBehavior.Instance;
            if (behavior == null) return;
            var settlement = village.Settlement;
            if (settlement == null) return;
            var st = behavior.GetSettlementStatus(settlement);
            if (st == null) return;
            foreach (var ad in st.Diseases.ToList())
            {
                var cfg = ad.Config;
                float vanilla = __result.ResultNumber;
                if (cfg.MinusHearthFlat != 0)
                {
                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    __result.Add(-cfg.MinusHearthFlat, line, null);
                }
                if (cfg.MinusHearthMultiplier != 0)
                {
                    float perc = (float)(vanilla * cfg.MinusHearthMultiplier);
                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    __result.Add(-perc, line, null);
                }
            }
        }

        // 7) SECURITY
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultSettlementSecurityModel), nameof(DefaultSettlementSecurityModel.CalculateSecurityChange))]
        public static void Postfix_Security(
            Town town,
            bool includeDescriptions,
            ref ExplainedNumber __result)
        {
            if (!includeDescriptions) return;
            var behavior = InjuriesAndDiseasesBehavior.Instance;
            if (behavior == null) return;
            var settlement = town.Settlement;
            if (settlement == null) return;
            var st = behavior.GetSettlementStatus(settlement);
            if (st == null) return;
            foreach (var ad in st.Diseases.ToList())
            {
                var cfg = ad.Config;
                float vanilla = town.Security;
                var minus = 0;
                if (cfg.MinusSecurityFlat != 0)
                {
                    minus += cfg.MinusSecurityFlat;
                }
                if (cfg.MinusSecurityMultiplier != 0)
                {
                    float perc = (float)(vanilla * cfg.MinusSecurityMultiplier);
                    minus += (int)perc;
                }

                if (minus != 0)
                {
                    //building
                    var bonusBuilding = 0f;
                    if (settlement.IsFortification && InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus != 0)
                    {
                        var buildings = settlement.Town.Buildings.ToList();
                        var positive = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(InjuriesAndDiseasesGlobalSettings.Instance.BuildingPositive);
                        var negative = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(InjuriesAndDiseasesGlobalSettings.Instance.BuildingNegative);
                        foreach (var b in buildings)
                        {
                            if (b.CurrentLevel != 0)
                            {
                                var raw = InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus;
                                if (InjuriesAndDiseasesGlobalSettings.Instance.LevelBuilding != 0 && b.CurrentLevel > 1) raw += raw * InjuriesAndDiseasesGlobalSettings.Instance.LevelBuilding * (b.CurrentLevel - 1);
                                foreach (var p in positive)
                                {
                                    if (b.Name.Contains(p))
                                    {
                                        bonusBuilding += minus * raw;
                                    }
                                }
                                foreach (var n in negative)
                                {
                                    if (b.Name.Contains(n))
                                    {
                                        bonusBuilding -= minus * raw;
                                    }
                                }

                            }

                        }

                        minus -= (int)bonusBuilding;

                    }
                    minus = Math.Min(minus, (int)town.Security);
                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    __result.Add(-minus, line, null);
                }
            }
        }
    }

    [HarmonyPatch]
    public static class HeroPatches
    {

        // 1) Max Hitpoints (flat + multiplier)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultCharacterStatsModel), nameof(DefaultCharacterStatsModel.MaxHitpoints))]
        public static void Postfix_MaxHitpoints(
            ref ExplainedNumber __result,
            CharacterObject character,
            bool includeDescriptions = false)
        {
            var hero = character.HeroObject;
            if (hero == null) return;
            var bh = InjuriesAndDiseasesBehavior.Instance;
            if(bh == null) return;
            var st = bh.GetHeroStatus(hero);
            if (st == null) return;
            foreach (var ad in st.Diseases)
            {
                var cfg = ad.Config;
                // Flat
                if (cfg.MinusHealthHitPointsFlat != 0)
                {
                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    __result.Add(-cfg.MinusHealthHitPointsFlat,
                        line);
                }
                // Multiplier (fractional)
                if (cfg.MinusHealthHitPointsMultiplier != 0)
                {
                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    float change = __result.ResultNumber * (float)cfg.MinusHealthHitPointsMultiplier;
                    __result.Add(-change,
                        line);
                }
            }

            foreach (var ad in st.Injuries)
            {
                var cfg = ad.Config;
                // Flat
                if (cfg.MinusHealthHitPointsFlat != 0)
                {
                    var line = new TextObject("{=IAD_DUEINJURY}Due to {INJURY}")
                        .SetTextVariable("INJURY", cfg.Name);
                    __result.Add(-cfg.MinusHealthHitPointsFlat,
                        line);
                }
                // Multiplier (fractional)
                if (cfg.MinusHealthHitPointsMultiplier != 0)
                {
                    float change = __result.ResultNumber * (float)cfg.MinusHealthHitPointsMultiplier;
                    var line = new TextObject("{=IAD_DUEINJURY}Due to {INJURY}")
                        .SetTextVariable("INJURY", cfg.Name);
                    __result.Add(-change,
                        line);
                }
            }
        }

        // 2) Agent stats (speed, absorb, damage) in battle
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SandboxAgentStatCalculateModel), nameof(SandboxAgentStatCalculateModel.UpdateAgentStats))]
        public static void Postfix_UpdateAgentStats(
            Agent agent,
            AgentDrivenProperties agentDrivenProperties)
        {
            if (agent?.Character == null || !agent.IsHero) return;
            var hero = Hero.AllAliveHeroes.Where(h => h.CharacterObject == agent.Character).FirstOrDefault();
            if (hero == null) return;
            var st = InjuriesAndDiseasesBehavior.Instance.GetHeroStatus(hero);
            if (st == null) return;
            foreach (var ad in st.Diseases)
            {
                var cfg = ad.Config;
                // Speed Flat
                if (cfg.MinusSpeedFlat != 0)
                {
                    agentDrivenProperties.MaxSpeedMultiplier -= (float)cfg.MinusSpeedFlat;
                }
                //Speed Multiplier
                if (cfg.MinusSpeedMultiplier != 0)
                {
                    agentDrivenProperties.MaxSpeedMultiplier *= 1f - (float)cfg.MinusSpeedMultiplier;
                }

            }

            foreach (var ad in st.Injuries)
            {
                var cfg = ad.Config;
                // Speed Flat
                if (cfg.MinusSpeedFlat != 0)
                {
                    agentDrivenProperties.MaxSpeedMultiplier -= (float)cfg.MinusSpeedFlat;
                }
                //Speed Multiplier
                if (cfg.MinusSpeedMultiplier != 0)
                {
                    agentDrivenProperties.MaxSpeedMultiplier *= 1f - (float)cfg.MinusSpeedMultiplier;
                }

            }


        }

        // 3) Daily healing HP for heroes
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultPartyHealingModel), nameof(DefaultPartyHealingModel.GetDailyHealingHpForHeroes))]
        public static void Postfix_HealingHp(
            ref ExplainedNumber __result,
            MobileParty party,
            bool includeDescriptions = false)
        {
            var leader = party.LeaderHero;
            if (leader == null) return;
            var bh = InjuriesAndDiseasesBehavior.Instance;
            if (bh == null) return;
            var st = bh.GetHeroStatus(leader);
            if (st == null) return;
            foreach (var ad in st.Diseases)
            {
                var cfg = ad.Config;
                // Flat reduction
                if (cfg.MinusHealthRegenFlat != 0)
                {
                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    __result.Add((float)-cfg.MinusHealthRegenFlat,
                        line);
                }
                // Multiplier
                if (cfg.MinusHealthRegenMultiplier != 0)
                {
                    float change = __result.ResultNumber * (float)cfg.MinusHealthRegenMultiplier;
                    var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                        .SetTextVariable("DISEASE", cfg.Name);
                    __result.Add(-change,
                        line);
                }
            }

            foreach (var ad in st.Injuries)
            {
                var cfg = ad.Config;
                // Flat reduction
                if (cfg.MinusHealthRegenFlat != 0)
                {
                    var line = new TextObject("{=IAD_DUEINJURY}Due to {INJURY}")
                        .SetTextVariable("INJURY", cfg.Name);
                    __result.Add((float)-cfg.MinusHealthRegenFlat,
                        line);
                }
                // Multiplier
                if (cfg.MinusHealthRegenMultiplier != 0)
                {
                    var line = new TextObject("{=IAD_DUEINJURY}Due to {INJURY}")
                        .SetTextVariable("INJURY", cfg.Name);
                    float change = __result.ResultNumber * (float)cfg.MinusHealthRegenMultiplier;
                    __result.Add(-change,
                        line);
                }
            }
        }

        // 4–5) Damage taken & dealt
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SandboxAgentApplyDamageModel), nameof(SandboxAgentApplyDamageModel.CalculateDamage))]
        public static void Postfix_CalculateDamage(
            ref float __result,
            in AttackInformation attackInformation,
            in AttackCollisionData collisionData,
            in MissionWeapon weapon,
            float baseDamage)
        {
            // DAMAGE TAKEN (Victim)
            var victimChar = attackInformation.VictimAgent?.Character;
            if (victimChar != null && victimChar.IsHero)
            {
                
                var bh = InjuriesAndDiseasesBehavior.Instance;
                if (bh == null) return;
                var victimHero = Hero.AllAliveHeroes.Where(h=> h.CharacterObject == victimChar).FirstOrDefault();
                if(victimHero != null)
                {
                    var st = bh.GetHeroStatus(victimHero);
                    if (st != null)
                    {
                        foreach (var ad in st.Diseases)
                        {

                            var cfg = ad.Config;
                            // Absorb flat then multiplier
                            if (cfg.MinusDamageAbsorbFlat != 0)
                                __result = MathF.Max(0f, __result + (float)cfg.MinusDamageAbsorbFlat);
                            if (cfg.MinusDamageAbsorbMultiplier != 0)
                                __result *= 1f + (float)cfg.MinusDamageAbsorbMultiplier;
                        }

                        foreach (var ad in st.Injuries)
                        {
                            var cfg = ad.Config;
                            // Absorb flat then multiplier
                            if (cfg.MinusDamageAbsorbFlat != 0)
                                __result = MathF.Max(0f, __result + (float)cfg.MinusDamageAbsorbFlat);
                            if (cfg.MinusDamageAbsorbMultiplier != 0)
                                __result *= 1f + (float)cfg.MinusDamageAbsorbMultiplier;
                        }
                    }
                }
 
                

            }

            // DAMAGE DEALT (Attacker)
            var attackerChar = attackInformation.AttackerAgentCharacter;
            if (attackerChar != null && attackerChar.IsHero)
            {
                var bh = InjuriesAndDiseasesBehavior.Instance;
                if (bh == null) return;
                var attackerHero = Hero.AllAliveHeroes.Where(h => h.CharacterObject == attackerChar).FirstOrDefault();
                if(attackerHero != null)
                {
                    var st = bh.GetHeroStatus(attackerHero);
                    if (st != null)
                    {
                        foreach (var ad in st.Diseases)
                        {
                            var cfg = ad.Config;
                            // Damage flat then multiplier
                            if (cfg.MinusDamageFlat != 0)
                                __result = MathF.Max(0f, __result - (float)cfg.MinusDamageFlat);
                            if (cfg.MinusDamageMultiplier != 0)
                                __result *= 1f - (float)cfg.MinusDamageMultiplier;
                        }

                        foreach (var ad in st.Injuries)
                        {
                            var cfg = ad.Config;
                            // Damage flat then multiplier
                            if (cfg.MinusDamageFlat != 0)
                                __result = MathF.Max(0f, __result - (float)cfg.MinusDamageFlat);
                            if (cfg.MinusDamageMultiplier != 0)
                                __result *= 1f - (float)cfg.MinusDamageMultiplier;
                        }
                    }
                }
                

            }
        }

        // 6) Pregnancy chance reduction + infection
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultPregnancyModel), nameof(DefaultPregnancyModel.GetDailyChanceOfPregnancyForHero))]
        public static void Postfix_Pregnancy(
            Hero hero,
            ref float __result)
        {
            var behavior = InjuriesAndDiseasesBehavior.Instance;
            if (behavior == null) return;
            var st = InjuriesAndDiseasesBehavior.Instance.GetHeroStatus(hero);
            if (st == null) return;
            foreach (var ad in st.Diseases)
            {
                
                var cfg = ad.Config;
                // Flat
                if (cfg.MinusPregnancyChanceFlat != 0)
                    __result -= (float)cfg.MinusPregnancyChanceFlat;
                // Multiplier
                if (cfg.MinusPregnancyChanceMultiplier != 0)
                {
                    __result *= 1f - (float)cfg.MinusPregnancyChanceMultiplier;
                }
                __result = MathF.Max(0f, __result);

            }

            var spouse = hero.Spouse;
            if (spouse == null) return;

            if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Checking pregnancy infections for : {hero.Name} and {spouse.Name}"));
            // Spread blood/contact diseases both ways
            behavior.TryBloodInfect(hero, spouse);
            behavior.TryBloodInfect(spouse, hero);
        }

        // 7) Survival chance after battle
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultPartyHealingModel), nameof(DefaultPartyHealingModel.GetSurvivalChance))]
        public static void Postfix_SurvivalChance(
            ref float __result,
            PartyBase party,
            CharacterObject character,
            DamageTypes damageType,
            bool canDamageKillEvenIfBlunt,
            PartyBase enemyParty = null)
        {
            // Lookup the hero from the CharacterObject
            var hero = character.HeroObject;
            if (hero == null) return;

            var bh = InjuriesAndDiseasesBehavior.Instance;
            if (bh == null) return;
            var st = bh.GetHeroStatus(hero);
            if (st == null) return;
            foreach (var ad in st.Diseases)
            {
                var cfg = ad.Config;

                // Flat bonus (could be negative if you want a penalty)
                if (cfg.MinusBattleSurvivalBonusFlat != 0)
                {
                    __result -= (float)cfg.MinusBattleSurvivalBonusFlat;
                }

                // Multiplier: e.g. 0.1 means +10% survival
                if (cfg.MinusBattleSurvivalBonusMultipler != 0)
                {
                    __result *= 1f - (float)cfg.MinusBattleSurvivalBonusMultipler;
                }
            }

            foreach (var ad in st.Injuries)
            {
                var cfg = ad.Config;

                // Flat bonus (could be negative if you want a penalty)
                if (cfg.MinusBattleSurvivalBonusFlat != 0)
                {
                    __result -= (float)cfg.MinusBattleSurvivalBonusFlat;
                }

                // Multiplier: e.g. 0.1 means +10% survival
                if (cfg.MinusBattleSurvivalBonusMultipler != 0)
                {
                    __result *= 1f - (float)cfg.MinusBattleSurvivalBonusMultipler;
                }
            }

            // Clamp into [0,1]
            __result = MathF.Clamp(__result, 0f, 1f);
        }

        //8) food consume
        [HarmonyPatch(typeof(DefaultMobilePartyFoodConsumptionModel))]
        static class FoodConsumption_PerksPatch
        {
            // Patch the private CalculatePerkEffects(MobileParty, ref ExplainedNumber) method:
            [HarmonyPostfix]
            [HarmonyPatch("CalculatePerkEffects")]
            static void Postfix_CalculatePerkEffects(
                DefaultMobilePartyFoodConsumptionModel __instance,
                MobileParty party,
                ref ExplainedNumber result)
            {
                // only patch if this party actually consumes food
                if (!__instance.DoesPartyConsumeFood(party))
                    return;

                var hero = party.LeaderHero?.CharacterObject?.HeroObject;
                if (hero == null) return;

                var bh = InjuriesAndDiseasesBehavior.Instance;
                if (bh == null) return;
                var st = bh.GetHeroStatus(hero);
                if (st == null) return;
                foreach (var ad in st.Diseases)
                {
                    var cfg = ad.Config;

                    // Flat bonus (could be negative if you want a penalty)
                    if (cfg.MinusFoodConsumptionFlat != 0)
                    {
                        var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                            .SetTextVariable("DISEASE", cfg.Name);
                        result.Add(-(float)cfg.MinusFoodConsumptionFlat, line);
                    }

                    // Multiplier: e.g. 0.1 means -10% food eating
                    if (cfg.MinusFoodConsumptionMultipler != 0)
                    {
                        var change = result.ResultNumber * (float)cfg.MinusFoodConsumptionMultipler;
                        var line = new TextObject("{=IAD_DUEDISEASE}Due to {DISEASE}")
                            .SetTextVariable("DISEASE", cfg.Name);
                        result.Add(-change, line);
                    }



                }
            }
        }


        [HarmonyPatch(
            typeof(HeroSpawnCampaignBehavior),
            "GetBestAvailableCommander")]
        public static class Patch_GetBestAvailableCommander
        {
            static void Postfix(Clan clan, ref Hero __result)
            {
                // if AI didn’t pick anyone, or it’s the player’s clan, or we’re not blocking diseases/injuries → bail
                if (__result == null
                 || clan == Clan.PlayerClan
                 || (!InjuriesAndDiseasesGlobalSettings.Instance.AllowDisease
                     && !InjuriesAndDiseasesGlobalSettings.Instance.AllowInjury))
                    return;
                var og = __result;

                // get their status
                var st = InjuriesAndDiseasesBehavior.Instance?.GetHeroStatus(og);
                if (st == null) return;
                

                var protectedNames = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(InjuriesAndDiseasesGlobalSettings.Instance.ProtectHeroes);
                bool isProtected = protectedNames.Any(n =>
                    string.Equals(n, og.Name.ToString(), StringComparison.OrdinalIgnoreCase));

                if(isProtected) return;

                // if any active disease or injury is marked “serious”, veto them
                if (st.Diseases.Any(d => d.Config.Serious == 1)
                 || st.Injuries.Any(i => i.Config.Serious == 1))
                {
                    if(InjuriesAndDiseasesGlobalSettings.Instance.Debug)
                    {
                        InformationManager.DisplayMessage(
                            new InformationMessage($"{__result.Name} can't lead party because of serious condition."));
                    }

                    
                    __result = null;  // AI will retry with the next-best candidate
                    var num = 0f;
                    var heroes = clan.Heroes.Where(h => h != og && h.IsActive && h.IsAlive && h.PartyBelongedTo == null && h.PartyBelongedToAsPrisoner == null && h.Age > (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && h.CharacterObject.Occupation == Occupation.Lord).ToList();
                    if (InjuriesAndDiseasesGlobalSettings.Instance.Debug)
                    {
                        InformationManager.DisplayMessage(
                        new InformationMessage($"IAD: HEROES LIST: {heroes.Count}."));
                    }
                    foreach (Hero hero3 in heroes)
                    {
                        var hero3st = InjuriesAndDiseasesBehavior.Instance?.GetHeroStatus(hero3);
                        if (InjuriesAndDiseasesGlobalSettings.Instance.Debug)
                        {
                            InformationManager.DisplayMessage(
                            new InformationMessage($"{hero3.Name} is new prospect."));
                        }
                        if (hero3.GetTraitLevel(DefaultTraits.Commander) > num &&
                           (!st.Diseases.Any(d => d.Config.Serious == 1)
                 || !st.Injuries.Any(i => i.Config.Serious == 1)))
                        {
                            num = hero3.GetTraitLevel(DefaultTraits.Commander);
                            __result = hero3;
                        }
                    }
                    //if no heroes found and don't have any parties
                    if (__result == null && clan.WarPartyComponents.Count < 0) __result = og;
                    if(InjuriesAndDiseasesGlobalSettings.Instance.Debug && __result != null) InformationManager.DisplayMessage(
                        new InformationMessage($"{__result.Name} replaced him."));
                }
            }
        }
    }
    //MapNotificationVM patch
    [HarmonyPatch(typeof(MapNotificationVM), "PopulateTypeDictionary")]
    static class _Patch_MapNotificationVM_Populate
    {
        static void Postfix(MapNotificationVM __instance)
        {
            __instance.RegisterMapNotificationType(
            typeof(DiseaseDeathMapNotification),
            typeof(DiseaseDeathNotificationItemVM)
            );
        }
    }
    //Worskhop OnHeroKilled patch
    [HarmonyPatch(typeof(WorkshopsCampaignBehavior), "OnHeroKilled")]
    static class WorkshopsCampaignBehavior_OnHeroKilled_Patch
    {
        static bool Prefix(Hero victim,
        Hero killer,
        KillCharacterAction.KillCharacterActionDetail detail,
                           bool showNotification)
        {
            // if this was one of *our* disease‑deaths, skip all workshop code
            if (InjuriesAndDiseasesBehavior._diseaseDeaths.Remove(victim))
                return false;

            // otherwise run the normal workshop logic
            return true;
        }
    }

    //Sick parties patch info
    [HarmonyPatch(typeof(MobileParty), "set_TargetParty")]
    static class set_TargetParty_Patch
    {
        private static MobileParty lastparty = new MobileParty();
        static void Postfix(MobileParty __instance, MobileParty value)
        {
            if (__instance == MobileParty.MainParty && InjuriesAndDiseasesGlobalSettings.Instance.InformInfectedParty && value != null && value != lastparty && value.LeaderHero != null)
            {
                var name = value.Name;
                var roster = value.MemberRoster;
                var heroes = roster.GetTroopRoster().Where(h=> h.Character.IsHero).Select(h=> h.Character.HeroObject).ToList();
                if(value.Army != null && value == value.Army.LeaderParty)
                {
                    name = value.Army.Name;
                    var parties = value.Army.Parties.Where(p => p.LeaderHero != null && p != value).ToList();
                    foreach(var party in parties)
                    {
                        var partyRoster = party.MemberRoster;
                        var partyHeroes = partyRoster.GetTroopRoster().Where(h => h.Character.IsHero).Select(h => h.Character.HeroObject).ToList();
                        heroes.AddRange(partyHeroes);
                    }
                }
                foreach(var h in heroes)
                {
                    var st = InjuriesAndDiseasesBehavior.Instance?.GetHeroStatus(h);
                    if (st == null) continue;
                    if (st.Diseases.Any())
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=IAD_INFECTED_PARTY}The {NAME} is infected! Cautious!")
                            .SetTextVariable("NAME", name)
                            .ToString(), Colors.Red));
                        lastparty = value;
                        break;
                    }
                    
                }
                
            }
        }
    }
    //Sick settlements patch info
    [HarmonyPatch(typeof(MobileParty), "set_TargetSettlement")]
    static class set_TargetSettlement_Patch
    {
        private static Settlement lastsettlement = new Settlement();
        static void Postfix(MobileParty __instance, Settlement value)
        {
            if (__instance == MobileParty.MainParty && InjuriesAndDiseasesGlobalSettings.Instance.InformInfectedParty && value != null && value != lastsettlement && (value.IsFortification || value.IsVillage) )
            {
                var st = InjuriesAndDiseasesBehavior.Instance?.GetSettlementStatus(value);
                if (st == null) return;
                if (st.Diseases.Any())
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=IAD_INFECTED_SETTLEMENT}The {NAME} is infected! Cautious!")
                        .SetTextVariable("NAME", value.Name)
                        .ToString(), Colors.Red));
                    lastsettlement = value;
                }

            }
        }
    }
}