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


        private Timer _timerOrder = new Timer(2000); //报单回报有时候会有1-2秒的延迟
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

        public static bool noOpening = true;

        private void BuyOrSell(ThostFtdcDepthMarketDataField data)
        {
            try
            {
                var instrumentId = data.InstrumentID;
                var dateTime = Convert.ToDateTime(data.UpdateTime);
                var currentSwing = (data.HighestPrice - data.LowestPrice) / data.PreClosePrice;

                if (dateTime.Hour == 14 && dateTime.Minute == 59 && dateTime.Second >= 50)
                {
                    var bProcessed = false;

                    Utils.WriteLine(string.Format("当前价:{0}，前收价:{1}，开盘价:{2}，最高价:{3}，最低价:{4}", data.LastPrice, data.PreClosePrice, data.OpenPrice, data.HighestPrice, data.LowestPrice), true);

                    if (data.LastPrice < data.PreClosePrice && data.LastPrice < data.OpenPrice && _trader.ContainsPositionByInstrument(instrumentId, EnumPosiDirectionType.Long) && data.LastPrice < data.LowestPrice * 1.01)
                    {
                        _trader.CloseLongPositionByInstrument(data.InstrumentID, "平多", false, 0);
                        Thread.Sleep(2000);
                    }

                    if (data.LastPrice > data.PreClosePrice && data.LastPrice > data.OpenPrice && _trader.ContainsPositionByInstrument(instrumentId, EnumPosiDirectionType.Short) && data.LastPrice > data.HighestPrice * 0.99)
                    {
                        _trader.CloseShortPositionByInstrument(data.InstrumentID, "平空", false, 9999);
                        Thread.Sleep(2000);
                    }

                    if (data.LastPrice / data.OpenPrice > 1.0095 && data.LastPrice > data.HighestPrice * 0.99)
                    {
                        var reason = string.Format("{0}收盘看多信号，开多", instrumentId);
                        if (noOpening)
                        {
                            noOpening = false;
                            _trader.OpenLongPositionByInstrument(instrumentId, reason, 0, true, true, 0, data.UpperLimitPrice);
                            bProcessed = true;
                            noOpening = true;
                        }
                        else
                        {
                            Utils.WriteLine("正在开仓", true);
                        }
                    }

                    if (data.LastPrice / data.OpenPrice < 0.9905 && data.LastPrice < data.LowestPrice * 1.01)
                    {
                        var reason = string.Format("{0}收盘看空信号，开空", instrumentId);
                        if (noOpening)
                        {
                            _trader.OpenShortPositionByInstrument(instrumentId, reason, 9999, true, true, 0, data.LowerLimitPrice);
                            bProcessed = true;
                            noOpening = true;
                        }
                        else
                        {
                            Utils.WriteLine("正在开仓", true);
                        }
                    }

                    if (bProcessed)
                    {
                        _timerOrder.Stop();
                    }

                }
                else
                {
                    var highestPriceFile = Utils.AssemblyPath + "highestPrice.ini";
                    if (_trader.ContainsPositionByInstrument(instrumentId, EnumPosiDirectionType.Long))
                    {
                        //读取持仓高点
                        var highestPrice = data.HighestPrice;

                        if (File.Exists(highestPriceFile))
                        {
                            bool store = false;
                            var sr = new StreamReader(highestPriceFile, Encoding.UTF8);
                            var line = sr.ReadLine();
                            if (line != null)
                            {
                                var storedPrice = Convert.ToDouble(line);
                                if (storedPrice < highestPrice)
                                {
                                    store = true;
                                }
                                else
                                {
                                    highestPrice = storedPrice;
                                }
                            }
                            sr.Close();

                            if (store)
                            {
                                Utils.StorePrice(highestPriceFile, highestPrice);
                            }
                        }
                        else
                        {
                            Utils.StorePrice(highestPriceFile, data.HighestPrice);
                        }

                        if (data.LastPrice / highestPrice < 0.982 && currentSwing > 0.02)
                        {
                            _trader.CloseLongPositionByInstrument(data.InstrumentID, "最高价回落,平多", false, 0);
                        }

                        if (data.LastPrice / data.OpenPrice < 0.982)
                        {
                            _trader.CloseLongPositionByInstrument(data.InstrumentID, "平多", false, 0);
                        }
                    }
                    else
                    {
                        Utils.DeleteStorePrice(highestPriceFile);
                    }

                    var lowestPriceFile = Utils.AssemblyPath + "lowestPrice.ini";
                    if (_trader.ContainsPositionByInstrument(instrumentId, EnumPosiDirectionType.Short))
                    {
                        //读取持仓低点
                        var lowestPrice = data.LowestPrice;

                        if (File.Exists(lowestPriceFile))
                        {
                            bool store = false;
                            var sr = new StreamReader(lowestPriceFile, Encoding.UTF8);
                            var line = sr.ReadLine();
                            if (line != null)
                            {
                                var storedPrice = Convert.ToDouble(line);
                                if (storedPrice > lowestPrice)
                                {
                                    store = true;
                                }
                                else
                                {
                                    lowestPrice = storedPrice;
                                }
                            }
                            sr.Close();

                            if (store)
                            {
                                Utils.StorePrice(lowestPriceFile, lowestPrice);
                            }
                        }
                        else
                        {
                            Utils.StorePrice(lowestPriceFile, data.LowestPrice);
                        }

                        if (data.LastPrice / data.LowestPrice > 1.018 && currentSwing > 0.02)
                        {
                            _trader.CloseShortPositionByInstrument(data.InstrumentID, "最低价上涨,平空", false, 9999);
                        }

                        if (data.LastPrice / data.OpenPrice > 1.018)
                        {
                            _trader.CloseShortPositionByInstrument(data.InstrumentID, "平空", false, 9999);
                        }
                    }
                    else
                    {
                        Utils.DeleteStorePrice(lowestPriceFile);
                    }

                    if (data.LastPrice / data.OpenPrice > 1.014 && data.LastPrice > data.HighestPrice * 0.99)
                    {
                        var reason = string.Format("{0}盘中看多信号，开多", instrumentId);
                        if (noOpening)
                        {
                            noOpening = false;
                            _trader.OpenLongPositionByInstrument(instrumentId, reason, 0, true, true, 0, data.UpperLimitPrice);
                            noOpening = true;
                        }
                        else
                        {
                            Utils.WriteLine("正在开仓", true);
                        }
                    }

                    if (data.LastPrice / data.OpenPrice < 0.986 && data.LastPrice < data.LowestPrice * 1.01)
                    {
                        var reason = string.Format("{0}盘中看空信号，开空", instrumentId);
                        if (noOpening)
                        {
                            noOpening = false;
                            _trader.OpenShortPositionByInstrument(instrumentId, reason, 9999, true, true, 0, data.LowerLimitPrice);
                            noOpening = true;
                        }
                        else
                        {
                            Utils.WriteLine("正在开仓", true);
                        }
                    }
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
