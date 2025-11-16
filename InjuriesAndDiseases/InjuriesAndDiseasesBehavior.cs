using Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace InjuriesAndDiseases
{
    public static class LinqExtensions
    {
        public static string Join(this IEnumerable<string> seq, string sep)
            => string.Join(sep, seq);
    }

    public class InjuriesAndDiseasesBehavior : CampaignBehaviorBase
    {
        public static InjuriesAndDiseasesBehavior Instance { get; private set; }
        private int _daysSinceLastCheck = 0;

        // Hero statuses
        public ConcurrentDictionary<Hero, HeroStatus> _heroStatuses = new ConcurrentDictionary<Hero, HeroStatus>();
        // Settlement statuses
        public ConcurrentDictionary<Settlement, SettlementStatus> _settlementStatuses = new ConcurrentDictionary<Settlement, SettlementStatus>();

        //Doc cost
        public int DocCost = 0;

        public int DocDay = 0;

        public Settlement DocSettlement;


        //Save Data
        public List<HeroStatusContainer> _heroStatusesSave
            = new List<HeroStatusContainer>();

        public List<SettlementStatusContainer> _settlementStatusesSave
            = new List<SettlementStatusContainer>();




        public override void RegisterEvents()
        {
            Instance = this;
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTickPersist);
            CampaignEvents.HeroWounded.AddNonSerializedListener(this, OnHeroWounded);
            CampaignEvents.OnHeroCombatHitEvent.AddNonSerializedListener(this, OnHeroCombatHit);
            CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirth);
            CampaignEvents.BattleStarted.AddNonSerializedListener(this, OnBattleStarted);
            CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, InjectPurgeOption); //purge
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEndedSiege);

        }

        //Start menus

        // remember which menu (town/village/castle) we came from
        public string LastHealthParentMenu { get; private set; }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            DocDay = 0;
            DocCost = 0;
            DocSettlement = null;
           
            IADMenus(starter);
            DoctorDial(starter);

        }

        private void IADMenus(CampaignGameStarter starter)
        {
            // For each world‑map menu we want to inject into:
            foreach (var parent in new[] { "town", "village", "castle" })
            {
                // 1) Add the “Health Status…” option in the parent menu,
                //    pointing at its own newly‑named submenu:
                var rootMenuId = $"iad_health_root_{parent}_menu";
                starter.AddGameMenuOption(
                    parent,                                   // e.g. "town"
                    $"iad_health_root_{parent}",               // unique option id
                    "{=IAD_HealthRoot}Health Status…",         // text
                    args =>
                    {

                        args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
                        return true;
                    },
                    args =>
                    {
                        Instance.LastHealthParentMenu = rootMenuId;
                        GameMenu.SwitchToMenu(rootMenuId);
                    },
                    false, 300, false, null
                );

                // 2) Define that submenu:
                starter.AddGameMenu(
                    rootMenuId,
                    "{=IAD_HealthRootTitle}View health status for:",  // title text
                    args => { /* no init needed */ },
                    GameOverlays.MenuOverlayType.None,
                    GameMenu.MenuFlags.None,
                    null
                );

                // 3) “Heroes…” option
                starter.AddGameMenuOption(
                    rootMenuId, "iad_health_heroes",
                    "{=IAD_Heroes}Heroes…",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return true;
                    },
                    args =>
                    {
                        ShowHeroKingdomInquiry();
                    },
                    false, 100, false, null
                );

                // 4) “Settlements…” option
                starter.AddGameMenuOption(
                    rootMenuId, "iad_health_settlements",
                    "{=IAD_Settlements}Settlements…",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return true;
                    },
                    args =>
                    {
                        ShowSettlementKingdomInquiry();
                    },
                    false, 200, false, null
                );
                starter.AddGameMenuOption(
                    rootMenuId, "iad_health_search",
                    GameTexts.FindText("str_ui_search").ToString(),
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return true;
                    },
                    args =>
                    {
                        ShowSearchInquiry();
                    },
                    false, 201, false, null
                );

                // 5) **Back** → return to the original parent menu:
                starter.AddGameMenuOption(
                    rootMenuId, "iad_health_root_back",
                    GameTexts.FindText("str_back").ToString(),
                    args =>
                    {
                        // pop this submenu and go back to e.g. "town"
                        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                        return true;
                    },
                    args => GameMenu.SwitchToMenu(parent),
                    true, 0, false, null
                );
            }

        }
        //End menus

        //Start MulitChoiceInqiry


        // Search Inquiry
        private void ShowSearchInquiry()
        {
            var title = GameTexts.FindText("str_ui_search").ToString();
            var prompt = new TextObject("{=IAD_Search_TEXT}Write Hero or Settlement Name").ToString();

            var data = new TextInquiryData(
                title,
                prompt,
                true,  // isTextBox
                true,  // submitOnRightClick
                GameTexts.FindText("str_next").ToString(),
                GameTexts.FindText("str_cancel").ToString(),

                // when they hit "Next"
                selectedText =>
                {
                    if (string.IsNullOrWhiteSpace(selectedText))
                    {
                        // nothing typed → just bail
                        return;
                    }
                    // Perform the search
                    ShowSearchResultsInquiry(selectedText);
                },

                // onCancel
                () => { }
            );

            InformationManager.ShowTextInquiry(data);
        }

        // 2) Build the results list (heroes + settlements)
        private void ShowSearchResultsInquiry(string query)
        {
            query = query.Trim();
            // find heroes whose name contains the query (ignore case)
            var heroMatches = Hero.AllAliveHeroes
                .Where(h => h.Name.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            // find settlements whose name contains the query
            var settMatches = Settlement.All
                .Where(s => s.Name.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            // Otherwise build a multi-select list of everything
            var elems = new List<InquiryElement>();

            // Add heroes first
            foreach (var h in heroMatches)
            {
                elems.Add(new InquiryElement(
                    h.StringId,
                    new TextObject("{=IAD_HeroMulti}{NAME} [Diseases:{SICK} Injuries:{INJ}] ")
            .SetTextVariable("NAME", h.Name.ToString())
            .SetTextVariable("SICK", GetDiseasesString(h))
            .SetTextVariable("INJ", GetInjuriesString(h))
            .ToString(),
                    new ImageIdentifier(CharacterCode.CreateFrom(h.CharacterObject)),
                    true,
                    ""
                ));
            }

            // Then settlements
            foreach (var s in settMatches)
            {
                elems.Add(new InquiryElement(
                    s.StringId,
                    new TextObject("{=IAD_SettlMulti}{NAME} [Diseases:{SICK}] ")
            .SetTextVariable("NAME", s.Name.ToString())
            .SetTextVariable("SICK", GetSettlementDiseasesString(s))
            .ToString(),
                    null,
                    true,
                    ""
                ));
            }

            // Otherwise show all matches in one multi-select dialog
            var resultData = new MultiSelectionInquiryData(
                new TextObject("{=IAD_SearchResults_TITLE}Search Results").ToString(),
                new TextObject("{=IAD_SearchResults_TEXT}Select one to view details").ToString(),
                elems,
                true,
                1,
                1,
                GameTexts.FindText("str_ok").ToString(),
                GameTexts.FindText("str_cancel").ToString(),

                // onConfirm
                list =>
                {
                    var id = (string)list[0].Identifier;
                    // see if it's a hero
                    var h = Hero.AllAliveHeroes.FirstOrDefault(x => x.StringId == id);
                    if (h != null)
                        ShowHeroDetailInquiry(h);
                    else
                    {
                        var s = Settlement.Find(id);
                        if (s != null)
                            ShowSettlementDetailInquiry(s);
                    }
                },

                // onCancel
                _ => { }
            );

            MBInformationManager.ShowMultiSelectionInquiry(resultData);
        }


        // ─ Heroes ───────────────────────────────────────────────────────────────

        private void ShowHeroKingdomInquiry()
        {
            var elems = new List<InquiryElement>();
            // “Any Kingdom”:
            elems.Add(new InquiryElement(null, new TextObject("{=IAD_AnyKingdom}Any Kingdom").ToString(), null, true, ""));
            foreach (var k in Kingdom.All)
                elems.Add(new InquiryElement(k.StringId, k.Name.ToString(), new ImageIdentifier(BannerCode.CreateFrom(k.Banner)), true, ""));

            var title = new TextObject("{=IAD_Kingdom_TITLE}Select Kingdom")
            .ToString();

            var text = new TextObject("{=IAD_Kingdom_TEXT}Choose kingdom")
            .ToString();

            var data = new MultiSelectionInquiryData(
                title,
                text,
                elems,
                true, 1, 1,
                GameTexts.FindText("str_next").ToString(), GameTexts.FindText("str_cancel").ToString(),
                selected =>
                {
                    var chosenId = (string)selected[0].Identifier;
                    ShowHeroClanInquiry(chosenId);
                },
                _ => { }
            );
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }

        private void ShowHeroClanInquiry(string kingdomId)
        {
            Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == kingdomId);
            var clans = new List<InquiryElement>();
            clans.Add(new InquiryElement(null, new TextObject("{=IAD_AnyClan}Any Clan").ToString(), null, true, ""));
            clans.Add(new InquiryElement("CLANLESS", new TextObject("{=IAD_Clanless}Clanless").ToString(), null, true, ""));
            if (kingdom != null)
            {
                foreach (var c in kingdom.Clans)
                    clans.Add(new InquiryElement(c.StringId, c.Name.ToString(), new ImageIdentifier(BannerCode.CreateFrom(c.Banner)), true, ""));
            }

            var title = new TextObject("{=IAD_Clan_TITLE}Select Clan")
            .ToString();

            var text = new TextObject("{=IAD_Clan_TEXT}Choose clan")
            .ToString();

            var data = new MultiSelectionInquiryData(
                title,
                text,
                clans,
                true, 1, 1,
                GameTexts.FindText("str_next").ToString(), GameTexts.FindText("str_back").ToString(),
                selected =>
                {
                    var clanId = (string)selected[0].Identifier;
                    ShowHeroCategoryInquiry(kingdomId, clanId);
                },
                _ => ShowHeroKingdomInquiry()
            );
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }

        private void ShowHeroCategoryInquiry(string kingdomId, string clanId)
        {
            var cats = new[]
            {
                new InquiryElement("ALL",       new TextObject("{=IAD_AllHeroes}All Heroes").ToString(),   null, true, ""),
                new InquiryElement("LORDS",     GameTexts.FindText("str_charactertype_noble").ToString(),        null, true, ""),
                new InquiryElement("WANDERERS", GameTexts.FindText("str_charactertype_wanderer").ToString(),    null, true, ""),
                new InquiryElement("NOTABLES",  GameTexts.FindText("str_charactertype_ruralnotable").ToString(),     null, true, ""),
            }.ToList();

            var title = new TextObject("{=IAD_HeroCategory_TITLE}Select Category")
            .ToString();

            var text = new TextObject("{=IAD_HeroCategory_TEXT}Filter by type:")
            .ToString();

            var data = new MultiSelectionInquiryData(
                title,
                text,
                cats,
                true, 1, 1,
                GameTexts.FindText("str_next").ToString(), GameTexts.FindText("str_back").ToString(),
                selected =>
                {
                    var catId = (string)selected[0].Identifier;
                    ShowHeroCultureInquiry(kingdomId, clanId, catId);
                },
                _ => ShowHeroClanInquiry(kingdomId)
            );
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }

        private void ShowHeroCultureInquiry(string kingdomId, string clanId, string catId)
        {
            // build the filtered list so far:
            var heroes = Hero.AllAliveHeroes.Where(h =>
            {
                bool kMatch = kingdomId == null
                    ? true
                    : (kingdomId == "null" ? h.MapFaction == null : h.MapFaction?.StringId == kingdomId);

                bool cMatch = clanId == null || clanId == "ANY"
                    ? true
                    : clanId == "CLANLESS"
                        ? h.Clan == null
                        : (h.Clan?.StringId == clanId);

                bool catMatch;
                switch (catId)
                {
                    case "ALL": catMatch = true; break;
                    case "LORDS": catMatch = h.IsLord; break;
                    case "WANDERERS": catMatch = h.IsWanderer; break;
                    case "NOTABLES": catMatch = h.IsNotable; break;
                    default: catMatch = true; break;
                };

                return kMatch && cMatch && catMatch;
            }).ToList();

            // build “Any Culture” + each distinct:
            var cultures = heroes.Select(h => h.Culture).Distinct().ToList();
            var elems = new List<InquiryElement> {
                new InquiryElement(null, new TextObject("{=IAD_AnyCulture}Any Culture").ToString(), null, true, "")
            };
            elems.AddRange(cultures.Select(c =>
                new InquiryElement(c.StringId, c.Name.ToString(), new ImageIdentifier(BannerCode.CreateFrom(c.BannerKey)), true, "")
            ));

            var title = new TextObject("{=IAD_Culture_TITLE}Select Culture")
            .ToString();

            var text = new TextObject("{=IAD_Culture_TEXT}You may further filter by culture.")
            .ToString();

            var data = new MultiSelectionInquiryData(
                title,
                text,
                elems,
                true, 1, 1,
                GameTexts.FindText("str_next").ToString(), GameTexts.FindText("str_back").ToString(),
                selected =>
                {
                    var culId = (string)selected[0].Identifier;
                    ShowHeroesMultiSelect(kingdomId, clanId, catId, culId);
                },
                _ => ShowHeroCategoryInquiry(kingdomId, clanId)
            );
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }

        private void ShowHeroesMultiSelect(
            string kingdomId, string clanId, string catId, string cultureId)
        {
            // final filtering:
            var heroes = Hero.AllAliveHeroes.Where(h =>
            {
                bool kMatch = kingdomId == null
                    ? true
                    : (kingdomId == "null" ? h.MapFaction == null : h.MapFaction?.StringId == kingdomId);

                bool cMatch = clanId == null || clanId == "ANY"
                    ? true
                    : clanId == "CLANLESS"
                        ? h.Clan == null
                        : (h.Clan?.StringId == clanId);

                bool catMatch;
                switch (catId)
                {
                    case "ALL": catMatch = true; break;
                    case "LORDS": catMatch = h.IsLord; break;
                    case "WANDERERS": catMatch = h.IsWanderer; break;
                    case "NOTABLES": catMatch = h.IsNotable; break;
                    default: catMatch = true; break;
                };

                bool culMatch = cultureId == null
                    ? true
                    : (cultureId == "null" ? true : h.Culture.StringId == cultureId);

                return kMatch && cMatch && catMatch && culMatch;
            }).ToList();

            // build a checkbox list of exactly those heroes
            var elems = heroes.Select(h =>
                new InquiryElement(
                    h.StringId,
                    new TextObject("{=IAD_HeroMulti}{NAME} [Diseases:{SICK} Injuries:{INJ}] ")
            .SetTextVariable("NAME", h.Name.ToString())
            .SetTextVariable("SICK", GetDiseasesString(h))
            .SetTextVariable("INJ", GetInjuriesString(h))
            .ToString(),
                    new ImageIdentifier(CharacterCode.CreateFrom(h.CharacterObject)), true, ""
                )
            ).ToList();

            var title = new TextObject("{=IAD_HeroMulti_TITLE}Choose Heroes")
            .ToString();

            var text = new TextObject("{=IAD_HeroMulti_TEXT}Select one or more heroes to inspect:")
            .ToString();

            var data = new MultiSelectionInquiryData(
                title,
                text,
                elems,
                true, 0, heroes.Count,
                GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_back").ToString(),
                selected =>
                {
                    // For each chosen hero, pop up a small TextInquiry:
                    foreach (var el in selected)
                    {
                        var h = Hero.AllAliveHeroes.FirstOrDefault(x => x.StringId == (string)el.Identifier);
                        if (h != null) ShowHeroDetailInquiry(h);
                    }
                },
                _ => ShowHeroCultureInquiry(kingdomId, clanId, catId)
            );
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }


        private void ShowHeroDetailInquiry(Hero h)
        {
            var st = GetHeroStatus(h);
            var diseases = GetDiseasesString(h);
            var injuries = GetInjuriesString(h);
            int today = (int)Math.Floor(CampaignTime.Now.ToDays);
            var day = 0;
            if (IsInCooldown(st.LastRecoveryDay)) day = InjuriesAndDiseasesGlobalSettings.Instance.ReinfectCooldownDays - (today - st.LastRecoveryDay);


            var text = new TextObject("{=IAD_HeroDetail}Diseases: {SICK} | Injuries: {INJ} | Immune Days: {DAY}")
            .SetTextVariable("SICK", diseases)
            .SetTextVariable("INJ", injuries)
            .SetTextVariable("DAY", day)
            .ToString();

            var inquiry = new InquiryData(
                h.Name.ToString(),
                text,
                true, false,
                GameTexts.FindText("str_ok").ToString(), null, null, null
            );
            InformationManager.ShowInquiry(inquiry);
        }

        // ─ Settlements ────────────────────────────────────────────────────────

        private void ShowSettlementKingdomInquiry()
        {
            var elems = new List<InquiryElement> {
                new InquiryElement(null,
                new TextObject("{=IAD_AnyKingdom}Any Kingdom")
                        .ToString(), null, true, "")
            };
            foreach (var k in Kingdom.All)
                elems.Add(new InquiryElement(k.StringId, k.Name.ToString(), new ImageIdentifier(BannerCode.CreateFrom(k.Banner)), true, ""));

            var title = new TextObject("{=IAD_Kingdom_TITLE}Select Kingdom")
            .ToString();

            var text = new TextObject("{=IAD_Kingdom_TEXT}Filter by kingdom:")
            .ToString();

            var data = new MultiSelectionInquiryData(
                title,
                text,
                elems,
                true, 1, 1,
                GameTexts.FindText("str_next").ToString(), GameTexts.FindText("str_cancel").ToString(),
                selected =>
                {
                    var kid = (string)selected[0].Identifier;
                    ShowSettlementClanInquiry(kid);
                },
                _ => { }
            );
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }

        private void ShowSettlementClanInquiry(string kingdomId)
        {
            Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == kingdomId);
            var elems = new List<InquiryElement> {
                new InquiryElement(null,
                new TextObject("{=IAD_AnyClan}Any Clan")
                        .ToString(), null, true, "")
            };
            if (kingdom != null)
            {
                foreach (var c in kingdom.Clans.Where(c => c.Settlements.Any()))
                    elems.Add(new InquiryElement(c.StringId, c.Name.ToString(), new ImageIdentifier(BannerCode.CreateFrom(c.Banner)), true, ""));
            }

            var title = new TextObject("{=IAD_Clan_TITLE}Select Clan")
            .ToString();

            var text = new TextObject("{=IAD_Clan_TEXT}Filter by clan:")
            .ToString();

            var data = new MultiSelectionInquiryData(
                title,
                text,
                elems,
                true, 1, 1,
                GameTexts.FindText("str_next").ToString(), GameTexts.FindText("str_back").ToString(),
                selected =>
                {
                    var cid = (string)selected[0].Identifier;
                    ShowSettlementMultiSelect(kingdomId, cid);
                },
                _ => ShowSettlementKingdomInquiry()
            );
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }

        private void ShowSettlementMultiSelect(string kingdomId, string clanId)
        {
            var settlements = Settlement.All.Where(s => s.IsTown || s.IsCastle || s.IsVillage)
                .Where(s =>
            {
                bool kMatch = kingdomId == null || kingdomId == ""
                    ? true
                    : (s.OwnerClan?.Kingdom?.StringId == kingdomId);

                bool cMatch = clanId == null
                    ? true
                    : (s.OwnerClan?.StringId == clanId);

                return kMatch && cMatch;
            }).ToList();


            var elems = settlements.Select(s =>
                new InquiryElement(
                    s.StringId,
                    new TextObject("{=IAD_SettlMulti}{NAME} [Diseases:{SICK}] ")
            .SetTextVariable("NAME", s.Name.ToString())
            .SetTextVariable("SICK", GetSettlementDiseasesString(s))
            .ToString(),
                    null, true, ""
                )
            ).ToList();

            var title = new TextObject("{=IAD_SettlMulti_TITLE}Choose Settlements")
            .ToString();

            var text = new TextObject("{=IAD_SettlMulti_TEXT}Select one or more to inspect:")
            .ToString();

            var data = new MultiSelectionInquiryData(
                title,
                text,
                elems,
                true, 0, settlements.Count,
                GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_back").ToString(),
                selected =>
                {
                    foreach (var el in selected)
                    {
                        var s = Settlement.Find(el.Identifier as string);
                        if (s != null) ShowSettlementDetailInquiry(s);
                    }
                },
                _ => ShowSettlementClanInquiry(kingdomId)
            );
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }

        private void ShowSettlementDetailInquiry(Settlement s)
        {
            var st = GetSettlementStatus(s);
            var diseases = GetSettlementDiseasesString(s);
            int today = (int)Math.Floor(CampaignTime.Now.ToDays);
            var day = 0;
            if (IsInCooldown(st.LastRecoveryDay)) day = InjuriesAndDiseasesGlobalSettings.Instance.ReinfectCooldownDays - (today - st.LastRecoveryDay);
            var text = new TextObject("{=IAD_SettlDetail}Diseases: {SICK} | Immune Days: {DAY}")
                        .SetTextVariable("SICK", diseases)
                        .SetTextVariable("DAY", day)
                        .ToString();

            var inquiry = new InquiryData(
                s.Name.ToString(),
                text,
                true, false,
                GameTexts.FindText("str_ok").ToString(), null, null, null
            );
            InformationManager.ShowInquiry(inquiry);
        }

        // ─ Helpers ──────────────────────────────────────────────────────────

        private string GetDiseasesString(Hero h)
            => GetHeroStatus(h).Diseases.Select(d => new TextObject("{=IAD_" + d.Config.Name + "}" + d.Config.Name).ToString()).DefaultIfEmpty(new TextObject("{=koX9okuG}None").ToString()).Join(", ");

        private string GetInjuriesString(Hero h)
            => GetHeroStatus(h).Injuries.Select(i => new TextObject("{=IAD_" + i.Config.Name + "}" + i.Config.Name).ToString()).DefaultIfEmpty(new TextObject("{=koX9okuG}None").ToString()).Join(", ");

        private string GetSettlementDiseasesString(Settlement s)
            => GetSettlementStatus(s).Diseases.Select(d => new TextObject("{=IAD_" + d.Config.Name + "}" + d.Config.Name).ToString()).DefaultIfEmpty(new TextObject("{=koX9okuG}None").ToString()).Join(", ");

        

        //End MulitChoiceInqiry

        //Dialogues
        private void DoctorDial(CampaignGameStarter starter)
        {
            var s = InjuriesAndDiseasesGlobalSettings.Instance;
            if (s == null) return;
            if (!s.DialDoctor) return;

            starter.AddPlayerLine(
                "iad_doctor_root_tavern",
                "tavernkeeper_talk",
                "iad_doctor_menu",
                "{=iad_doctor_root}I need a doctor immediately.",
                new ConversationSentence.OnConditionDelegate(() =>
                {
                    if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"CharacterObject StringId: {CharacterObject.OneToOneConversationCharacter?.StringId}, Name: {CharacterObject.OneToOneConversationCharacter?.Name}"));

                    var st = GetHeroStatus(Hero.MainHero);
                    bool isAny = CharacterObject.OneToOneConversationCharacter != null && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Tavernkeeper;
                    return
                    st.Diseases.Any() && isAny;
                }),
                null, 
                100, 
                new ConversationSentence.OnClickableConditionDelegate((out TextObject explanation) =>
                {
                    explanation = TextObject.Empty;
                    var day = CampaignTime.Now.ToDays - DocDay;
                    if (day <= InjuriesAndDiseasesGlobalSettings.Instance.DoctorDays)
                    {
                        explanation = new TextObject("{=IAD_DOC_DAYS}You have to wait {DAYS} days for another visit.")
                        .SetTextVariable("DAYS", InjuriesAndDiseasesGlobalSettings.Instance.DoctorDays - (int)day);
                        return false;
                    }

                    var st = GetHeroStatus(Hero.MainHero);
                    var days = 0;
                    bool CanHeal = false;
                    foreach (var ad in st.Diseases)
                    {
                        if (ad.DaysSick >= ad.Config.MinDays)
                        {
                            CanHeal = true;
                            break;
                        }
                        days = ad.Config.MinDays - ad.DaysSick;
                    }
                    if (!CanHeal)
                    {
                        explanation = new TextObject("{=IAD_DOC_DAYS_MIN}You have to wait {DAYS} days to be able to heal disease.")
                        .SetTextVariable("DAYS", days);
                        return false;
                    }

                    return true;
                }), null
            );

            starter.AddPlayerLine(
                "iad_doctor_root",
                "hero_main_options",
                "iad_doctor_menu",
                "{=iad_doctor_root}I need a doctor immediately.",
                new ConversationSentence.OnConditionDelegate(() =>
                {
                    if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"CharacterObject StringId: {CharacterObject.OneToOneConversationCharacter?.StringId}, Name: {CharacterObject.OneToOneConversationCharacter?.Name}"));

                    var st = GetHeroStatus(Hero.MainHero);
                    bool isAny = Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero?.Clan == Clan.PlayerClan;
                    return
                    st.Diseases.Any() && isAny;
                }),
                null, 
                100,
                new ConversationSentence.OnClickableConditionDelegate((out TextObject explanation) =>
                {
                    explanation = TextObject.Empty;
                    var day = CampaignTime.Now.ToDays - DocDay;
                    if (day <= InjuriesAndDiseasesGlobalSettings.Instance.DoctorDays)
                    {
                        explanation = new TextObject("{=IAD_DOC_DAYS}You have to wait {DAYS} days for another visit.")
                        .SetTextVariable("DAYS", InjuriesAndDiseasesGlobalSettings.Instance.DoctorDays - (int)day);
                        return false;
                    }

                    var st = GetHeroStatus(Hero.MainHero);
                    var days = 0;
                    bool CanHeal = false;
                    foreach (var ad in st.Diseases)
                    {
                        if (ad.DaysSick >= ad.Config.MinDays)
                        {
                            CanHeal = true;
                            break;
                        }
                        days = ad.Config.MinDays - ad.DaysSick;
                    }
                    if (!CanHeal)
                    {
                        explanation = new TextObject("{=IAD_DOC_DAYS_MIN}You have to wait {DAYS} days to be able to heal disease.")
                        .SetTextVariable("DAYS", days);
                        return false;
                    }

                    return true;
                }), null
            );
            starter.AddDialogLine(
                "iad_doctor_menu_npc",
                "iad_doctor_menu",
                "iad_doctor_menu",
                "{=iad_doctor_menu_npc}Very well then. It will cost you {COST}{GOLD_ICON}", //they ask
                                                new ConversationSentence.OnConditionDelegate(() =>
                                                {
                                                    var st = GetHeroStatus(Hero.MainHero);
                                                    var cost = DocCost;
                                                    var diseases = st.Diseases.Count;
                                                    var settlement = Settlement.CurrentSettlement ?? HeroHelper.GetClosestSettlement(Hero.MainHero);
                                                    if (settlement == null) return false;

                                                    if (cost <= 0 || DocSettlement != settlement)
                                                    {
                                                        DocSettlement = settlement;
                                                        if (settlement.IsTown) cost = MBRandom.RandomInt(100, settlement.Town.Gold);
                                                        else if (settlement.IsVillage) cost = MBRandom.RandomInt(100, settlement.Village.Gold);

                                                        cost *= diseases;
                                                        cost += (int)(cost * s.DoctorMult);
                                                    }

                                                    DocCost = cost;
                                                    MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                                                    MBTextManager.SetTextVariable("COST", DocCost);
                                                    return true;
                                                }
                ),
                null,
                100,
                null
            );
            starter.AddPlayerLine(
                "iad_doctor_confirm", "iad_doctor_menu", "start",
                "{=iad_doctor_confirm}Pay {COST}{GOLD_ICON}",
                new ConversationSentence.OnConditionDelegate(() =>
                {
                    MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                    MBTextManager.SetTextVariable("COST", DocCost);
                    return true;
                }
                ),
                new ConversationSentence.OnConsequenceDelegate(() =>
                {
                    DocDay = (int)CampaignTime.Now.ToDays;
                    GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, HeroHelper.GetClosestSettlement(Hero.MainHero), DocCost);
                    var st = GetHeroStatus(Hero.MainHero);
                    for (int i = st.Diseases.Count - 1; i >= 0; i--)
                    {
                        var ad = st.Diseases[i];
                        var medSkill = Hero.MainHero.GetSkillValue(DefaultSkills.Medicine);
                        if (ad.TryHeal(medSkill, InjuriesAndDiseasesGlobalSettings.Instance.DoctorBonus))
                        {

                            // ← record the day they recovered:
                            st.LastRecoveryDay = (int)Math.Floor(CampaignTime.Now.ToDays);
                            st.Diseases.Remove(ad);

                            var text = new TextObject("{=IAD_PLAYER_RECOVERY}You have recovered from {SICK}!")
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", Hero.MainHero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                            .ToString(), Colors.Green));
                        }
                        else
                        {
                            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=IAD_DOC_FAILED}The healing failed. The {SICK} is still active.")
                                .SetTextVariable("SICK", ad.Config.ToString() )
                            .ToString(), Colors.Red));
                        }
                    }
                }
                ), 50,
        // clickable condition: returns false (disabled) and explanation if not enough gold
        new ConversationSentence.OnClickableConditionDelegate((out TextObject explanation) =>
        {
            explanation = TextObject.Empty;
            if (Hero.MainHero.Gold < DocCost)
            {
                explanation = new TextObject("{=xVZVYNan}You don't have enough{GOLD_ICON}.")
                .SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">"); // vanilla string id for "You don't have enough" – keep for localization
                return false;
            }
            return true;
        }), null
            );
            starter.AddPlayerLine(
                "iad_doctor_cancel", "iad_doctor_menu", "start",
                "{=iad_doctor_cancel}On second thought, never mind.",
                null, null, 40, null, null
            );
        }



        //End Dialogues

        private void OnDailyTickPersist()
        {
            try
            {
                // run your normal daily logic...
                OnDailyTick();

                var emptyHeroes = _heroStatuses
                    .Where(kv => kv.Value.Diseases.Count == 0 && kv.Value.Injuries.Count == 0 && !IsInCooldown(kv.Value.LastRecoveryDay))
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var id in emptyHeroes)
                    _heroStatuses.TryRemove(id, out _);

                var emptySettlements = _settlementStatuses
                    .Where(kv => kv.Value.Diseases.Count == 0 && !IsInCooldown(kv.Value.LastRecoveryDay))
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var id in emptySettlements)
                    _settlementStatuses.TryRemove(id, out _);
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"[IAD] Error in OnDailyTickPersist: {e.Message}")
                );
            }

        }

        public void ShuffleList<T>(IList<T> source, int howManyToShuffle)
        {
            if (source == null) return;

            var count = source.Count;
            if (count <= 1) return;
            
            int k = Math.Min(howManyToShuffle, count);

            for (int i = 0; i < k; i++)
            {
                int j = MBRandom.RandomInt(i, count);
                (source[i], source[j]) = (source[j], source[i]);
            }
        }

        private void OnDailyTick()
        {
            try
            {
                _daysSinceLastCheck++;
                if (_daysSinceLastCheck >= InjuriesAndDiseasesGlobalSettings.Instance.IntervalDays)
                {
                    _daysSinceLastCheck = 0;


                    if (InjuriesAndDiseasesGlobalSettings.Instance.AllowDisease)
                    {
                        var maxObjects = InjuriesAndDiseasesGlobalSettings.Instance.MaxObjects;

                        var heroes = Hero.AllAliveHeroes
                            .Where(IsEligibleForChecks)
                            .ToList();

                        if (heroes.Count > maxObjects)
                        {
                            ShuffleList(heroes, heroes.Count);

                            heroes = heroes.Take(maxObjects).ToList();
                        }

                        RunSicknessChecksHero(heroes);

                        var settlements = Settlement.All
                            .Where(s => s.IsTown || s.IsVillage || s.IsCastle)
                            .ToList();

                        if(settlements.Count > maxObjects)
                        {
                            ShuffleList(settlements, settlements.Count);

                            settlements = settlements.Take(maxObjects).ToList();
                        }

                        RunSicknessChecksSettlement(settlements);
                    }

                }

                // 3) Always update statuses on the same snapshot(s)
                var heroesToUpdate = Hero.AllAliveHeroes
                    .Where(IsEligibleForChecks)
                    .ToList();
                UpdateHeroStatuses(heroesToUpdate);

                var settlementsToUpdate = Settlement.All
                    .Where(s => s.IsTown || s.IsVillage || s.IsCastle)
                    .ToList();
                UpdateSettlementStatuses(settlementsToUpdate);
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"[IAD] Error in OnDailyTick: {e.Message}")
                );
            }

        }

        private bool IsEligibleForChecks(Hero h)
        {
            if (h == Hero.MainHero) return InjuriesAndDiseasesGlobalSettings.Instance.AllowPlayer;
            if (h.Clan == Clan.PlayerClan) return InjuriesAndDiseasesGlobalSettings.Instance.AllowPlayerClan;
            if (h.IsWanderer) return InjuriesAndDiseasesGlobalSettings.Instance.AllowWanderer;
            if (h.IsNotable) return InjuriesAndDiseasesGlobalSettings.Instance.AllowNotables;
            if (h.IsChild) return InjuriesAndDiseasesGlobalSettings.Instance.AllowChildren;
            return false;
        }

        private void RunSicknessChecksHero(IEnumerable<Hero> heroes)
        {

            // Heroes catch disease
            foreach (var hero in heroes)
            {
                var s = InjuriesAndDiseasesGlobalSettings.Instance;
                if (s == null) return;
                var st = GetHeroStatus(hero);
                if (st.Diseases.Any() || IsInCooldown(st.LastRecoveryDay) || InjuriesAndDiseasesEditor.Instance.DiseasesList == null || InjuriesAndDiseasesEditor.Instance.DiseasesList.Count == 0) continue;
                var cfg = InjuriesAndDiseasesEditor.Instance.DiseasesList.GetRandomElement();

                var chance = cfg.GetSickChance;

                var terrain = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(hero.GetPosition().AsVec2);
                var weather = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(hero.GetPosition().AsVec2);
                
                if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Terrain = {terrain.ToString()}, Weather = {weather.ToString()}"));

                if (cfg.RainChance != 1f && (terrain == TerrainType.Water || terrain == TerrainType.River || terrain == TerrainType.Bridge || terrain == TerrainType.Fording || weather == MapWeatherModel.WeatherEvent.LightRain || weather == MapWeatherModel.WeatherEvent.HeavyRain))
                {
                    chance *= cfg.RainChance;
                }
                if (cfg.SnowChance != 1f && (terrain == TerrainType.Snow || weather == MapWeatherModel.WeatherEvent.Snowy || weather == MapWeatherModel.WeatherEvent.Blizzard))
                {
                    chance *= cfg.SnowChance;
                }
                if (cfg.DesertChance != 1f && (terrain == TerrainType.Desert || terrain == TerrainType.Dune))
                {
                    chance *= cfg.DesertChance;
                }

                var ageDiff = Campaign.Current.Models.AgeModel.BecomeOldAge - Campaign.Current.Models.AgeModel.HeroComesOfAge;
                if(ageDiff > 0)
                {
                    var age = hero.Age / ageDiff;
                    if (chance > 0 && age > 0) chance *= age;
                }

                var medSkill = Math.Abs(1f - hero.GetSkillValue(DefaultSkills.Medicine) / 150f);
                medSkill = Math.Max(0f , medSkill);
                if (medSkill < 0) medSkill = 0.01f;
                if (chance > 0 && medSkill != 0) chance *= medSkill;

                if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Chance of getting sick: {chance}"));

                chance = MathF.Clamp(chance, 0f, 1f);

                if (MBRandom.RandomFloat < chance)
                {
                    st.Diseases.Add(new ActiveDisease(cfg, 0));

                    if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && hero.Clan != Clan.PlayerClan && hero != Hero.MainHero) 
                    {
                        var text = new TextObject("{=IAD_HERO_ILL}{HERO} has fallen ill with {SICK}!")
                                                .SetTextVariable("HERO", hero.Name)
                                                .SetTextVariable("SICK", new TextObject("{=IAD_" + cfg.Name + "}" + cfg.Name).ToString());
                        StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);

                        InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Colors.Gray));
                        
                    } 
                    else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan && hero.Clan == Clan.PlayerClan && hero != Hero.MainHero) 
                    {
                        var text = new TextObject("{=IAD_HERO_ILL}{HERO} has fallen ill with {SICK}!")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + cfg.Name + "}" + cfg.Name).ToString());
                        StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);

                        InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Colors.Red));
                    }

                    else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayer && hero == Hero.MainHero) 
                    {

                        var text = new TextObject("{=IAD_PLAYER_ILL}You have fallen ill with {SICK}!")
                                                .SetTextVariable("SICK", new TextObject("{=IAD_" + cfg.Name + "}" + cfg.Name).ToString());
                        StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                        InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Colors.Red));
                    } 
                }
            }



        }

        private void RunSicknessChecksSettlement(IEnumerable<Settlement> settlements)
        {
            // Settlements catch disease
            foreach (var settlement in settlements)
            {

                if (!settlement.IsTown && !settlement.IsVillage && !settlement.IsCastle)
                    continue;

                var st = GetSettlementStatus(settlement);
                if (st.Diseases.Any() || IsInCooldown(st.LastRecoveryDay) || InjuriesAndDiseasesEditor.Instance.DiseasesList == null || InjuriesAndDiseasesEditor.Instance.DiseasesList.Count == 0) continue;

                var cfg = InjuriesAndDiseasesEditor.Instance.DiseasesList.GetRandomElement();

                var chance = cfg.GetSickChance;

                var terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(settlement.CurrentNavigationFace);
                var weather = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(settlement.Position2D);
                if (cfg.RainChance != 1f && (terrain == TerrainType.Water || terrain == TerrainType.River || terrain == TerrainType.Bridge || terrain == TerrainType.Fording || weather == MapWeatherModel.WeatherEvent.LightRain || weather == MapWeatherModel.WeatherEvent.HeavyRain))
                {
                    chance *= cfg.RainChance;
                }
                if (cfg.SnowChance != 1f && (terrain == TerrainType.Snow || weather == MapWeatherModel.WeatherEvent.Snowy || weather == MapWeatherModel.WeatherEvent.Blizzard))
                {
                    chance *= cfg.SnowChance;
                }
                if (cfg.DesertChance != 1f && (terrain == TerrainType.Desert || terrain == TerrainType.Dune))
                {
                    chance *= cfg.DesertChance;
                }

                var governor = settlement?.Town?.Governor;
                if(governor != null)
                {
                    var medSkill = Math.Abs(1f - governor.GetSkillValue(DefaultSkills.Medicine) / 150f);
                    medSkill = Math.Max(0f, medSkill);
                    if (medSkill < 0) medSkill = 0.01f;
                    if (chance > 0 && medSkill != 0) chance *= medSkill;
                }


                var bonusBuilding = 0f;
                if (settlement.IsFortification && InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus != 0 && chance != 0f && chance != 1f)
                {
                    if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Checking Buildings"));
                    var buildings = settlement.Town.Buildings.ToList();
                    if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Buildings: {string.Join(", ", buildings.Select(b => $"{b.Name.Value}:{b.CurrentLevel}"))}"));
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
                                    bonusBuilding += chance * raw;
                                }
                            }
                            foreach (var n in negative)
                            {
                                if (b.Name.Contains(n))
                                {
                                    bonusBuilding -= chance * raw;
                                }
                            }

                        }

                    }

                    chance -= bonusBuilding;
                }
                chance = MathF.Clamp(chance, 0f, 1f);

                if (MBRandom.RandomFloat < chance)
                {
                    st.Diseases.Add(new ActiveDisease(cfg, 0));

                    if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && settlement.OwnerClan != Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=IAD_SETTLEMENT_ILL}{NAME} contracted {SICK}!")
                        .SetTextVariable("NAME", settlement.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + cfg.Name + "}" + cfg.Name).ToString())
                        .ToString(), Colors.Gray));

                    else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan && settlement.OwnerClan == Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=IAD_SETTLEMENT_ILL}{NAME} contracted {SICK}!")
                        .SetTextVariable("NAME", settlement.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + cfg.Name + "}" + cfg.Name).ToString())
                        .ToString(), Colors.Red));
                }
            }
        }


        private void UpdateHeroStatuses(IEnumerable<Hero> heroes)
        {
            var s = InjuriesAndDiseasesGlobalSettings.Instance;
            if (s == null) return;
            var protectedNames = InjuriesAndDiseasesGlobalSettings.ParseNameCsv(s.ProtectHeroes);
            foreach (var hero in heroes)
            {
                if (!hero.IsAlive)
                    continue;   // skip anybody already dead
                var st = GetHeroStatus(hero);
                bool isProtected = protectedNames.Any(n =>
                    string.Equals(n, hero.Name.ToString(), StringComparison.OrdinalIgnoreCase));

                // Diseases
                for (int i = st.Diseases.Count - 1; i >= 0; i--)
                {
                    var ad = st.Diseases[i];
                    ad.DayPassed();
                    bool DeathDay = ad.DaysSick % s.IntervalDeathDays == 0;

                    float deathChance = ad.Config.DeathChance;
                    float age = hero.Age / Campaign.Current.Models.AgeModel.BecomeOldAge - Campaign.Current.Models.AgeModel.HeroComesOfAge;
                    if (age > 0) deathChance *= age;


                    // 1) Death roll
                    if (DeathDay && MBRandom.RandomFloat < deathChance && !isProtected &&
                        (hero.PartyBelongedTo == null || (hero.PartyBelongedTo.MapEvent == null && hero.PartyBelongedTo.SiegeEvent == null)))
                    {
                        if (s.Debug)
                        {
                            InformationManager.DisplayMessage(new InformationMessage($"Hero:{hero.Name}, Death Mark: {hero.DeathMark}, PartyActive: " + hero?.PartyBelongedTo?.IsActive
                            .ToString(), Colors.Red));

                        }

                        KillCharacterAction.ApplyByDeathMark(hero);

                            if (hero.IsLord && hero.Clan != Clan.PlayerClan)
                            {
                                var text = new TextObject("{=IAD_ILL_DEAD}{HERO} died from {SICK}!")
                                    .SetTextVariable("HERO", hero.Name)
                                    .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                                StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                                InformationManager.DisplayMessage(new InformationMessage(text
                                    .ToString(), new Color(147f / 255f, 112f / 255f, 219f / 255f)));
                            }

                            else if (hero.Clan == Clan.PlayerClan)
                            {
                                var text = new TextObject("{=IAD_ILL_DEAD}{HERO} died from {SICK}!")
                                    .SetTextVariable("HERO", hero.Name)
                                    .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                                StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                                InformationManager.DisplayMessage(new InformationMessage(text
                                    .ToString(), new Color(255f / 255f, 20f / 255f, 147f / 255f)));


                            }
                            if (!hero.IsHumanPlayerCharacter)
                            {
                                if (hero.Clan == Clan.PlayerClan && hero.IsLord)
                                {
                                    // before you actually kill them:
                                    var notif = new DiseaseDeathMapNotification(
                                        hero,
                                        new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString(),     // disease
                                        CampaignTime.Now
                                    );
                                    MBInformationManager.AddNotice(notif);
                                }
                                _diseaseDeaths.Add(hero);
                            }

                            if (hero.IsHumanPlayerCharacter)
                            {
                                MBInformationManager.ShowSceneNotification(new DiseaseDeathSceneNotificationItem(hero, new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString()));
                            }

                            // grab the private CreateObituary method
                            var createObit = typeof(KillCharacterAction)
                                .GetMethod("CreateObituary", BindingFlags.NonPublic | BindingFlags.Static);

                            if (createObit != null)
                            {
                                var obituary = (TextObject)createObit.Invoke(
                                  null,
                                  new object[] { hero, KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge }
                                );

                                // 2) overwrite the embedded FURTHER_DETAILS with your own disease‑specific text:
                                var customDetails = new TextObject("{=IAD_DIED_OF_DISEASE}{?CHARACTER.GENDER}She{?}He{\\?} succumbed to {SICK} in {YEAR}")
                                    .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name))
                                    .SetTextVariable("YEAR", CampaignTime.Now.GetYear.ToString());
                                StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, customDetails, true);

                                obituary.SetTextVariable("FURTHER_DETAILS", customDetails);
                                // invoke it: (object) null = static, params are (Hero hero, detail)
                                hero.EncyclopediaText = obituary;
                            }

                            // Remove from our list so we don’t try to heal them
                            st.Diseases.RemoveAt(i);
                            // Skip processing any further diseases/injuries on this now‐dead hero
                            break;
                        

                    }
                    var medSkill = hero.GetSkillValue(DefaultSkills.Medicine);
                    if (ad.TryHeal(medSkill, 0))
                    {

                        // ← record the day they recovered:
                        st.LastRecoveryDay = (int)Math.Floor(CampaignTime.Now.ToDays);
                        st.Diseases.RemoveAt(i);

                        if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && hero.Clan != Clan.PlayerClan && hero != Hero.MainHero)
                        {
                            var text = new TextObject("{=IAD_HERO_RECOVERY}{HERO} recovered from {SICK}!")
                            .SetTextVariable("HERO", hero.Name)
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                            .ToString(), Colors.Gray));
                        }
                        else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan && hero.Clan == Clan.PlayerClan && hero != Hero.MainHero)
                        {
                            var text = new TextObject("{=IAD_HERO_RECOVERY}{HERO} recovered from {SICK}!")
                            .SetTextVariable("HERO", hero.Name)
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                            .ToString(), Colors.Green));
                        }
                        else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayer && hero == Hero.MainHero)
                        {
                            var text = new TextObject("{=IAD_PLAYER_RECOVERY}You have recovered from {SICK}!")
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                            .ToString(), Colors.Green));
                        }
                    }
                }

                // Injuries
                for (int i = st.Injuries.Count - 1; i >= 0; i--)
                {
                    var ai = st.Injuries[i];
                    ai.DayPassed();
                    bool DeathDay = ai.DaysInjured % s.IntervalDeathDays == 0;

                    float deathChance = ai.Config.DeathChance;
                    float age = hero.Age / Campaign.Current.Models.AgeModel.BecomeOldAge - Campaign.Current.Models.AgeModel.HeroComesOfAge;
                    if (age > 0) deathChance *= age;

                    // 1) Death roll
                    if (DeathDay && MBRandom.RandomFloat < deathChance && !isProtected &&
                        (hero.PartyBelongedTo == null || (hero.PartyBelongedTo.MapEvent == null && hero.PartyBelongedTo.SiegeEvent == null)))
                    {
                        if (hero.IsLord && hero.Clan != Clan.PlayerClan)
                        {
                            var text = new TextObject("{=IAD_ILL_DEAD}{HERO} died from {SICK}!")
                                .SetTextVariable("HERO", hero.Name)
                                .SetTextVariable("SICK", new TextObject("{=IAD_" + ai.Config.Name + "}" + ai.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                                .ToString(), new Color(147f / 255f, 112f / 255f, 219f / 255f)));
                        }

                        else if (hero.Clan == Clan.PlayerClan)
                        {
                            var text = new TextObject("{=IAD_ILL_DEAD}{HERO} died from {SICK}!")
                                .SetTextVariable("HERO", hero.Name)
                                .SetTextVariable("SICK", new TextObject("{=IAD_" + ai.Config.Name + "}" + ai.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                                .ToString(), new Color(255f / 255f, 20f / 255f, 147f / 255f)));


                        }
                        KillCharacterAction.ApplyByWounds(hero);

                        // Remove from our list so we don’t try to heal them
                        st.Injuries.RemoveAt(i);
                        // Skip processing any further diseases/injuries on this now‐dead hero
                        break;
                    }

                    var medSkill = hero.GetSkillValue(DefaultSkills.Medicine);
                    if (ai.TryHeal(medSkill))
                    {
                        st.Injuries.RemoveAt(i);

                        if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && hero.Clan != Clan.PlayerClan && hero != Hero.MainHero)
                        {
                            var text = new TextObject("{=IAD_HERO_HEAL}{HERO} healed from {SICK}!")
                            .SetTextVariable("HERO", hero.Name)
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ai.Config.Name + "}" + ai.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                            .ToString(), Colors.Gray));
                        }
                        else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan && hero.Clan == Clan.PlayerClan && hero != Hero.MainHero)
                        {
                            var text = new TextObject("{=IAD_HERO_HEAL}{HERO} healed from {SICK}!")
                            .SetTextVariable("HERO", hero.Name)
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ai.Config.Name + "}" + ai.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                            .ToString(), Colors.Green));
                        }
                        else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayer && hero == Hero.MainHero)
                        {
                            var text = new TextObject("{=IAD_PLAYER_HEAL}You have healed from {SICK}!")
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ai.Config.Name + "}" + ai.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                            .ToString(), Colors.Green));
                        }
                    }
                }
            }
        }

        private void UpdateSettlementStatuses(IEnumerable<Settlement> settlements)
        {
            foreach (var settlement in settlements)
            {
                var st = GetSettlementStatus(settlement);

                for (int i = st.Diseases.Count - 1; i >= 0; i--)
                {
                    var ad = st.Diseases[i];
                    ad.DayPassed();

                    int medskill = 0;

                    if (settlement.IsTown && settlement.Town.Governor != null) medskill = settlement.Town.Governor.GetSkillValue(DefaultSkills.Medicine);

                    if (ad.TryHeal(medskill, 0))
                    {
                        // ← record the day they recovered:
                        st.LastRecoveryDay = (int)Math.Floor(CampaignTime.Now.ToDays);
                        st.Diseases.RemoveAt(i);
                        if (InjuriesAndDiseasesGlobalSettings.Instance.Debug)
                            InformationManager.DisplayMessage(
                              new InformationMessage($"[Settlement] {settlement.Name} cleared {ad.Config.Name}."));

                        if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && settlement.OwnerClan != Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=IAD_SETTLEMENT_RECOVERY}{NAME} cleared {SICK}!")
                            .SetTextVariable("NAME", settlement.Name)
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString())
                            .ToString(), Colors.Gray));

                        else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan && settlement.OwnerClan == Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=IAD_SETTLEMENT_RECOVERY}{NAME} cleared {SICK}!")
                            .SetTextVariable("NAME", settlement.Name)
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString())
                            .ToString(), Colors.Green));
                    }
                }
            }
        }


        private void OnHeroWounded(Hero victim)
        {
            try
            {
                if (!InjuriesAndDiseasesGlobalSettings.Instance.AllowInjury) return;
                if (victim == null || !victim.IsAlive) return;
                if (!IsEligibleForChecks(victim)) return;

                var st = GetHeroStatus(victim);

                var injuries = InjuriesAndDiseasesEditor.Instance.InjuriesList.Where(d => !st.Injuries.Any(c => c.Config.Name == d.Name)).ToList();
                if (injuries == null || injuries.Count() == 0) return;

                // pick a random injury
                var cfg = injuries.GetRandomElement();

                var chance = cfg.GetInjuryChance;

                var medSkill = Math.Abs(1f - victim.GetSkillValue(DefaultSkills.Medicine) / 150f);
                medSkill = Math.Max(0f, medSkill);
                if (medSkill < 0) medSkill = 0.01f;
                if (chance > 0 && medSkill != 0) chance *= medSkill;

                var athSkill = Math.Abs(1f - victim.GetSkillValue(DefaultSkills.Athletics) / 150f);
                athSkill = Math.Max(0f, athSkill);
                if (athSkill < 0) athSkill = 0.01f;
                if (chance > 0 && athSkill != 0) chance *= athSkill;

                MathF.Clamp(chance, 0f, 1f);

                if (MBRandom.RandomFloat > chance) return;

                st.Injuries.Add(new ActiveInjury(cfg, 0));

                if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && victim.Clan != Clan.PlayerClan && victim != Hero.MainHero)
                {
                    var text = new TextObject("{=IAD_HERO_INJURY}{HERO} suffered {SICK}!")
                        .SetTextVariable("HERO", victim.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + cfg.Name + "}" + cfg.Name).ToString());
                    StringHelpers.SetCharacterProperties("CHARACTER", victim.CharacterObject, text, true);
                    InformationManager.DisplayMessage(new InformationMessage(text
                        .ToString(), Colors.Gray));
                }
                else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan && victim.Clan == Clan.PlayerClan && victim != Hero.MainHero)
                {
                    var text = new TextObject("{=IAD_HERO_INJURY}{HERO} suffered {SICK}!")
                        .SetTextVariable("HERO", victim.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + cfg.Name + "}" + cfg.Name).ToString());
                    StringHelpers.SetCharacterProperties("CHARACTER", victim.CharacterObject, text, true);
                    InformationManager.DisplayMessage(new InformationMessage(text
                        .ToString(), Colors.Red));
                }
                else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayer && victim == Hero.MainHero)
                {
                    var text = new TextObject("{=IAD_PLAYER_INJURY}You suffered {SICK}!")
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + cfg.Name + "}" + cfg.Name).ToString());
                    StringHelpers.SetCharacterProperties("CHARACTER", victim.CharacterObject, text, true);
                    InformationManager.DisplayMessage(new InformationMessage(text
                        .ToString(), Colors.Red));
                }
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"[IAD] Error in OnHeroWounded: {e.Message}")
                );
            }

        }

        private void OnHeroCombatHit(
            CharacterObject attackerTroop,
            CharacterObject attackedTroop,
            PartyBase party,
            WeaponComponentData usedWeapon,
            bool isFatal,
            int xp)
        {
            if (!InjuriesAndDiseasesGlobalSettings.Instance.AllowDisease) return;


            // Find the Hero for each CharacterObject
            var attackerHero = attackerTroop.HeroObject;
            var victimHero = attackedTroop.HeroObject;

            if (attackerHero == null || victimHero == null) return;
            if(!attackerHero.IsAlive || !victimHero.IsAlive) return;
            if (!IsEligibleForChecks(attackerHero) && !IsEligibleForChecks(victimHero)) return;

            // Spread blood/contact diseases both ways
            TryBloodInfect(attackerHero, victimHero);
            TryBloodInfect(victimHero, attackerHero);
        }

        private void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount)
        {

            if (!InjuriesAndDiseasesGlobalSettings.Instance.AllowDisease) return;
            if (mother == null || !mother.IsAlive || aliveChildren == null || aliveChildren.Count <= 0) return;

            // Only transfer diseases that are blood‑borne or contact
            foreach (var child in aliveChildren)
            {
                if (child == null || !child.IsAlive) continue;
                if (!IsEligibleForChecks(mother) || !IsEligibleForChecks(child)) continue;



                // Mother → child
                TryBloodInfect(mother, child);

                // (Optional) child → mother
                TryBloodInfect(child, mother);
            }
        }


        private void OnBattleStarted(
            PartyBase attackerParty,
            PartyBase defenderParty,
            object subject,
            bool showNotification)
        {
            // Only proceed if disease is on
            if (!InjuriesAndDiseasesGlobalSettings.Instance.AllowDisease) return;

            // Only care about battles between two MOBILE parties
            if (!(attackerParty.MobileParty is MobileParty attackersMp) ||
                !(defenderParty.MobileParty is MobileParty defendersMp))
                return;

            if (attackersMp.LeaderHero == null || defendersMp.LeaderHero == null) return;
            // (Optional) Skip if main hero is NOT part of either side
            var main = Hero.MainHero;
            bool heroInvolved =
                attackersMp.LeaderHero == main || defendersMp.LeaderHero == main || main.PartyBelongedToAsPrisoner == attackerParty || main.PartyBelongedToAsPrisoner == defenderParty;
            if (!heroInvolved)
                return;

            // Now safe to pull rosters
            var attackers = attackersMp.MemberRoster
                                     .GetTroopRoster()
                                     .Where(e => e.Character?.IsHero ?? false)
                                     .Select(e => e.Character.HeroObject)
                                     .Where(h => h.IsAlive && IsEligibleForChecks(h))
                                     .ToList();

            var defenders = defendersMp.MemberRoster
                                     .GetTroopRoster()
                                     .Where(e => e.Character?.IsHero ?? false)
                                     .Select(e => e.Character.HeroObject)
                                     .Where(h => h.IsAlive && IsEligibleForChecks(h))
                                     .ToList();

            // Roll infections
            foreach (var a in attackers)
                foreach (var d in defenders)
                {
                    TryContactInfect(a, d);
                    TryContactInfect(d, a);
                }
        }

        private void OnConversationEnded(IEnumerable<CharacterObject> characters)
        {

            if (!InjuriesAndDiseasesGlobalSettings.Instance.AllowDisease) return;

            // Find all eligible heroes who participated
            var heroes = characters
                .Select(c => c.HeroObject)
                .Where(h => h != null && h.IsAlive && IsEligibleForChecks(h))
                .ToList();

            if (heroes == null || heroes.Count <= 0) return;

            // For each pair (A → B), attempt a contact‐based infection
            for (int i = 0; i < heroes.Count; i++)
            {
                for (int j = 0; j < heroes.Count; j++)
                {
                    if (i == j) continue;
                    TryContactInfect(heroes[i], heroes[j]);
                }
            }
        }

        private void OnDailyTickParty(MobileParty party)
        {
            try
            {
                if (!InjuriesAndDiseasesGlobalSettings.Instance.AllowDisease) return;

                if (party == null) return;

                // wound troops
                if(party.LeaderHero != null)
                CheckPartyTroopsToInfect(party);

                //first check if party army

                var army = party.Army;
                
                if(army != null)
                {
                    var parmy = army.Parties;
                    var harmy = new List<Hero>();
                    
                    foreach(var p in parmy)
                    {
                    var list = p.MemberRoster
                    .GetTroopRoster()
                    .Where(e => e.Character.IsHero)
                    .Select(e => e.Character.HeroObject)
                    .Where(h => h != null && h.IsAlive && IsEligibleForChecks(h)).ToList();
                    harmy.AddRange(list);
                    }

                    if(harmy != null)
                    {
                        // For each pair (A → B), attempt a contact‐based infection
                        for (int i = 0; i < harmy.Count; i++)
                        {
                            for (int j = 0; j < harmy.Count; j++)
                            {
                                if (i == j) continue;
                                TryContactInfect(harmy[i], harmy[j]);
                            }
                        }
                        return;
                    }
                }
                // if not army, between party only
                var heroes = party.MemberRoster
                    .GetTroopRoster()
                    .Where(e => e.Character.IsHero)
                    .Select(e => e.Character.HeroObject)
                    .Where(h => h != null && h.IsAlive && IsEligibleForChecks(h)).ToList();

                if (heroes == null) return;
                // For each pair (A → B), attempt a contact‐based infection
                for (int i = 0; i < heroes.Count; i++)
                {
                    for (int j = 0; j < heroes.Count; j++)
                    {
                        if (i == j) continue;
                        TryContactInfect(heroes[i], heroes[j]);
                    }
                }
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage($"[IAD] Error in OnDailyTickParty: {e.Message}")
                );
            }

        }

        // This is where TaleWorlds.SaveSystem hooks you in on BOTH
        // saving *and* loading.  Bannerlord calls this automatically.
        public override void SyncData(IDataStore dataStore)
        {
            // The same container instance is passed by ref on load & save.
            dataStore.SyncData("IAD_heroStatusesSave", ref _heroStatusesSave);

            if (dataStore.IsLoading)
            {
                // loading
                _heroStatuses.Clear();

                foreach (var entry in _heroStatusesSave)
                {
                    // reconstruct disease objects from saved DTOs + available configs
                    var diseases = new List<ActiveDisease>();
                    foreach (var de in entry.Diseases)
                    {
                        var cfg = InjuriesAndDiseasesEditor.Instance.DiseasesList.FirstOrDefault(c => c.Name == de.ConfigId);
                        if (cfg != null)
                        {
                            // use internal ctor or restore method to set days
                            var active = new ActiveDisease(cfg, de.Days); // if you added internal ctor
                                                                          // or: var active = new ActiveDisease(cfg); active.RestoreDays(de.Days);
                            diseases.Add(active);
                        }
                    }
                    var injuries = new List<ActiveInjury>();
                    foreach (var ie in entry.Injuries)
                    {
                        var cfg = InjuriesAndDiseasesEditor.Instance.InjuriesList.FirstOrDefault(c => c.Name == ie.ConfigId);
                        if (cfg != null)
                        {
                            var active = new ActiveInjury(cfg, ie.Days);
                            injuries.Add(active);
                        }
                    }

                    if (diseases.Count <= 0 && injuries.Count <= 0 && !IsInCooldown(entry.LastRecoveryDay)) continue;

                    var hero = Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == entry.HeroId);
                    if (hero != null)
                    {
                        var status = new HeroStatus();
                        status.LastRecoveryDay = entry.LastRecoveryDay; // ensure HeroStatus exposes this
                        status.Diseases.AddRange(diseases);
                        status.Injuries.AddRange(injuries);
                        _heroStatuses[hero] = status;
                    }
                }
            }
            else
            {
                // saving
                _heroStatusesSave.Clear();

                foreach (var kv in _heroStatuses.Where(kv => kv.Key != null && kv.Value != null && kv.Key.IsAlive &&
                      (kv.Value.Diseases.Count > 0 || kv.Value.Injuries.Count > 0 || IsInCooldown(kv.Value.LastRecoveryDay))))
                {
                    var diseaseEntries = kv.Value.Diseases
                        .Select(d => new DiseaseEntry(d.Config.Name, d.DaysSick))
                        .ToList();

                    var injuryEntries = kv.Value.Injuries
                        .Select(i => new InjuryEntry(i.Config.Name, i.DaysInjured))
                        .ToList();

                    _heroStatusesSave.Add(new HeroStatusContainer(kv.Key.StringId, diseaseEntries, injuryEntries, kv.Value.LastRecoveryDay));
                }
            }

            dataStore.SyncData("IAD_settlementStatusesSave", ref _settlementStatusesSave);

            if (dataStore.IsLoading)
            {
                // loading
                _settlementStatuses.Clear();

                foreach (var entry in _settlementStatusesSave)
                {
                    // reconstruct disease objects from saved DTOs + available configs
                    var diseases = new List<ActiveDisease>();
                    foreach (var de in entry.Diseases)
                    {
                        var cfg = InjuriesAndDiseasesEditor.Instance.DiseasesList.FirstOrDefault(c => c.Name == de.ConfigId);
                        if (cfg != null)
                        {
                            // use internal ctor or restore method to set days
                            var active = new ActiveDisease(cfg, de.Days); 
                            diseases.Add(active);
                        }
                    }

                    if (diseases.Count <= 0 && !IsInCooldown(entry.LastRecoveryDay)) continue;

                    var settlement = Settlement.All.FirstOrDefault(h => h.StringId == entry.SettlementId);
                    if (settlement != null)
                    {
                        var status = new SettlementStatus();
                        status.LastRecoveryDay = entry.LastRecoveryDay; // ensure SettlementStatus exposes this
                        status.Diseases.AddRange(diseases);
                        _settlementStatuses[settlement] = status;
                    }
                }
            }
            else
            {
                // saving
                _settlementStatusesSave.Clear();

                foreach (var kv in _settlementStatuses.Where(kv => kv.Key != null && kv.Value != null &&
                      (kv.Value.Diseases.Count > 0 || IsInCooldown(kv.Value.LastRecoveryDay))))
                {
                    var diseaseEntries = kv.Value.Diseases
                        .Select(d => new DiseaseEntry(d.Config.Name, d.DaysSick))
                        .ToList();

                    _settlementStatusesSave.Add(new SettlementStatusContainer(kv.Key.StringId, diseaseEntries, kv.Value.LastRecoveryDay));
                }
            }

        }

        // — Helpers —

        public HeroStatus GetHeroStatus(Hero hero)
        {
            return _heroStatuses.GetOrAdd(hero, _ => new HeroStatus());
        }

        public SettlementStatus GetSettlementStatus(Settlement settlement)
        {

            return _settlementStatuses.GetOrAdd(settlement, _ => new SettlementStatus());
        }

        private bool IsInCooldown(int day)
        {
            if (day <= 0) return false;
            return (int)Math.Floor(CampaignTime.Now.ToDays) - day < InjuriesAndDiseasesGlobalSettings.Instance.ReinfectCooldownDays;
        }

        // A helper to try infecting target from source via Blood, Contact
        public void TryBloodInfect(Hero source, Hero target)
        {
            if (!IsEligibleForChecks(source) || !IsEligibleForChecks(target)) return;
            var srcSt = GetHeroStatus(source);
            var tgtSt = GetHeroStatus(target);

            if (IsInCooldown(tgtSt.LastRecoveryDay)) return;

            float age = target.Age / Campaign.Current.Models.AgeModel.BecomeOldAge - Campaign.Current.Models.AgeModel.HeroComesOfAge;
            var terrain = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(target.GetPosition().AsVec2);
            var weather = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(target.GetPosition().AsVec2);


            foreach (var ad in srcSt.Diseases)
            {
                if (ad.Config.InfectWays != (int)InfectWay.Blood && ad.Config.InfectWays != (int)InfectWay.Contact) continue;
                if (tgtSt.Diseases.Any(d => d.Config.Name == ad.Config.Name))
                    continue;

                float infectChane = ad.Config.InfectChance;

                if (age > 0) infectChane *= age;


                if (ad.Config.RainChance != 1f && (terrain == TerrainType.Water || terrain == TerrainType.River || terrain == TerrainType.Bridge || terrain == TerrainType.Fording || weather == MapWeatherModel.WeatherEvent.LightRain || weather == MapWeatherModel.WeatherEvent.HeavyRain))
                {
                    infectChane *= ad.Config.RainChance;
                }
                if (ad.Config.SnowChance != 1f && (terrain == TerrainType.Snow || weather == MapWeatherModel.WeatherEvent.Snowy || weather == MapWeatherModel.WeatherEvent.Blizzard))
                {
                    infectChane *= ad.Config.SnowChance;
                }
                if (ad.Config.DesertChance != 1f && (terrain == TerrainType.Desert || terrain == TerrainType.Dune))
                {
                    infectChane *= ad.Config.DesertChance;
                }


                if (MBRandom.RandomFloat < infectChane)
                {
                    tgtSt.Diseases.Add(new ActiveDisease(ad.Config, 0));

                    if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && target.Clan != Clan.PlayerClan && target != Hero.MainHero)
                    {
                        var text = new TextObject("{=IAD_HERO_BLOODINFECT}{HERO} contracted {SICK} from {NAME}’s blood!")
                        .SetTextVariable("HERO", target.Name)
                        .SetTextVariable("NAME", source.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                        StringHelpers.SetCharacterProperties("CHARACTER_A", target.CharacterObject, text, true);
                        StringHelpers.SetCharacterProperties("CHARACTER_B", source.CharacterObject, text, true);
                        InformationManager.DisplayMessage(new InformationMessage(text
                        .ToString(), Colors.Gray));
                    }
                    else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan && target.Clan == Clan.PlayerClan && target != Hero.MainHero)
                    {
                        var text = new TextObject("{=IAD_HERO_BLOODINFECT}{HERO} contracted {SICK} from {NAME}’s blood!")
                        .SetTextVariable("HERO", target.Name)
                        .SetTextVariable("NAME", source.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                        StringHelpers.SetCharacterProperties("CHARACTER_A", target.CharacterObject, text, true);
                        StringHelpers.SetCharacterProperties("CHARACTER_B", source.CharacterObject, text, true);
                        InformationManager.DisplayMessage(new InformationMessage(text
                        .ToString(), Colors.Red));
                    }
                    else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayer && target == Hero.MainHero)
                    {
                        var text = new TextObject("{=IAD_PLAYER_BLOODINFECT}You contracted {SICK} from {NAME}’s blood!")
                        .SetTextVariable("NAME", source.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                        StringHelpers.SetCharacterProperties("CHARACTER_A", target.CharacterObject, text, true);
                        StringHelpers.SetCharacterProperties("CHARACTER_B", source.CharacterObject, text, true);
                        InformationManager.DisplayMessage(new InformationMessage(text
                        .ToString(), Colors.Red));
                    }
                    break;
                }
            }
        }

        private void TryContactInfect(Hero source, Hero target)
        {
            if (!IsEligibleForChecks(source) || !IsEligibleForChecks(target)) return;

            var srcSt = GetHeroStatus(source);
            var tgtSt = GetHeroStatus(target);

            if (IsInCooldown(tgtSt.LastRecoveryDay)) return;

            float age = target.Age / Campaign.Current.Models.AgeModel.BecomeOldAge - Campaign.Current.Models.AgeModel.HeroComesOfAge;
            var terrain = Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(target.GetPosition().AsVec2);
            var weather = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(target.GetPosition().AsVec2);

            foreach (var ad in srcSt.Diseases)
            {
                // only diseases that list Contact
                if (ad.Config.InfectWays != (int)InfectWay.Contact)
                    continue;
                if (tgtSt.Diseases.Any(d => d.Config.Name == ad.Config.Name))
                    continue;

                float infectChane = ad.Config.InfectChance;

                if (age > 0) infectChane *= age;

                if (ad.Config.RainChance != 1f && (terrain == TerrainType.Water || terrain == TerrainType.River || terrain == TerrainType.Bridge || terrain == TerrainType.Fording || weather == MapWeatherModel.WeatherEvent.LightRain || weather == MapWeatherModel.WeatherEvent.HeavyRain))
                {
                    infectChane *= ad.Config.RainChance;
                }
                if (ad.Config.SnowChance != 1f && (terrain == TerrainType.Snow || weather == MapWeatherModel.WeatherEvent.Snowy || weather == MapWeatherModel.WeatherEvent.Blizzard))
                {
                    infectChane *= ad.Config.SnowChance;
                }
                if (ad.Config.DesertChance != 1f && (terrain == TerrainType.Desert || terrain == TerrainType.Dune))
                {
                    infectChane *= ad.Config.DesertChance;
                }

                if (MBRandom.RandomFloat < infectChane)
                {
                    tgtSt.Diseases.Add(new ActiveDisease(ad.Config, 0));

                    if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && target.Clan != Clan.PlayerClan && target != Hero.MainHero)
                    {
                        var text = new TextObject("{=IAD_HERO_CONTACTINFECT}{HERO} caught {SICK} from contact with {NAME}!")
                        .SetTextVariable("HERO", target.Name)
                        .SetTextVariable("NAME", source.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                        StringHelpers.SetCharacterProperties("CHARACTER_A", target.CharacterObject, text, true);
                        StringHelpers.SetCharacterProperties("CHARACTER_B", source.CharacterObject, text, true);
                        InformationManager.DisplayMessage(new InformationMessage(text
                        .ToString(), Colors.Gray));
                    }
                    else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan && target.Clan == Clan.PlayerClan && target != Hero.MainHero)
                    {
                        var text = new TextObject("{=IAD_HERO_CONTACTINFECT}{HERO} caught {SICK} from contact with {NAME}!")
                        .SetTextVariable("HERO", target.Name)
                        .SetTextVariable("NAME", source.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                        StringHelpers.SetCharacterProperties("CHARACTER_A", target.CharacterObject, text, true);
                        StringHelpers.SetCharacterProperties("CHARACTER_B", source.CharacterObject, text, true);
                        InformationManager.DisplayMessage(new InformationMessage(text
                        .ToString(), Colors.Red));
                    }
                    else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayer && target == Hero.MainHero)
                    {
                        var text = new TextObject("{=IAD_PLAYER_CONTACTINFECT}You caught {SICK} from contact with {NAME}!")
                        .SetTextVariable("NAME", source.Name)
                        .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                        StringHelpers.SetCharacterProperties("CHARACTER_A", target.CharacterObject, text, true);
                        StringHelpers.SetCharacterProperties("CHARACTER_B", source.CharacterObject, text, true);
                        InformationManager.DisplayMessage(new InformationMessage(text
                        .ToString(), Colors.Red));
                    }
                    break;
                }
            }
        }

        //Purge

        public void InjectPurgeOption(CampaignGameStarter starter)
        {
            // hook into all three world‐map settlement menus
            foreach (var parentMenu in new[] { "town", "village", "castle" })
            {
                // add the “Purge Infected” entry at a high order so it sits near the bottom
                starter.AddGameMenuOption(
                    parentMenu,
                    $"iad_purge_infected_{parentMenu}",
                    "{=IAD_PurgeInfected}Purge Infected Citizens",
                    // show only if this settlement is tracked and has diseases
                    args =>
                    {
                        var settlement = Settlement.CurrentSettlement;
                        var st = GetSettlementStatus(settlement);
                        bool hasDisease = st.Diseases.Any();
                        if (settlement.OwnerClan != Clan.PlayerClan) hasDisease = false;

                        args.optionLeaveType = GameMenuOption.LeaveType.Raid;

                        return hasDisease;
                    },
                    args =>
                    {
                        PurgeSettlementInfected(Settlement.CurrentSettlement);
                        // Notify
                        var text = new TextObject("{=IAD_PurgeDone}You purged {FIEF}. All diseases removed, but stats were halved.")
                              .SetTextVariable("FIEF", Settlement.CurrentSettlement.Name);
                        StringHelpers.SetCharacterProperties("CHARACTER", Hero.MainHero.CharacterObject, text, true);
                        InformationManager.DisplayMessage(
                          new InformationMessage(
                            text
                              .ToString(),
                            Colors.Red
                          )
                        );
                        // reload the menu so the “Purge” option disappears
                        GameMenu.SwitchToMenu(parentMenu);
                    },
                    /*enabled*/ true,
                    /*order*/ 900,
                    /*icon*/ false,
                    null
                );
            }
        }

        // Does the actual half‑stat reduction + disease clearance.
        private void PurgeSettlementInfected(Settlement settlement)
        {
            if (settlement == null || (!settlement.IsTown && !settlement.IsCastle && !settlement.IsVillage) ) return;

            var st = GetSettlementStatus(settlement);
            if (!st.Diseases.Any()) return;


            // Helper to halve (integer division) and assign back
            float Half(float x) => x / 2;

            // Militia
            settlement.Militia = Half(settlement.Militia);
            

            // Town and Castle
            if (settlement.IsTown || settlement.IsCastle)
            {
                settlement.Town.Loyalty = Half(settlement.Town.Loyalty);
                settlement.Town.Prosperity = Half(settlement.Town.Prosperity);
                settlement.Town.Security = Half(settlement.Town.Security);
                settlement.Town.FoodStocks = Half(settlement.Town.FoodStocks);
            }
            // Village
            else if (settlement.IsVillage)
            {
                settlement.Village.Hearth = Half(settlement.Village.Hearth);
            }

            // Clear all your tracked diseases
            st.Diseases.Clear();
            st.LastRecoveryDay = (int)Math.Floor(CampaignTime.Now.ToDays); //save last recovery day


            if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && settlement.OwnerClan != Clan.PlayerClan)
            {
                var text = new TextObject("{=IAD_SETTLEMENT_PURGE_NPC}{HERO} purged {NAME}!")
                .SetTextVariable("HERO", settlement.Owner.Name)
                .SetTextVariable("NAME", settlement.Name);
                StringHelpers.SetCharacterProperties("CHARACTER", settlement.Owner.CharacterObject, text, true);
                InformationManager.DisplayMessage(new InformationMessage(text
                .ToString(), Colors.Gray));
            }
        }

        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (!InjuriesAndDiseasesGlobalSettings.Instance.AllowDisease || settlement == null || (!settlement.IsTown && !settlement.IsCastle && !settlement.IsVillage)) return;
            //First infections
            // 2) Build a single list of all the heroes present in settlement
            var heroes = new List<Hero>();

            heroes.AddRange(
                settlement.HeroesWithoutParty.Where(h=> h != null && h.IsAlive && IsEligibleForChecks(h)));

            //   b) In-settlement parties (garrison, militia, visitors, etc.)
            foreach (var party in settlement.Parties)
            {
                heroes.AddRange(party.MemberRoster
                    .GetTroopRoster()
                    .Where(e => e.Character.IsHero)
                    .Select(e => e.Character.HeroObject)
                    .Where(h => h != null && h.IsAlive && IsEligibleForChecks(h)));
            }

            heroes = heroes.Distinct().ToList();

            SettlementHeroInfect(settlement, heroes);

            if (settlement.IsTown && (int)CampaignTime.Now.ToDays % InjuriesAndDiseasesGlobalSettings.Instance.DoctorDays == 0)
            {
                foreach (var hero in heroes)
                {
                    NPCVisitDoctor(hero, settlement);
                }
            }


            var st = GetSettlementStatus(settlement);
            if (st.Diseases.Any())
            {
                if (settlement.IsFortification)
                {
                    if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Garrison checking for {settlement.Name}"));
                    //Now garrison changes
                    var troopRosterGarrison = settlement.Town.GarrisonParty.MemberRoster;
                    var troopRosterMilitia = settlement.MilitiaPartyComponent.MobileParty.MemberRoster;
                    int totalhealthyBeforeMilitia = troopRosterMilitia.TotalRegulars - troopRosterMilitia.TotalWoundedRegulars;
                    int totalhealthyBeforeGarrison = troopRosterGarrison.TotalRegulars - troopRosterGarrison.TotalWoundedRegulars;
                    if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Troop amount for {settlement.Name}: {troopRosterGarrison.TotalRegulars} and wounded: {troopRosterGarrison.TotalWoundedRegulars}, Militia: {troopRosterMilitia.TotalRegulars} and wounded: {troopRosterMilitia.TotalWoundedRegulars}"));

                    // 1) Calculate total wounds to apply this tick (sum of all diseases effects)
                    int woundsToApplyMilitia = 0;
                    int woundsToApplyGarrison = 0;
                    var diseasesWounded = new System.Text.StringBuilder();
                    foreach (var ad in st.Diseases)
                    {
                        if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Disease = {ad.Config.Name}, it's garrison minus: {ad.Config.MinusGarrisonFlat}"));
                        bool added = false;
                        if (ad.Config.MinusGarrisonFlat > 0)
                        {
                            woundsToApplyGarrison += ad.Config.MinusGarrisonFlat;
                            if (!added)
                            {
                                added = true;
                                if (diseasesWounded.Length > 0) diseasesWounded.Append(", ");
                                diseasesWounded.Append(new TextObject(ad.Config.ToString()).ToString());

                            }
                        }
                        if (ad.Config.MinusGarrisonMultiplier > 0)
                        {
                            woundsToApplyGarrison += (int)(ad.Config.MinusGarrisonMultiplier * totalhealthyBeforeGarrison);
                            if (!added)
                            {
                                added = true;
                                if (diseasesWounded.Length > 0) diseasesWounded.Append(", ");
                                diseasesWounded.Append(new TextObject(ad.Config.ToString()).ToString());
                            }
                        }
                        if (ad.Config.MinusMilitiaFlat > 0)
                        {
                            woundsToApplyMilitia += ad.Config.MinusMilitiaFlat;
                            if (!added)
                            {
                                added = true;
                                if (diseasesWounded.Length > 0) diseasesWounded.Append(", ");
                                diseasesWounded.Append(new TextObject(ad.Config.ToString()).ToString());

                            }
                        }
                        if (ad.Config.MinusMilitiaMultiplier > 0)
                        {
                            woundsToApplyMilitia += (int)(ad.Config.MinusMilitiaMultiplier * totalhealthyBeforeMilitia);
                            if (!added)
                            {
                                added = true;
                                if (diseasesWounded.Length > 0) diseasesWounded.Append(", ");
                                diseasesWounded.Append(new TextObject(ad.Config.ToString()).ToString());
                            }
                        }
                    }
                    //building
                    if (settlement.IsFortification && InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus != 0 &&
                        (woundsToApplyGarrison != 0 || woundsToApplyMilitia != 0))
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
                                    if (b.Name.Contains(p) )
                                    {
                                        woundsToApplyGarrison -= (int)(woundsToApplyGarrison * raw);
                                        woundsToApplyMilitia -= (int)(woundsToApplyMilitia * raw);

                                    }
                                }
                                foreach (var n in negative)
                                {
                                    if (b.Name.Contains(n) )
                                    {
                                        woundsToApplyGarrison += (int)(woundsToApplyGarrison * raw);
                                        woundsToApplyMilitia += (int)(woundsToApplyMilitia * raw);
                                    }
                                }

                            }

                        }

                    }
                    woundsToApplyMilitia = Math.Min(woundsToApplyMilitia, totalhealthyBeforeMilitia);
                    woundsToApplyGarrison = Math.Min(woundsToApplyGarrison, totalhealthyBeforeGarrison);
                    woundsToApplyMilitia = Math.Max(woundsToApplyMilitia, 0);
                    woundsToApplyGarrison = Math.Max(woundsToApplyGarrison, 0);
                    if (woundsToApplyGarrison > 0 || woundsToApplyMilitia > 0)
                    {
                        if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Applying wounds for {settlement.Name}"));
                        if(woundsToApplyGarrison > 0)
                        troopRosterGarrison.WoundNumberOfTroopsRandomly(woundsToApplyGarrison);
                        if (woundsToApplyMilitia > 0)
                            troopRosterMilitia.WoundNumberOfTroopsRandomly(woundsToApplyMilitia);

                        InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=IAD_TROOP_DISEASE_WOUND}{SETTLEMENT}: {WOUND} troops wounded due to {SICK}.")
                            .SetTextVariable("SETTLEMENT", settlement.Name)
                            .SetTextVariable("WOUND", Math.Max(woundsToApplyMilitia + woundsToApplyGarrison, 0) )
                            .SetTextVariable("SICK", diseasesWounded.ToString())
                            .ToString()
                            ,
                            Colors.Red));
                    }
                    float combinedDeathChance = 0f;
                    var diseasesDead = new System.Text.StringBuilder();
                    foreach (var ad in st.Diseases)
                    {
                        if (ad.Config.DeathChance <= 0f) continue;
                        // combined P = 1 - ∏(1 - p_i)
                        combinedDeathChance = 1f - (1f - combinedDeathChance) * (1f - ad.Config.DeathChance);
                        if (diseasesDead.Length > 0) diseasesDead.Append(", ");
                        diseasesDead.Append(new TextObject(ad.Config.ToString()).ToString());
                    }
                    if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"{settlement.Name} death chance: {combinedDeathChance}"));
                    if (combinedDeathChance > 0f)
                    {
                        int totalDeaths = 0;
                        var woundedGarrison = troopRosterGarrison.GetTroopRoster().Where(t => t.Character.IsSoldier && t.WoundedNumber > 0).ToList();
                        var woundedMilitia = troopRosterMilitia.GetTroopRoster().Where(t => t.Character.IsSoldier && t.WoundedNumber > 0).ToList();
                        foreach (var w in woundedGarrison)
                        {
                            if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Checking death for {settlement.Name}"));

                            int woundedCount = w.WoundedNumber;
                            if (woundedCount <= 0) continue;

                            // fast binomial approx: expected deaths = woundedCount * p, add some randomness
                            int deaths = (int)Math.Floor(woundedCount * combinedDeathChance + MBRandom.RandomFloat);

                            deaths = Math.Min(deaths, woundedCount);
                            if (deaths <= 0) continue;

                            // remove dead from wounded
                            // Use same RemoveTroop signature you used elsewhere; example:
                            troopRosterGarrison.RemoveTroop(w.Character, deaths);
                            totalDeaths += deaths;
                        }
                        foreach (var w in woundedMilitia)
                        {
                            if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Checking death for {settlement.Name}"));

                            int woundedCount = w.WoundedNumber;
                            if (woundedCount <= 0) continue;

                            // fast binomial approx: expected deaths = woundedCount * p, add some randomness
                            int deaths = (int)Math.Floor(woundedCount * combinedDeathChance + MBRandom.RandomFloat);
                            deaths = Math.Min(deaths, woundedCount);
                            if (deaths <= 0) continue;

                            // remove dead from wounded
                            // Use same RemoveTroop signature you used elsewhere; example:
                            troopRosterMilitia.RemoveTroop(w.Character, deaths);
                            totalDeaths += deaths;
                        }

                        if (totalDeaths > 0)
                        {
                            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=IAD_TROOP_DISEASE_KILL}{SETTLEMENT}: {DEATH} troops died due to {SICK}.")
                                .SetTextVariable("SETTLEMENT", settlement.Name)
                                .SetTextVariable("DEATH", totalDeaths)
                                .SetTextVariable("SICK", diseasesDead.ToString())
                                .ToString()
                                ,
                                Colors.Red));
                        }

                    }
                }
                    


                // Now let non‑player clans purge:
                if (settlement.OwnerClan != Clan.PlayerClan && settlement.OwnerClan.Leader != null)
                {
                        float purgeRoll = MBRandom.RandomFloat;
                        float purgeChance = GetPurgeChance(st, settlement);
                        if (purgeRoll < purgeChance)
                        {
                            PurgeSettlementInfected(settlement);
                        }
                    
                }
            }


        }

        private void OnMapEventEndedSiege(MapEvent mapEvent)
        {
            var s = InjuriesAndDiseasesGlobalSettings.Instance;
            if (s == null || s.GetBodiesChance <= 0) return;

            if ((mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside) && mapEvent.WinningSide == BattleSideEnum.Attacker && mapEvent.MapEventSettlement != null)
            {
                
                var besiegerParty = mapEvent.AttackerSide.LeaderParty.MobileParty;
                if (besiegerParty == null || s == null) return;
                var settlement = mapEvent.MapEventSettlement;
                if (settlement.Town == null) return;
                var cost = (int)(MBRandom.RandomFloatRanged(100f, settlement.Town.Gold / 2f) * s.BodiesCostMult);
                var loyal = -s.BodiesLoyalty;
                var morale = -s.BodiesMorale;
                var st = GetSettlementStatus(settlement);
                if (besiegerParty == MobileParty.MainParty && MBRandom.RandomFloat < s.GetBodiesChance)
                    {
                        // 1) Build your options
                        var options = new List<InquiryElement>
                        {
                            new InquiryElement("NONE",   new TextObject("{=IAD_BODIES_NOTHING}Do nothing").ToString(),       null, true, 
                                new TextObject("{=IAD_BODIES_NOTHING_HINT}Chance for getting disease by settlement").ToString()),
                            new InquiryElement("CLEAN",  new TextObject("{=IAD_BODIES_CLEAN}Clean bodies yourself").ToString(), null, true,
                                new TextObject("{=IAD_BODIES_CLEAN_HINT}Chance of getting sick by heroes in your party, {MORALE} party morale, chance of cleaning settlement diseases")
                                .SetTextVariable("MORALE", morale)
                                .ToString()),
                            new InquiryElement("HIRE",   new TextObject("{=IAD_BODIES_HIRE}Hire locals to clear").ToString(),   null, true,
                                new TextObject("{=IAD_BODIES_HIRE_HINT}Cost {COST} denars, {LOYALTY} settlement loyalty, chance of cleaning settlement diseases")
                                .SetTextVariable("COST", cost)
                                .SetTextVariable("LOYALTY", loyal)
                                .ToString())
                        };

                        // 2) Show the multi-choice dialog
                        var data = new MultiSelectionInquiryData(
                            new TextObject("{=IAD_BODIES_TITLE}Siege Aftermath: Clean Up?").ToString(),
                            new TextObject("{=IAD_BODIES_HINT}Your troops have left bodies littered around the walls. What do you do?").ToString(),
                            options,
                            false,
                            1,
                            1,
                            GameTexts.FindText("str_ok").ToString(),
                            null,

                            // OK button
                            list =>
                            {
                                var choice = (string)list[0].Identifier;
                                switch (choice)
                                {
                                    case "CLEAN":
                                        // -5 recent events morale on main party
                                        besiegerParty.RecentEventsMorale += morale;
                                        var heroes = besiegerParty.MemberRoster.GetTroopRoster().Where(h => h.Character.IsHero).Select(h => h.Character.HeroObject).ToList();
                                        InformationManager.DisplayMessage(
                                            new InformationMessage(new TextObject("{=IAD_BODIES_CLEAN_INFO}Your party cleaned up. Morale drops.").ToString(), Colors.Yellow));
                                        RunSicknessChecksHero(heroes);
                                        if (st != null && st.Diseases.Any())
                                        {
                                            // Clear all your tracked diseases
                                            st.Diseases.Clear();
                                            st.LastRecoveryDay = (int)Math.Floor(CampaignTime.Now.ToDays); //save last recovery day
                                        }
                                        break;

                                    case "HIRE":
                                        // cost money
                                        GiveGoldAction.ApplyForPartyToSettlement(
                                            besiegerParty.Party,
                                            settlement,
                                            cost, false);
                                        // -3 loyalty on the captured settlement
                                        settlement.Town.Loyalty += loyal;
                                        InformationManager.DisplayMessage(
                                            new InformationMessage(new TextObject("{=IAD_BODIES_HIRE_INFO}Hired locals cleared the bodies. Settlement loyalty falls.").ToString(), Colors.Yellow));
                                        if (st != null && st.Diseases.Any())
                                        {
                                            // Clear all your tracked diseases
                                            st.Diseases.Clear();
                                            st.LastRecoveryDay = (int)Math.Floor(CampaignTime.Now.ToDays); //save last recovery day
                                        }
                                        break;

                                    default: // "NONE"
                                        var settlements = new List<Settlement> { settlement };
                                        InformationManager.DisplayMessage(
                                            new InformationMessage(new TextObject("{=IAD_BODIES_NOTHING_INFO}Nothing was done. The mess remains.").ToString(), Colors.Yellow));
                                        RunSicknessChecksSettlement(settlements);
                                        break;
                                }
                            },
                            // Skip button
                            _ =>
                            {

                            }
                        );

                        MBInformationManager.ShowMultiSelectionInquiry(data);
                }
                else if(mapEvent.MapEventSettlement.SiegeEvent != null && MBRandom.RandomFloat < s.GetBodiesChance && besiegerParty.ActualClan != null && besiegerParty.ActualClan.Leader != null)
                {
                    //AI
                    if (besiegerParty.LeaderHero == null) return;
                    var clean = MBRandom.RandomFloat;
                    var hire = MBRandom.RandomFloat;
                    var cbonus = 0f;
                    var hbonus = 0f;
                    cbonus += clean * besiegerParty.LeaderHero.GetTraitLevel(DefaultTraits.Generosity);
                    cbonus += clean * besiegerParty.LeaderHero.GetTraitLevel(DefaultTraits.Mercy);
                    hbonus += clean * besiegerParty.LeaderHero.GetTraitLevel(DefaultTraits.Honor);
                    hbonus += clean * besiegerParty.LeaderHero.GetTraitLevel(DefaultTraits.Calculating);
                    clean += cbonus;
                    hire += hbonus;
                    var roll = MBRandom.RandomFloat * (clean + hire);

                    if(roll < clean)
                    {
                        if(s.Debug) InformationManager.DisplayMessage(
                                            new InformationMessage(new TextObject($"Went to cleaning for {besiegerParty.LeaderHero.Name}").ToString(), Colors.Yellow));
                        besiegerParty.RecentEventsMorale += morale;
                        var heroes = besiegerParty.MemberRoster.GetTroopRoster().Where(h => h.Character.IsHero).Select(h => h.Character.HeroObject).ToList();
                        RunSicknessChecksHero(heroes);
                        if (st != null && st.Diseases.Any())
                        {
                            // Clear all your tracked diseases
                            st.Diseases.Clear();
                            st.LastRecoveryDay = (int)Math.Floor(CampaignTime.Now.ToDays); //save last recovery day
                        }
                    }
                    else if (roll < clean + hire && besiegerParty.ActualClan.Gold > cost)
                    {
                        if (s.Debug) InformationManager.DisplayMessage(
                                            new InformationMessage(new TextObject($"Went to hiring for {besiegerParty.LeaderHero.Name}").ToString(), Colors.Yellow));
                        // cost money
                        GiveGoldAction.ApplyForCharacterToSettlement(
                            besiegerParty.ActualClan.Leader,
                            settlement,
                            cost, false);
                        // -3 loyalty on the captured settlement
                        settlement.Town.Loyalty += loyal;
                        if (st != null && st.Diseases.Any())
                        {
                            // Clear all your tracked diseases
                            st.Diseases.Clear();
                            st.LastRecoveryDay = (int)Math.Floor(CampaignTime.Now.ToDays); //save last recovery day
                        }
                    } 
                    else
                    {
                        if (s.Debug) InformationManager.DisplayMessage(
                                            new InformationMessage(new TextObject($"Went to nothing for {besiegerParty.LeaderHero.Name}").ToString(), Colors.Yellow));
                        var settlements = new List<Settlement> { settlement };
                        RunSicknessChecksSettlement(settlements);
                    }

                }


            }

        }


        public void NPCVisitDoctor(Hero hero, Settlement settlement)
        {
            var s = InjuriesAndDiseasesGlobalSettings.Instance;
            if (s == null || !s.NPCDoctor || hero == null || hero.IsHumanPlayerCharacter || settlement == null || !settlement.IsTown) return;

            if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Doctor visit for: {hero.Name}"));

            var st = GetHeroStatus(hero);
            if(st != null && st.Diseases.Any())
            {

                if (st.Diseases.Any(d=> d.DaysSick < d.Config.MinDays)) return;

                var chance = s.NPCDoctorChance;

                float diseasesAmount = st.Diseases.Count;
                if (chance > 0 && diseasesAmount > 1) chance *= 1f + diseasesAmount / 10f;

                var diseaseForCheck = st.Diseases.OrderByDescending(d => d.Config.DeathChance).FirstOrDefault();
                var death = 1f + diseaseForCheck.Config.DeathChance;
                if (chance > 0 && death > 0) chance *= death;

                float age = hero.Age / Campaign.Current.Models.AgeModel.BecomeOldAge - Campaign.Current.Models.AgeModel.HeroComesOfAge;
                if (chance > 0 && age > 0) chance /= age;


                if (chance <= MBRandom.RandomFloat) return;


                for (int i = st.Diseases.Count - 1; i >= 0; i--)
                {
                    var ad = st.Diseases[i];
                    var medSkill = hero.GetSkillValue(DefaultSkills.Medicine);
                    if (ad.TryHeal(medSkill, s.DoctorBonus))
                    {

                        // ← record the day they recovered:
                        st.LastRecoveryDay = (int)Math.Floor(CampaignTime.Now.ToDays);
                        st.Diseases.Remove(ad);

                        if (s.Inform && hero.Clan != Clan.PlayerClan && hero != Hero.MainHero)
                        {
                            var text = new TextObject("{=IAD_HERO_RECOVERY}{HERO} recovered from {SICK}!")
                            .SetTextVariable("HERO", hero.Name)
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                            .ToString(), Colors.Gray));
                        }
                        else if (s.InformPlayerClan && hero.Clan == Clan.PlayerClan && hero != Hero.MainHero)
                        {
                            var text = new TextObject("{=IAD_HERO_RECOVERY}{HERO} recovered from {SICK}!")
                            .SetTextVariable("HERO", hero.Name)
                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                            StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                            InformationManager.DisplayMessage(new InformationMessage(text
                            .ToString(), Colors.Green));
                        }
                    }
                }


            }
        }


        private void SettlementHeroInfect(Settlement settlement, List<Hero> heroes)
        {
            if (heroes.Count > 0)
            {
                var settSt = GetSettlementStatus(settlement);

                var terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(settlement.CurrentNavigationFace);
                var weather = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(settlement.Position2D);

                if (!IsInCooldown(settSt.LastRecoveryDay))
                {
                    // 3) HERO → SETTLEMENT
                    foreach (var hero in heroes)
                    {
                        var heroSt = GetHeroStatus(hero);
                        foreach (var ad in heroSt.Diseases)
                        {
                            if (ad.Config.InfectWays != (int)InfectWay.Contact)
                                continue;
                            if (settSt.Diseases.Any(d => d.Config.Name == ad.Config.Name))
                                continue; // already has it

                            var infectChance = ad.Config.InfectChance;
                            //building
                            var bonusBuilding = 0f;
                            if (settlement.IsFortification && InjuriesAndDiseasesGlobalSettings.Instance.BuildingBonus != 0 && infectChance != 1f && infectChance != 0)
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
                                                bonusBuilding += infectChance * raw;
                                            }
                                        }
                                        foreach (var n in negative)
                                        {
                                            if (b.Name.Contains(n))
                                            {
                                                bonusBuilding -= infectChance * raw;
                                            }
                                        }

                                    }

                                }
                                infectChance -= bonusBuilding;

                            }

                            infectChance = MathF.Clamp(infectChance, 0f, 1f);

                            if (ad.Config.RainChance != 1f && (terrain == TerrainType.Water || terrain == TerrainType.River || terrain == TerrainType.Bridge || terrain == TerrainType.Fording || weather == MapWeatherModel.WeatherEvent.LightRain || weather == MapWeatherModel.WeatherEvent.HeavyRain))
                            {
                                infectChance *= ad.Config.RainChance;
                            }
                            if (ad.Config.SnowChance != 1f && (terrain == TerrainType.Snow || weather == MapWeatherModel.WeatherEvent.Snowy || weather == MapWeatherModel.WeatherEvent.Blizzard))
                            {
                                infectChance *= ad.Config.SnowChance;
                            }
                            if (ad.Config.DesertChance != 1f && (terrain == TerrainType.Desert || terrain == TerrainType.Dune))
                            {
                                infectChance *= ad.Config.DesertChance;
                            }

                            if (MBRandom.RandomFloat < (float)infectChance)
                            {
                                settSt.Diseases.Add(new ActiveDisease(ad.Config, 0));
                                // optional: notify
                                if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan
                                    && settlement.OwnerClan == Clan.PlayerClan)
                                {
                                    var text = new TextObject("{=IAD_HERO_INFECT_SETTLEMENT}{NAME} was infected by {HERO} with {SICK}!")
                                                .SetTextVariable("NAME", settlement.Name)
                                                .SetTextVariable("HERO", hero.Name)
                                                .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                                    StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                                    InformationManager.DisplayMessage(
                                        new InformationMessage(
                                            text
                                                .ToString(),
                                            Colors.Red
                                        )
                                    );
                                }
                                else if (InjuriesAndDiseasesGlobalSettings.Instance.Inform && settlement.OwnerClan != Clan.PlayerClan)
                                {
                                    var text = new TextObject("{=IAD_HERO_INFECT_SETTLEMENT}{NAME} was infected by {HERO} with {SICK}!")
                                                .SetTextVariable("NAME", settlement.Name)
                                                .SetTextVariable("HERO", hero.Name)
                                                .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                                    StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                                    InformationManager.DisplayMessage(
                                        new InformationMessage(
                                            text
                                                .ToString(),
                                            Colors.Gray
                                        )
                                    );
                                }
                                break;
                            }
                        }
                    }
                }

                // 4) SETTLEMENT → HERO
                foreach (var hero in heroes)
                {
                    var age = hero.Age / Campaign.Current.Models.AgeModel.BecomeOldAge - Campaign.Current.Models.AgeModel.HeroComesOfAge;

                    var heroSt = GetHeroStatus(hero);

                    if (IsInCooldown(heroSt.LastRecoveryDay)) continue;

                    foreach (var ad in settSt.Diseases)
                    {
                        if (ad.Config.InfectWays != (int)InfectWay.Contact)
                            continue;
                        if (heroSt.Diseases.Any(d => d.Config.Name == ad.Config.Name))
                            continue; // already has it

                        float infectChance = ad.Config.InfectChance;
                        if (infectChance > 0 && age > 0) infectChance *= age;


                        if (ad.Config.RainChance != 1f && (terrain == TerrainType.Water || terrain == TerrainType.River || terrain == TerrainType.Bridge || terrain == TerrainType.Fording || weather == MapWeatherModel.WeatherEvent.LightRain || weather == MapWeatherModel.WeatherEvent.HeavyRain))
                        {
                            infectChance *= ad.Config.RainChance;
                        }
                        if (ad.Config.SnowChance != 1f && (terrain == TerrainType.Snow || weather == MapWeatherModel.WeatherEvent.Snowy || weather == MapWeatherModel.WeatherEvent.Blizzard))
                        {
                            infectChance *= ad.Config.SnowChance;
                        }
                        if (ad.Config.DesertChance != 1f && (terrain == TerrainType.Desert || terrain == TerrainType.Dune))
                        {
                            infectChance *= ad.Config.DesertChance;
                        }

                        if (MBRandom.RandomFloat < infectChance)
                        {
                            heroSt.Diseases.Add(new ActiveDisease(ad.Config, 0));
                            // optional: notify
                            if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayerClan
                                && hero.Clan == Clan.PlayerClan && hero != Hero.MainHero)
                            {
                                var text = new TextObject("{=IAD_SETTLEMENT_INFECT_HERO}{HERO} caught {SICK} at {NAME}!")
                                            .SetTextVariable("NAME", settlement.Name)
                                            .SetTextVariable("HERO", hero.Name)
                                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                                StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                                InformationManager.DisplayMessage(
                                    new InformationMessage(
                                        text
                                            .ToString(),
                                        Colors.Red
                                    )
                                );
                            }
                            else if (InjuriesAndDiseasesGlobalSettings.Instance.Inform
                                && hero.Clan != Clan.PlayerClan && hero != Hero.MainHero)
                            {
                                var text = new TextObject("{=IAD_SETTLEMENT_INFECT_HERO}{HERO} caught {SICK} at {NAME}!")
                                            .SetTextVariable("NAME", settlement.Name)
                                            .SetTextVariable("HERO", hero.Name)
                                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                                StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                                InformationManager.DisplayMessage(
                                    new InformationMessage(
                                        text
                                            .ToString(),
                                        Colors.Gray
                                    )
                                );
                            }
                            else if (InjuriesAndDiseasesGlobalSettings.Instance.InformPlayer
                                && hero == Hero.MainHero)
                            {
                                var text = new TextObject("{=IAD_SETTLEMENT_INFECT_PLAYER}You caught {SICK} at {NAME}!")
                                            .SetTextVariable("NAME", settlement.Name)
                                            .SetTextVariable("SICK", new TextObject("{=IAD_" + ad.Config.Name + "}" + ad.Config.Name).ToString());
                                StringHelpers.SetCharacterProperties("CHARACTER", hero.CharacterObject, text, true);
                                InformationManager.DisplayMessage(
                                    new InformationMessage(
                                        text
                                            .ToString(),
                                        Colors.Red
                                    )
                                );
                            }
                            break;
                        }
                    }
                }

            }
        }

        private float GetPurgeChance(SettlementStatus st, Settlement settlement)
        {
            // Sum up the longest‑lasting disease
            int days = st.Diseases.Any()
              ? st.Diseases.Max(d => d.DaysSick)
              : 0;

            float baseRate = (float)InjuriesAndDiseasesGlobalSettings.Instance.GetPurgeChance;
            var bonus = -settlement.OwnerClan?.Leader.GetTraitLevel(DefaultTraits.Mercy) * baseRate;
            bonus += -settlement.OwnerClan?.Leader.GetTraitLevel(DefaultTraits.Generosity) * baseRate;
            bonus += settlement.OwnerClan?.Leader.GetTraitLevel(DefaultTraits.Calculating) * baseRate;
            bonus += settlement.OwnerClan?.Leader.GetTraitLevel(DefaultTraits.Authoritarian) * baseRate;
            bonus += -settlement.OwnerClan?.Leader.GetTraitLevel(DefaultTraits.Egalitarian) * baseRate;
            bonus += settlement.OwnerClan?.Leader.GetTraitLevel(DefaultTraits.Oligarchic) * baseRate;
            float chance = baseRate * days;
            return MathF.Clamp((float)(chance + bonus), 0f, 0.5f);
        }

        //End purge

        public void CheckPartyTroopsToInfect(MobileParty party)
        {
            var partyRoster = party.MemberRoster;

            if (partyRoster.TotalRegulars <= 0) return;

            var heroes = partyRoster.GetTroopRoster().Where(h=> h.Character.IsHero).Select(h=> h.Character.HeroObject).ToList();

            

            foreach(var hero in heroes)
            {

                var st = GetHeroStatus(hero);
                if (st.Diseases.Any())
                {
                    partyRoster = party.MemberRoster;

                    if (partyRoster.TotalRegulars <= 0) break;


                        if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Troops checking for {party.Name}"));
                        //Now garrison changes
                        int totalhealthyBefore = partyRoster.TotalRegulars - partyRoster.TotalWoundedRegulars;
                        if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Troop amount for {party.Name}: {partyRoster.TotalRegulars} and wounded: {partyRoster.TotalWoundedRegulars}."));

                        // 1) Calculate total wounds to apply this tick (sum of all diseases effects)
                        int woundsToApply = 0;
                        var diseasesWounded = new System.Text.StringBuilder();
                        foreach (var ad in st.Diseases)
                        {
                            if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Disease = {ad.Config.Name}, it's party troops minus: {ad.Config.MinusPartyTroopsFlat}"));
                            bool added = false;
                            if (ad.Config.MinusPartyTroopsFlat > 0)
                            {
                                woundsToApply += ad.Config.MinusPartyTroopsFlat;
                                if (!added)
                                {
                                    added = true;
                                    if (diseasesWounded.Length > 0) diseasesWounded.Append(", ");
                                    diseasesWounded.Append(new TextObject(ad.Config.ToString()).ToString());

                                }
                            }
                            if (ad.Config.MinusPartyTroopsMultipler > 0)
                            {
                                woundsToApply += (int)(ad.Config.MinusPartyTroopsMultipler * totalhealthyBefore);
                                if (!added)
                                {
                                    added = true;
                                    if (diseasesWounded.Length > 0) diseasesWounded.Append(", ");
                                    diseasesWounded.Append(new TextObject(ad.Config.ToString()).ToString());
                                }
                            }
                        }

                        woundsToApply = Math.Min(woundsToApply, totalhealthyBefore);
                    woundsToApply = Math.Max(woundsToApply, 0);
                        if (woundsToApply > 0)
                        {
                            if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Applying wounds for {party.Name}"));
                            if (woundsToApply > 0)
                                partyRoster.WoundNumberOfTroopsRandomly(woundsToApply);

                            if(party.IsMainParty)
                            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=IAD_TROOP_DISEASE_WOUND}{SETTLEMENT}: {WOUND} troops wounded due to {SICK}.")
                                .SetTextVariable("SETTLEMENT", party.Name)
                                .SetTextVariable("WOUND", Math.Max(woundsToApply, 0))
                                .SetTextVariable("SICK", diseasesWounded.ToString())
                                .ToString()
                                ,
                                Colors.Red));
                        }
                        float combinedDeathChance = 0f;
                        var diseasesDead = new System.Text.StringBuilder();
                        foreach (var ad in st.Diseases)
                        {
                            if (ad.Config.DeathChance <= 0f) continue;
                            // combined P = 1 - ∏(1 - p_i)
                            combinedDeathChance = 1f - (1f - combinedDeathChance) * (1f - ad.Config.DeathChance);
                            if (diseasesDead.Length > 0) diseasesDead.Append(", ");
                            diseasesDead.Append(new TextObject(ad.Config.ToString()).ToString());
                        }
                        if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"{party.Name} death chance: {combinedDeathChance}"));
                        if (combinedDeathChance > 0f)
                        {
                            int totalDeaths = 0;
                            var woundedTroops = partyRoster.GetTroopRoster().Where(t => !t.Character.IsHero && t.WoundedNumber > 0).ToList();
                            foreach (var w in woundedTroops)
                            {
                                if (InjuriesAndDiseasesGlobalSettings.Instance.Debug) InformationManager.DisplayMessage(new InformationMessage($"Checking death for {party.Name}"));

                                int woundedCount = w.WoundedNumber;
                                if (woundedCount <= 0) continue;

                                // fast binomial approx: expected deaths = woundedCount * p, add some randomness
                                int deaths = (int)Math.Floor(woundedCount * combinedDeathChance + MBRandom.RandomFloat);

                                deaths = Math.Min(deaths, woundedCount);
                                if (deaths <= 0) continue;

                                // remove dead from wounded
                                // Use same RemoveTroop signature you used elsewhere; example:
                                partyRoster.RemoveTroop(w.Character, deaths);
                                totalDeaths += deaths;
                            }

                            if (totalDeaths > 0)
                            {

                            if (party.IsMainParty)
                                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=IAD_TROOP_DISEASE_KILL}{SETTLEMENT}: {DEATH} troops died due to {SICK}.")
                                    .SetTextVariable("SETTLEMENT", party.Name)
                                    .SetTextVariable("DEATH", totalDeaths)
                                    .SetTextVariable("SICK", diseasesDead.ToString())
                                    .ToString()
                                    ,
                                    Colors.Red));
                            }

                        
                    }
                }
            }


        }


        // — Data classes —

        public class HeroStatus
        {
            public List<ActiveDisease> Diseases { get; } = new List<ActiveDisease>();
            public List<ActiveInjury> Injuries { get; } = new List<ActiveInjury>();

            public int LastRecoveryDay { get; set; } = 0;
        }

        public class SettlementStatus
        {
            public List<ActiveDisease> Diseases { get; } = new List<ActiveDisease>();

            public int LastRecoveryDay { get; set; } = 0;
        }

        public class ActiveDisease
        {
            public DiseaseConfig Config { get; }
            public int DaysSick { get; set; }

            public ActiveDisease(DiseaseConfig cfg, int days) 
            { 
                Config = cfg; 
                DaysSick = days;
            }

            public void DayPassed() => DaysSick++;

            public bool TryHeal(int medicineSkill, float DocBonus)
            {
                if (DaysSick < Config.MinDays)
                    return false;
                if (Config.MaxDays > 0 && DaysSick >= Config.MaxDays)
                    return true;

                int daysBeyondMin = DaysSick - Config.MinDays;
                // e.g. DayHealMultiplier=1.1 means +10% per extra day
                float dayheal = 1;
                if(Config.DayHealMultiplier != 0) dayheal = MathF.Pow(Config.DayHealMultiplier, daysBeyondMin);
                float scaledHealChance = Config.HealChance * dayheal;
                
                // medicineSkill: 0–300 → bonus 0.0–1.0
                float medBonus = MathF.Min(0.99f, medicineSkill / 300f);

                // combine and clamp at 100%
                float finalChance = MathF.Min(1.0f, scaledHealChance * (1f + medBonus));

                if (DocBonus > 0) finalChance *= 1f + DocBonus;

                return MBRandom.RandomFloat < (float)finalChance;
            }
        }

        public class ActiveInjury
        {
            public InjuryConfig Config { get; }
            public int DaysInjured { get; set; }

            public ActiveInjury(InjuryConfig cfg, int days)
            {
                Config = cfg;
                DaysInjured = days;
            }

            public void DayPassed() => DaysInjured++;

            public bool TryHeal(int medicineSkill)
            {
                if (DaysInjured < Config.MinDays)
                    return false;
                if (Config.MaxDays > 0 && DaysInjured >= Config.MaxDays)
                    return true;

                int daysBeyondMin = DaysInjured - Config.MinDays;
                double dayheal = 1;
                if (Config.DayHealMultiplier != 0) dayheal = Math.Pow(Config.DayHealMultiplier, daysBeyondMin);
                double scaledHealChance = Config.HealChance * dayheal;

                // medicineSkill: 0–300 → bonus 0.0–1.0
                double medBonus = Math.Min(1.0, medicineSkill / 300.0);

                // combine and clamp at 100%
                double finalChance = Math.Min(1.0, scaledHealChance * (1 + medBonus));

                return MBRandom.RandomFloat < (float)finalChance;
            }
        }

        // heroes we’ve just killed for disease
        public static readonly HashSet<Hero> _diseaseDeaths = new HashSet<Hero>();

    }
}
