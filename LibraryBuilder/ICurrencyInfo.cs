using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLibrary
{
    public interface ICurrencyInfo
    {
        List<DailyRates> GetRatesList();
        Dictionary<DateTime, Double> GetOpenPrices();
        Dictionary<DateTime, Double> GetHighPrices();
        Dictionary<DateTime, Double> GetLowPrices();
        Dictionary<DateTime, Double> GetClosePrices();
        string GetSymbolName();
    }
}
