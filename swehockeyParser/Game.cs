using System;

namespace SweHockey
{
    /// <summary>
    /// Representation of a game from swehockey
    /// </summary>
    public class Game
    {
        public string Uid { get; set; }
        public string Lag { get; set; }
        public string Location { get; set; }
        public DateTime Tid { get; set; }
        public DateTime End { get; set; }
        public string Series { get; set; }
        public string Url { get; set; }
        /// <summary>
        /// The results of the game, if finished. Left as null for unfinished games
        /// </summary>
        public string Results { get; set; }
    }
}
