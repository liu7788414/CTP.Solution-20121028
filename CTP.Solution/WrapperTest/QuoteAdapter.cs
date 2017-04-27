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
                    Utils.WriteLine("检查是否需要开平仓...");

                    foreach (var kv in Utils.InstrumentToQuotes)
                    {
                        var movingAverageCount = 10;

                        if (kv.Value.Count >= movingAverageCount)
                        {
                            var listTemp = new List<ThostFtdcDepthMarketDataField>();
                            for (var i = kv.Value.Count - 1; i >= kv.Value.Count - movingAverageCount; i--)
                            {
                                //行情里面会有空值
                                if (kv.Value[i] != null && kv.Value[i].LastPrice >= kv.Value[i].LowerLimitPrice &&
                                    kv.Value[i].LastPrice <= kv.Value[i].UpperLimitPrice)
                                {
                                    listTemp.Add(kv.Value[i]);
                                }
                            }

                            var depthMarketDataField = new ThostFtdcDepthMarketDataField
                            {
                                InstrumentID = kv.Key,
                                LastPrice = listTemp.Average(p => p.LastPrice),
                                AveragePrice = listTemp.Average(p => Utils.GetAveragePrice(p)),
                                UpperLimitPrice = listTemp.Average(p => p.UpperLimitPrice),
                                LowerLimitPrice = listTemp.Average(p => p.LowerLimitPrice),
                                PreSettlementPrice = listTemp.Average(p => p.PreSettlementPrice),
                                PreClosePrice = listTemp.Average(p => p.PreClosePrice)
                            };

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

                    //var directions = MinuteAllDirection(data.InstrumentID);

                    //if (directions.Item1)//大趋势向上，平空仓
                    //{
                    //    _trader.CloseShortPositionByInstrument(data.InstrumentID, "大趋势向上，平掉空仓");
                    //}

                    //if (directions.Item2)//大趋势向下，平多仓
                    //{
                    //    _trader.CloseLongPositionByInstrument(data.InstrumentID, "大趋势向下，平掉多仓");
                    //}

                    var stopLossValue = data.LastPrice * Utils.SwingLimit * 2;

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

                        if (highestDistance > data.LastPrice*Utils.HighestDistanceConsiderLimit &&
                            highestDistance > currentDistance &&
                            currentDistance <= Utils.CurrentDistanceToHighestDistanceRatioLimit*highestDistance)
                        {
                            var reason = string.Format("{0}从多仓的最高趋势点{1}跌到了{2}以下，即{3}，平掉多仓，多仓成本价{4}，多仓最高盈利价{5}",
                                data.InstrumentID, highestDistance + trendDistance,
                                Utils.CurrentDistanceToHighestDistanceRatioLimit, currentDistance + trendDistance,
                                stopLossPrices.CostLong, stopLossPrices.ForLong);
                            _trader.CloseLongPositionByInstrument(data.InstrumentID, "移动止损平多仓");
                        }

                        if (highestDistance > data.LastPrice*Utils.StopProfitRatio)
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

                        if (highestDistance > data.LastPrice*Utils.HighestDistanceConsiderLimit &&
                            highestDistance > currentDistance &&
                            currentDistance <= Utils.CurrentDistanceToHighestDistanceRatioLimit*highestDistance)
                        {
                            var reason = string.Format("{0}从空仓的最低趋势点{1}涨到了{2}以上，即{3}，平掉空仓，空仓成本价{4}，空仓最高盈利价{5}",
                                data.InstrumentID, highestDistance + trendDistance,
                                Utils.CurrentDistanceToHighestDistanceRatioLimit,
                                currentDistance + trendDistance, stopLossPrices.CostShort, stopLossPrices.ForShort);
                            _trader.CloseShortPositionByInstrument(data.InstrumentID, "移动止损平空仓");
                        }

                        if (highestDistance > data.LastPrice*Utils.StopProfitRatio)
                        {
                            _trader.CloseShortPositionByInstrument(data.InstrumentID, "空仓止盈");
                        }
                    }
                }

                //接近涨停价，平掉空仓
                var upperLimitRange = (data.UpperLimitPrice + data.LowerLimitPrice)/2;
                var lowerLimitRange = upperLimitRange;

                if (data.LastPrice > data.PreSettlementPrice + upperLimitRange*Utils.LimitCloseRange)
                {
                    var reason = string.Format("{0}最新价{1}上涨到了涨停价{2}的{3}以上，平掉空仓", data.InstrumentID, data.LastPrice,
                        data.UpperLimitPrice, Utils.LimitCloseRange);
                    _trader.CloseShortPositionByInstrument(data.InstrumentID, reason);
                }

                //接近跌停价，平掉多仓
                if (data.LastPrice < data.PreSettlementPrice - lowerLimitRange*Utils.LimitCloseRange)
                {
                    var reason = string.Format("{0}最新价{1}下跌到了跌停价{2}的{3}以下，平掉多仓", data.InstrumentID, data.LastPrice,
                        data.LowerLimitPrice, Utils.LimitCloseRange);
                    _trader.CloseLongPositionByInstrument(data.InstrumentID, reason);
                }

                #endregion

                #region 开仓策略

                if (Utils.InstrumentToMinuteByMinuteChart.ContainsKey(data.InstrumentID))
                {
                    OpenStrategy(data, false);
                }

                #endregion
            }
            catch (Exception ex)
            {
                Utils.WriteException(ex);
            }
        }

        private Tuple<bool, bool> MinuteAllDirection(string instrumentId)
        {
            if (Utils.InstrumentToMinuteByMinuteChart.ContainsKey(instrumentId))
            {
                var minuteByMinute = Utils.InstrumentToMinuteByMinuteChart[instrumentId];

                //当前大趋势,大趋势向上只开多仓，大趋势向下，只开空仓
                var minuteAllXData = new List<double>();
                var minuteAllYData = new List<double>();
                for (var i = 0; i < minuteByMinute.Count; i++)
                {
                    if (minuteByMinute[i] != null)
                    {
                        minuteAllXData.Add(i);
                        minuteAllYData.Add(minuteByMinute[i].Item2.LastPrice);
                    }
                }

                var isPointingUpMinuteAll = MathUtils.IsPointingUp(minuteAllXData, minuteAllYData, 0.4, false);
                var isPointingDownMinuteAll = MathUtils.IsPointingDown(minuteAllXData, minuteAllYData, 0.4, false);

                return new Tuple<bool, bool>(isPointingUpMinuteAll, isPointingDownMinuteAll);
            }

            return new Tuple<bool, bool>(false, false);
        }

        private void OpenStrategy(ThostFtdcDepthMarketDataField data, bool ma)
        {
            try
            {
                var minuteByMinute = Utils.InstrumentToMinuteByMinuteChart[data.InstrumentID];

                if (minuteByMinute.Count >= Utils.MinuteByMinuteSizeLong)
                {
                    //当前大趋势,大趋势向上只开多仓，大趋势向下，只开空仓
                    var minuteAllXData = new List<double>();
                    var minuteAllYData = new List<double>();
                    for (var i = 0; i < minuteByMinute.Count; i++)
                    {
                        if (minuteByMinute[i] != null)
                        {
                            minuteAllXData.Add(i);
                            minuteAllYData.Add(minuteByMinute[i].Item2.LastPrice);
                        }
                    }

                    var isPointingUpMinuteAll = MathUtils.IsPointingUp(minuteAllXData, minuteAllYData, 0.4, ma);
                    var isPointingDownMinuteAll = MathUtils.IsPointingDown(minuteAllXData, minuteAllYData, 0.4, ma);

                    var sizeHalf = Utils.MinuteByMinuteSizeLong/2;
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
                        minuteByMinuteQuotesLong, MathUtils.Slope2, ma);
                    var isPointingDownMinuteLong2 = MathUtils.IsPointingDown(Utils.MinuteLongXData,
                        minuteByMinuteQuotesLong, MathUtils.Slope2, ma);

                    var minuteHalfXData = new List<double>();

                    for (var i = 0; i < sizeHalf; i++)
                    {
                        minuteHalfXData.Add(i);
                    }

                    var isPointingUpMinuteHalf = MathUtils.IsPointingUp(minuteHalfXData, minuteByMinuteQuotesHalf,
                        MathUtils.Slope2, ma);
                    var isPointingDownMinuteHalf = MathUtils.IsPointingDown(minuteHalfXData, minuteByMinuteQuotesHalf,
                        MathUtils.Slope2, ma);

                    //根据分时图走势下单
                    if (isPointingUpMinuteAll && isPointingUpMinuteLong2 && isPointingUpMinuteHalf)
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
                        var reason = string.Format("{0}最近长趋势向上{1},开出多仓,移动均价开仓{2},开仓启动点{3}", data.InstrumentID,
                            isPointingUpMinuteLong2, ma, min);
                        _trader.OpenLongPositionByInstrument(data.InstrumentID, reason, min, true);
                        return;
                    }

                    if (!isPointingUpMinuteAll && isPointingUpMinuteLong2 && isPointingUpMinuteHalf)
                    {
                        Utils.WriteLine("虽然长趋势向上，中趋势向上，但是大趋势不是，不开多仓", true);
                    }

                    if (!isPointingUpMinuteHalf && isPointingUpMinuteLong2)
                    {
                        Utils.WriteLine("虽然长趋势向上，但是中趋势不是，不开多仓", true);
                    }

                    if (isPointingDownMinuteAll && isPointingDownMinuteLong2 && isPointingDownMinuteHalf)
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
                        var reason = string.Format("{0}最近长趋势向下{1},开出空仓,移动均价开仓{2},开仓启动点{3}", data.InstrumentID,
                            isPointingDownMinuteLong2, ma, max);
                        _trader.OpenShortPositionByInstrument(data.InstrumentID, reason, max, true);
                        return;
                    }

                    if (!isPointingDownMinuteAll && isPointingDownMinuteLong2 && isPointingDownMinuteHalf)
                    {
                        Utils.WriteLine("虽然长趋势向下，中趋势向下，但是大趋势不是，不开空仓", true);
                    }

                    if (!isPointingDownMinuteHalf && isPointingDownMinuteLong2)
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
                var dtNow = DateTime.Now;

                if (Utils.CurrentChannel != ChannelType.模拟24X7 &&
                    ((dtNow.Hour >= 0 && dtNow.Hour <= 8) || (dtNow.Hour >= 16 && dtNow.Hour <= 20) ||
                     dtNow.DayOfWeek == DayOfWeek.Saturday || dtNow.DayOfWeek == DayOfWeek.Sunday)) //排除无效时段的行情
                {
                    return;
                }

                Utils.WriteQuote(pDepthMarketData);
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
