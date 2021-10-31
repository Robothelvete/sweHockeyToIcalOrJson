using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;

namespace SweHockey
{

    /// <summary>
    /// Parses HTML from stats.swehockey.se to create schedules
    /// </summary>
    public class ScheduleParser
    {

        /// <summary>
        /// Parse HTML to get a list of all games it contains
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static List<Game> GamesScheduleFromHtml(string html, ParserMode mode = ParserMode.Unknown)
        {
            List<Game> games = new List<Game>();
            var svCulture = System.Globalization.CultureInfo.GetCultureInfo("sv-SE");

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            string lastDate = null; //needed to keep track of dates in SDHL/HA/SHL mode, since the date isn't given in every row

            foreach (var node in doc.DocumentNode.SelectNodes("//table[contains(@class,'tblContent')]/tr"))
            {
                if (node.ChildNodes.Any(n => n.Name == "th"))
                {
                    if (mode == ParserMode.Unknown && node.ChildNodes.Any(n => n.HasClass("tdHeader")))
                    {
                        //Use this to get the layout of the rest of the table
                        if (node.ChildNodes.Last().InnerText == "Group") { mode = ParserMode.HockeyEttan; }
                        else { mode = ParserMode.SHL; }
                    }
                    continue;
                }
                string strDate;
                string teams;
                string location;
                string series = null; //assign null explicitly to tell compiler to STFU
                string url;
                if ((int)mode == 1)
                {
                    strDate = node.SelectSingleNode("td/div[@class='dateLink']/span").InnerText;
                    teams = ParserServices.CleanTeams(node.SelectSingleNode("td[3]").InnerText);
                    location = System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td[7]").InnerText);
                    series = System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td[8]").InnerText);
                    url = node.SelectSingleNode("td/a")?.Attributes["href"].Value.Replace("&#xD;&#xA;", "");
                }
                else if ((int)mode == 2)
                {
                    if (!string.IsNullOrEmpty(node.SelectSingleNode("td[2]").InnerText))
                    {
                        lastDate = node.SelectSingleNode("td[2]/bold").InnerText + " ";
                    }
                    strDate = lastDate + node.SelectSingleNode("td/div[@class='dateLink']/span").InnerText;
                    teams = ParserServices.CleanTeams(node.SelectSingleNode("td[4]").InnerText);
                    location = System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td[8]").InnerText);
                    url = node.SelectSingleNode("td/a")?.Attributes["href"].Value.Replace("&#xD;&#xA;", "");
                }
                else { throw new Exception("Could not figure out how to parse this HTML"); }
                Game game = new Game()
                {
                    Lag = teams,
                    Location = location,
                    Tid = DateTime.ParseExact(strDate, "yyyy-MM-dd HH:mm", svCulture)
                };
                game.End = game.Tid.AddHours(2); //basically just for ical compatibility anyway
                if (!string.IsNullOrEmpty(url))
                {
                    game.Url = ParserServices.CleanURL(url);
                }
                else { game.Url = ""; }
                game.Uid = "swehockey_" + ParserServices.GetGameId(game.Url); //basically just for ical compatibility anyway
                if (!string.IsNullOrEmpty(series))
                {
                    game.Series = series;
                }


                games.Add(game);
            }

            return games;
        }

        /// <summary>
        /// Parses the HTML to return the games in a day, grouped by league
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        /// <remarks>For a version that also parses results after the day is finished, see <see cref="ResultsParser.GamesResultsFromHtml(string)"/></remarks>
        public static List<LeagueGames> GamesScheduleFromDailyHtml(string html)
        {
            var gamesByLeage = new List<LeagueGames>();
            var svCulture = System.Globalization.CultureInfo.GetCultureInfo("sv-SE");

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            LeagueGames currentLeage = null;
            string day = ""; //empty string so compiler will STFU

            foreach (var node in doc.DocumentNode.SelectNodes("//table[contains(@class,'tblContent')]/tr"))
            {
                if (node.ChildNodes.Any(n => n.Name == "th"))
                {
                    if (node.ChildNodes.Any(n => n.HasClass("tdTitleRight")))
                    {
                        day = node.ChildNodes.First(n => n.HasClass("tdTitleRight")).InnerText;
                    }
                    continue;
                }
                if (node.ChildNodes.Any(n => n.Attributes["colspan"]?.Value == "5"))
                {
                    if (currentLeage != null) { gamesByLeage.Add(currentLeage); }
                    currentLeage = new LeagueGames(System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td/a").InnerText.Trim()));
                    continue;
                }

                var strDate = day + " " + node.SelectSingleNode("td[1]").InnerText.Trim();
                string url = node.SelectSingleNode("td/a")?.Attributes["href"].Value.Replace("&#xD;&#xA;", "");
                var game = new Game()
                {
                    Tid = DateTime.ParseExact(strDate, "yyyy-MM-dd HH:mm", svCulture),
                    Lag = ParserServices.CleanTeams(node.SelectSingleNode("td[2]").InnerText),
                    Location = System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td[4]").InnerText)
                };
                game.End = game.Tid.AddHours(2); //basically just for compatibility reasons, no one should care if it's inaccurate
                if (!string.IsNullOrEmpty(url))
                {
                    game.Url = ParserServices.CleanURL(url);
                }
                else { game.Url = ""; }
                game.Uid = "swehockey_" + ParserServices.GetGameId(game.Url); //basically just for ical compatibility anyway
                game.Series = currentLeage.League; //redundant, but consistent 


                currentLeage.Games.Add(game);
            }

            if (currentLeage != null) { gamesByLeage.Add(currentLeage); } //add the last league


            return gamesByLeage;
        }

        /// <summary>
        /// Fetch the HTML from swehockey 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string FetchScheduleHtml(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                return client.GetStringAsync(url).Result;
            }
        }

        /// <summary>
        /// Fetch the schedule HTML for a specific day
        /// </summary>
        /// <param name="date">Optional: which date to get the schedule from. Leave empty to get todays results</param>
        /// <returns></returns>
        public static string FetchScheduleHtml(DateTime? date = null)
        {
            if (!date.HasValue)
            {
                date = DateTime.Now;
            }
            string url = string.Format("https://stats.swehockey.se/GamesByDate/{0}/ByTime/90", date.Value.ToString("yyyy-MM-dd"));
            return FetchScheduleHtml(url);
        }

        public static string ParseHtmlToJSON(string html)
        {
            var games = GamesScheduleFromHtml(html);
            //TODO: serialize json
            throw new NotImplementedException("TODO");
        }

        public static string ParseHtmlToIcal(string html)
        {
            var games = GamesScheduleFromHtml(html);
            foreach (Game game in games)
            {
                //TODO: write ical stuff
            }
            throw new NotImplementedException("TODO");
        }

    }

    /// <summary>
    /// Determines which parsing algorithm to use, since not all leagues' formats are exactly the same
    /// </summary>
    public enum ParserMode
    {
        Unknown = 0,
        HockeyEttan = 1,
        SHL = 2,
        HA = 2,
        SDHL = 2
    }
}
