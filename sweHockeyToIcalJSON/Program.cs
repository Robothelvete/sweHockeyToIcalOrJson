using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using HtmlAgilityPack;

namespace sweHockeyToIcalJSON
{
    class Program
    {
        public class Options
        {
            [Option('i', "inputHtml", Required = false, HelpText = "Input filepath to HTML to parse")]
            public string inputHtml { get; set; }

            [Option('u', "url", Required = false, HelpText = "URL from where to fetch HTML to parse", Default = "http://stats.swehockey.se/ScheduleAndResults/Schedule/10344")] //Default is hockeyettan 19/20
            public string inputURL { get; set; }

            [Option('j', "json", Required = false, HelpText = "Output directly to JSON instead of ical", Default = false)]
            public bool formatToJSON { get; set; }

            [Option('f', "output", Required = false, HelpText = "File to output text to (default writes to stdout)")]
            public string outputFile { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(opts =>
            {
                //TODO: basic sanity check of options
                string html;
                if (string.IsNullOrEmpty(opts.inputHtml))
                {

                    html = getHtml(opts.inputURL);
                }
                else
                {
                    html = System.IO.File.ReadAllText(opts.inputHtml);
                }
                var games = gamesFromHtml(html);

            });
            //TODO: error handling


        }

        private static string cleanURL(string link)
        {
            if (link.TrimStart().StartsWith("javascript:openonlinewindow")) //TODO: this is quick and dirty as hell, probably won't always work (or never work, I dunno)
            {
                return link.Trim().Replace("javascript:openonlinewindow('", "http://stats.swehockey.se").Replace("','')", "");
            }
            return link;
        }

        /// <summary>
        /// Parse HTML to get a list of all games it contains
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static List<Game> gamesFromHtml(string html)
        {
            List<Game> games = new List<Game>();
            var svCulture = System.Globalization.CultureInfo.GetCultureInfo("sv-SE");

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            foreach (var node in doc.DocumentNode.SelectNodes("//table[@class='tblContent table']/tr"))
            {
                if (node.ChildNodes.Any(n => n.Name == "th"))
                {
                    continue;
                }
                string strDate = node.SelectSingleNode("td/div[@class='dateLink']/span").InnerText;
                string teams = node.SelectSingleNode("td[3]").InnerText;
                string location = node.SelectSingleNode("td[7]").InnerText;
                string series = node.SelectSingleNode("td[8]").InnerText;
                string url = node.SelectSingleNode("td/a")?.Attributes["href"].Value;
                Game game = new Game()
                {
                    Lag = teams,
                    Location = location,
                    Tid = DateTime.ParseExact(strDate, "yyyy-MM-dd HH:mm", svCulture),
                    Series = series
                };
                game.End = game.Tid.AddHours(2); //basically just for ical compatibility anyway
                if (!string.IsNullOrEmpty(url))
                {
                    game.Url = cleanURL(url);
                }
                else { game.Url = ""; }
                game.Uid = "swehockey_" + getGameId(game.Url); //basically just for ical compatibility anyway

                games.Add(game);
            }

            return games;
        }

        /// <summary>
        /// Finds the ID given to the game report on swehockey
        /// </summary>
        private static System.Text.RegularExpressions.Regex rxDoneGame = new System.Text.RegularExpressions.Regex(@"\/Game\/Events\/(\d+)");
        /// <summary>
        /// Finds the game ID given to upcoming game on hockeyettan.se 
        /// </summary>
        private static System.Text.RegularExpressions.Regex rxUpcomingGame = new System.Text.RegularExpressions.Regex(@"\/live\/(\d+)");
        /// <summary>
        /// Tries to give the game a unique ID based off the URL or just a random GUID 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string getGameId(string url)
        {
            if (rxDoneGame.IsMatch(url))
            {
                return rxDoneGame.Match(url).Groups[1].Value;
            }
            else if (rxUpcomingGame.IsMatch(url))
            {
                return rxUpcomingGame.Match(url).Groups[1].Value;
            }
            else
            {
                return new Guid().ToString();
            }
        }

        /// <summary>
        /// Fetch the HTML from swehockey 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static string getHtml(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                return client.GetStringAsync(url).Result;

            }
        }

        static string parseHtmlToJSON(string html)
        {
            var games = gamesFromHtml(html);
            //TODO: serialize json
            throw new NotImplementedException("TODO");
        }

        static string parseHtmlToIcal(string html)
        {
            var games = gamesFromHtml(html);
            foreach (Game game in games)
            {
                //TODO: write ical stuff
            }
            throw new NotImplementedException("TODO");
        }


    }
}
