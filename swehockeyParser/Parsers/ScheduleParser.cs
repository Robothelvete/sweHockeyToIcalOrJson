using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
						if (node.ChildNodes.Last().InnerText == "Group") { mode = ParserMode.HockeyEttan; } else { mode = ParserMode.SHL; }
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
				} else if ((int)mode == 2)
				{
					if (!string.IsNullOrEmpty(node.SelectSingleNode("td[2]").InnerText))
					{
						lastDate = node.SelectSingleNode("td[2]/bold").InnerText + " ";
					}
					strDate = lastDate + node.SelectSingleNode("td/div[@class='dateLink']/span").InnerText;
					teams = ParserServices.CleanTeams(node.SelectSingleNode("td[4]").InnerText);
					location = System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td[8]").InnerText);
					url = node.SelectSingleNode("td/a")?.Attributes["href"].Value.Replace("&#xD;&#xA;", "");
				} else if (mode == ParserMode.Slutspel)
				{
					strDate = node.SelectSingleNode("td/div[@class='dateLink']/span").InnerText;
					url = node.SelectSingleNode("td/a")?.Attributes["href"].Value.Replace("&#xD;&#xA;", "");
					location = System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td[7]").InnerText);
					teams = ParserServices.CleanTeams(node.SelectSingleNode("td[3]").InnerHtml.Split(new[] { "<br>", "<br />" }, StringSplitOptions.RemoveEmptyEntries)[0]);
					series = node.SelectSingleNode("td[3]/i").InnerText;
				} else { throw new Exception("Could not figure out how to parse this HTML"); }
				Game game = new Game()
				{
					Lag = teams,
					Location = location,
					Tid = DateTime.ParseExact(strDate.Trim(), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
				};
				game.End = game.Tid.AddHours(3); //basically just for ical compatibility anyway
				if (!string.IsNullOrEmpty(url))
				{
					game.Url = ParserServices.CleanURL(url);
				} else { game.Url = ""; }
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
					Tid = DateTime.ParseExact(strDate, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
					Lag = ParserServices.CleanTeams(node.SelectSingleNode("td[2]").InnerText),
					Location = System.Net.WebUtility.HtmlDecode(node.SelectSingleNode("td[4]").InnerText)
				};
				game.End = game.Tid.AddHours(2); //basically just for compatibility reasons, no one should care if it's inaccurate
				if (!string.IsNullOrEmpty(url))
				{
					game.Url = ParserServices.CleanURL(url);
				} else { game.Url = ""; }
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
		public static async Task<string> FetchScheduleHtml(string url)
		{
			using (HttpClient client = new HttpClient())
			{
				var result = await client.GetStringAsync(url);
				return result;
			}
		}

		/// <summary>
		/// Fetch the schedule HTML for a specific day
		/// </summary>
		/// <param name="date">Optional: which date to get the schedule from. Leave empty to get todays results</param>
		/// <returns></returns>
		public static async Task<string> FetchScheduleHtml(DateTime? date = null)
		{
			if (!date.HasValue)
			{
				date = DateTime.Now;
			}
			string url = string.Format("https://stats.swehockey.se/GamesByDate/{0}/ByTime/90", date.Value.ToString("yyyy-MM-dd"));
			return await FetchScheduleHtml(url);
		}

		public static string ParseHtmlToJSON(string html)
		{
			var games = GamesScheduleFromHtml(html);
			//TODO: serialize json
			throw new NotImplementedException("TODO");
		}

		public static string OutputToIcal(IEnumerable<Game> games)
		{
			var tzSweden = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

			var sb = new StringBuilder();
			sb.AppendLine("BEGIN:VCALENDAR");
			sb.AppendLine("VERSION:2.0");
			sb.AppendLine("PRODID:-//rishockey//calendar//EN");
			foreach (var game in games)
			{
				sb.AppendLine("BEGIN:VEVENT");
				sb.AppendLine("UID:" + game.Uid);
				sb.AppendLine("DTSTART:" + TimeZoneInfo.ConvertTimeToUtc(game.Tid, tzSweden).ToString("yyyyMMddTHHmmss") + "Z");
				sb.AppendLine("DTEND:" + TimeZoneInfo.ConvertTimeToUtc(game.End, tzSweden).ToString("yyyyMMddTHHmmss") + "Z");
				sb.AppendLine("DTSTAMP:" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "Z"); //TODO: could this be somehow set from data instead?
				sb.AppendLine("DESCRIPTION:" + game.Series);
				sb.AppendLine("SUMMARY:" + game.Lag);
				sb.AppendLine("LOCATION:" + game.Location);
				sb.AppendLine("END:VEVENT");
			}
			sb.AppendLine("END:VCALENDAR");

			return sb.ToString();
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
		SDHL = 2,
		Slutspel = 3
	}
}
