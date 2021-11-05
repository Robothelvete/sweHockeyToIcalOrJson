using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SweHockey
{
	public class ScoringParser
	{

		/// <summary>
		/// Gets the standings table from stats.swehockey.se
		/// </summary>
		/// <param name="leagueID">Which league to fetch the table for. Default is SHL. See <see cref="ParserServices.mapLeagueToSifID"/> for looking up some common league IDs</param>
		/// <returns></returns>
		public static string FetchScoringHtml(string leagueID = "12318") {
			string url = string.Format("https://stats.swehockey.se/Players/Statistics/ScoringLeaders/{0}", leagueID);
			using (HttpClient client = new HttpClient()) {
				return client.GetStringAsync(url).Result;
			}
		}

		public static List<SkaterStat> ScoringLeadersFromHtml(string html) {
			var skaterStats = new List<SkaterStat>();

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);

			foreach (var nodeRow in doc.DocumentNode.SelectNodes("//table[contains(@class,'tblContent')]/tr")) {

				if (nodeRow.ChildNodes.Any(n => n.Name == "td")) {
					var stat = new SkaterStat() {
						JerseyNumber = int.Parse(nodeRow.ChildNodes[1].InnerText),
						Name = nodeRow.ChildNodes[2].InnerText,
						Team = nodeRow.ChildNodes[3].InnerText,
						Position = nodeRow.ChildNodes[4].InnerText,
						GamesPlayed = int.Parse(nodeRow.ChildNodes[5].InnerText),
						Goals = int.Parse(nodeRow.ChildNodes[6].InnerText),
						Assists = int.Parse(nodeRow.ChildNodes[7].InnerText),
						Points = int.Parse(nodeRow.ChildNodes[8].InnerText),
						AveragePoints = double.Parse(nodeRow.ChildNodes[9].InnerText, System.Globalization.CultureInfo.InvariantCulture),
						PenaltyMinutes = int.Parse(nodeRow.ChildNodes[10].InnerText)
					};

					skaterStats.Add(stat);
				} else if (nodeRow.ChildNodes.Any(n => n.Name == "th" && n.HasClass("tdHeader"))) {
					//header row with column labels
				}
			}

			return skaterStats;
		}



	}
}
