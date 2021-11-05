using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SweHockey
{
	public class StandingsRow
	{
		public int Rank;
		public string Team;
		public int Points;
		public int GamesPlayed;
		public int Wins;
		public int Losses;
		public int Ties;
		public int GoalsFor;
		public int GoalsAgainst;
		public int GoalDifferential;
		public int OvertimeLosses;
		public int OvertimeWins;
		public double pointsAverage {
			get {
				if(GamesPlayed == 0) { return 0; }
				return (double)Points / (double)GamesPlayed;
			}
		}
	}
}
