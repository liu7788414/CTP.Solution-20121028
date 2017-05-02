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
                #region 平仓策略

                if (Utils.InstrumentToStopLossPrices.ContainsKey(data.InstrumentID))
                {
                    var stopLossPrices = Utils.InstrumentToStopLossPrices[data.InstrumentID];

                    var stopLossValue = data.LastPrice * Utils.SwingLimit * 1.5;

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

                    var minuteByMinute = Utils.InstrumentToMinuteByMinuteChart[data.InstrumentID];

                    Tuple<bool, double, double> isPointingUpMinuteLong2 = new Tuple<bool, double, double>(false, 0, 0);
                    Tuple<bool, double, double> isPointingDownMinuteLong2 = new Tuple<bool, double, double>(false, 0, 0);

                    if (minuteByMinute.Count >= Utils.MinuteByMinuteSizeLong)
                    {
                        var count = minuteByMinute.Count;

                        var minuteByMinuteQuotesLong = new List<double>();
                        for (var i = count - Utils.MinuteByMinuteSizeLong; i < count; i++)
                        {
                            if (minuteByMinute[i] != null)
                            {
                                minuteByMinuteQuotesLong.Add(minuteByMinute[i].Item2.LastPrice);
                            }
                        }

                        isPointingUpMinuteLong2 = MathUtils.IsPointingUp(Utils.MinuteLongXData,
                            minuteByMinuteQuotesLong, MathUtils.Slope2);
                        isPointingDownMinuteLong2 = MathUtils.IsPointingDown(Utils.MinuteLongXData,
                            minuteByMinuteQuotesLong, MathUtils.Slope2);

                        Utils.WriteLine(string.Format("检查当前角度{0}", isPointingUpMinuteLong2.Item3));
                    }

                    double openTrendStartPoint = 0;

                    //从多仓的最高盈利跌了一定幅度，平掉多仓，保护盈利，忽略掉小波动
                    if (stopLossPrices.CostLong > 0 && stopLossPrices.ForLong > stopLossPrices.CostLong &&
                        stopLossPrices.ForLong > data.LastPrice)
                    {
                        var keyLongPosition = Utils.GetOpenTrendStartPointKey(data.InstrumentID,
                            EnumPosiDirectionType.Long);

                        if (Utils.InstrumentToOpenTrendStartPoint.ContainsKey(keyLongPosition))
                        {
                            openTrendStartPoint = Utils.InstrumentToOpenTrendStartPoint[keyLongPosition];
                        }

                        var highestDistance = Math.Abs(stopLossPrices.ForLong - stopLossPrices.CostLong);
                        var currentDistance = Math.Abs(data.LastPrice - stopLossPrices.CostLong);
                        var trendDistance = Math.Abs(stopLossPrices.CostLong - openTrendStartPoint);

                        var keyLongAngle = Utils.GetOpenPositionKey(data.InstrumentID, EnumDirectionType.Buy);
                        double currentAngle = 0;
                        if (Utils.InstrumentToOpenAngle.ContainsKey(keyLongAngle))
                        {
                            currentAngle = Utils.InstrumentToOpenAngle[keyLongAngle];
                        }

                        if (currentDistance >= 1 && isPointingUpMinuteLong2.Item3 < currentAngle)
                        {
                            var reason = string.Format("{0}的多仓开仓角度开始减小，平掉多仓", data.InstrumentID);
                            _trader.CloseLongPositionByInstrument(data.InstrumentID, reason);
                        }

                        if (isPointingUpMinuteLong2.Item3 > currentAngle)
                        {
                            Utils.InstrumentToOpenAngle[keyLongAngle] = currentAngle;
                        }

                        if (highestDistance >= 5)
                        {
                            _trader.CloseLongPositionByInstrument(data.InstrumentID, "多仓止盈");
                        }
                    }

                    //从空仓的最高盈利跌了一半，平掉空仓，保护盈利，忽略掉小波动
                    if (stopLossPrices.CostShort > 0 && stopLossPrices.ForShort < stopLossPrices.CostShort &&
                        stopLossPrices.ForShort < data.LastPrice)
                    {
                        var keyShortPosition = Utils.GetOpenTrendStartPointKey(data.InstrumentID,
                            EnumPosiDirectionType.Short);

                        if (Utils.InstrumentToOpenTrendStartPoint.ContainsKey(keyShortPosition))
                        {
                            openTrendStartPoint = Utils.InstrumentToOpenTrendStartPoint[keyShortPosition];
                        }

                        var highestDistance = Math.Abs(stopLossPrices.ForShort - stopLossPrices.CostShort);
                        var currentDistance = Math.Abs(data.LastPrice - stopLossPrices.CostShort);
                        var trendDistance = Math.Abs(openTrendStartPoint - stopLossPrices.CostShort);

                        var keyShortAngle = Utils.GetOpenPositionKey(data.InstrumentID, EnumDirectionType.Sell);
                        double currentAngle = 0;
                        if (Utils.InstrumentToOpenAngle.ContainsKey(keyShortAngle))
                        {
                            currentAngle = Utils.InstrumentToOpenAngle[keyShortAngle];
                        }

                        if (currentDistance >= 1 && isPointingDownMinuteLong2.Item3 > currentAngle)
                        {
                            var reason = string.Format("{0}的空仓开仓角度开始减小，平掉空仓", data.InstrumentID);
                            _trader.CloseShortPositionByInstrument(data.InstrumentID, reason);
                        }

                        if (isPointingDownMinuteLong2.Item3 < currentAngle)
                        {
                            Utils.InstrumentToOpenAngle[keyShortAngle] = currentAngle;
                        }

                        if (highestDistance >= 5)
                        {
                            _trader.CloseShortPositionByInstrument(data.InstrumentID, "空仓止盈");
                        }
                    }
                }

                //接近涨停价，平掉空仓
                var upperLimitRange = (data.UpperLimitPrice + data.LowerLimitPrice) / 2;
                var lowerLimitRange = upperLimitRange;

                if (data.LastPrice > data.PreSettlementPrice + upperLimitRange * Utils.LimitCloseRange)
                {
                    var reason = string.Format("{0}最新价{1}上涨到了涨停价{2}的{3}以上，平掉空仓", data.InstrumentID, data.LastPrice,
                        data.UpperLimitPrice, Utils.LimitCloseRange);
                    _trader.CloseShortPositionByInstrument(data.InstrumentID, reason);
                }

                //接近跌停价，平掉多仓
                if (data.LastPrice < data.PreSettlementPrice - lowerLimitRange * Utils.LimitCloseRange)
                {
                    var reason = string.Format("{0}最新价{1}下跌到了跌停价{2}的{3}以下，平掉多仓", data.InstrumentID, data.LastPrice,
                        data.LowerLimitPrice, Utils.LimitCloseRange);
                    _trader.CloseLongPositionByInstrument(data.InstrumentID, reason);
                }

                #endregion

                #region 开仓策略

                if (Utils.InstrumentToMinuteByMinuteChart.ContainsKey(data.InstrumentID))
                {
                    lock (Utils.Locker2)
                    {
                        OpenStrategy(data, false);
                    }     
                }

                #endregion
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        //private Tuple<bool, bool> MinuteAllDirection(string instrumentId)
        //{
        //    if (Utils.InstrumentToMinuteByMinuteChart.ContainsKey(instrumentId))
        //    {
        //        var minuteByMinute = Utils.InstrumentToMinuteByMinuteChart[instrumentId];

        //        //当前大趋势,大趋势向上只开多仓，大趋势向下，只开空仓
        //        var minuteAllXData = new List<double>();
        //        var minuteAllYData = new List<double>();
        //        for (var i = 0; i < minuteByMinute.Count; i++)
        //        {
        //            if (minuteByMinute[i] != null)
        //            {
        //                minuteAllXData.Add(i);
        //                minuteAllYData.Add(minuteByMinute[i].Item2.LastPrice);
        //            }
        //        }

        //        var isPointingUpMinuteAll = MathUtils.IsPointingUp(minuteAllXData, minuteAllYData, 0.4);
        //        var isPointingDownMinuteAll = MathUtils.IsPointingDown(minuteAllXData, minuteAllYData, 0.4);

        //        return new Tuple<bool, bool>(isPointingUpMinuteAll.Item1, isPointingDownMinuteAll.Item1);
        //    }

        //    return new Tuple<bool, bool>(false, false);
        //}

        /// <summary>
        /// 新策略，比最低(最高)趋势点高于(低于)一定比例，开多（空）仓
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ma"></param>
        private void OpenStrategy(ThostFtdcDepthMarketDataField data, bool ma)
        {
            try
            {
                var minuteByMinute = Utils.InstrumentToMinuteByMinuteChart[data.InstrumentID];

                if (minuteByMinute.Count >= Utils.MinuteByMinuteSizeLong)
                {
                    var sizeHalf = Utils.MinuteByMinuteSizeLong / 2;
                    var count = minuteByMinute.Count;

                    var minuteByMinuteQuotesLong = new List<double>();
                    for (var i = count - Utils.MinuteByMinuteSizeLong; i < count; i++)
                    {
                        if (minuteByMinute[i] != null)
                        {
                            minuteByMinuteQuotesLong.Add(minuteByMinute[i].Item2.LastPrice);
                        }
                    }

                    var minuteByMinuteQuotesHalf = new List<double>();
                    for (var i = count - sizeHalf; i < count; i++)
                    {
                        if (minuteByMinute[i] != null)
                        {
                            minuteByMinuteQuotesHalf.Add(minuteByMinute[i].Item2.LastPrice);
                        }
                    }

                    var isPointingUpMinuteLong2 = MathUtils.IsPointingUp(Utils.MinuteLongXData,
                        minuteByMinuteQuotesLong, MathUtils.Slope2);
                    var isPointingDownMinuteLong2 = MathUtils.IsPointingDown(Utils.MinuteLongXData,
                        minuteByMinuteQuotesLong, MathUtils.Slope2);

                    Utils.WriteLine(string.Format("当前长角度{0}", isPointingUpMinuteLong2.Item3));
                    var minuteHalfXData = new List<double>();

                    for (var i = 0; i < sizeHalf; i++)
                    {
                        minuteHalfXData.Add(i);
                    }

                    var isPointingUpMinuteHalf = MathUtils.IsPointingUp(minuteHalfXData, minuteByMinuteQuotesHalf,
                        MathUtils.Slope2);
                    var isPointingDownMinuteHalf = MathUtils.IsPointingDown(minuteHalfXData, minuteByMinuteQuotesHalf,
                        MathUtils.Slope2);
                    Utils.WriteLine(string.Format("当前短角度{0}", isPointingUpMinuteHalf.Item3));

                    //开多仓
                    if (isPointingUpMinuteLong2.Item1 && isPointingUpMinuteHalf.Item1)
                    {
                        var min = minuteByMinuteQuotesLong.Min(p => p);
                        var keyMissedLongOpenTrendStartPoint = Utils.GetOpenPositionKey(data.InstrumentID,
                            EnumDirectionType.Buy);
                        if (Utils.InstrumentToMissedOpenTrendStartPoint.ContainsKey(keyMissedLongOpenTrendStartPoint))
                        {
                            min = Math.Min(min,
                                Utils.InstrumentToMissedOpenTrendStartPoint[keyMissedLongOpenTrendStartPoint]);
                        }
                        else
                        {
                            Utils.SetMissedOpenStartPoint(data.InstrumentID, EnumPosiDirectionType.Long, min);
                        }

                        if (data.LastPrice > min + data.LastPrice * Utils.SwingLimit)
                        {
                            var reason = string.Format("{0}最近趋势向上{1},且高于多仓启动点{2},开出多仓,开仓启动点{3},开仓正切{4},开仓角度{5}",
                                data.InstrumentID,
                                isPointingUpMinuteLong2, data.LastPrice * Utils.SwingLimit, min,
                                isPointingUpMinuteLong2.Item2, isPointingUpMinuteLong2.Item3);

                            if (Utils.InstrumentToLastPosiDirectionType.ContainsKey(data.InstrumentID) &&
                                Utils.InstrumentToLastPosiDirectionType[data.InstrumentID] ==
                                EnumPosiDirectionType.Short)
                            {
                                Utils.WriteLine("反向强制开多仓", true);
                                _trader.OpenLongPositionByInstrument(data.InstrumentID, reason, min, true, true, isPointingUpMinuteLong2.Item3);
                            }
                            else
                            {
                                _trader.OpenLongPositionByInstrument(data.InstrumentID, reason, min, true, false, isPointingUpMinuteLong2.Item3);
                            }
                        }

                        return;
                    }


                    if (!isPointingUpMinuteHalf.Item1 && isPointingUpMinuteLong2.Item1)
                    {
                        Utils.WriteLine("虽然长趋势向上，但是中趋势不是，不开多仓", true);
                    }

                    //开空仓
                    if (isPointingDownMinuteLong2.Item1 && isPointingDownMinuteHalf.Item1)
                    {
                        var max = minuteByMinuteQuotesLong.Max(p => p);
                        var keyMissedShortOpenTrendStartPoint = Utils.GetOpenPositionKey(data.InstrumentID,
                            EnumDirectionType.Sell);
                        if (Utils.InstrumentToMissedOpenTrendStartPoint.ContainsKey(keyMissedShortOpenTrendStartPoint))
                        {
                            max = Math.Max(max,
                                Utils.InstrumentToMissedOpenTrendStartPoint[keyMissedShortOpenTrendStartPoint]);
                        }
                        else
                        {
                            Utils.SetMissedOpenStartPoint(data.InstrumentID, EnumPosiDirectionType.Short, max);
                        }

                        if (data.LastPrice < max - data.LastPrice * Utils.SwingLimit)
                        {
                            var reason = string.Format("{0}最近长趋势向下{1},且低于空仓启动点{2},开出空仓,开仓启动点{3},开仓正切{4},开仓角度{5}",
                                data.InstrumentID,
                                isPointingDownMinuteLong2, data.LastPrice * Utils.SwingLimit, max,
                                isPointingDownMinuteLong2.Item2, isPointingDownMinuteLong2.Item3);


                            if (Utils.InstrumentToLastPosiDirectionType.ContainsKey(data.InstrumentID) &&
                                Utils.InstrumentToLastPosiDirectionType[data.InstrumentID] == EnumPosiDirectionType.Long)
                            {
                                Utils.WriteLine("反向强制开空仓", true);
                                _trader.OpenShortPositionByInstrument(data.InstrumentID, reason, max, true, true, isPointingDownMinuteLong2.Item3);
                            }
                            else
                            {
                                _trader.OpenShortPositionByInstrument(data.InstrumentID, reason, max, true, false, isPointingDownMinuteLong2.Item3);
                            }
                        }

                        return;
                    }

                    if (!isPointingDownMinuteHalf.Item1 && isPointingDownMinuteLong2.Item1)
                    {
                        Utils.WriteLine("虽然长趋势向下，但是中趋势不是，不开空仓", true);
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
