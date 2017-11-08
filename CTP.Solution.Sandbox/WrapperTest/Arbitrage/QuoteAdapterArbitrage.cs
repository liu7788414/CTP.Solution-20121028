using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CTP;
using SendMail;
using Timer = System.Timers.Timer;

namespace WrapperTest
{
    public class QuoteAdapter : CTPMDAdapter
    {
        public static int RequestId = 1;

        private int _frontId;
        private int _sessionId;

        private List<string> _subscribedQuotes = new List<string>();

        public List<string> SubscribedQuotes
        {
            get { return _subscribedQuotes; }
            set { _subscribedQuotes = value; }
        }

        private string _brokerId;

        public string BrokerId
        {
            get { return _brokerId; }
            set { _brokerId = value; }
        }

        private string _investorId;

        public string InvestorId
        {
            get { return _investorId; }
            set { _investorId = value; }
        }

        private string _password;

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        private string[] _front;

        public string[] Front
        {
            get { return _front; }
            set { _front = value; }
        }

        private bool _isReady;

        public bool IsReady
        {
            get { return _isReady; }
            set { _isReady = value; }
        }

        private TraderAdapter _trader;

        public TraderAdapter Trader
        {
            get { return _trader; }
            set { _trader = value; }
        }


        private Timer _timerOrder = new Timer(500); //报单回报有时候会有1-2秒的延迟
        //private Timer _timerSaveStopLossPrices = new Timer(1000); //每隔一段时间保存当前的止损参考价，供下次启动时读取

        public QuoteAdapter(TraderAdapter trader)
        {
            _timerOrder.Elapsed += timer_Elapsed;
            //_timerSaveStopLossPrices.Elapsed += _timerSaveStopLossPrices_Elapsed;

            _trader = trader;
            OnFrontConnected += QuoteAdapter_OnFrontConnected;
            OnRspUserLogin += QuoteAdapter_OnRspUserLogin;
            OnFrontDisconnected += QuoteAdapter_OnFrontDisconnected;
            OnRspError += QuoteAdapter_OnRspError;
            OnRspSubMarketData += QuoteAdapter_OnRspSubMarketData;
            OnRspUnSubMarketData += QuoteAdapter_OnRspUnSubMarketData;
            OnRspUserLogout += QuoteAdapter_OnRspUserLogout;
            OnRtnDepthMarketData += QuoteAdapter_OnRtnDepthMarketData;
        }

        public void StartTimer()
        {
            Utils.WriteLine("打开检查开平仓的定时器...", true);
            _timerOrder.Start();
        }

        private double averagePriceDiff = 0;
        private double lowestPriceDiff = 9999;
        private double highestPriceDiff = -9999;
        private Dictionary<double, int> priceDiffToCount = new Dictionary<double, int>();

        private void CheckOpenOrClose()
        {
            try
            {
                if (Utils.InstrumentToLastTick.ContainsKey(Utils.A1) && Utils.InstrumentToLastTick.ContainsKey(Utils.A2) && Utils.IsInInstrumentTradingTime(Utils.A1) && Utils.IsInInstrumentTradingTime(Utils.A2))
                {
                    var A1LastTick = Utils.InstrumentToLastTick[Utils.A1];
                    var A2LastTick = Utils.InstrumentToLastTick[Utils.A2];

                    var a1 = Utils.InstrumentToLastTick[Utils.A1].LastPrice;
                    var a2 = Utils.InstrumentToLastTick[Utils.A2].LastPrice;
                    var currentPriceDiff = a1 - a2;

                    if (currentPriceDiff < lowestPriceDiff)
                    {
                        lowestPriceDiff = currentPriceDiff;
                    }

                    if (currentPriceDiff > highestPriceDiff)
                    {
                        highestPriceDiff = currentPriceDiff;
                    }

                    if (priceDiffToCount.ContainsKey(currentPriceDiff))
                    {
                        priceDiffToCount[currentPriceDiff]++;
                    }
                    else
                    {
                        priceDiffToCount[currentPriceDiff] = 1;
                    }


                    double total = 0;
                    double count = 0;
                    foreach(var kv in priceDiffToCount)
                    {
                        total += kv.Key * kv.Value;
                        count += kv.Value;
                    }

                    averagePriceDiff = total / count;

                    Utils.WriteLine(string.Format("价差:{0:N2} - {1:N2} = {2:N2}, 平均价差:{3:N2}, 最低最高价差:{4:N2}, {5:N2}", a1, a2, currentPriceDiff, averagePriceDiff, lowestPriceDiff, highestPriceDiff), true);

                    if (a1 - a2 >= Utils.价差上限)  //卖a1买a2
                    {
                        if (!_trader.ContainsPositionByInstrument(Utils.A1, EnumPosiDirectionType.Short))
                        {
                            _trader.ReqOrderInsert(Utils.A1, EnumDirectionType.Sell, A1LastTick.LowerLimitPrice, Utils.OpenVolumePerTime, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "卖a1");
                            _trader.ReqOrderInsert(Utils.A2, EnumDirectionType.Buy, A2LastTick.UpperLimitPrice, Utils.OpenVolumePerTime, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "买a2");
                        }
                    }

                    if (a1 - a2 <= Utils.价差下限) //买a1卖a2
                    {
                        if (!_trader.ContainsPositionByInstrument(Utils.A1, EnumPosiDirectionType.Long))
                        {
                            _trader.ReqOrderInsert(Utils.A1, EnumDirectionType.Buy, A1LastTick.UpperLimitPrice, Utils.OpenVolumePerTime, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "买a1");
                            _trader.ReqOrderInsert(Utils.A2, EnumDirectionType.Sell, A2LastTick.LowerLimitPrice, Utils.OpenVolumePerTime, EnumOffsetFlagType.Open, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "卖a2");
                        }
                    }

                    if (a1 - a2 <= Utils.价差上限 - 12)  //平a2多a1空
                    {
                        if (_trader.ContainsPositionByInstrument(Utils.A2, EnumPosiDirectionType.Long))
                        {
                            _trader.CloseLongPositionByInstrument(Utils.A2, "平a2多", false, 0);
                            _trader.CloseShortPositionByInstrument(Utils.A1, "平a1空", false, 9999);
                        }                  
                    }

                    if (a1 - a2 >= Utils.价差下限 + 12) //平a1多a2空
                    {
                        if (_trader.ContainsPositionByInstrument(Utils.A1, EnumPosiDirectionType.Long))
                        {
                            _trader.CloseLongPositionByInstrument(Utils.A1, "平a1多", false, 0);
                            _trader.CloseShortPositionByInstrument(Utils.A2, "平a2空", false, 9999);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

        }

        /// <summary>
        /// 每隔一段时间检查是不是要报单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (Utils.Locker)
            {
                CheckOpenOrClose();
            }
        }

        public void Connect()
        {
            foreach (var server in _front)
            {
                RegisterFront(server);
            }

            Init();
            Join();
        }

        private void QuoteAdapter_OnRtnDepthMarketData(ThostFtdcDepthMarketDataField pDepthMarketData)
        {
            try
            {
                lock (Utils.LockerQuote)
                {
                    var dtNow = DateTime.Now;

                    if (Utils.CurrentChannel != ChannelType.模拟24X7 &&
                        ((dtNow.Hour >= 0 && dtNow.Hour <= 8) || (dtNow.Hour >= 16 && dtNow.Hour <= 20) ||
                         dtNow.DayOfWeek == DayOfWeek.Saturday || dtNow.DayOfWeek == DayOfWeek.Sunday)) //排除无效时段的行情
                    {
                        return;
                    }

                    Utils.WriteQuote(pDepthMarketData);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

        }

        private void QuoteAdapter_OnRspUserLogout(ThostFtdcUserLogoutField pUserLogout, ThostFtdcRspInfoField pRspInfo,
            int nRequestId, bool bIsLast)
        {
            try
            {
                Utils.ReportError(pRspInfo, "登出账号回报错误");

                if (pUserLogout != null)
                {
                    Utils.WriteLine("登出回报", true);
                    Utils.OutputField(pUserLogout);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

        }

        private void QuoteAdapter_OnRspUnSubMarketData(ThostFtdcSpecificInstrumentField pSpecificInstrument,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            Utils.ReportError(pRspInfo, "退订行情回报错误");

            if (pSpecificInstrument != null)
            {
                Utils.OutputField(pSpecificInstrument);
            }
        }

        private void QuoteAdapter_OnRspSubMarketData(ThostFtdcSpecificInstrumentField pSpecificInstrument,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            Utils.ReportError(pRspInfo, "订阅行情回报错误");

            if (pSpecificInstrument != null)
            {
                Utils.OutputField(pSpecificInstrument);
            }
        }

        private void QuoteAdapter_OnRspError(ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            Utils.ReportError(pRspInfo, "错误报告");

            if (pRspInfo != null)
            {
                Utils.OutputField(pRspInfo);
            }
        }

        private void QuoteAdapter_OnFrontDisconnected(int nReason)
        {
            Utils.WriteLine(nReason.ToString());
            Email.SendMail("错误：行情断线,尝试重连...", DateTime.Now.ToString(CultureInfo.InvariantCulture),
                Utils.IsMailingEnabled);
            _isReady = false;
        }

        public void QuoteAdapter_OnRspUserLogin(ThostFtdcRspUserLoginField pRspUserLogin,
            ThostFtdcRspInfoField pRspInfo, int nRequestId, bool bIsLast)
        {
            try
            {
                Utils.ReportError(pRspInfo, "行情登录回报错误");

                if (bIsLast && Utils.IsCorrectRspInfo(pRspInfo))
                {
                    _isReady = true;
                    var temp =
                        string.Format(
                            "行情登录回报:经纪公司代码:{0},郑商所时间:{1},大商所时间:{2},中金所时间:{3},前置编号:{4},登录成功时间:{5},最大报单引用:{6},会话编号:{7},上期所时间:{8},交易系统名称:{9},交易日:{10},用户代码:{11}",
                            pRspUserLogin.BrokerID, pRspUserLogin.CZCETime, pRspUserLogin.DCETime,
                            pRspUserLogin.FFEXTime,
                            pRspUserLogin.FrontID, pRspUserLogin.LoginTime, pRspUserLogin.MaxOrderRef,
                            pRspUserLogin.SessionID, pRspUserLogin.SHFETime, pRspUserLogin.SystemName,
                            pRspUserLogin.TradingDay, pRspUserLogin.UserID);

                    Utils.WriteLine(temp, true);

                    _frontId = pRspUserLogin.FrontID;
                    _sessionId = pRspUserLogin.SessionID;

                    //行情重连的时候，重新订阅需要的行情
                    if (SubscribedQuotes.Count > 0)
                    {
                        SubscribeMarketData(SubscribedQuotes.ToArray());
                    }
                }
                else
                {
                    Utils.WriteLine("行情登录失败", true);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

        }

        private void QuoteAdapter_OnFrontConnected()
        {
            try
            {
                var loginField = new ThostFtdcReqUserLoginField
                {
                    BrokerID = _brokerId,
                    UserID = _investorId,
                    Password = _password
                };

                if (_isReady)
                {
                    _isReady = false;
                    Utils.WriteLine("行情重连中...", true);
                }

                ReqUserLogin(loginField, RequestId++);
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }

        }
    }
}
