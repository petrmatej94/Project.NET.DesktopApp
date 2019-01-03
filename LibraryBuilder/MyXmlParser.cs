using LibraryBuilder;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TestLibrary
{
    public class MyXmlParser : ICurrencyInfo
    {
        public string apiKey = "PC55IMQVIAWCD1X9";
        private string url;
        private string symbolName = null;
        private List<DailyRates> dailyPrices;

        private Dictionary<DateTime, Double> openPrices;
        private Dictionary<DateTime, Double> highPrices;
        private Dictionary<DateTime, Double> lowPrices;
        private Dictionary<DateTime, Double> closePrices;
       

        private string fromSymbol = "gbp";
        private string toSymbol = "usd";

        public MyXmlParser()
        {
            //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.WriteLine("Library builder started. Processing " + fromSymbol.ToUpper() + toSymbol.ToUpper());

            url = String.Format("https://www.alphavantage.co/query?function=FX_DAILY&from_symbol={0}&to_symbol={1}&apikey={2}", fromSymbol, toSymbol, apiKey);
            dailyPrices = new List<DailyRates>();

            openPrices = new Dictionary<DateTime, double>();
            highPrices = new Dictionary<DateTime, double>();
            lowPrices = new Dictionary<DateTime, double>();
            closePrices = new Dictionary<DateTime, double>();

            SaveXmlFriendlyDoc();
            ParseMyXmlDoc(symbolName);
        }

        
        //Methods to call
        public List<DailyRates> GetRatesList()
        {
            return dailyPrices;
        }

        public string GetSymbolName()
        {
            return this.symbolName;
        }

        public Dictionary<DateTime, Double> GetOpenPrices()
        {
            return this.openPrices;
        }

        public Dictionary<DateTime, Double> GetHighPrices()
        {
            return this.highPrices;
        }

        public Dictionary<DateTime, Double> GetLowPrices()
        {
            return this.lowPrices;
        }

        public Dictionary<DateTime, Double> GetClosePrices()
        {
            return this.closePrices;
        }


        private string DownloadXml()
        {
            Trace.WriteLine("Downloading JSON file");

            string json = null;
            using (WebClient wc = new WebClient())
            {
                json = wc.DownloadString(url);
            }

            if (json.Contains("Error"))
            {
                Trace.WriteLine("Download failed, repeating " + fromSymbol + toSymbol);
                throw new DownloadErrorException("Error while downloading file");
            }

            if (json.Contains("Note"))
            {
                Trace.WriteLine("Download failed, repeating " + fromSymbol + toSymbol);
                Thread.Sleep(5000);
                throw new DownloadErrorException("Limit 5 API calls per minute, please wait");
            }

            Trace.Assert(json != null, "Successfuly downloaded JSON");
            return json;
        }


        public void SaveXmlFriendlyDoc()
        {
            string json = null;
            try
            {
                json = DownloadXml();
            }
            catch(DownloadErrorException ex)
            {
                Trace.WriteLine(ex + ". Repeating process\n");
                SaveXmlFriendlyDoc();
                return;
            }


            Trace.WriteLine("Converting JSON to XML");
            //Remove wrong syntax of JSON - numbers at start of nodes etc...
            for (int i = 1; i <= 6; i++)
            {
                json = json.Replace((i + ". "), "");
            }
            json = json.Replace(" ", "");
            json = json.Replace(".", ",");
            json = json.Replace("2018-", "D2018-");
            json = json.Replace("(", "");
            json = json.Replace(")", "");
            json = json.Replace("open,high,low,close", "");

            XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(json, "Root");

            if (doc == null)
            {
                Trace.WriteLine("XML failed to load... retrying " + fromSymbol + toSymbol);
                SaveXmlFriendlyDoc();
                return;
            }

            XmlNode from = doc.SelectSingleNode("Root/MetaData/FromSymbol");
            XmlNode to = doc.SelectSingleNode("Root/MetaData/ToSymbol");

            this.symbolName = from.InnerText + to.InnerText;

            doc.Save("./data/" + symbolName + ".xml");
            Trace.WriteLine("XML File saved");
        }


        public void ParseMyXmlDoc(string fileName)
        {
            Trace.WriteLine("Reading data from XML");
            XmlDocument doc = new XmlDocument();
            doc.Load("./data/" + fileName + ".xml");
            
            XmlNode TimeSeriesNode = doc.SelectSingleNode("Root/TimeSeriesFXDaily");
            
            
            foreach (XmlNode dateNode in TimeSeriesNode)
            {
                string newDate = dateNode.Name.Replace("_x0032_", "2").Replace("D", "");
                DateTime date = DateTime.Parse(newDate);
                Double open = Double.Parse(dateNode.SelectSingleNode("open").InnerText);
                Double high = Double.Parse(dateNode.SelectSingleNode("high").InnerText);
                Double low = Double.Parse(dateNode.SelectSingleNode("low").InnerText);
                Double close = Double.Parse(dateNode.SelectSingleNode("close").InnerText);
                
                dailyPrices.Add(new DailyRates()
                {
                    Date = date,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                });
                
                openPrices.Add(date, open);
                highPrices.Add(date, high);
                lowPrices.Add(date, low);
                closePrices.Add(date, close);
            }


            if(openPrices == null || highPrices == null || lowPrices == null || closePrices == null)
            {
                SaveXmlFriendlyDoc();
                ParseMyXmlDoc(symbolName);
            }
            Trace.WriteLine("Reading finished, data loaded successfuly");
        }
    }
}
