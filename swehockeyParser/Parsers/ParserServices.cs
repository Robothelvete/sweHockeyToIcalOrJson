using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SweHockey
{
	public class ParserServices
	{
		public static Dictionary<string, string> mapLeagueToSifID = new Dictionary<string, string>() {
			{"SHL", "12318" },
			{"HockeyAllsvenskan", "12320" },
			{"ATG Hockeyettan Norra", "12444" },
			{"ATG Hockeyettan Södra", "12384" },
			{"ATG Hockeyettan Västra", "12358" },
			{"ATG Hockeyettan Östra", "12436" },
			{"SDHL", "12317" },
			{"J20 SuperElit Top 10","10340" },
			{"J20 - Nationell Södra", "12312" },
			{"J20 - Nationell Norra", "12313" },
		};

		/// <summary>
		/// Cleans up the URL to a game that comes from swehockey
		/// </summary>
		/// <param name="link"></param>
		/// <returns></returns>
		public static string CleanURL(string link) {
			if (link.TrimStart().StartsWith("javascript:openonlinewindow")) //TODO: this is quick and dirty as hell, probably won't always work (or never work, I dunno)
			{
				return link.Trim().Replace("javascript:openonlinewindow('", "http://stats.swehockey.se").Replace("','')", "");
			}
			return link;
		}

		/// <summary>
		/// Cleans up the string saying which teams are playing
		/// </summary>
		/// <param name="teams"></param>
		/// <returns></returns>
		public static string CleanTeams(string teams) {
			return System.Net.WebUtility.HtmlDecode(teams.Replace(System.Environment.NewLine, "").Replace("            ", ""));
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
		public static string GetGameId(string url) {
			if (rxDoneGame.IsMatch(url)) {
				return rxDoneGame.Match(url).Groups[1].Value;
			} else if (rxUpcomingGame.IsMatch(url)) {
				return rxUpcomingGame.Match(url).Groups[1].Value;
			} else {
				return Guid.NewGuid().ToString();
			}
		}
	}

	/// <summary>
	/// Games, grouped by league
	/// </summary>
	public class LeagueGames
	{
		public string League { get; set; }
		public readonly List<Game> Games;
		public LeagueGames(string leage) {
			this.League = leage;
			this.Games = new List<Game>();
		}
	}
}
