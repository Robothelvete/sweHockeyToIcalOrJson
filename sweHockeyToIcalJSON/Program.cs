using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace sweHockeyToIcalJSON
{
    class Program
    {
        public class Options
        {
            [Option('i', "inputHtml", Required = false, HelpText = "Input filepath to HTML to parse")]
            public string inputHtml { get; set; }

            [Option('u', "url", Required = false, HelpText = "URL from where to fetch HTML to parse")]
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
                    string url;
                    if (string.IsNullOrEmpty(opts.inputURL))
                    {
                        url = "http://stats.swehockey.se/ScheduleAndResults/Schedule/10344"; //Hockeyettan 19/20
                    } else
                    {
                        url = opts.inputURL;
                    }

                    html = getHtml(url);
                    Console.Write(html);
                    Console.ReadLine();
                } else
                {
                    html = System.IO.File.ReadAllText(opts.inputHtml);
                }
            });
            //TODO: error handling


        }



        static string getHtml(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                return client.GetStringAsync(url).Result;

            }
        }

        static string parseHtmlToJSON(string html)
        {
            throw new NotImplementedException("TODO");
        }

        static string parseHtmlToIcal(string html)
        {
            throw new NotImplementedException("TODO");
        }
    }
}
