using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace InjuriesAndDiseases
{
    public class InjuriesAndDiseasesCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("debug_get_hero_status", "injuriesanddiseases")]
        private static string DebugGetHeroStatus(List<string> args)
        {


            if (args.Count <= 0) return "Hero not specified. Use injuriesanddiseases.debug_get_hero_status HeroNameHere";
            var hero = Hero.AllAliveHeroes
                .FirstOrDefault(h => h.Name.ToString() == args[0]);

            if (hero == null) return "Hero not found";
            if (!hero.IsAlive) return "Hero is dead";
            if (hero.IsChild) return "Hero is child";

            var behavior = Campaign.Current.GetCampaignBehavior<InjuriesAndDiseasesBehavior>();
            if (behavior == null)
                return "InjuriesAndDiseasesBehavior is not loaded yet.";

            var st = behavior.GetHeroStatus(hero);
            if (st == null)
                return $"No status data for {hero.Name}.";

            string clanName = hero.Clan?.Name.ToString() ?? "No Clan";
            string diseaseText = "Healthy";
            if (st.Diseases.Any())
                diseaseText = string.Join(", ", st.Diseases.Select(d => d.Config.Name));
            string injuryText = "Healthy";
            if (st.Injuries.Any())
                diseaseText = string.Join(", ", st.Injuries.Select(d => d.Config.Name));

            return $"Status for {hero.Name} from {clanName}: [Disease: {diseaseText}], [Injuries: {injuryText}], [Recovery Day: {st.LastRecoveryDay}]";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("debug_infect_hero", "injuriesanddiseases")]
        private static string DebugInfectHero(List<string> args)
        {
            // args[0] = hero name, args[1] = disease name
            if (args.Count < 1)
                return "Usage: injuriesanddiseases.debug_infect_hero <HeroName>|<DiseaseName>";

            var raw = string.Join(" ", args);
            var parts = raw.Split('|');

            if (parts.Length != 2)
                return "Usage: injuriesanddiseases.debug_infect_hero <HeroName>|<DiseaseName>";

            var heroName = parts[0].Trim();
            var diseaseName = parts[1].Trim();

            var hero = Hero.AllAliveHeroes
                .FirstOrDefault(h => h.Name.ToString().Equals(heroName, System.StringComparison.OrdinalIgnoreCase));
            if (hero == null) return $"Hero \"{heroName}\" not found or not alive.";

            var behavior = Campaign.Current.GetCampaignBehavior<InjuriesAndDiseasesBehavior>();
            if (behavior == null)
                return "InjuriesAndDiseasesBehavior is not registered yet.";

            // grab the Disease from the editor

            var diseaseCfg = InjuriesAndDiseasesEditor.Instance.DiseasesList
                .FirstOrDefault(d => d.Name.Equals(diseaseName, System.StringComparison.OrdinalIgnoreCase));
            var injuryCfg = InjuriesAndDiseasesEditor.Instance.InjuriesList
                .FirstOrDefault(d => d.Name.Equals(diseaseName, System.StringComparison.OrdinalIgnoreCase));
            bool injury = injuryCfg != null;
            bool disease = diseaseCfg != null;
            if (!injury && !disease)
                return $"Disease \"{diseaseName}\" not found in your config.";

            // Finally get the hero’s status and add a new ActiveDisease
            var status = behavior.GetHeroStatus(hero);
            if (disease)
            {
                var activeDisease = new InjuriesAndDiseasesBehavior.ActiveDisease(diseaseCfg, 0);
                if (status.Diseases.Contains(activeDisease)) return $"Hero already has \"{diseaseName}\".";

                status.Diseases.Add(activeDisease);

                return $"{hero.Name} has been infected with {diseaseCfg.Name}.";
            }
            else if (injury)
            {
                var activeInjury = new InjuriesAndDiseasesBehavior.ActiveInjury(injuryCfg, 0);
                if (status.Injuries.Contains(activeInjury)) return $"Hero already has \"{diseaseName}\".";

                status.Injuries.Add(activeInjury);

                return $"{hero.Name} has got {injuryCfg.Name}.";
            }
            else return "Sth went wrong";

        }
    }
}
