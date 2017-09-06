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

        private Timer _timerOrder = new Timer(250); //报单回报有时候会有1-2秒的延迟
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

        private void CheckOpenOrClose()
        {
            try
            {
                lock (Utils.Locker)
                {
                    foreach (var kv in Utils.InstrumentToQuotes)
                    {
                        var depthMarketDataField = kv.Value[kv.Value.Count - 1];
                        if (depthMarketDataField != null)
                        {
                            BuyOrSell(depthMarketDataField);
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
            CheckOpenOrClose();
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

        private void BuyOrSell(ThostFtdcDepthMarketDataField data)
        {
            try
            {
                if (Utils.InstrumentToStopLossPrices.ContainsKey(data.InstrumentID))
                {
                    var stopLossPrices = Utils.InstrumentToStopLossPrices[data.InstrumentID];

                    var stopLossValue = Utils.绝对止损点数;
                    var averagePrice = Utils.GetAveragePrice(data);
                    //多仓止损
                    if (data.LastPrice < stopLossPrices.CostLong - stopLossValue && data.LastPrice < averagePrice)
                    {
                        var reason = string.Format("{0}从多仓的成本价{1}跌到了绝对止损值{2}以下，即{3}，平掉多仓", data.InstrumentID,
                            stopLossPrices.CostLong, stopLossValue, data.LastPrice);
                        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                    }

                    //空仓止损
                    if (data.LastPrice > stopLossPrices.CostShort + stopLossValue && data.LastPrice > averagePrice)
                    {
                        var reason = string.Format("{0}从空仓的成本价{1}涨到了绝对止损值{2}以上，即{3}，平掉空仓", data.InstrumentID,
                            stopLossPrices.CostShort, stopLossValue, data.LastPrice);
                        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                    }

                    var longDistance = stopLossPrices.ForLong - stopLossPrices.CostLong;
                    var currentLongDistance = data.LastPrice - stopLossPrices.CostLong;
                    var movingStopLossValueForLong = longDistance * 0.5;

                    //检查持仓是否超时还未盈利
                    var dtNow = DateTime.Now;
                    var timeSpan = dtNow - Utils.PositionTime;

                    if (timeSpan > Utils.timeSpan5)
                    {
                        if (currentLongDistance < -10 && data.LastPrice < averagePrice)
                        {
                            var reason = string.Format("{0}止损，平掉多仓", data.InstrumentID);
                            _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                        }
                    }

                    //if (timeSpan > Utils.timeSpan10)
                    //{
                    //    if (currentLongDistance < -3 && data.LastPrice < averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉多仓", data.InstrumentID);
                    //        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                    //    }
                    //}

                    //if (timeSpan > Utils.timeSpan15)
                    //{
                    //    if (currentLongDistance < -1 && data.LastPrice < averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉多仓", data.InstrumentID);
                    //        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                    //    }
                    //}

                    //if (timeSpan > Utils.timeSpan20)
                    //{
                    //    if (currentLongDistance < 0 && data.LastPrice < averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉多仓", data.InstrumentID);
                    //        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                    //    }
                    //}

                    //if (timeSpan > Utils.timeSpan30)
                    //{
                    //    if (currentLongDistance < 10 && data.LastPrice < averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉多仓", data.InstrumentID);
                    //        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                    //    }
                    //}

                    if (timeSpan > Utils.timeSpan60)
                    {
                        if (data.LastPrice < averagePrice)
                        {
                            var reason = string.Format("{0}超时未盈利，平掉多仓", data.InstrumentID);
                            _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                        }
                    }

                    //if (timeSpan > Utils.timeSpan120)
                    //{
                    //    if (currentLongDistance < 40 && data.LastPrice < averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉多仓", data.InstrumentID);
                    //        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                    //    }
                    //}

                    //if (timeSpan > Utils.timeSpan180)
                    //{
                    //    if (currentLongDistance < 60 && data.LastPrice < averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉多仓", data.InstrumentID);
                    //        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                    //    }
                    //}

                    var shortDistance = stopLossPrices.CostShort - stopLossPrices.ForShort;
                    var currentShortDistance = stopLossPrices.CostShort - data.LastPrice;
                    var movingStopLossValueForShort = shortDistance * 0.5;

                    if (timeSpan > Utils.timeSpan5)
                    {
                        if (currentShortDistance < -10 && data.LastPrice > averagePrice)
                        {
                            var reason = string.Format("{0}止损，平掉空仓", data.InstrumentID);
                            _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                        }
                    }

                    //if (timeSpan > Utils.timeSpan10)
                    //{
                    //    if (currentShortDistance < -3 && data.LastPrice > averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉空仓", data.InstrumentID);
                    //        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                    //    }
                    //}

                    //if (timeSpan > Utils.timeSpan15)
                    //{
                    //    if (currentShortDistance < -1 && data.LastPrice > averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉空仓", data.InstrumentID);
                    //        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                    //    }
                    //}

                    //if (timeSpan > Utils.timeSpan20)
                    //{
                    //    if (currentShortDistance < 0 && data.LastPrice > averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉空仓", data.InstrumentID);
                    //        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                    //    }
                    //}

                    //if (timeSpan > Utils.timeSpan30)
                    //{
                    //    if (currentLongDistance < 10 && data.LastPrice > averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉空仓", data.InstrumentID);
                    //        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                    //    }
                    //}

                    if (timeSpan > Utils.timeSpan60)
                    {
                        if (data.LastPrice > averagePrice)
                        {
                            var reason = string.Format("{0}超时未盈利，平掉空仓", data.InstrumentID);
                            _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                        }
                    }

                    //if (timeSpan > Utils.timeSpan120)
                    //{
                    //    if (currentLongDistance < 40 && data.LastPrice > averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉空仓", data.InstrumentID);
                    //        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                    //    }
                    //}

                    //if (timeSpan > Utils.timeSpan180)
                    //{
                    //    if (currentLongDistance < 60 && data.LastPrice > averagePrice)
                    //    {
                    //        var reason = string.Format("{0}超时未盈利，平掉空仓", data.InstrumentID);
                    //        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                    //    }
                    //}

                    if (data.LastPrice >= (data.UpperLimitPrice * 0.995))
                    {
                        var reason = string.Format("{0}接近涨停，平掉空仓", data.InstrumentID);
                        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                    }

                    if (data.LastPrice <= (data.LowerLimitPrice * 1.005))
                    {
                        var reason = string.Format("{0}接近跌停，平掉多仓", data.InstrumentID);
                        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                    }

                    //if (data.LastPrice.Equals(data.UpperLimitPrice))
                    //{
                    //    var reason = string.Format("{0}涨停，平掉多仓", data.InstrumentID);
                    //    _trader.CloseLongPositionByInstrument(data.InstrumentID, reason, false, 0);
                    //}

                    //if (data.LastPrice.Equals(data.LowerLimitPrice))
                    //{
                    //    var reason = string.Format("{0}跌停，平掉空仓", data.InstrumentID);
                    //    _trader.CloseShortPositionByInstrument(data.InstrumentID, reason, false, 99999);
                    //}
                }
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
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
