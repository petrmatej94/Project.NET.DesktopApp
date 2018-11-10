using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyVisualization
{
    public sealed class SymbolsList
    {
        private static SymbolsList instance = null;
        private static Dictionary<string, List<DailyRates>> allSymbols;


        private SymbolsList()
        {
            allSymbols = new Dictionary<string, List<DailyRates>>();
        }


        public static SymbolsList Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SymbolsList();
                }
                return instance;
            }
        }


        public static void AddToDatabase(string symbolName, List<DailyRates> prices)
        {
            allSymbols.Add(symbolName, prices);
        }
    }
}
