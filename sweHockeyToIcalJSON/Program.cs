using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using swehockey;

namespace sweHockeyToIcalJSON
{
    /// <summary>
    /// This program demonstrates simple examples of how to use the <see cref="SweHockeyScheduleParser"/>
    /// </summary>
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

                    html = SweHockeyScheduleParser.getHtml(opts.inputURL);
                }
                else
                {
                    html = System.IO.File.ReadAllText(opts.inputHtml);
                }
                var games = SweHockeyScheduleParser.gamesScheduleFromHtml(html);

            });
            //TODO: error handling


        }

        

    }

    
}
