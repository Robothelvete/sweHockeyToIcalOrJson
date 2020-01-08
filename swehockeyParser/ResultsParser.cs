using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SweHockey
{
    public class ResultsParser
    {

        /// <summary>
        /// Gets the results table from stats.swehockey.se
        /// </summary>
        /// <param name="date">Optional: which date to get results from. Leave empty to get todays results</param>
        /// <returns></returns>
        public static string FetchResultsHtml(DateTime? date = null)
        {
            if (!date.HasValue)
            {
                date = DateTime.Now;
            }
            string url = string.Format("http://stats.swehockey.se/GamesByDate/{0}/ByTime/90", date.Value.ToString("yyyy-MM-dd"));
            using (HttpClient client = new HttpClient())
            {
                return client.GetStringAsync(url).Result;
            }
        }


        /// <summary>
        /// Parses the HTML to return the games and results, grouped by league
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        /// <remarks>For a version that handles days in the future (and no results parsing), see <see cref="ScheduleParser.GamesScheduleFromDailyHtml(string)"/></remarks>
        public static List<LeagueGames> GamesResultsFromHtml(string html)
        {
            var results = new List<LeagueGames>();
            var svCulture = System.Globalization.CultureInfo.GetCultureInfo("sv-SE");

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            LeagueGames currentLeage = null;
            string day = ""; //empty string so compiler will STFU

            foreach (var node in doc.DocumentNode.SelectNodes("//table[contains(@class,'tblContent')]/tr"))
            {
                if (node.ChildNodes.Any(n => n.Name == "th"))
                {
                    if(node.ChildNodes.Any(n => n.HasClass("tdTitleRight")))
                    {
                        day = node.ChildNodes.First(n => n.HasClass("tdTitleRight")).InnerText;
                    }
                    continue;
                }
                if(node.ChildNodes.Any(n => n.Attributes["colspan"]?.Value == "5"))
                {
                    if(currentLeage != null) { results.Add(currentLeage); }
                    currentLeage = new LeagueGames(System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td/a").InnerText.Trim()));
                    continue;
                }

                var strDate = day + " " + node.SelectSingleNode("td[1]").InnerText.Trim();
                var resultsNode = node.SelectSingleNode("td[3]/a");
                string url = node.SelectSingleNode("td/a")?.Attributes["href"].Value.Replace("&#xD;&#xA;", "");
                var game = new Game()
                {
                    Tid = DateTime.ParseExact(strDate, "yyyy-MM-dd HH:mm", svCulture),
                    Lag = ParserServices.CleanTeams(node.SelectSingleNode("td[2]").InnerText),
                    Location = System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td[4]").InnerText)
                };
                game.End = game.Tid.AddHours(2); //basically just for compatibility reasons, no one should care if it's inaccurate
                if (!string.IsNullOrEmpty(resultsNode.InnerText))
                {
                    game.Results = resultsNode.InnerText;
                }
                if (!string.IsNullOrEmpty(url))
                {
                    game.Url = ParserServices.CleanURL(url);
                }
                else { game.Url = ""; }
                game.Uid = "swehockey_" + ParserServices.GetGameId(game.Url); //basically just for ical compatibility anyway
                game.Series = currentLeage.League; //redundant, but consistent 


                currentLeage.Games.Add(game);
            }
            if (currentLeage != null) { results.Add(currentLeage); } //add the last league


            return results;
        }
    }
   
}
