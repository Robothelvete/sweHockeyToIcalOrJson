using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SweHockey
{
	public class StandingsParser
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
		/// Gets the standings table from stats.swehockey.se
		/// </summary>
		/// <param name="leagueID">Which league to fetch the table for. Default is SHL. See <see cref="mapLeagueToSifID"/> for looking up some common league IDs</param>
		/// <returns></returns>
		public static string FetchStandingsHtml(string leagueID = "12318") {
			string url = string.Format("https://stats.swehockey.se/ScheduleAndResults/Standings/{0}", leagueID);
			using (HttpClient client = new HttpClient()) {
				return client.GetStringAsync(url).Result;
			}
		}

		public static List<StandingsRow> StandingsFromHtml(string html) {
			var standings = new List<StandingsRow>();

			var rxGoals = new Regex(@"(?<for>-?\d+):(?<against>-?\d+)\s+\((?<diff>-?\d+)\)");

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);

			foreach (var nodeRow in doc.DocumentNode.SelectNodes("//div[@id='groupStandingResultContent']/table/tr/td[@class='tdWrapper']/table[@class='tblBorderNoPad'][1]/tr/td/table[contains(@class,'tblContent')]/tr")) {

				if (nodeRow.ChildNodes.Any(n => n.Name == "td" && n.HasClass("tdNormal") && n.Attributes["colspan"] == null)) {
					var row = new StandingsRow() {
						Rank = int.Parse(nodeRow.ChildNodes[0].InnerText),
						Team = nodeRow.ChildNodes[1].InnerText,
						GamesPlayed = int.Parse(nodeRow.ChildNodes[2].InnerText),
						Wins = int.Parse(nodeRow.ChildNodes[3].InnerText),
						Ties = int.Parse(nodeRow.ChildNodes[4].InnerText),
						Losses = int.Parse(nodeRow.ChildNodes[5].InnerText),
						Points = int.Parse(nodeRow.ChildNodes[8].InnerText),
						OvertimeWins = int.Parse(nodeRow.ChildNodes[9].InnerText),
						OvertimeLosses = int.Parse(nodeRow.ChildNodes[10].InnerText)
					};
					var strGoalDiff = nodeRow.ChildNodes[6].InnerText.Trim();
					var goals = rxGoals.Match(strGoalDiff);
					row.GoalsFor = int.Parse(goals.Groups["for"].Value);
					row.GoalsAgainst = int.Parse(goals.Groups["against"].Value);
					row.GoalDifferential = int.Parse(goals.Groups["diff"].Value);

					standings.Add(row);
				} else if (nodeRow.ChildNodes.Any(n => n.Name == "th" && n.HasClass("tdHeader"))) {
					//header row with column labels
				}
			}

			return standings;
		}



	}

}
