using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using CTP;
using log4net;
using log4net.Repository.Hierarchy;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using SendMail;
using System.Windows.Forms;

namespace WrapperTest
{
    public enum 信号
    {
        L,
        S,
        _
    }
    public class MarketData
    {
        public ThostFtdcDepthMarketDataField pDepthMarketData;
        public int 现手 = 0;
        public int 总多 = 0;
        public int 总空 = 0;
        public double 仓差 = 0.0;
        public 多空性质 性质 = 多空性质.__;
        public double 近期多头势力 = 0;
        public double 近期空头势力 = 0;
        public string 时段开始;
        public string 时段结束;
        public string 多空比
        {
            get
            {
                if (近期空头势力 != 0)
                {
                    return string.Format("{0:N2}", 近期多头势力 / 近期空头势力);
                }

                return "0";
            }
        }

        public 信号 信号
        {
            get
            {
                //if (近期空头势力 > Utils.多空比计算的阈值 && 多空比 > Utils.多空比计算的比例)
                //{
                //    return WrapperTest.信号.多开空平;
                //}

                //if (近期多头势力 > Utils.多空比计算的阈值 && 多空比 < 1 / Utils.多空比计算的比例)
                //{
                //    return WrapperTest.信号.空开多平;
                //}

                if (近期多头势力 - 近期空头势力 > Utils.多空差幅度)
                {
                    return WrapperTest.信号.L;
                }

                if (近期空头势力 - 近期多头势力 > Utils.多空差幅度)
                {
                    return WrapperTest.信号.S;
                }

                return 信号._;
            }
        }

        public double 多空差
        {
            get
            {
                return 近期多头势力 - 近期空头势力;
            }
        }
    }
    public enum ChannelType
    {
        模拟24X7,
        模拟交易所,
        华泰期货,
        宏源期货
    }

    public enum 多空性质
    {
        DK,
        DP,
        KK,
        KP,
        __,
        WD
    }

    public static class MathUtils
    {
        /// <summary>
        /// 拟合的直线斜角要求，单位是角度
        /// </summary>
        public static double Slope = 45;

        /// <summary>
        /// 仅根据走势开仓的拟合的直线斜角要求，单位是角度
        /// </summary>
        public static double Slope2 = 50;

        public static List<double> GetMovingAverage(List<double> source, int interval = 3)
        {
            try
            {
                var result = new List<double>();

                //补interval - 1个数在最前面
                var temp = new List<double>();
                for (var i = 0; i < interval - 1; i++)
                {
                    temp.Add(source[0]);
                }
                temp.AddRange(source);

                for (var i = interval - 1; i < temp.Count; i++)
                {
                    double sum = 0;
                    for (var j = i; j >= i - interval + 1; j--)
                    {
                        sum += temp[j];
                    }

                    var average = sum / interval;
                    result.Add(average);
                }
                return result;
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
            return null;
        }

        /// <summary>
        /// 根据连续几个行情，判断最近的趋势是不是向上，排除太平的，要求大于tan15度
        /// </summary>
        /// <param name="xdata"></param>
        /// <param name="ydata"></param>
        /// <param name="slope"></param>
        /// <param name="ma"></param>
        /// <returns></returns>
        public static Tuple<bool, double, double> IsPointingUp(List<double> xdata, List<double> ydata, double slope)
        {
            try
            {
                //至少需要两个点
                if (xdata.Count < 2 || ydata.Count < 2)
                {
                    return new Tuple<bool, double, double>(false, 0, 0);
                }

                //万一遇到数量不等的时候，进行补救
                if (xdata.Count != ydata.Count)
                {
                    var diff = Math.Abs(xdata.Count - ydata.Count);

                    if (xdata.Count > ydata.Count)
                    {
                        var last = ydata[ydata.Count - 1];

                        for (var i = 0; i < diff; i++)
                        {
                            ydata.Add(last);
                        }
                    }
                    else
                    {
                        var last = xdata[xdata.Count - 1];

                        for (var i = 0; i < diff; i++)
                        {
                            xdata.Add(last);
                        }
                    }
                }

                var line = Fit.Line(xdata.ToArray(), ydata.ToArray());
                return new Tuple<bool, double, double>(line.Item2 > Math.Tan(slope / 180.0 * Math.PI), line.Item2,
                    Math.Atan(line.Item2) * 180 / Math.PI);
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

            return new Tuple<bool, double, double>(false, 0, 0);
        }

        /// <summary>
        /// 根据连续几个行情，判断最近的趋势是不是向下，排除太平的，要求大于tan15度
        /// </summary>
        /// <param name="xdata"></param>
        /// <param name="ydata"></param>
        /// <param name="slope"></param>
        /// <param name="ma"></param>
        /// <returns></returns>
        public static Tuple<bool, double, double> IsPointingDown(List<double> xdata, List<double> ydata, double slope)
        {
            try
            {
                //至少需要两个点
                if (xdata.Count < 2 || ydata.Count < 2)
                {
                    return new Tuple<bool, double, double>(false, 0, 0);
                }

                //万一遇到数量不等的时候，进行补救
                if (xdata.Count != ydata.Count)
                {
                    var diff = Math.Abs(xdata.Count - ydata.Count);

                    if (xdata.Count > ydata.Count)
                    {
                        var last = ydata[ydata.Count - 1];

                        for (var i = 0; i < diff; i++)
                        {
                            ydata.Add(last);
                        }
                    }
                    else
                    {
                        var last = xdata[xdata.Count - 1];

                        for (var i = 0; i < diff; i++)
                        {
                            xdata.Add(last);
                        }
                    }
                }

                var line = Fit.Line(xdata.ToArray(), ydata.ToArray());
                return new Tuple<bool, double, double>(line.Item2 < Math.Tan(-slope / 180.0 * Math.PI), line.Item2,
                    Math.Atan(line.Item2) * 180 / Math.PI);
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

            return new Tuple<bool, double, double>(false, 0, 0);
        }
    }

    public interface ITraderAdapter
    {
        void CloseAllPositions(string a, string b);
    }

    public class HighLowProfit
    {
        public double High;
        public double HighTick;
        public double Low;
        public double LowTick;
    };

    public static class Utils
    {
        public static object Locker = new object();
        public static object Locker2 = new object();
        public static object LockerQuote = new object();
        public static bool IsTraderReady = false;
        public static object Trader;
        public static object QuoteMain;
        public static ILog LogDebug;
        public static ILog LogInfo;
        public static ConcurrentDictionary<string, ILog> LogQuotes;
        public static ILog LogStopLossPrices;
        public static Dictionary<string, string> messages = new Dictionary<string, string>();
        public static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                                                     "\\";

        public static bool IsMailingEnabled;
        public static bool IsInitialized = false;
        public static double GoUpRangeComparedToPreClosePrice = 1.01;
        public static double FallDownRangeComparedToPreClosePrice = 0.99;
        public static double GoUpRangeComparedToLowestPrice = 1.005;
        public static double LargeNumber = 999999;
        public static PromptForm.PromptForm promptForm = new PromptForm.PromptForm();
        public static bool IsPromptDisplaying = false;
        public static ConcurrentDictionary<string, HighLowProfit> PositionKeyToHighLowProfit = new ConcurrentDictionary<string, HighLowProfit>();
        /// <summary>
        /// 开仓时沿均线的误差值
        /// </summary>
        public static double OpenTolerance = 0.0003;

        /// <summary>
        /// 止损平仓时，最新价偏离成本价的幅度限制
        /// </summary>
        public static double CloseTolerance = 0.01;

        /// <summary>
        /// 当前距离比最高距离的比值限制，低于该值时止损
        /// </summary>
        public static double CurrentDistanceToHighestDistanceRatioLimit = 0.7;

        /// <summary>
        /// 当最高距离为最新价的某个幅度时，才考虑这种止损，避免多次小止损
        /// </summary>
        public static double HighestDistanceConsiderLimit = 0.004;

        public static double InstrumentTotalPrice = 40000;

        public static double FallDonwRangeComparedToHighestPrice = 0.995;

        public static double StopLossUpperRange = 1.005;
        public static double StopLossLowerRange = 0.995;
        public static double LimitCloseRange = 0.995;
        public static int OpenVolumePerTime = 1;
        public static int CategoryUpperLimit = 8;
        public static int 总多 = 0;
        public static int 总空 = 0;
        /// <summary>
        /// 用于拟合的分时图节点个数之一，短
        /// </summary>
        public static int MinuteByMinuteSizeShort = 5;

        /// <summary>
        /// 用于拟合的分时图节点个数之一，长
        /// </summary>
        public static int MinuteByMinuteSizeLong = 20;

        /// <summary>
        /// 用于拟合的分时图节点个数之一，中
        /// </summary>
        public static int MinuteByMinuteSizeMiddle = 10;

        /// <summary>
        /// simnow账号
        /// </summary>
        public static string SimNowAccount;

        /// <summary>
        /// simnow密码
        /// </summary>
        public static string SimNowPassword;

        /// <summary>
        /// 仅根据走势开仓时，偏离均线的幅度限制，如果超过了，认为已经错过了开仓时机
        /// </summary>
        public static double OpenAccordingToTrendLimit = 0.01;

        /// <summary>
        /// 振幅要求
        /// </summary>
        public static double SwingLimit = 0.005;

        public static double 多空比计算的秒数周期 = 60;

        public static double 多空比计算的阈值 = 1000;

        public static double 多空比计算的比例 = 3;

        public static double 多空差幅度 = 5000;
        public static double 开仓偏移量 = 1;
        public static double 涨跌幅提示 = 0.0045;
        public static int 分钟数 = 5;

        public static double availableMoney = 0;
        /// <summary>
        /// 止盈比例
        /// </summary>
        public static double StopProfitRatio = 0.02;

        public static List<string> AllowedCategories = new List<string>();
        public static List<string> AllowedShortTradeCategories = new List<string>();
        public static ChannelType CurrentChannel = ChannelType.模拟交易所;
        public static int ExchangeTimeOffset = 0;
        public static bool IsOpenLocked = false;

        public static void GetQuoteLoggers()
        {
            LogQuotes = new ConcurrentDictionary<string, ILog>();


            foreach (var category in AllowedCategories)
            {
                var log = LogManager.GetLogger(category);
                LogQuotes[category] = log;
            }
        }

        //public static void GetStopLossPricesLogger()
        //{
        //    LogStopLossPrices = LogManager.GetLogger(string.Format("{0}StopLossPrices", CurrentChannel));
        //}

        public static void GetDebugAndInfoLoggers()
        {
            LogDebug = LogManager.GetLogger("logDebug");
            LogInfo = LogManager.GetLogger("logInfo");
        }

        public static string GetInstrumentCategory(string instrumentId)
        {
            var regex = new Regex("^[a-zA-Z]+");
            var match = regex.Match(instrumentId);

            if (match.Success)
            {
                Debug.Assert(match.Value.Length <= 2);
                return match.Value;
            }

            return null;
        }

        public static string GetHourAndMinute(string time)
        {
            //21:09:00
            return time.Substring(0, 5);
        }

        public static string FormatQuote(MarketData marketData)
        {
            var s =
                string.Format(
                    ",{0},{1,-5},{9}.{10,-3},{13,-6},{15},{16},{18},{20,-5},{21,-5},{22},{23,-5},{24,-5},{25,-4},{26,-5},{27},{28,-12}|{29,-12},{30,-6},{31,-6},{32,-5}",
                    marketData.pDepthMarketData.InstrumentID, marketData.pDepthMarketData.LastPrice, marketData.pDepthMarketData.OpenPrice,
                    marketData.pDepthMarketData.PreSettlementPrice,
                    marketData.pDepthMarketData.PreClosePrice, marketData.pDepthMarketData.HighestPrice, marketData.pDepthMarketData.LowestPrice,
                    marketData.pDepthMarketData.UpperLimitPrice, marketData.pDepthMarketData.LowerLimitPrice, marketData.pDepthMarketData.UpdateTime,
                    marketData.pDepthMarketData.UpdateMillisec,
                    GetAveragePrice(marketData.pDepthMarketData), marketData.pDepthMarketData.LastPrice - GetAveragePrice(marketData.pDepthMarketData),
                    marketData.pDepthMarketData.Volume, marketData.pDepthMarketData.TradingDay,
                    marketData.pDepthMarketData.OpenInterest, marketData.pDepthMarketData.AskPrice1,
                    marketData.pDepthMarketData.BidVolume1, marketData.pDepthMarketData.BidPrice1,
                    marketData.pDepthMarketData.AskVolume1, marketData.现手, marketData.仓差, marketData.性质,
                    marketData.近期多头势力, marketData.近期空头势力, marketData.多空比, marketData.多空差, marketData.信号, marketData.时段开始, marketData.时段结束, marketData.总多, marketData.总空, GetAveragePrice(marketData.pDepthMarketData));

            return s;
        }

        public static bool DoubleEqual(double x, double y)
        {
            return Math.Abs(x - y) < 0.001;
        }

        public static void WriteQuote(ThostFtdcDepthMarketDataField pDepthMarketData)
        {
            try
            {
                if (pDepthMarketData == null)
                {
                    WriteLine("排除空行情", true);
                    return;
                }

                var instrumentId = pDepthMarketData.InstrumentID;

                //保存当前行情
                if (!InstrumentToQuotes.ContainsKey(pDepthMarketData.InstrumentID))
                {
                    InstrumentToQuotes[pDepthMarketData.InstrumentID] = new List<ThostFtdcDepthMarketDataField>();
                }

                var 现手 = 0;
                var 仓差 = 0.0;
                var 性质 = 多空性质.__;

                //计算多开、多平、空开、空平
                if (InstrumentToLastTick.ContainsKey(instrumentId)) //至少要两个tick行情才能计算
                {
                    var preTick = InstrumentToLastTick[instrumentId];

                    现手 = pDepthMarketData.Volume - preTick.Volume;
                    仓差 = pDepthMarketData.OpenInterest - preTick.OpenInterest;
                    性质 = 多空性质.__;

                    //记录成交价分布
                    if (!InstrumentToMatchPrice.ContainsKey(instrumentId))
                    {
                        InstrumentToMatchPrice[instrumentId] = new Dictionary<double, MatchPriceVolume>();
                    }

                    var dictMatchPriceVolume = InstrumentToMatchPrice[instrumentId];

                    var price = Math.Round(pDepthMarketData.LastPrice, 2);

                    if (!dictMatchPriceVolume.ContainsKey(price))
                    {
                        dictMatchPriceVolume[price] = new MatchPriceVolume { Price = price, Volume = (int)Math.Abs(仓差) };
                    }
                    else
                    {
                        var d = dictMatchPriceVolume[price];
                        d.Volume += (int)Math.Abs(仓差);
                    }

                    if (现手 != (int)Math.Abs(仓差)) //不考虑双开\双平\多换\空换情况
                    {
                        if (仓差 != 0)
                        {
                            if (仓差 > 0) //开仓
                            {
                                if (DoubleEqual(pDepthMarketData.LastPrice, preTick.AskPrice1) || pDepthMarketData.LastPrice >= preTick.AskPrice1) //以卖价成交，多开，价涨
                                {
                                    性质 = 多空性质.DK;
                                    总多 += (int)Math.Abs(仓差);
                                }
                                else
                                {
                                    if (DoubleEqual(pDepthMarketData.LastPrice, preTick.BidPrice1) || pDepthMarketData.LastPrice <= preTick.BidPrice1) //以买价成交，空开，价跌
                                    {
                                        性质 = 多空性质.KK;
                                        总空 += (int)Math.Abs(仓差);
                                    }
                                    else
                                    {
                                        性质 = 多空性质.WD;
                                        WriteLine(string.Format("未定:仓差{0},现价{1},前卖{2},前买{3}", 仓差, pDepthMarketData.LastPrice, preTick.AskPrice1, preTick.BidPrice1), true);
                                    }
                                }
                            }
                            else
                            {
                                if (仓差 < 0) //平仓
                                {
                                    if (DoubleEqual(pDepthMarketData.LastPrice, preTick.AskPrice1) || pDepthMarketData.LastPrice >= preTick.AskPrice1) //以卖价成交，空平，价涨
                                    {
                                        性质 = 多空性质.KP;
                                        总多 += (int)Math.Abs(仓差);
                                    }
                                    else
                                    {
                                        if (DoubleEqual(pDepthMarketData.LastPrice, preTick.BidPrice1) || pDepthMarketData.LastPrice <= preTick.BidPrice1) //以买价成交，多平，价跌
                                        {
                                            性质 = 多空性质.DP;
                                            总空 += (int)Math.Abs(仓差);
                                        }
                                        else
                                        {
                                            性质 = 多空性质.WD;
                                            WriteLine(string.Format("未定:仓差{0},现价{1},前卖{2},前买{3}", 仓差, pDepthMarketData.LastPrice, preTick.AskPrice1, preTick.BidPrice1), true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                InstrumentToLastTick[instrumentId] = pDepthMarketData;
                InstrumentToQuotes[pDepthMarketData.InstrumentID].Add(pDepthMarketData);

                var marketData = new MarketData
                {
                    pDepthMarketData = pDepthMarketData,
                    现手 = 现手,
                    仓差 = 仓差,
                    性质 = 性质,
                    总多 = 总多,
                    总空 = 总空
                };

                if (!InstrumentToMarketData.ContainsKey(instrumentId))
                {
                    InstrumentToMarketData[instrumentId] = new List<MarketData>();
                }

                var count = InstrumentToMarketData[instrumentId].Count;

                if (count > 0)
                {
                    var backCount = (int)多空比计算的秒数周期 * 2;
                    var dtNow = Convert.ToDateTime(pDepthMarketData.UpdateTime);

                    if (count >= backCount)
                    {
                        var iStart = count - backCount;
                        var start = InstrumentToMarketData[instrumentId][count - backCount];

                        var list = InstrumentToMarketData[instrumentId];
                        var listRange = list.GetRange(iStart, backCount);

                        var p = from d in listRange where d.性质 == 多空性质.DK || d.性质 == 多空性质.KP select Math.Abs(d.仓差);
                        marketData.近期多头势力 = p.Sum();

                        var q = from d in listRange where d.性质 == 多空性质.KK || d.性质 == 多空性质.DP select Math.Abs(d.仓差);
                        marketData.近期空头势力 = q.Sum();

                        //补上当前tick的数据
                        if (marketData.性质 == 多空性质.DK || marketData.性质 == 多空性质.KP)
                        {
                            marketData.近期多头势力 += Math.Abs(marketData.仓差);
                        }

                        if (marketData.性质 == 多空性质.KK || marketData.性质 == 多空性质.DP)
                        {
                            marketData.近期空头势力 += Math.Abs(marketData.仓差);
                        }

                        marketData.时段开始 = string.Format("{0}.{1}", start.pDepthMarketData.UpdateTime, start.pDepthMarketData.UpdateMillisec);
                        marketData.时段结束 = string.Format("{0}.{1}", pDepthMarketData.UpdateTime, pDepthMarketData.UpdateMillisec);
                    }
                    else //小于周期时
                    {
                        var list = InstrumentToMarketData[instrumentId];

                        var p = from d in list where d.性质 == 多空性质.DK || d.性质 == 多空性质.KP select Math.Abs(d.仓差);
                        marketData.近期多头势力 = p.Sum();

                        var q = from d in list where d.性质 == 多空性质.KK || d.性质 == 多空性质.DP select Math.Abs(d.仓差);
                        marketData.近期空头势力 = q.Sum();

                        //补上当前tick的数据
                        if (marketData.性质 == 多空性质.DK || marketData.性质 == 多空性质.KP)
                        {
                            marketData.近期多头势力 += Math.Abs(marketData.仓差);
                        }

                        if (marketData.性质 == 多空性质.KK || marketData.性质 == 多空性质.DP)
                        {
                            marketData.近期空头势力 += Math.Abs(marketData.仓差);
                        }

                        if (list.Count > 0)
                        {
                            marketData.时段开始 = string.Format("{0}.{1}", list[0].pDepthMarketData.UpdateTime, list[0].pDepthMarketData.UpdateMillisec);
                            marketData.时段结束 = string.Format("{0}.{1}", pDepthMarketData.UpdateTime, pDepthMarketData.UpdateMillisec);
                        }
                    }
                }
                else
                {
                    marketData.时段开始 = string.Format("{0}.{1}", pDepthMarketData.UpdateTime, pDepthMarketData.UpdateMillisec);
                    marketData.时段结束 = string.Format("{0}.{1}", pDepthMarketData.UpdateTime, pDepthMarketData.UpdateMillisec);
                }

                //监控品种的涨跌幅提示
                if (!AllowedShortTradeCategories.Contains(GetInstrumentCategory(instrumentId)))
                {
                    if ((pDepthMarketData.LastPrice < pDepthMarketData.OpenPrice * 1.0110 && pDepthMarketData.LastPrice > pDepthMarketData.OpenPrice * 1.0090) || (pDepthMarketData.LastPrice < pDepthMarketData.OpenPrice * 0.9910 && pDepthMarketData.LastPrice > pDepthMarketData.OpenPrice * 0.9890)) //大窗口提示
                    {
                        var up = pDepthMarketData.LastPrice < pDepthMarketData.OpenPrice * 1.0110 && pDepthMarketData.LastPrice > pDepthMarketData.OpenPrice * 1.0090;


                        if (!messages.ContainsKey(instrumentId))
                        {
                            var ratio = (pDepthMarketData.LastPrice - pDepthMarketData.OpenPrice) / pDepthMarketData.OpenPrice;
                            var prompt = string.Format("信号：{0}{1},开盘:{2},当前:{3},幅度:{4:P},时间{5}", instrumentId, up ? "兴" : "衰", pDepthMarketData.OpenPrice, pDepthMarketData.LastPrice, ratio, DateTime.Now);
                            WriteLine(prompt, true);
                            var list = new List<string>();
                            list.Add(instrumentId);
                            list.Add(up ? "兴" : "衰");

                            list.Add(pDepthMarketData.OpenPrice.ToString());
                            list.Add(pDepthMarketData.LastPrice.ToString());
                            list.Add(ratio.ToString("P"));

                            list.Add(marketData.pDepthMarketData.LastPrice.ToString());
                            list.Add(DateTime.Now.ToString("mm:ss"));

                            var promptItem = new PromptForm.PromptItem();
                            promptItem.MessageItems = list;
                            promptItem.InstrumentId = instrumentId;
                            promptItem.Direction = up ? "Buy" : "Sell";
                            promptItem.Price = marketData.pDepthMarketData.LastPrice;
                            promptItem.Volume = 1;
                            promptItem.Offset = 5;

                            if (promptForm.IsHandleCreated)
                            {
                                promptForm.Invoke(new Action(() =>
                                {
                                    promptForm.AddMessage(promptItem);
                                }));
                            }

                            messages[instrumentId] = prompt;
                        }
                    }
                }

                if (pDepthMarketData.LastPrice > pDepthMarketData.OpenPrice * 1.01 || pDepthMarketData.LastPrice < pDepthMarketData.OpenPrice * 0.99) //状态栏提示
                {
                    if (!AllowedShortTradeCategories.Contains(GetInstrumentCategory(instrumentId)))
                    {
                        var b = pDepthMarketData.LastPrice > pDepthMarketData.OpenPrice * 1.01;

                        var message = string.Format("{0}{1}{2:P}", instrumentId, b ? "上涨" : "下跌", (pDepthMarketData.LastPrice - pDepthMarketData.OpenPrice) / pDepthMarketData.OpenPrice);

                        if (b && promptForm.IsHandleCreated)
                        {
                            promptForm.Invoke(new Action(() =>
                            {
                                promptForm.SetUpStatus(message);
                            }));
                        }

                        if (!b && promptForm.IsHandleCreated)
                        {
                            promptForm.Invoke(new Action(() =>
                            {
                                promptForm.SetDownStatus(message);
                            }));
                        }
                    }
                }



                InstrumentToMarketData[instrumentId].Add(marketData);

                var dataQueue = InstrumentToMarketData[instrumentId];

                var step = 10;


                int startIndex = 0;
                var endIndex = dataQueue.Count - 1;

                for (var i = dataQueue.Count - 1; i >= 0; i -= step)
                {
                    if (i < 0)
                    {
                        startIndex = 0;
                        break;
                    }
                    else
                    {
                        var endTime = Convert.ToDateTime(dataQueue[endIndex].pDepthMarketData.UpdateTime);
                        var currentTime = Convert.ToDateTime(dataQueue[i].pDepthMarketData.UpdateTime);
                        if (endTime - currentTime >= new TimeSpan(0, 分钟数, 0))
                        {
                            startIndex = i;
                            break;
                        }
                    }
                }

                List<MarketData> dataQueueSub = new List<MarketData>();

                for (var i = startIndex; i <= endIndex; i++)
                {
                    dataQueueSub.Add(dataQueue[i]);
                }

                var min = dataQueueSub.Min(d => d.pDepthMarketData.LastPrice);
                var max = dataQueueSub.Max(d => d.pDepthMarketData.LastPrice);

                var minQuote = dataQueueSub.FindLast(d => d.pDepthMarketData.LastPrice.Equals(min));
                var maxQuote = dataQueueSub.FindLast(d => d.pDepthMarketData.LastPrice.Equals(max));

                if ((max - min) / min > 涨跌幅提示 && AllowedShortTradeCategories.Contains(GetInstrumentCategory(instrumentId)))
                {
                    var maxTime = Convert.ToDateTime(maxQuote.pDepthMarketData.UpdateTime);
                    var minTime = Convert.ToDateTime(minQuote.pDepthMarketData.UpdateTime);

                    bool up = true;
                    if (maxTime > minTime)
                    {
                        up = true;
                    }
                    else
                    {
                        up = false;
                    }

                    if (!messages.ContainsKey(instrumentId))
                    {
                        double ratio;
                        if (up)
                        {
                            ratio = (max - min) / min;
                        }
                        else
                        {
                            ratio = (max - min) / max;
                        }

                        var prompt = string.Format("信号：{0}{1},最低:{2},最高:{3},当前:{4},幅度:{5},时间:{6}", instrumentId, up ? "上涨" : "下跌", min, max, marketData.pDepthMarketData.LastPrice, ratio, DateTime.Now);
                        WriteLine(prompt, true);
                        var list = new List<string>();
                        list.Add(instrumentId);
                        list.Add(up ? "涨" : "跌");
                        if (up)
                        {
                            list.Add(min.ToString());
                            list.Add(max.ToString());
                            list.Add(((max - min) / min).ToString("P"));
                        }
                        else
                        {
                            list.Add(max.ToString());
                            list.Add(min.ToString());
                            list.Add(((max - min) / max).ToString("P"));
                        }

                        list.Add(marketData.pDepthMarketData.LastPrice.ToString());
                        list.Add(DateTime.Now.ToString("mm:ss"));

                        var promptItem = new PromptForm.PromptItem();
                        promptItem.MessageItems = list;
                        promptItem.InstrumentId = instrumentId;
                        promptItem.Direction = up ? "Buy" : "Sell";
                        promptItem.Price = marketData.pDepthMarketData.LastPrice;
                        promptItem.Volume = 1;
                        promptItem.Offset = 5;

                        if (promptForm.IsHandleCreated)
                        {
                            promptForm.Invoke(new Action(() =>
                                             {
                                                 promptForm.AddMessage(promptItem);
                                             }));
                        }

                        messages[instrumentId] = prompt;
                    }
                }


                var s = FormatQuote(marketData);

                try
                {
                    LogQuotes[GetInstrumentCategory(pDepthMarketData.InstrumentID)].Debug(s);

                    if (promptForm.IsHandleCreated && AllowedShortTradeCategories.Contains(GetInstrumentCategory(instrumentId)))
                    {
                        promptForm.Invoke(new Action(() =>
                        {
                            promptForm.SetTitle(s);
                        }));
                    }

                    if (promptForm.IsHandleCreated)
                    {
                        promptForm.Invoke(new Action(() =>
                        {
                            promptForm.SetTime(Convert.ToDateTime(marketData.pDepthMarketData.UpdateTime));
                        }));
                    }
                }
                catch (Exception)
                {

                }

                Console.WriteLine(s);
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }

        }

        public static double GetAveragePrice(ThostFtdcDepthMarketDataField data)
        {
            return data.AveragePrice / data.PreClosePrice < 2
                ? data.AveragePrice
                : InstrumentToInstrumentInfo.ContainsKey(data.InstrumentID)
                    ? data.AveragePrice / InstrumentToInstrumentInfo[data.InstrumentID].VolumeMultiple
                    : data.AveragePrice;
        }

        public static void WriteException(Exception ex)
        {
            WriteLine(ex.Source + ex.Message + ex.StackTrace, true);
        }

        public static void WriteLine(string line = "\n", bool writeInfo = false)
        {
            try
            {
                LogDebug.Debug(line);
                if (writeInfo)
                {
                    LogInfo.Debug(line);
                }
                Console.WriteLine(line);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Source + ex.Message + ex.StackTrace);
            }
        }

        public static void OutputLine()
        {
            WriteLine("********************************************************");
        }

        public static string OutputField(object obj, bool outputToFile = true, bool writeInfo = false)
        {
            var sb = new StringBuilder();

            if (outputToFile)
            {
                WriteLine("\n", writeInfo);
                OutputLine();
            }

            var type = obj.GetType();
            var fields = type.GetFields();

            foreach (var field in fields)
            {
                var temp = string.Format("[{0}]:[{1}]", field.Name, field.GetValue(obj));

                if (outputToFile)
                {
                    WriteLine(temp, writeInfo);
                }

                sb.AppendLine(temp);
            }

            if (outputToFile)
            {
                OutputLine();
                WriteLine("\n", writeInfo);
            }

            return sb.ToString();
        }

        public static bool IsWrongRspInfo(ThostFtdcRspInfoField pRspInfo)
        {
            return pRspInfo != null && pRspInfo.ErrorID != 0;
        }

        public static void ReportError(ThostFtdcRspInfoField pRspInfo, string title)
        {
            if (IsWrongRspInfo(pRspInfo))
            {
                var message = string.Format("{0}:{1}", title, pRspInfo.ErrorMsg);
                WriteLine(message, true);
                Email.SendMail(message, DateTime.Now.ToString(CultureInfo.InvariantCulture));
            }
        }


        public static bool IsCorrectRspInfo(ThostFtdcRspInfoField pRspInfo)
        {
            return pRspInfo != null && pRspInfo.ErrorID == 0;
        }

        /// <summary>
        /// 读取程序的配置参数
        /// </summary>
        public static void ReadConfig()
        {
            var configFile = AssemblyPath + "config.ini";

            if (File.Exists(configFile))
            {
                var sr = new StreamReader(configFile, Encoding.UTF8);

                var line = sr.ReadLine();
                var s = GetLineData(line).Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                AllowedCategories.AddRange(s.Where(t => !string.IsNullOrWhiteSpace(t)));

                line = sr.ReadLine();
                s = GetLineData(line).Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                AllowedShortTradeCategories.AddRange(s.Where(t => !string.IsNullOrWhiteSpace(t)));

                line = sr.ReadLine();

                GoUpRangeComparedToPreClosePrice = 1 + Convert.ToDouble(GetLineData(line));
                FallDownRangeComparedToPreClosePrice = 1 - Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();

                GoUpRangeComparedToLowestPrice = 1 + Convert.ToDouble(GetLineData(line));
                FallDonwRangeComparedToHighestPrice = 1 - Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                StopLossUpperRange = 1 + Convert.ToDouble(GetLineData(line));
                StopLossLowerRange = 1 - Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                LimitCloseRange = 1 - Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                OpenVolumePerTime = Convert.ToInt32(GetLineData(line));

                line = sr.ReadLine();
                CategoryUpperLimit = Convert.ToInt32(GetLineData(line));

                line = sr.ReadLine();
                OpenTolerance = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                CloseTolerance = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                CurrentDistanceToHighestDistanceRatioLimit = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                MinuteByMinuteSizeShort = Convert.ToInt32(GetLineData(line));

                line = sr.ReadLine();
                MinuteByMinuteSizeLong = Convert.ToInt32(GetLineData(line));

                line = sr.ReadLine();
                HighestDistanceConsiderLimit = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                InstrumentTotalPrice = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                MinuteByMinuteSizeMiddle = Convert.ToInt32(GetLineData(line));

                line = sr.ReadLine();
                MathUtils.Slope = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                SimNowAccount = GetLineData(line);

                line = sr.ReadLine();
                SimNowPassword = GetLineData(line);

                line = sr.ReadLine();
                MathUtils.Slope2 = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                OpenAccordingToTrendLimit = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                SwingLimit = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                StopProfitRatio = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                多空比计算的秒数周期 = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                多空比计算的阈值 = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                多空比计算的比例 = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                多空差幅度 = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                开仓偏移量 = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                涨跌幅提示 = Convert.ToDouble(GetLineData(line));

                line = sr.ReadLine();
                分钟数 = Convert.ToInt32(GetLineData(line));
                sr.Close();
            }
        }

        public static string GetLineData(string line)
        {
            var s = line.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

            if (s.Length > 0)
            {
                return s[0].Trim();
            }

            return null;
        }

        /// <summary>
        /// 根据合约名、持仓方向、昨仓今仓，生成该仓位的键
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="direction"></param>
        /// <param name="positionDate"></param>
        /// <returns></returns>
        public static string GetPositionKey(string instrumentId, EnumPosiDirectionType direction,
            EnumPositionDateType positionDate)
        {
            return string.Format("{0}:{1}:{2}", instrumentId, direction, positionDate);
        }

        public static string GetOpenTrendStartPointKey(string instrumentId, EnumPosiDirectionType direction)
        {
            return string.Format("{0}:{1}", instrumentId, direction);
        }

        /// <summary>
        /// 根据合约名、开仓方向，生成开仓操作的键
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <param name="buyOrSell"></param>
        /// <returns></returns>
        public static string GetOpenPositionKey(string instrumentId, EnumDirectionType buyOrSell)
        {
            return string.Format("{0}:{1}", instrumentId, buyOrSell);
        }

        public static string GetInstrumentIdFromPositionKey(string positionKey)
        {
            var s = positionKey.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            return s[0];
        }

        /// <summary>
        /// 判断该合约是不是允许交易的品种以及是不是主力合约
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <returns></returns>
        public static bool IsTradableInstrument(string instrumentId)
        {
            if (CategoryToMainInstrument.ContainsKey(GetInstrumentCategory(instrumentId)))
            {
                var v = CategoryToMainInstrument[GetInstrumentCategory(instrumentId)];

                if (v.Equals(instrumentId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断当前时间是否在合约的交易时间之内
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <returns></returns>
        public static bool IsInInstrumentTradingTime(string instrumentId)
        {
            if (CurrentChannel == ChannelType.模拟24X7)
            {
                return true;
            }

            var category = GetInstrumentCategory(instrumentId);

            if (CategoryToExchangeId.ContainsKey(category))
            {
                return ExchangeTime.Instance.IsTradingTime(category, CategoryToExchangeId[category]);
            }

            WriteLine(string.Format("当前时段{0}不是合约{1}的交易时间段", DateTime.Now, instrumentId), true);
            return false;
        }

        /// <summary>
        /// 判断合约是不是上期所的合约
        /// </summary>
        /// <param name="instrumentId"></param>
        /// <returns></returns>
        public static bool IsShfeInstrument(string instrumentId)
        {
            var category = GetInstrumentCategory(instrumentId);

            if (CategoryToExchangeId.ContainsKey(category))
            {
                return CategoryToExchangeId[category].Equals("SHFE");
            }

            return false;
        }

        public static StopLossPrices CreateStopLossPrices(ThostFtdcDepthMarketDataField pDepthMarketData)
        {
            try
            {
                //新交易日未开盘时，最高价和最低价为无效值，要排除；交易日中途启动时，暂时设最高价最低价，其实应该读取上次的参考价。
                var stopLossPrices = new StopLossPrices { Instrument = pDepthMarketData.InstrumentID };

                if (pDepthMarketData.HighestPrice > 1 && pDepthMarketData.LowestPrice > 1)
                {
                    stopLossPrices.ForLong = pDepthMarketData.HighestPrice;
                    stopLossPrices.ForShort = pDepthMarketData.LowestPrice;
                }
                else
                {
                    stopLossPrices.ForLong = pDepthMarketData.PreClosePrice;
                    stopLossPrices.ForShort = pDepthMarketData.PreClosePrice;
                }

                return stopLossPrices;
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
            return null;
        }

        public static bool IsInstrumentLocked(string instrumentId)
        {
            if (LockedInstruments.ContainsKey(instrumentId))
            {
                WriteLine(string.Format("合约{0}还有报单在途中，不报单", instrumentId), true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 锁定正在开仓的合约
        /// </summary>
        /// <param name="instrumentId"></param>
        public static void LockOpenInstrument(string instrumentId)
        {
            LockedOpenInstruments[instrumentId] = instrumentId;
            WriteLine(string.Format("增加开仓在途记录{0}", instrumentId), true);
        }

        public static void RemoveLockedOpenInstrument(string instrumentId)
        {
            if (LockedOpenInstruments.ContainsKey(instrumentId))
            {
                string temp;
                LockedOpenInstruments.TryRemove(instrumentId, out temp);
                WriteLine(string.Format("减少开仓在途记录{0}", instrumentId), true);
            }
        }

        /// <summary>
        /// 锁定正在报单的合约
        /// </summary>
        /// <param name="instrumentId"></param>
        public static void LockInstrument(string instrumentId)
        {
            LockedInstruments[instrumentId] = instrumentId;
            WriteLine(string.Format("锁定{0}", instrumentId), true);
        }

        public static void RemoveLockedInstrument(string instrumentId)
        {
            if (LockedInstruments.ContainsKey(instrumentId))
            {
                string temp;
                LockedInstruments.TryRemove(instrumentId, out temp);
                WriteLine(string.Format("解锁{0}", instrumentId), true);
            }
        }

        public static void UnlockInstrument(string instrumentId, EnumOffsetFlagType flag)
        {
            if (flag == EnumOffsetFlagType.Open)
            {
                RemoveLockedOpenInstrument(instrumentId);
            }

            RemoveLockedInstrument(instrumentId);
        }

        /// <summary>
        /// 读取止损参考价
        /// </summary>
        public static void ReadStopLossPrices()
        {
            var file = string.Format(AssemblyPath + "{0}_StopLossPrices.txt", CurrentChannel);

            if (File.Exists(file))
            {
                var sr = new StreamReader(file, Encoding.UTF8);
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    var s = line.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

                    if (s.Length > 4)
                    {
                        string instrument;
                        double costLong;
                        double costShort;
                        double forLong;
                        double forShort;

                        try
                        {
                            instrument = s[0];
                        }
                        catch (Exception)
                        {
                            instrument = null;
                        }

                        try
                        {
                            costLong = Convert.ToDouble(s[1]);
                        }
                        catch (Exception)
                        {
                            costLong = 0;
                        }

                        try
                        {
                            costShort = Convert.ToDouble(s[2]);
                        }
                        catch (Exception)
                        {
                            costShort = 0;
                        }

                        try
                        {
                            forLong = Convert.ToDouble(s[3]);
                        }
                        catch (Exception)
                        {
                            forLong = 0;
                        }

                        try
                        {
                            forShort = Convert.ToDouble(s[4]);
                        }
                        catch (Exception)
                        {
                            forShort = 0;
                        }

                        var stopLossPrices = new StopLossPrices
                        {
                            Instrument = instrument,
                            CostLong = costLong,
                            CostShort = costShort,
                            ForLong = forLong,
                            ForShort = forShort
                        };
                        InstrumentToStopLossPrices[s[0]] = stopLossPrices;
                        WriteLine(
                            string.Format("读取合约{0},多仓成本价{1},空仓成本价{2},多仓止损价{3},空仓止损价{4}", stopLossPrices.Instrument,
                                stopLossPrices.CostLong, stopLossPrices.CostShort, stopLossPrices.ForLong,
                                stopLossPrices.ForShort), true);
                    }
                }

                sr.Close();
            }
        }

        public static void SaveInstrumentTotalPrices()
        {
            try
            {
                //保存一手合约的当前总价
                if (CategoryToMainInstrument.Count > 0)
                {
                    var sw = new StreamWriter(AssemblyPath + string.Format("{0}_InstrumentPrices.txt", CurrentChannel),
                        false, Encoding.UTF8);
                    var sb = new StringBuilder();
                    foreach (var kv in CategoryToMainInstrument)
                    {
                        if (InstrumentToInstrumentInfo.ContainsKey(kv.Value) &&
                            InstrumentToQuotes.ContainsKey(kv.Value))
                        {
                            var instrumentInfo = InstrumentToInstrumentInfo[kv.Value];
                            var quotes = InstrumentToQuotes[kv.Value];
                            if (quotes.Count > 0)
                            {
                                var quote = quotes[quotes.Count - 1];
                                var totalPrice = instrumentInfo.VolumeMultiple * quote.LastPrice;
                                InstrumentToTotalPrice[kv.Value] = totalPrice;
                                var temp = string.Format("{0}:{1}:{2}", kv.Key, kv.Value, totalPrice);
                                sb.AppendLine(temp);
                            }
                        }
                    }

                    sw.WriteLine(sb);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }

        }

        public static void Exit(object trader = null)
        {
            try
            {
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }

        }

        /// <summary>
        /// 保存止损参考价
        /// </summary>
        public static void SaveStopLossPrices()
        {
            try
            {
                //保存止损价
                if (InstrumentToStopLossPrices.Count > 0)
                {
                    var sw = new StreamWriter(string.Format("{0}_StopLossPrices.txt", CurrentChannel), false,
                        Encoding.UTF8);
                    var sb = new StringBuilder();
                    foreach (var kv in InstrumentToStopLossPrices)
                    {
                        var temp = string.Format("{0}:{1}:{2}:{3}:{4}", kv.Key, kv.Value.CostLong, kv.Value.CostShort,
                            kv.Value.ForLong, kv.Value.ForShort);
                        sb.AppendLine(temp);
                    }

                    sw.WriteLine(sb);
                    WriteLine("保存止损价" + sb);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }

        }

        public static ConcurrentDictionary<string, double> InstrumentToTotalPrice =
            new ConcurrentDictionary<string, double>();

        /// <summary>
        /// 所有合约行情，最后一个是最新行情
        /// </summary>
        public static ConcurrentDictionary<string, List<ThostFtdcDepthMarketDataField>> InstrumentToQuotes =
            new ConcurrentDictionary<string, List<ThostFtdcDepthMarketDataField>>();

        public static ConcurrentDictionary<string, ThostFtdcInstrumentField> InstrumentToInstrumentInfo =
            new ConcurrentDictionary<string, ThostFtdcInstrumentField>();

        public static ConcurrentDictionary<string, List<ThostFtdcDepthMarketDataField>> InstrumentToInstrumentsDepthMarketData =
                new ConcurrentDictionary<string, List<ThostFtdcDepthMarketDataField>>();

        public static ConcurrentDictionary<string, string> CategoryToMainInstrument =
            new ConcurrentDictionary<string, string>();

        public static ConcurrentDictionary<string, string> CategoryToExchangeId =
            new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 合约的止损参考价，分为多仓和空仓的成本价，多仓和空仓的止损参考价
        /// </summary>
        public static ConcurrentDictionary<string, StopLossPrices> InstrumentToStopLossPrices =
            new ConcurrentDictionary<string, StopLossPrices>();

        /// <summary>
        /// 还未收到报单响应的合约，暂时不能报单
        /// </summary>
        public static ConcurrentDictionary<string, string> LockedInstruments =
            new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 记录开仓在途的合约，防止开仓超过品种数量限制
        /// </summary>
        public static ConcurrentDictionary<string, string> LockedOpenInstruments =
            new ConcurrentDictionary<string, string>();

        /// <summary>
        /// 合约的开仓次数记录，如果超过，不再开仓
        /// </summary>
        public static ConcurrentDictionary<string, int> InstrumentToOpenCount = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// 合约的分时图数据，取每分钟最后一个行情数据
        /// </summary>
        public static ConcurrentDictionary<string, List<Tuple<string, Quote>>> InstrumentToMinuteByMinuteChart
            = new ConcurrentDictionary<string, List<Tuple<string, Quote>>>();

        /// <summary>
        /// 记录上次合约的买入平仓、卖出平仓时间，防止刚刚平仓又立即开同方向的仓
        /// </summary>
        public static ConcurrentDictionary<string, DateTime> InstrumentToLastCloseTime =
            new ConcurrentDictionary<string, DateTime>();

        public static ConcurrentDictionary<string, ThostFtdcDepthMarketDataField> InstrumentToLastTick =
            new ConcurrentDictionary<string, ThostFtdcDepthMarketDataField>();

        /// <summary>
        /// 记录合约开仓的趋势启动点，分为多仓和空仓
        /// </summary>
        public static ConcurrentDictionary<string, double> InstrumentToOpenTrendStartPoint =
            new ConcurrentDictionary<string, double>();

        /// <summary>
        /// 记录合约开仓被禁止时丢失的趋势启动点，分为多仓和空仓
        /// </summary>
        public static ConcurrentDictionary<string, double> InstrumentToMissedOpenTrendStartPoint =
            new ConcurrentDictionary<string, double>();

        /// <summary>
        /// 记录合约从程序启动时到目前的最高价、最低价，以便计算振幅
        /// </summary>
        public static ConcurrentDictionary<string, MaxAndMinPrice> InstrumentToMaxAndMinPrice =
            new ConcurrentDictionary<string, MaxAndMinPrice>();

        /// <summary>
        /// 记录合约上一笔是多仓还是空仓
        /// </summary>
        public static ConcurrentDictionary<string, EnumPosiDirectionType> InstrumentToLastPosiDirectionType =
            new ConcurrentDictionary<string, EnumPosiDirectionType>();


        /// <summary>
        /// 记录合约开仓时刻的角度
        /// </summary>
        public static ConcurrentDictionary<string, double> InstrumentToOpenAngle =
            new ConcurrentDictionary<string, double>();

        /// </summary>
        public static ConcurrentDictionary<string, List<MarketData>> InstrumentToMarketData =
            new ConcurrentDictionary<string, List<MarketData>>();

        /// <summary>
        /// 合约成交价分布
        /// </summary>
        public static ConcurrentDictionary<string, Dictionary<double, MatchPriceVolume>> InstrumentToMatchPrice =
            new ConcurrentDictionary<string, Dictionary<double, MatchPriceVolume>>();
    }

    public class MatchPriceVolume : IComparable<MatchPriceVolume>
    {
        public double Price;
        public int Volume;

        public int CompareTo(MatchPriceVolume other)
        {
            return Price.CompareTo(other.Price);
        }
    }

    public class ExchangeTime
    {
        private ExchangeTime()
        {
            try
            {
                //无夜盘的品种
                m_sExchsh = "SHFE 9:0:0-10:15:0;10:30:0-11:30:0;13:30:0-15:0:0";
                m_sExchdl = "DCE 9:0:0-10:15:0;10:30:0-11:30:0;13:30:0-15:0:0";
                m_sExchzz = "CZCE 9:0:0-10:15:0;10:30:0-11:30:0;13:30:0-15:0:0";

                //所有的夜盘
                m_sExchzzNight = "RM;FG;MA;SR;TA;ZC;CF; 21:0:0-23:30:0";
                m_sExchdlNight = "i;j;jm;a;m;p;y; 21:0:0-23:30:0";
                m_sExchshNight1 = "ag;au; 21:0:0-23:59:59;0:0:0-2:30:0";
                m_sExchshNight3 = "rb;bu;hc 21:0:0-23:0:0";

                m_mapTradingTime = new Dictionary<string, List<TradingTime>>();
                m_sLog = "交易时间";

                InitDefault();
            }
            catch (Exception ex)
            {
            }
        }

        public static ExchangeTime Instance
        {
            get { return _exchangeTime; }
        }

        // sCate在交易时间段返回true
        public bool IsTradingTime(string category, string exchange)
        {
            var result = true;
            try
            {
                foreach (var kvp in m_mapTradingTime)
                {
                    var s = new List<string>();

                    if (kvp.Key.Contains(";"))
                    {
                        var instrumentIds = kvp.Key.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        s.AddRange(instrumentIds);
                    }

                    if (s.Contains(category) || kvp.Key.Equals(exchange))
                    {
                        var currentDateTime = DateTime.Now;
                        var currentSecond = GetSecFromDateTime(currentDateTime) + Utils.ExchangeTimeOffset;
                        foreach (var time in kvp.Value)
                        {
                            if ((currentSecond >= time.StartSecond) && (currentSecond <= time.EndSecond))
                            {
                                return true;
                            }
                        }
                    }

                    result = false;
                }
            }
            catch (Exception ex)
            {
                return true;
            }

            return result;
        }

        private void ParseFromString(string sLine)
        {
            try
            {
                var sKeyValue = sLine.Split(' ');
                var sKey = sKeyValue[0];
                m_sLog += sKey;

                var sValueString = sKeyValue[1];
                var sValue = sValueString.Split(';');
                foreach (var s in sValue)
                {
                    var s1 = s.Split('-');
                    var dtStart = Convert.ToDateTime(s1[0]);
                    var dtEnd = Convert.ToDateTime(s1[1]);
                    var time = new TradingTime
                    {
                        StartTimeString = dtStart.ToLongTimeString(),
                        EndTimeString = dtEnd.ToLongTimeString(),
                        StartSecond = GetSecFromDateTime(dtStart),
                        EndSecond = GetSecFromDateTime(dtEnd)
                    };
                    if (!m_mapTradingTime.ContainsKey(sKey))
                    {
                        m_mapTradingTime[sKey] = new List<TradingTime>();
                    }
                    m_mapTradingTime[sKey].Add(time);
                    m_sLog += s1[0];
                    m_sLog += s1[1];
                }
            }
            catch (Exception ex)
            {
            }
        }

        public int GetSecFromDateTime(DateTime dt)
        {
            try
            {
                return dt.Hour * 3600 + dt.Minute * 60 + dt.Second;
            }
            catch (Exception ex)
            {
            }
            return 0;
        }

        private void InitDefault()
        {
            try
            {
                if (m_mapTradingTime.Count > 0)
                    m_mapTradingTime.Clear();

                ParseFromString(m_sExchsh);
                ParseFromString(m_sExchdl);
                ParseFromString(m_sExchzz);
                ParseFromString(m_sExchzzNight);
                ParseFromString(m_sExchdlNight);
                ParseFromString(m_sExchshNight1);
                ParseFromString(m_sExchshNight2);
                ParseFromString(m_sExchshNight3);
            }
            catch (Exception ex)
            {
            }
        }

        private static readonly ExchangeTime _exchangeTime = new ExchangeTime();

        private string m_sLog;
        private readonly string m_sExchsh; // 上海
        private readonly string m_sExchdl; // 大连
        private readonly string m_sExchzz; // 郑州

        private readonly string m_sExchzzNight; // TA;SR;CF;RM;ME;MA夜盘
        private readonly string m_sExchdlNight; // p;j;a;b;m;y;jm;i夜盘
        private readonly string m_sExchshNight1; // ag;au夜盘
        private string m_sExchshNight2; // cu;al;zn;pb;rb;hc;bu夜盘
        private string m_sExchshNight3; // ru夜盘

        private Dictionary<string, List<TradingTime>> m_mapTradingTime;
    }

    public class TradingTime
    {
        public string StartTimeString;
        public string EndTimeString;
        public int StartSecond; // 时间段开始时间的秒数
        public int EndSecond;
    }

    public class StopLossPrices
    {
        /// <summary>
        /// 合约
        /// </summary>
        public string Instrument;

        /// <summary>
        /// 当前多仓的持仓成本价
        /// </summary>
        public double CostLong;

        /// <summary>
        /// 当前空仓的持仓成本价
        /// </summary>
        public double CostShort;

        /// <summary>
        /// 多仓止损参考价
        /// </summary>
        public double ForLong;

        /// <summary>
        /// 空仓止损参考价
        /// </summary>
        public double ForShort;
    }

    public class Quote
    {
        public ThostFtdcDepthMarketDataField MarketData;

        public double LastPrice;

        public double AveragePrice;

        /// <summary>
        /// 最新价和平均价的距离,LastPrice - AveragePrice;
        /// </summary>
        public double Distance
        {
            get { return LastPrice - AveragePrice; }
        }
    }

    public class MaxAndMinPrice
    {
        public double MaxPrice;
        public double MinPrice;

        public double Swing
        {
            get { return MaxPrice - MinPrice; }
        }
    }
}
