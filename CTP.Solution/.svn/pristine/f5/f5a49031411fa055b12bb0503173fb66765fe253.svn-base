using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTP;

namespace WrapperTest
{
    public class InstrumentComparer : IComparer<ThostFtdcDepthMarketDataField>
    {
        public int Compare(ThostFtdcDepthMarketDataField x, ThostFtdcDepthMarketDataField y)
        {
            return x.OpenInterest.CompareTo(y.OpenInterest);
        }
    }
}

