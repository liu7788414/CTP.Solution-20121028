﻿using System;
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

                    Utils.SaveInstrumentTotalPrices();
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
                if (Utils.InstrumentToMarketData.ContainsKey(data.InstrumentID))
                {
                    var marketDataList = Utils.InstrumentToMarketData[data.InstrumentID];

                    if (marketDataList.Count > 0)
                    {
                        var marketData = marketDataList[marketDataList.Count - 1];

                        if (marketData.信号 == 信号.L)
                        {
                            var reason = string.Format("{0}看多信号，开多", data.InstrumentID);
                            _trader.OpenLongPositionByInstrument(data.InstrumentID, reason, 0, true, true, 0);
                        }

                        if (marketData.信号 == 信号.S)
                        {
                            var reason = string.Format("{0}看空信号，开空", data.InstrumentID);
                            _trader.OpenShortPositionByInstrument(data.InstrumentID, reason, 9999, true, true, 0);
                        }
                    }
                }

                if (Utils.InstrumentToStopLossPrices.ContainsKey(data.InstrumentID))
                {
                    var stopLossPrices = Utils.InstrumentToStopLossPrices[data.InstrumentID];

                    var stopLossValue = 10;

                    //多仓止损
                    if (data.LastPrice < stopLossPrices.CostLong - stopLossValue)
                    {
                        var reason = string.Format("{0}从多仓的成本价{1}跌到了绝对止损值{2}以下，即{3}，平掉多仓", data.InstrumentID,
                            stopLossPrices.CostLong, stopLossValue, data.LastPrice);
                        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason);
                    }

                    //空仓止损
                    if (data.LastPrice > stopLossPrices.CostShort + stopLossValue)
                    {
                        var reason = string.Format("{0}从空仓的成本价{1}涨到了绝对止损值{2}以上，即{3}，平掉空仓", data.InstrumentID,
                            stopLossPrices.CostShort, stopLossValue, data.LastPrice);
                        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason);
                    }

                    var longDistance = stopLossPrices.ForLong - stopLossPrices.CostLong;
                    var currentLongDistance = data.LastPrice - stopLossPrices.CostLong;
                    var movingStopLossValueForLong = longDistance * 0.5;

                    //多仓止损
                    if (longDistance > 10 && longDistance <= 20 && data.LastPrice < stopLossPrices.ForLong - movingStopLossValueForLong)
                    {
                        var reason = string.Format("{0}从多仓的最高盈利价{1}跌到了移动止损价{2}以下，即{3}，平掉多仓", data.InstrumentID,
                            stopLossPrices.ForLong, stopLossPrices.ForLong - movingStopLossValueForLong, data.LastPrice);
                        _trader.CloseLongPositionByInstrument(data.InstrumentID, reason);
                    }
                    else
                    {
                        if (longDistance > 20 && currentLongDistance < 10)
                        {
                            _trader.CloseLongPositionByInstrument(data.InstrumentID, "移动止损，平掉多仓");
                        }
                    }

                    var shortDistance = stopLossPrices.CostShort - stopLossPrices.ForShort;
                    var currentShortDistance = stopLossPrices.CostShort - data.LastPrice;
                    var movingStopLossValueForShort = shortDistance * 0.5;

                    //空仓止损
                    if (shortDistance > 10 && shortDistance <= 20 && data.LastPrice > stopLossPrices.ForShort + movingStopLossValueForShort)
                    {
                        var reason = string.Format("{0}从空仓的最高盈利价{1}涨到了移动止损价{2}以上，即{3}，平掉空仓", data.InstrumentID,
                            stopLossPrices.ForShort, stopLossPrices.ForShort + movingStopLossValueForShort, data.LastPrice);
                        _trader.CloseShortPositionByInstrument(data.InstrumentID, reason);
                    }
                    else
                    {
                        if (shortDistance > 20 && currentShortDistance < 10)
                        {
                            _trader.CloseShortPositionByInstrument(data.InstrumentID, "移动止损，平掉空仓");
                        }
                    }

                    if (stopLossPrices.ForLong - stopLossPrices.CostLong >= 20)
                    {
                        _trader.CloseLongPositionByInstrument(data.InstrumentID, "多仓止盈");
                    }

                    if (stopLossPrices.CostShort - stopLossPrices.ForShort >= 20)
                    {
                        _trader.CloseShortPositionByInstrument(data.InstrumentID, "空仓止盈");
                    }

                    var stop = 3;

                    if (data.LastPrice - stopLossPrices.CostLong >= stop)
                    {
                        var key = Utils.GetPositionKey(data.InstrumentID, EnumPosiDirectionType.Long, EnumPositionDateType.Today);
                        if (_trader.PositionFields.ContainsKey(key))
                        {
                            var position = _trader.PositionFields[key];
                            _trader.ReqOrderInsert(data.InstrumentID, EnumDirectionType.Sell, stopLossPrices.CostLong + stop, position.Position, EnumOffsetFlagType.CloseToday, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "立即平多仓");
                        }
                    }

                    if (stopLossPrices.CostShort - data.LastPrice >= stop)
                    {
                        var key = Utils.GetPositionKey(data.InstrumentID, EnumPosiDirectionType.Short, EnumPositionDateType.Today);
                        if (_trader.PositionFields.ContainsKey(key))
                        {
                            var position = _trader.PositionFields[key];
                            _trader.ReqOrderInsert(data.InstrumentID, EnumDirectionType.Buy, stopLossPrices.CostShort - stop, position.Position, EnumOffsetFlagType.CloseToday, EnumTimeConditionType.GFD, EnumVolumeConditionType.AV, "立即平空仓");
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
