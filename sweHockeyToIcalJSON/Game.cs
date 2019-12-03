using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sweHockeyToIcalJSON
{
    public class Game
    {
        public string Uid { get; set; }
        public string Lag { get; set; }
        public string Location { get; set; }
        public DateTime Tid { get; set; }
        public DateTime End { get; set; }
        public string Series { get; set; }
        public string Url { get; set; }
    }
}
